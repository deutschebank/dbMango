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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rms.Risk.Mango.Language.AfhEditorAnalysis;

namespace Rms.Risk.Mango.Controllers;

/// <summary>
/// Controller for AFH editor intellisense analysis.
/// Provides context-aware hints for the CodeMirror AFH editor.
/// </summary>
[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class AfhEditorController(ILogger<AfhEditorController> logger) : ControllerBase
{
    /// <summary>
    /// Analyze AFH script at cursor position for intellisense hints.
    /// </summary>
    /// <param name="request">Analysis request containing script and cursor position.</param>
    /// <returns>Analysis result with completions and stage context.</returns>
    [HttpPost("analyze")]
    [RequestSizeLimit(1_000_000)]
    [Consumes("application/json")]
    [Produces("application/json")]
    public ActionResult<AfhEditorAnalysis> Analyze([FromBody] AnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Script))
        {
            return BadRequest(new { error = "Script cannot be empty" });
        }

        try
        {
            var analysis = AfhEditorAnalysisService.AnalyzeForIntellisense(
                request.Script,
                request.CursorLine,
                request.CursorColumn
            );

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AFH editor analysis failed.");
            return StatusCode(500, new { error = "AFH analysis failed." });
        }
    }

    /// <summary>
    /// Get all AFH stage metadata for the editor.
    /// </summary>
    /// <returns>List of all stages with metadata.</returns>
    [HttpGet("stages")]
    [Produces("application/json")]
    public ActionResult<IEnumerable<AfhStageMetadata>> GetAllStages()
    {
        try
        {
            var stages = AfhEditorAnalysisService.GetAllStages();
            return Ok(stages);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load AFH stages.");
            return StatusCode(500, new { error = "AFH stage metadata could not be loaded." });
        }
    }

    /// <summary>
    /// Get a specific stage metadata by keyword.
    /// </summary>
    /// <param name="keyword">Stage keyword (e.g., "WHERE", "ADD").</param>
    /// <returns>Stage metadata if found; 404 otherwise.</returns>
    [HttpGet("stages/{keyword}")]
    [Produces("application/json")]
    public ActionResult<AfhStageMetadata> GetStage(string keyword)
    {
        try
        {
            var stage = AfhEditorAnalysisService.GetStageMetadata(keyword);
            if (stage == null)
            {
                return NotFound(new { error = $"Stage '{keyword}' not found" });
            }

            return Ok(stage);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load AFH stage '{Keyword}'.", keyword);
            return StatusCode(500, new { error = "AFH stage metadata could not be loaded." });
        }
    }

    /// <summary>
    /// Get all stage keywords.
    /// </summary>
    /// <returns>List of all stage keywords.</returns>
    [HttpGet("stage-keywords")]
    [Produces("application/json")]
    public ActionResult<IEnumerable<string>> GetStageKeywords()
    {
        try
        {
            var keywords = AfhEditorAnalysisService.GetAllStageKeywords();
            return Ok(keywords.OrderBy(k => k));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load AFH stage keywords.");
            return StatusCode(500, new { error = "AFH stage keywords could not be loaded." });
        }
    }
}

/// <summary>
/// Request model for AFH editor analysis.
/// </summary>
public class AnalysisRequest
{
    /// <summary>
    /// The AFH script to analyze.
    /// </summary>
    public string? Script { get; set; }

    /// <summary>
    /// 0-based cursor line number.
    /// </summary>
    public int CursorLine { get; set; }

    /// <summary>
    /// 0-based cursor column number.
    /// </summary>
    public int CursorColumn { get; set; }
}
