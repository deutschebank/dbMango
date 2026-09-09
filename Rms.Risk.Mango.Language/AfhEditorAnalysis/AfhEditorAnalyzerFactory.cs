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
/// Factory for creating AFH editor analyzers.
/// </summary>
public static class AfhEditorAnalyzerFactory
{
    private static IAfhEditorAnalyzer? _instance;

    /// <summary>
    /// Create or retrieve the singleton AFH editor analyzer.
    /// </summary>
    public static IAfhEditorAnalyzer CreateAnalyzer()
    {
        return _instance ??= new TolerantAfhParser();
    }

    /// <summary>
    /// Analyze an AFH script at a cursor position for editor intellisense.
    /// </summary>
    public static AfhEditorAnalysis Analyze(string script, int cursorLine, int cursorColumn)
    {
        var analyzer = CreateAnalyzer();
        return analyzer.Analyze(script, cursorLine, cursorColumn);
    }
}
