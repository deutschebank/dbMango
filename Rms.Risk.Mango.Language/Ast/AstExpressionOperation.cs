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
using System.Diagnostics.CodeAnalysis;

namespace Rms.Risk.Mango.Language.Ast;

public class AstExpressionOperation : AstExpression
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum OperationType
    {
        AND,
        OR,
        EQ,
        NEQ,
        GT,
        GTE,
        LT,
        LTE,
        PLUS,
        MINUS,
        DIVIDE,
        MULTIPLY
    }

    public static OperationType GetOperationType(string op) => op switch
    {
        "$and"      => OperationType.AND,
        "AND"       => OperationType.AND,
        "$or"       => OperationType.OR,
        "OR"        => OperationType.OR,
        "$eq"       => OperationType.EQ,
        "=="        => OperationType.EQ,
        "$ne"       => OperationType.NEQ,
        "<>"        => OperationType.NEQ,
        "!="        => OperationType.NEQ,
        "$gt"       => OperationType.GT,
        ">"         => OperationType.GT,
        "$gte"      => OperationType.GTE,
        ">="        => OperationType.GTE,
        "$lt"       => OperationType.LT,
        "<"         => OperationType.LT,
        "$lte"      => OperationType.LTE,
        "<="        => OperationType.LTE,
        "$add"      => OperationType.PLUS,
        "+"         => OperationType.PLUS,
        "$subtract" => OperationType.MINUS,
        "-"         => OperationType.MINUS,
        "$divide"   => OperationType.DIVIDE,
        "/"         => OperationType.DIVIDE,
        "$multiply" => OperationType.MULTIPLY,
        "*"         => OperationType.MULTIPLY,
        _ => throw new($"Invalid operator '{op}'")
    };

    public static string GetOperationStr(OperationType op) => op switch
    {
        OperationType.AND      => "AND",
        OperationType.OR       => "OR",
        OperationType.EQ       => "==",
        OperationType.NEQ      => "!=",
        OperationType.GT       => ">",
        OperationType.GTE      => ">=",
        OperationType.LT       => "<",
        OperationType.LTE      => "<=",
        OperationType.PLUS     => "+",
        OperationType.MINUS    => "-",
        OperationType.DIVIDE   => "/",
        OperationType.MULTIPLY => "*",
        _ => throw new($"Invalid operator '{op}'")
    };

    public static string GetOperationCall(OperationType op) => op switch
    {
        OperationType.AND      => "$and",
        OperationType.OR       => "$or",
        OperationType.EQ       => "$eq",
        OperationType.NEQ      => "$ne",
        OperationType.GT       => "$gt",
        OperationType.GTE      => "$gte",
        OperationType.LT       => "$lt",
        OperationType.LTE      => "$lte",
        OperationType.PLUS     => "$add",
        OperationType.MINUS    => "$subtract",
        OperationType.DIVIDE   => "$divide",
        OperationType.MULTIPLY => "$multiply",
        _ => throw new($"Invalid operator '{op}'")
    };

    private static HashSet<OperationType> _condition =
    [
        OperationType.EQ,
        OperationType.NEQ,
        OperationType.LT,
        OperationType.LTE,
        OperationType.GT,
        OperationType.GTE
    ];

    private static HashSet<OperationType> _groupable =
    [
        OperationType.AND,
        OperationType.OR,
        OperationType.PLUS,
        OperationType.MULTIPLY
    ];


    public AstExpressionOperation(string op, params IEnumerable<AstExpression> args)
    : this(GetOperationType(op), args)
    {
    }

    public AstExpressionOperation(OperationType op, params IEnumerable<AstExpression> args)
    {
        Operator = op;
        foreach (var arg in args)
            Add(arg);
    }

    public OperationType Operator    { get; }
    public string        OperatorStr                => GetOperationStr(Operator);
    public IReadOnlyList<AstExpression> Args        => Children.OfType<AstExpression>().ToList();

    public override void Append(StringBuilder sb, int indent)
    {
        var first = true;
        foreach (var arg in Args)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                if (Operator == OperationType.AND || Operator == OperationType.OR)
                {
                    sb.AppendLine();
                    sb.Append($"{Spaces(indent)}{OperatorStr} ");
                }
                else
                {
                    sb.Append($" {OperatorStr} ");
                }
            }
            arg.Append(sb, indent);
        }
    }

    public override JsonNode? AsJson()
    {
        var functionArgs   = Args;
        var withinFunction = GetProperty(AstExpressionFunctionCall.WithinFunctionProperty, false);
        // special case - equality operator
        if (_condition.Contains(Operator) && !withinFunction)
        {
            // for root level = use this form: { field : value }
            if (Operator == OperationType.EQ 
             && functionArgs is [
                    AstExpressionVariable varArg,  
                    { } condArg and (AstExpressionNumber or AstExpressionString or AstExpressionVariable)
                ]
            )
            {
                var op = new JsonObject(
                [
                    new(
                        varArg.Name,
                        condArg.AsJson()
                    )
                ]);

                return op;
            }

            if (Operator == OperationType.EQ 
             && functionArgs is [
                    { } condArg1 and (AstExpressionNumber or AstExpressionString or AstExpressionVariable),  
                    AstExpressionVariable varArg1
                ]
               )
            {
                var op = new JsonObject(
                [
                    new(
                        varArg1.Name,
                        condArg1.AsJson()
                    )
                ]);

                return op;
            }

            if (functionArgs is
                [
                    AstExpressionVariable varArg2,
                    { } condArg2
                ])
            {
                // for other conditions use { field : { $op : value } }
                var op1 = new JsonObject(
                    [
                        new(
                            varArg2.Name,
                            new JsonObject([
                                new(
                                    GetOperationCall(Operator),
                                    condArg2.AsJson()
                                )
                            ])
                        )
                    ]
                );
                return op1;
            }
        }

        var funcArgs = Args;

        // make one for expressions like  a+b+c+d
        if (_groupable.Contains(Operator))
        {
            var op = CreateFunctionCall(funcArgs);
            return op;
        }

        // make separate calls for expressions like  a-b-c-d
        var revArgs     = funcArgs.Reverse().ToList();
        var currentFunc = CreateFunctionCall(revArgs[1], revArgs[0]);
        for (var i = 2; i < revArgs.Count; i++)
            currentFunc = ChainFunctionCall(revArgs[i], currentFunc);

        return currentFunc;

        JsonObject CreateFunctionCall(params IReadOnlyList<AstExpression> a)
        {
            var args     = new JsonArray();
            foreach (var v in a.Select(x => x.AsJson()))
            {
                args.Add(v);
            }

            var op = new JsonObject(
                [
                    new(
                        GetOperationCall(Operator),
                        args
                    )
                ]
            );
            return op;
        }

        JsonObject ChainFunctionCall(AstExpression arg1, JsonObject arg2)
        {
            JsonArray args =
            [
                arg1.AsJson(),
                arg2
            ];

            var op = new JsonObject(
                [
                    new(
                        GetOperationCall(Operator),
                        args
                    )
                ]
            );
            return op;
        }

    }
}
