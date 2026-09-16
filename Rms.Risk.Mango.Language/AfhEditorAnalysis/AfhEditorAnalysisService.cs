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
namespace Rms.Risk.Mango.Language.AfhEditorAnalysis;

/// <summary>
/// AFH editor analysis service for CodeMirror intellisense.
/// Provides context-aware completion suggestions and syntax previews.
/// </summary>
public static class AfhEditorAnalysisService
{
    /// <summary>
    /// Analyze AFH script for editor intellisense at the cursor position.
    /// </summary>
    /// <param name="script">The AFH script (may be incomplete or broken).</param>
    /// <param name="cursorLine">0-based line number of the cursor.</param>
    /// <param name="cursorColumn">0-based column number of the cursor.</param>
    /// <returns>Analysis result with stage context and completion suggestions.</returns>
    public static AfhEditorAnalysis AnalyzeForIntellisense(string script, int cursorLine, int cursorColumn)
    {
        return AfhEditorAnalyzerFactory.Analyze(script, cursorLine, cursorColumn);
    }

    /// <summary>
    /// Get all available stage keywords and their metadata for the editor.
    /// </summary>
    public static IEnumerable<AfhStageMetadata> GetAllStages()
    {
        return AfhStageCatalog.Stages.Values;
    }

    /// <summary>
    /// Get metadata for a specific stage by keyword.
    /// </summary>
    public static AfhStageMetadata? GetStageMetadata(string keyword)
    {
        return AfhStageCatalog.GetStage(keyword);
    }

    /// <summary>
    /// Get all valid stage keywords for use in the editor.
    /// </summary>
    public static IEnumerable<string> GetAllStageKeywords()
    {
        return AfhStageCatalog.AllStageKeywords;
    }
}
