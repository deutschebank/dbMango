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

public class AstStageGroupBy : AstStage
{
    private List<AstLet> _id = [];

    public AstStageGroupBy(IEnumerable<AstLet> id, IEnumerable<AstLet> fields)
    {
        foreach (var x in fields)
            Add(x);
        foreach (var x in id)
        {
            _id.Add(x);
            x.Parent = this;
        }
    }

    public void AddId(AstLet field) => _id.Add(field);

    public IReadOnlyList<AstLet> Id => _id;
    public IReadOnlyList<AstLet> Fields => Children.OfType<AstLet>().ToList();

    public override void Append(StringBuilder sb, int indent)
    {
        sb.AppendLine($"{Spaces(indent)}GROUP BY");

        var first = true;
        foreach (var field in Id)
        {
            if ( !first )
                sb.AppendLine(",");
            else
                first = false;

            field.Append(sb, indent + 1);
        }
        sb.AppendLine();

        first = true;
        foreach (var field in Fields)
        {
            if ( !first )
                sb.AppendLine(",");
            else
            {
                first = false;
                sb.AppendLine($"{Spaces(indent+1)}LET");
            }
            field.Append(sb, indent + 2);
        }
        
        if ( !first )
            sb.AppendLine();
    }    

    public override JsonNode? AsJson()
    {
        var body = new JsonObject();
        
        var _id = Id.OfType<AstLetExpression>().ToList();
        // _id can be a single field or an expression
        if (_id.Count == 1 && string.IsNullOrWhiteSpace(_id[0].Name) )
            body.Add("_id", _id[0].Expression.AsJson());
        
        // alternative method for object-style _id
        if (body.Count == 0)
        {
            var idFields = new JsonObject();
            foreach (var field in _id)
                field.AddToJson(idFields);
            body.Add("_id", idFields);
        }
        
        var fields = Fields.OfType<AstLetExpression>().ToList();
        
        foreach (var field in fields)
            field.AddToJson(body);

        var stage = new JsonObject
        {
            { "$group", body }
        };

        return ApplyOptions(stage);
    }

}
