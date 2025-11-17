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

public class AstLetExpression : AstLet
{          
    public AstLetExpression(AstExpression expression, string? name = null)
    {
        Add(expression);
        if ( expression is not AstExpressionVariable ev || ev.Name != PreprocessFieldName(name))
        {
            Name = name?.Trim('\"').Trim('\'');//PreprocessFieldName(name);
            if ( string.IsNullOrWhiteSpace(Name))
                Name = null;
        }
    }

    public AstExpression Expression => Children.OfType<AstExpression>().First();

    public override void Append(StringBuilder sb, int indent)
    {
        sb.Append(Spaces(indent));
        Expression.Append(sb, indent);

        if ( !string.IsNullOrWhiteSpace(Name) )
        {
            sb.Append(" AS ");
            AppendFieldName(sb, Name);
        }
    }

    public override void AddToJson(JsonObject res, bool simplifyTargetNames = false)
    {
        if ( !string.IsNullOrWhiteSpace(Name) )
            res.Add(Name, Expression.AsJson());
        else
        {
            var name = PreprocessFieldName((Expression as AstExpressionVariable)?.Name);
            if ( string.IsNullOrWhiteSpace(name) )
                throw new ($"{Expression} must have a name");
//            res.Add(name, simplifyTargetNames ? JsonValue.Create(1) : JsonValue.Create($"${name}"));
            res.Add(name, JsonValue.Create($"${name}"));
        }
    }

    public override void AddToJson(JsonArray res, bool simplifyTargetNames = false)
    {
        if ( !string.IsNullOrWhiteSpace(Name) )
            res.Add( new JsonObject([ new(Name, Expression.AsJson())]));
        else
        {
            var name = PreprocessFieldName((Expression as AstExpressionVariable)?.Name);
            if ( string.IsNullOrWhiteSpace(name) )
                throw new ($"{Expression} must have a name");
//            res.Add( new JsonObject([ new(name, simplifyTargetNames ? JsonValue.Create(1) : JsonValue.Create($"${name}"))]));
            res.Add( new JsonObject([ new(name, JsonValue.Create($"${name}"))]));
        }
    }
}
