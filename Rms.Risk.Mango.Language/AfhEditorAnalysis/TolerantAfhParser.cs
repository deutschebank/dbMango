/* 
 *                                dbMango
 *
 * Copyright 2025 Deutsche Bank AG
 * SPDX-License-Identifier: Apache-2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Rms.Risk.Mango.Language.AfhEditorAnalysis;

/// <summary>
/// Tolerant AFH parser for editor intellisense.
/// Recovers from syntax errors and incomplete scripts without throwing exceptions.
/// </summary>
internal class TolerantAfhParser : IAfhEditorAnalyzer
{
    private class ErrorCollector : IAntlrErrorListener<IToken>
    {
        public List<AfhDiagnostic> Diagnostics { get; } = [];

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Diagnostics.Add(new AfhDiagnostic
            {
                Line = line - 1,
                Column = charPositionInLine,
                Message = msg,
                Severity = "error"
            });
        }
    }

    private class LexerErrorCollector : IAntlrErrorListener<int>
    {
        public List<AfhDiagnostic> Diagnostics { get; } = [];

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Diagnostics.Add(new AfhDiagnostic
            {
                Line = line - 1,
                Column = charPositionInLine,
                Message = msg,
                Severity = "error"
            });
        }
    }

    public AfhEditorAnalysis Analyze(string script, int cursorLine, int cursorColumn)
    {
        var result = new AfhEditorAnalysis();

        try
        {
            // Try to parse the script tolerantly
            var (diagnostics, parseTree) = TryParse(script);
            result.Diagnostics = diagnostics.ToArray();

            // Infer active stage and context from the parse tree
            InferActiveStage(script, cursorLine, cursorColumn, parseTree, result);

            // Generate completions based on context
            GenerateCompletions(script, cursorLine, cursorColumn, result);

            result.IsValid = result.Diagnostics.Length == 0;
        }
        catch (Exception ex)
        {
            // Fallback: token-based analysis when ANTLR recovery fails
            FallbackAnalysis(script, cursorLine, cursorColumn, result, ex);
        }

        return result;
    }

    /// <summary>
    /// Attempt to parse the AFH script and collect errors.
    /// Returns diagnostics and the parse tree (may be incomplete).
    /// </summary>
    private (List<AfhDiagnostic>, IParseTree?) TryParse(string script)
    {
        var diagnostics = new List<AfhDiagnostic>();

        try
        {
            var str = new AntlrInputStream(script);
            var lexer = new MongoAggregationForHumansLexer(str);
            var tokens = new CommonTokenStream(lexer);
            var parser = new MongoAggregationForHumansParser(tokens);

            var errorCollector = new ErrorCollector();
            var lexerErrorCollector = new LexerErrorCollector();

            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorCollector);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(lexerErrorCollector);

            // Use error recovery mode (not bail-on-first-error)
            parser.ErrorHandler = new DefaultErrorStrategy();

            var tree = parser.file();

            diagnostics.AddRange(errorCollector.Diagnostics);
            diagnostics.AddRange(lexerErrorCollector.Diagnostics);
            return (diagnostics, tree);
        }
        catch (Exception)
        {
            // ANTLR failed entirely; return empty diagnostics and null tree
            return (diagnostics, null);
        }
    }

    /// <summary>
    /// Infer the active stage and context from the parse tree or fallback to token analysis.
    /// </summary>
    private void InferActiveStage(string script, int cursorLine, int cursorColumn, IParseTree? parseTree, AfhEditorAnalysis result)
    {
        if (parseTree is not null)
        {
            // Use parse tree to infer stage (more reliable if available)
            InferFromParseTree(script, cursorLine, cursorColumn, parseTree, result);
        }
        else
        {
            // Fall back to token-based inference
            InferFromTokens(script, cursorLine, cursorColumn, result);
        }
    }

    /// <summary>
    /// Infer active stage from the parse tree.
    /// </summary>
    private void InferFromParseTree(string script, int cursorLine, int cursorColumn, IParseTree parseTree, AfhEditorAnalysis result)
    {
        // Walk the tree to find stages at the cursor location
        var stageVisitor = new StageContextVisitor(cursorLine, cursorColumn, script);
        var walker = new ParseTreeWalker();
        walker.Walk(stageVisitor, parseTree);

        result.CurrentStage = stageVisitor.ActiveStage;
        result.CurrentStageSyntaxPreview = stageVisitor.ActiveStage.StageMetadata?.SyntaxPreview ?? "";
    }

    /// <summary>
    /// Fallback: infer active stage from tokens when parse tree is not available.
    /// </summary>
    private void InferFromTokens(string script, int cursorLine, int cursorColumn, AfhEditorAnalysis result)
    {
        var lines = script.Split('\n');
        if (cursorLine >= lines.Length)
            return;

        var currentLine = lines[cursorLine];

        // Look for stage keywords before the cursor position
        var stageKeywords = AfhStageCatalog.AllStageKeywords.ToList();
        var nearestStage = FindNearestStageKeyword(script, cursorLine, cursorColumn, stageKeywords);

        if (nearestStage != null)
        {
            var meta = AfhStageCatalog.GetStage(nearestStage);
            result.CurrentStage = new ActiveStageContext
            {
                StageKeyword = nearestStage,
                StageMetadata = meta,
                NestingLevel = EstimateNestingLevel(script, cursorLine),
                IsInsideJsonBlock = IsInsideJsonBlock(script, cursorLine, cursorColumn),
                ValidNextStages = meta?.ValidContinuations ?? []
            };
            result.CurrentStageSyntaxPreview = meta?.SyntaxPreview ?? "";
        }
    }

    /// <summary>
    /// Find the nearest stage keyword before the cursor.
    /// </summary>
    private string? FindNearestStageKeyword(string script, int cursorLine, int cursorColumn, List<string> stageKeywords)
    {
        var lines = script.Split('\n');

        // Search backwards from cursor line
        for (int line = cursorLine; line >= 0; line--)
        {
            var searchText = (line == cursorLine) 
                ? lines[line][..cursorColumn]
                : lines[line];

            // Case-insensitive search
            foreach (var keyword in stageKeywords)
            {
                if (searchText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    // Verify it's a whole word
                    var idx = searchText.LastIndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var before = idx == 0 ? ' ' : searchText[idx - 1];
                        var after = (idx + keyword.Length >= searchText.Length) ? ' ' : searchText[idx + keyword.Length];

                        if (!char.IsLetterOrDigit(before) && !char.IsLetterOrDigit(after))
                            return keyword;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Estimate the nesting level (0 = root, 1+ = inside nested pipelines).
    /// </summary>
    private int EstimateNestingLevel(string script, int cursorLine)
    {
        var lines = script.Split('\n');
        var beforeCursor = string.Join('\n', lines.Take(cursorLine + 1));

        var openPipelines = beforeCursor.Count(c => c == '{');
        var closedPipelines = beforeCursor.Count(c => c == '}');

        return Math.Max(0, openPipelines - closedPipelines - 1);
    }

    /// <summary>
    /// Check if the cursor is inside a DO { ... } JSON block.
    /// </summary>
    private bool IsInsideJsonBlock(string script, int cursorLine, int cursorColumn)
    {
        var lines = script.Split('\n');

        // Look backwards for "DO" keyword
        for (int line = cursorLine; line >= 0; line--)
        {
            var searchText = (line == cursorLine)
                ? lines[line][..cursorColumn]
                : lines[line];

            var doIdx = searchText.LastIndexOf("DO", StringComparison.OrdinalIgnoreCase);
            if (doIdx >= 0)
            {
                // Found DO; count braces after it
                var afterDo = searchText[(doIdx + 2)..];
                var openBraces = afterDo.Count(c => c == '{');
                var closeBraces = afterDo.Count(c => c == '}');

                return openBraces > closeBraces;
            }
        }

        return false;
    }

    /// <summary>
    /// Generate completion suggestions based on the current context.
    /// </summary>
    private void GenerateCompletions(string script, int cursorLine, int cursorColumn, AfhEditorAnalysis result)
    {
        var completions = new List<AfhCompletion>();

        if (result.CurrentStage.IsInsideJsonBlock)
        {
            // Inside DO { ... }, don't suggest AFH stages
            return;
        }

        var validNextStages = result.CurrentStage.ValidNextStages.Length > 0
            ? result.CurrentStage.ValidNextStages
            : AfhStageCatalog.AllStageKeywords.ToArray();

        // Add completions for valid next stages
        int sortOrder = 100;
        foreach (var stageName in validNextStages.OrderBy(x => x))
        {
            var meta = AfhStageCatalog.GetStage(stageName);
            if (meta != null)
            {
                completions.Add(new AfhCompletion
                {
                    DisplayText = meta.Keyword,
                    InsertText = meta.CompletionTemplate,
                    Detail = meta.SyntaxPreview,
                    Type = "stage",
                    SortOrder = sortOrder--
                });
            }
        }

        result.Completions = completions.OrderByDescending(c => c.SortOrder).ToArray();
    }

    /// <summary>
    /// Fallback analysis when ANTLR fails completely.
    /// Uses token-based heuristics to still provide hints.
    /// </summary>
    private void FallbackAnalysis(string script, int cursorLine, int cursorColumn, AfhEditorAnalysis result, Exception ex)
    {
        result.Diagnostics = [
            new AfhDiagnostic
            {
                Line = cursorLine,
                Column = cursorColumn,
                Message = "Parse error; falling back to token-based analysis",
                Severity = "warning"
            }
        ];

        result.IsValid = false;

        // Use token-based inference
        InferFromTokens(script, cursorLine, cursorColumn, result);

        // Still generate completions from the inferred context
        GenerateCompletions(script, cursorLine, cursorColumn, result);
    }

    /// <summary>
    /// Visitor to extract stage context from the parse tree.
    /// </summary>
    private class StageContextVisitor : MongoAggregationForHumansBaseListener
    {
        private readonly int _cursorLine;
        private readonly int _cursorColumn;
        private readonly string _script;
        public ActiveStageContext ActiveStage { get; } = new();

        public StageContextVisitor(int cursorLine, int cursorColumn, string script)
        {
            _cursorLine = cursorLine;
            _cursorColumn = cursorColumn;
            _script = script;
        }

        public override void EnterStage_def(MongoAggregationForHumansParser.Stage_defContext context)
        {
            // Determine which stage this is
            var stageKeyword = FindStageKeywordInContext(context);
            if (stageKeyword is not null && IsCursorInContext(context))
            {
                var meta = AfhStageCatalog.GetStage(stageKeyword);
                ActiveStage.StageKeyword = stageKeyword;
                ActiveStage.StageMetadata = meta;
                ActiveStage.ValidNextStages = meta?.ValidContinuations ?? [];
            }

            base.EnterStage_def(context);
        }

        private string? FindStageKeywordInContext(MongoAggregationForHumansParser.Stage_defContext context)
        {
            // Check for stage keywords in order
            if (context.match_def() != null) return "WHERE";
            if (context.addfields_def() != null) return "ADD";
            if (context.project_def() != null) return "PROJECT";
            if (context.group_by_def() != null) return "GROUP";
            if (context.sort_def() != null) return "SORT";
            if (context.join_def() != null) return "JOIN";
            if (context.unwind_def() != null) return "UNWIND";
            if (context.bucket_def() != null) return "BUCKET";
            if (context.facet_def() != null) return "FACET";
            if (context.replace_def() != null) return "REPLACE";
            if (context.do_def() != null) return "DO";

            return null;
        }

        private bool IsCursorInContext(ParserRuleContext context)
        {
            var startLine = context.Start?.Line - 1 ?? 0;
            var stopLine = context.Stop?.Line - 1 ?? int.MaxValue;

            if (_cursorLine < startLine || _cursorLine > stopLine)
                return false;

            if (_cursorLine == startLine && _cursorColumn < (context.Start?.Column ?? 0))
                return false;

            if (_cursorLine == stopLine && _cursorColumn > (context.Stop?.Column + (context.Stop?.Text?.Length ?? 0) ?? int.MaxValue))
                return false;

            return true;
        }
    }
}
