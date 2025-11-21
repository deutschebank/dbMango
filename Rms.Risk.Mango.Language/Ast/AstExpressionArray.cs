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

public class AstExpressionArray : AstExpression
{
    public AstExpressionArray(IEnumerable<AstExpression> elements)
    {
        foreach( var v in elements)
            Add(v);
    }

    public IReadOnlyList<AstExpression> Elements => Children.OfType<AstExpression>().ToList();

    public override void Append(StringBuilder sb, int indent)
    {
        sb.AppendLine("[");
        var first = true;
        foreach (var v in Elements)
        {
            if ( !first )
                sb.AppendLine(",");
            else
                first = false;

            sb.Append(Spaces(indent + 1));
            v.Append(sb, indent + 1);
        }
        sb.AppendLine();
        sb.Append($"{Spaces(indent)}]");
    }

    public override JsonNode? AsJson() => new JsonArray([.. Elements.Select(x => x.AsJson())]);
}
