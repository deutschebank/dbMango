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

public class AstExpressionIn : AstExpression
{
    private string _variable;
    private bool _not;

    public AstExpressionIn(string variable, bool not, IEnumerable<AstExpression> values)
    {
        _variable = variable;
        _not = not;
        foreach (var value in values)
            Add(value);
    }

    public string Variable => _variable;
    public bool Not => _not;
    public IReadOnlyList<AstExpression> Values => [.. Children.OfType<AstExpression>()];

    public override void Append(StringBuilder sb, int indent)
    {
        sb.Append($"{Variable} IN ( ");
        bool first = true;
        foreach (var value in Values)
        {
            if ( !first )
                sb.Append(", ");
            else
                first = false;
                
            value.Append(sb, indent);
        }
        sb.Append($" )");
    }    

    public override JsonNode? AsJson() => 
        new JsonObject(
            [
                new(
                    "$in",
                    new JsonArray
                    {
                        new JsonNode?[]{ JsonValue.Create(Variable) }
                        .Concat(Values.Select(x => x.AsJson())
                        )
                    }
                )
            ]
        );

}
