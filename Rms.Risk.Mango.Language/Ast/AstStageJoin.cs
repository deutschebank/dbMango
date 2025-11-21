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

public class AstStageJoin : AstStage
{
    public AstStageJoin()
    {

    }

    public AstStageJoin(string collection, string asField, IEnumerable<AstEquivalence> on, IEnumerable<AstLet> let, AstPipeline? pipeline = null)
    {
        Init(collection, asField, on, let, pipeline);
    }

    public void Init(string collection, string asField, IEnumerable<AstEquivalence> on, IEnumerable<AstLet> let, AstPipeline? pipeline = null)
    {
        Collection = collection.Trim('\"');
        AsField = PreprocessFieldName(asField);
        foreach(var x in on)
            Add(x);
        foreach(var x in let)
            Add(x);
        if (pipeline != null)
            Add(pipeline);
    }

    public string Collection { get; private set;} = "";
    public string AsField { get; private set; } = "";

    public IReadOnlyList<AstEquivalence> On => Children.OfType<AstEquivalence>().ToList();
    public IReadOnlyList<AstLet> Let => Children.OfType<AstLet>().ToList();
    public AstPipeline? Pipeline => Children.OfType<AstPipeline>().FirstOrDefault();

    public override void Append(StringBuilder sb, int indent)
    {
        sb.Append($"{Spaces(indent)}JOIN \"{Collection}\" AS ");
        AppendField(sb, AsField);
        sb.AppendLine($" ON");
            
        var first = true;
        foreach (var field in On)
        {
            if ( !first )
                sb.AppendLine(",");
            else
                first = false;

            field.Append(sb, indent + 1);
        }
        sb.AppendLine();

        if ( Let.Count > 0 )
        {
            sb.AppendLine($"{Spaces(indent+1)}LET");
            foreach (var field in Let)
            {
                field.Append(sb, indent + 2);
            }
            sb.AppendLine();
        }

        if( Pipeline != null )
        {
            sb.AppendLine($"{Spaces(indent + 1)}PIPELINE {{");
            Pipeline.Append(sb, indent + 2);
            sb.AppendLine($"{Spaces(indent + 1)}}}");
        }
    }    

    public override JsonNode? AsJson()
    {
        var eqv = On.First();

        var stage = new JsonObject
        {
            {
                "$lookup",
                new JsonObject
                {
                    {"from", Collection},
                    {"localField", JsonValue.Create(eqv.Left.Name)},
                    {"foreignField", JsonValue.Create(eqv.Right.Name)}
                }
            }
        };

        var body = (JsonObject)stage.ElementAt(0).Value!;

        if (!string.IsNullOrWhiteSpace(AsField))
        {
            body.Add( new("as", JsonValue.Create(AsField)) );
        }

        if ( Let?.Count > 0 ) 
        {
            var fields =AstLet.AsJson(Let);
            body.Add( new("let", fields) );
        }

        if ( Pipeline != null ) 
            body.Add( new("pipeline", Pipeline.AsJson()) );

        return ApplyOptions(stage);        
    }

}
