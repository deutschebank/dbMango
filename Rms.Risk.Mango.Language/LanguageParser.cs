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

namespace Rms.Risk.Mango.Language;

public static class LanguageParser
{
    public static AstAggregation ParseScriptToAST(string input)
    {
        var str    = new AntlrInputStream(input);
        var lexer  = new MongoAggregationForHumansLexer(str);
        var tokens = new CommonTokenStream(lexer);
        var parser = new MongoAggregationForHumansParser(tokens);

        parser.AddErrorListener(new ErrorListener<IToken>("parser"));
        lexer.AddErrorListener(new ErrorListener<int>("lexer"));
            
        var tree = parser.file();

        var astListener = new MongoGrammarListener();
        var walker      = new ParseTreeWalker();

        walker.Walk(astListener, tree);

        return astListener.Aggregate ?? throw new ("No Aggregation parsed");
    }

    public static AstAggregation ParseAggregationJsonToAST(string collection, string json)
        => AggregationPipelineParser.Parse(collection, json);

    public static AstAggregation ParseAggregationJsonToAST(string collection, JsonArray json)
        => AggregationPipelineParser.Parse(collection, json);

    public static void ParseJsonForFun(string input)
    {
        var str    = new AntlrInputStream(input);
        var lexer  = new JsonGrammarLexer(str);
        var tokens = new CommonTokenStream(lexer);
        var parser = new JsonGrammarParser(tokens);

        parser.AddErrorListener(new ErrorListener<IToken>("parser"));
        lexer.AddErrorListener(new ErrorListener<int>("lexer"));
            
        var tree = parser.json();

        var astListener = new JsonGrammarBaseListener();
        var walker      = new ParseTreeWalker();

        walker.Walk(astListener, tree);
    }

}
