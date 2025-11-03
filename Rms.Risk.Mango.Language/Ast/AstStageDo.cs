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

public class AstStageDo : AstStage
{
    public AstStageDo()
    {

    }

    public AstStageDo(JsonNode json)
    {
        Json = json;
    }


    public JsonNode? Json { get; set; }

    public override void Append(StringBuilder sb, int indent)
    {
        if ( Json == null )
            return;

        sb.AppendLine($"{Spaces(indent)}DO ");
        var json = JsonSerializer.Serialize(Json, PrettyPrint)
            .Replace("\r", "")
            .Split("\n");
        sb.AppendJoin("\n", json.Select( x => $"{Spaces(indent)}{x}"));
        sb.AppendLine();
    }

    public override JsonNode? AsJson() => Json;

    private static JsonSerializerOptions PrettyPrint = new()
    {
        WriteIndented = true
    };
}
