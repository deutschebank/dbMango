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
using Microsoft.JSInterop;
using Rms.Risk.Mango.Language.AfhEditorAnalysis;

namespace Rms.Risk.Mango.Services;

/// <summary>
/// Service for AFH editor analysis accessible via JSInterop.
/// Provides direct access to the analysis parser for CodeMirror intellisense.
/// </summary>
public static class AfhEditorAnalysisJsInteropService
{
    /// <summary>
    /// Analyze AFH script at cursor position for intellisense hints via JSInterop.
    /// </summary>
    /// <param name="script">The AFH script to analyze.</param>
    /// <param name="cursorLine">0-based cursor line number.</param>
    /// <param name="cursorColumn">0-based cursor column number.</param>
    /// <returns>Analysis result as JSON-serializable object.</returns>
    [JSInvokable("AfhEditorAnalysisService.AnalyzeScript")]
    public static AfhEditorAnalysis AnalyzeScript(string script, int cursorLine, int cursorColumn)
    {
        try
        {
            return AfhEditorAnalysisService.AnalyzeForIntellisense(script, cursorLine, cursorColumn);
        }
        catch (Exception ex)
        {
            // Return error-safe result
            return new AfhEditorAnalysis
            {
                IsValid = false,
                Diagnostics = [
                    new AfhDiagnostic
                    {
                        Line = cursorLine,
                        Column = cursorColumn,
                        Message = ex.Message,
                        Severity = "error"
                    }
                ]
            };
        }
    }

    /// <summary>
    /// Get all AFH stage metadata via JSInterop.
    /// </summary>
    [JSInvokable("AfhEditorAnalysisService.GetAllStages")]
    public static IEnumerable<AfhStageMetadata> GetAllStages()
    {
        return AfhEditorAnalysisService.GetAllStages();
    }

    /// <summary>
    /// Get stage metadata by keyword via JSInterop.
    /// </summary>
    [JSInvokable("AfhEditorAnalysisService.GetStageMetadata")]
    public static AfhStageMetadata? GetStageMetadata(string keyword)
    {
        return AfhEditorAnalysisService.GetStageMetadata(keyword);
    }

    /// <summary>
    /// Get all stage keywords via JSInterop.
    /// </summary>
    [JSInvokable("AfhEditorAnalysisService.GetStageKeywords")]
    public static IEnumerable<string> GetStageKeywords()
    {
        return AfhEditorAnalysisService.GetAllStageKeywords();
    }
}
