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
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rms.Risk.Mango.Language;


namespace Rms.Risk.Mango.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AfhController(ILogger<AfhController> logger) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("from-json-to-script")]
        [RequestSizeLimit(1_000_000)]
        [Produces("text/plain")]
        [ProducesErrorResponseType(typeof(JsonObject))]
        [ProducesResponseType(200)]
        [Consumes("application/json")]
        public ActionResult<string> FromJsonToScript([FromBody] JsonArray json)
        {
            try
            {
                var ast = LanguageParser.ParseAggregationJsonToAST("<collection name here>", json);
                return ast.AsText();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AFH JSON to script conversion failed.");
                return BadRequest(new { error = "Invalid AFH JSON input." });
            }
        }

        [AllowAnonymous]
        [HttpPost("from-script-to-json")]
        [RequestSizeLimit(1_000_000)]
        [Consumes("text/plain")]
        [ProducesErrorResponseType(typeof(JsonObject))]
        [ProducesResponseType(200)]
        public async Task<ActionResult<JsonArray>> FromScriptToJson()
        {
            string script;
            using (StreamReader reader = new(Request.Body, Encoding.UTF8))
            {
                script = await reader.ReadToEndAsync();
            }

            try
            {
                var ast = LanguageParser.ParseScriptToAST(script);
                var json = ast.AsJson();
                return (JsonArray)json!;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AFH script to JSON conversion failed.");
                return BadRequest(new { error = "Invalid AFH script input." });
            }
        }

        [AllowAnonymous]
        [HttpPost("format")]
        [RequestSizeLimit(1_000_000)]
        [Consumes("text/plain")]
        [Produces("text/plain")]
        [ProducesErrorResponseType(typeof(JsonObject))]
        [ProducesResponseType(200)]
        public async Task<ActionResult<string>> Format()
        {
            string script;
            using (StreamReader reader = new(Request.Body, Encoding.UTF8))
            {
                script = await reader.ReadToEndAsync();
            }

            try
            {
                var ast = LanguageParser.ParseScriptToAST(script);
                return ast.AsText();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AFH formatting failed.");
                return BadRequest(new { error = "Invalid AFH script input." });
            }
        }
    }
}
