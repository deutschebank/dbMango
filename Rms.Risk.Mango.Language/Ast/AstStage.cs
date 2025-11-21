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
namespace Rms.Risk.Mango.Language.Ast;

public abstract class AstStage : AstNodeBase
{
    public JsonObject? Options { get; internal set; }

    protected JsonNode? ApplyOptions(JsonNode? json )
    {
        if ( json is not JsonObject jo || Options == null)
            return json;

        // apply overrides from Options object to jo and return the result
        var stage = jo.ElementAt(0).Value as JsonObject;
        if (stage == null)
            return json;

        foreach (var (key, value) in Options)
            stage![key] = value?.DeepClone();

        return jo;
    }
}
