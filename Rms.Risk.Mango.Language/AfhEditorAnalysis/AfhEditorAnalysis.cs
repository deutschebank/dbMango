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
/// A single diagnostic (warning/error) found during tolerant parsing.
/// </summary>
public class AfhDiagnostic
{
    public int Line { get; set; }
    public int Column { get; set; }
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "warning"; // "error", "warning", "info"
}

/// <summary>
/// Information about the stage the cursor is currently in.
/// </summary>
public class ActiveStageContext
{
    /// <summary>
    /// The keyword of the current stage (e.g., "WHERE", "ADD", "GROUP").
    /// Null if cursor is outside any recognized stage or at pipeline level.
    /// </summary>
    public string? StageKeyword { get; set; }

    /// <summary>
    /// Stage metadata if StageKeyword is known; null otherwise.
    /// </summary>
    public AfhStageMetadata? StageMetadata { get; set; }

    /// <summary>
    /// Nesting level: 0 = root pipeline, 1 = inside JOIN/FACET pipeline, etc.
    /// </summary>
    public int NestingLevel { get; set; }

    /// <summary>
    /// True if cursor is inside a DO {...} block (raw JSON region where AFH stages should not be suggested).
    /// </summary>
    public bool IsInsideJsonBlock { get; set; }

    /// <summary>
    /// Valid stage keywords that can follow the current stage.
    /// </summary>
    public string[] ValidNextStages { get; set; } = [];
}

/// <summary>
/// A completion suggestion with display text, detail, and insertion text.
/// </summary>
public class AfhCompletion
{
    /// <summary>
    /// Text displayed in the completion popup.
    /// </summary>
    public string DisplayText { get; set; } = "";

    /// <summary>
    /// Text to be inserted when the completion is selected.
    /// </summary>
    public string InsertText { get; set; } = "";

    /// <summary>
    /// Additional detail shown next to the completion (e.g., syntax preview).
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Type of completion: "stage", "keyword", "variable", etc.
    /// </summary>
    public string Type { get; set; } = "stage";

    /// <summary>
    /// Sort order for prioritization (higher = shown first).
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Full editor analysis result: diagnostics, active stage, and completion suggestions.
/// </summary>
public class AfhEditorAnalysis
{
    /// <summary>
    /// Parse diagnostics (errors, warnings, info messages).
    /// </summary>
    public AfhDiagnostic[] Diagnostics { get; set; } = [];

    /// <summary>
    /// Information about the stage at the cursor position.
    /// </summary>
    public ActiveStageContext CurrentStage { get; set; } = new();

    /// <summary>
    /// Syntax preview for the current stage (e.g., "WHERE expression").
    /// </summary>
    public string CurrentStageSyntaxPreview { get; set; } = "";

    /// <summary>
    /// Completion suggestions for the current cursor position.
    /// </summary>
    public AfhCompletion[] Completions { get; set; } = [];

    /// <summary>
    /// True if the parse tree is valid and complete.
    /// </summary>
    public bool IsValid { get; set; } = true;
}
