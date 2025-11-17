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

public class AstExpressionUnary : AstExpression
{
    public enum OperationType
    {
        NOT,
        MINUS,
        PLUS
    
    }

   public AstExpressionUnary(OperationType op, AstExpression arg1)
    {
        Operator = op;
        Add(arg1);
    }

    public OperationType Operator { get; }
    public string OperatorStr => Operator switch
    {
        OperationType.NOT => "NOT",
        OperationType.MINUS => "-",
        OperationType.PLUS => "+",
        _ => throw new($"Invalid operator '{Operator}'")
    };

    public AstExpression Arg1 => Children.OfType<AstExpression>().First();

    public override void Append(StringBuilder sb, int indent)
    {
        switch (Operator)
        {
            case OperationType.MINUS:
                sb.Append("- ");
                break;
            case OperationType.PLUS:
                sb.Append("+ ");
                break;
            case OperationType.NOT:
                sb.Append("NOT ");
                break;
        }
        Arg1.Append(sb, indent);
    }

    public override JsonNode? AsJson()
    {
        switch (Operator)
        {
            case OperationType.PLUS:
                return Arg1.AsJson();
            case OperationType.MINUS:
            {
                var left = JsonValue.Create(0);
                var right = Arg1.AsJson();

                var op = new JsonObject(
                    [
                        new(
                            "$subtract",
                            new JsonArray { new []{left,right} }
                        )
                    ]
                );

                return op;

            }
            case OperationType.NOT:
            {
                var right = Arg1.AsJson();
                var op = new JsonObject(
                    [
                        new(
                            "$not",
                            new JsonArray { new []{right} }
                        )
                    ]
                );

                return op;                    
            }
            default:
                throw new($"Invalid operator '{Operator}'");
        }
    }
}
