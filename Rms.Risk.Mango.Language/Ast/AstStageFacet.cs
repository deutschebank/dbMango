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

public class AstStageFacet : AstStage
{
    public IReadOnlyList<AstNamedPipeline> Pipelines => Children.OfType<AstNamedPipeline>().ToList();

    public override void Append(StringBuilder sb, int indent)
    {
        sb.AppendLine($"{Spaces(indent)}FACET");
            
        var first = true;
        foreach (var field in Pipelines)
        {
            if ( !first )
                sb.AppendLine(",");
            else
                first = false;

            field.Append(sb, indent + 1);
        }
        sb.AppendLine();
    }    

    public override JsonNode? AsJson()
    {
        var stage = new JsonObject
        {
            {
                "$facet",
                new JsonObject
                {
                }
            }
        };

        var body = (JsonObject)stage.ElementAt(0).Value!;

        foreach (var pipeline in Pipelines)
        {
            body.Add(pipeline.Name, pipeline.Pipeline!.AsJson());
        }

        return ApplyOptions(stage);        
    }

}
