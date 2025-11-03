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

public class AstStageReplace : AstStage
{
    public AstStageReplace() {}

    public AstStageReplace(IEnumerable<AstLet> id, IEnumerable<AstLet> fields)
    {
        _id.AddRange(id);
        foreach (var field in fields.OfType<AstNodeBase>())
            Add(field);
    }

    public void AddId(AstLet field)
    {
        _id.Add(field);
    }

    private List<AstLet> _id = [];

    public IReadOnlyList<AstLet> IdFields => _id;
    public IReadOnlyList<AstLet> Fields => Children.OfType<AstLet>().ToList();


    public override void Append(StringBuilder sb, int indent)
    {
        bool first;

        if ( IdFields.Count == 0)
            sb.AppendLine($"{Spaces(indent)}REPLACE");
        else
        {
            sb.AppendLine($"{Spaces(indent)}REPLACE ID {{");

            first = true;
            foreach (var field in IdFields)
            {
                if ( !first )
                    sb.AppendLine(",");
                else
                    first = false;
                field.Append(sb, indent + 2);
            }
            sb.AppendLine();
            sb.AppendLine($"{Spaces(indent+1)}}}");
        }

        first = true;
        foreach (var field in Fields)
        {
            if ( !first )
                sb.AppendLine(",");
            else
                first = false;

            if ( field is AstLetExpression let)
            {
                if ( (let.Expression is AstExpressionVariable ev) && string.IsNullOrWhiteSpace(let.Name))
                {
                    sb.Append($"{Spaces(indent + 1)}");
                    ev.Append(sb, indent + 1);
                }
                else
                    let.Append(sb, indent + 1);
            }
            else
                field.Append(sb, indent + 1);
        }
        sb.AppendLine();
    }

    public override JsonNode? AsJson()
    {

        var fields = new JsonObject();
        if ( IdFields.Count > 0 )
        {
            var idFields = new JsonObject();
            
            foreach (var field in IdFields)
            {
                if ( field is AstLetExpression let)
                    AddFields(let, idFields);
            }

            fields.Add("_id", idFields);
        }

        foreach (var field in Fields)
        {
            if ( field is AstLetExpression let)
                AddFields(let, fields);
        }

        var stage = new JsonObject([ new("$replaceWith", fields)]);
        return ApplyOptions(stage);
    }

    private void AddFields(AstLetExpression field, JsonObject fields)
    {
            var name = field.Name;
            if ( string.IsNullOrWhiteSpace(name) )
                name = (field.Expression as AstExpressionVariable)?.Name;

            if ( string.IsNullOrWhiteSpace(name) )
                throw new ($"{field.Expression} must have a name");

            fields.Add(name, field.Expression.AsJson());
    }
}
