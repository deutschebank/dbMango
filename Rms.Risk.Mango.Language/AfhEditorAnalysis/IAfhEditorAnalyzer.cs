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
/// Editor-focused AFH analyzer for providing context-aware intellisense hints.
/// This is separate from the strict runtime parser and designed to work on incomplete/broken AFH scripts.
/// </summary>
public interface IAfhEditorAnalyzer
{
    /// <summary>
    /// Analyze the AFH script at a given cursor position and return editor metadata.
    /// </summary>
    /// <param name="script">The AFH script (may be incomplete or broken).</param>
    /// <param name="cursorLine">0-based line number where the cursor is positioned.</param>
    /// <param name="cursorColumn">0-based column number where the cursor is positioned.</param>
    /// <returns>Analysis result with diagnostics, active stage, and completions.</returns>
    AfhEditorAnalysis Analyze(string script, int cursorLine, int cursorColumn);
}
