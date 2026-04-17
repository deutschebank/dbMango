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
    public class AfhController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("from-json-to-script")]
        [Produces("text/plain")]
        [ProducesErrorResponseType(typeof(JsonObject))]
        [ProducesResponseType(200)]
        [Consumes("application/json")]
        public string FromJsonToScript([FromBody] JsonArray json)
        {
            var ast = LanguageParser.ParseAggregationJsonToAST("<collection name here>",json);
            var text = ast.AsText();
            return text;
        }

        [AllowAnonymous]
        [HttpPost("from-script-to-json")]
        [Consumes("text/plain")]
        [ProducesErrorResponseType(typeof(JsonObject))]
        [ProducesResponseType(200)]
        public async Task<JsonArray> FromScriptToJson()
        {
            string script;
            using (StreamReader reader = new(Request.Body, Encoding.UTF8))
            {  
                script = await reader.ReadToEndAsync();
            }            

            var ast = LanguageParser.ParseScriptToAST(script);
            var json = ast.AsJson();
            return (JsonArray)json!;
        }

        [AllowAnonymous]
        [HttpPost("format")]
        [Consumes("text/plain")]
        [Produces("text/plain")]
        [ProducesErrorResponseType(typeof(JsonObject))]
        [ProducesResponseType(200)]
        public async Task<string> Format()
        {
            string script;
            using (StreamReader reader = new(Request.Body, Encoding.UTF8))
            {  
                script = await reader.ReadToEndAsync();
            }            

            var ast = LanguageParser.ParseScriptToAST(script);
            var text = ast.AsText();
            return text;
        }
    }
}
