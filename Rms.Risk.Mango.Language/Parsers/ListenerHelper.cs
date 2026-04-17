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
using Parser =Rms.Risk.Mango.Language.MongoAggregationForHumansParser;

namespace Rms.Risk.Mango.Language.Parsers;

internal static class ListenerHelper
{
    public static List<AstLet> BuildLet(Parser.Let_listContext? context)
    {
        var lets = new List<AstLet>();
        if ( context == null )
            return lets;

        foreach (var let in context.let_list().SelectMany(BuildLet))
        {
            lets.Add(let);
        }
        if (context.let_list_item() != null)
        {
            var let = BuildSingleLet(context.let_list_item());
            lets.Add(let);
        }
        return lets;

        static AstLet BuildSingleLet(Parser.Let_list_itemContext let)
        {
            switch (let)
            {
                //case Parser.LetVariableContext vc:
                //    return new AstLetExpression(new AstExpressionVariable(vc.VARIABLE().GetText()));
                case Parser.LetExpressionAsContext ec:
                    {
                        var expression = BuildAst(ec.expression());
                        
                        var name = ec.STRING()?.GetText()?.Trim('"');
                        if (string.IsNullOrWhiteSpace(name))
                            name = ec.VARIABLE()?.GetText();
                        
                        if (string.IsNullOrWhiteSpace(name) && expression is AstExpressionString se && se.Value.StartsWith("$"))
                            expression = new AstExpressionVariable(se.Value[1..]);
                        
                        if (string.IsNullOrWhiteSpace(name) && expression is not AstExpressionVariable)
                            throw new($"Only plain variable allowed to be unnamed. Please add 'AS'. Got {ec.GetText()} ({expression.GetType()})");
                        return new AstLetExpression(expression, name);
                    }

                case Parser.LetArrayContext ac:
                    return ParseLetArray(ac);
                case Parser.LetObjectContext oc:
                    return ParseLetObject(oc);
                default:
                    throw new($"Invalid let: {let.GetType().Name}: {let.GetText()}");
            }
        }
    }

    private static AstLetArray ParseLetObject(Parser.LetObjectContext oc)
    {
        var fields = new List<AstLet>();
        foreach (var field in oc.let_list().let_list())
        {
            var x = BuildLet(field);
            fields.AddRange(x);
        }
        return new (oc.VARIABLE()?.GetText(), fields, false);
    }

    private static AstLetArray ParseLetArray(Parser.LetArrayContext ac)
    {
        var fields = new List<AstLet>();
        foreach (var field in ac.let_list().let_list())
        {
            var x = BuildLet(field);
            fields.AddRange(x);
        }
        return new ((ac.VARIABLE() ?? ac.STRING())?.GetText(), fields, true);
    }

    public static List<AstSortField> BuildSortFieldList(Parser.Sort_var_listContext context)
    {
        var lets = new List<AstSortField>();
        foreach (var let in context.sort_var_list().SelectMany(BuildSortFieldList))
        {
            lets.Add(let);
        }
        if (context.VARIABLE() != null)
        {
            var let = new AstSortField(context.VARIABLE().GetText(), AstSortField.SortOrder.Ascending);
            lets.Add(let);
        }
        return lets;
    }

    public static List<AstLet> BuildVarList(Parser.Var_listContext context)
    {
        var lets = new List<AstLet>();
        foreach (var let in context.var_list().SelectMany(BuildVarList))
        {
            lets.Add(let);
        }
        if (context.VARIABLE() != null)
        {
            var let = new AstLetExpression(new AstExpressionVariable(context.VARIABLE().GetText()));
            lets.Add(let);
        }
        return lets;
    }

    public static List<AstEquivalence> BuildEquivalence(Parser.Equivalence_listContext context)
    {
        var eqs = new List<AstEquivalence>();
        if (context is Parser.VarEquivalenceContext vc)
        {
            var left = new AstExpressionVariable(vc.left.Text);
            var right = new AstExpressionVariable(vc.right.Text);
            eqs.Add(new(left, right));
        }
        if (context is Parser.EquivalenceListContext ec)
        {
            foreach (var eq in ec.equivalence_list().SelectMany(BuildEquivalence))
            {
                eqs.Add(eq);
            }
        }
        return eqs;
    }

    public static List<AstFunctionArgument> BuildUnnamedArgumentsList(Parser.Unnamed_args_listContext? context)
    {
        var arguments = new List<AstFunctionArgument>();
        foreach (var exp in context?.unnamed_args_list()?.SelectMany(BuildUnnamedArgumentsList) ?? [])
        {
            arguments.Add(exp);
        }
        if (context?.expression() != null)
        {
            var exp = BuildAst(context.expression());
            arguments.Add(new("", exp));
        }
        else if (context?.expression_array() != null)
        {
            var exp = BuildArray(context.expression_array());
            arguments.Add(new("", exp));
        }
        return arguments;
    }

    public static AstExpressionArray BuildArray(Parser.Expression_arrayContext context)
    {
            var fields = new List<AstFunctionArgument>();
            foreach (var field in context.expression_array_item())
            {
                if ( field.expression() != null)
                {
                    var exp = BuildAst(field.expression());
                    fields.Add(new("", exp));
                }
                else if ( field.expression_array() != null)
                {
                    var exp = BuildArray(field.expression_array());
                    fields.Add(new("", exp));
                }
            }
            return new(fields);
    }

    public static List<AstFunctionArgument> BuildNamedArgumentsList(Parser.Named_args_listContext? context)
    {
        var arguments = new List<AstFunctionArgument>();
        if (context == null)
            return arguments;

        arguments.AddRange(context.named_args_list()?.SelectMany(BuildNamedArgumentsList) ?? []);
        if (context.expression() != null)
        {
            var exp = BuildAst(context.expression());
            arguments.Add(new(context.VARIABLE().GetText(), exp));
        }
        else if (context.expression_array() != null)
        {
            var exp = BuildArray(context.expression_array());
            arguments.Add(new(context.VARIABLE().GetText(), exp));
        }
        return arguments;
    }

    public static AstExpression BuildAst(Parser.ExpressionContext context)
    {
        if ( context.comparizon_expression().Length == 1 )
            return BuildAst(context.comparizon_expression()[0]);

        var op = context.AND().Length > 0
        ? AstExpressionOperation.OperationType.AND
        : context.OR().Length > 0
            ? AstExpressionOperation.OperationType.OR
            : throw new($"Invalid operation: {context?.GetType().Name}: {context?.GetText()}");

        var list = new List<AstExpression>();
        foreach (var exp in context.comparizon_expression())
        {
            list.Add(BuildAst(exp));
        }
        return new AstExpressionOperation(op, list);
    }

    public static AstExpression BuildAst(Parser.Comparizon_expressionContext context)
    {
        if ( context.additive_expression().Length == 1 )
            return BuildAst(context.additive_expression()[0]);

        var op =
            context.LT().Length > 0
            ? AstExpressionOperation.OperationType.LT
            : context.GT().Length > 0
                ? AstExpressionOperation.OperationType.GT
                : context.EQ().Length > 0
                    ? AstExpressionOperation.OperationType.EQ
                    : context.NEQ().Length > 0
                       ? AstExpressionOperation.OperationType.NEQ
                       : context.LTE().Length > 0
                           ? AstExpressionOperation.OperationType.LTE
                           : context.GTE().Length > 0
                               ? AstExpressionOperation.OperationType.GTE
                               : throw new($"Invalid operation: {context?.GetType().Name}: {context?.GetText()}");

        var list = new List<AstExpression>();
        foreach (var exp in context.additive_expression())
        {
            list.Add(BuildAst(exp));
        }
        return new AstExpressionOperation(op, list);
    }

    public static AstExpression BuildAst(Parser.Additive_expressionContext context)
    {
        if ( context.multiplicative_expression().Length == 1 )
            return BuildAst(context.multiplicative_expression()[0]);

        var op =
            context.PLUS().Length > 0
            ? AstExpressionOperation.OperationType.PLUS
            : context.MINUS().Length > 0
                ? AstExpressionOperation.OperationType.MINUS
                : throw new($"Invalid operation: {context?.GetType().Name}: {context?.GetText()}");

        var list = new List<AstExpression>();
        foreach (var exp in context.multiplicative_expression())
        {
            list.Add(BuildAst(exp));
        }
        return new AstExpressionOperation(op, list);
    }

    public static AstExpression BuildAst(Parser.Multiplicative_expressionContext context)
    {
        if ( context.unary_expression().Length == 1 )
            return BuildAst(context.unary_expression()[0]);

        var op =
            context.MUL().Length > 0
            ? AstExpressionOperation.OperationType.MULTIPLY
            : context.DIV().Length > 0
                ? AstExpressionOperation.OperationType.DIVIDE
                : throw new($"Invalid operation: {context?.GetType().Name}: {context?.GetText()}");

        var list = new List<AstExpression>();
        foreach (var exp in context.unary_expression())
        {
            list.Add(BuildAst(exp));
        }
        return new AstExpressionOperation(op, list);
    }

    public static AstExpression BuildAst(Parser.Unary_expressionContext context)
    {
        var expr = context switch
        {
            Parser.UnaryExpressionContext unary => 
                new AstExpressionUnary( 
                    unary.NOT() != null 
                        ? AstExpressionUnary.OperationType.NOT 
                        : unary.MINUS() != null
                            ? AstExpressionUnary.OperationType.MINUS  
                            : unary.PLUS() != null 
                                ? AstExpressionUnary.OperationType.PLUS 
                                : throw new($"Invalid operation: {context?.GetType().Name}: {context?.GetText()}")
                    , BuildAst(unary.unary_expression())),
            Parser.PrimaryExpressionContext brackets => BuildAst(brackets.brackets_expression()),
            _ => throw new($"Invalid expression: {context?.GetType().Name}: {context?.GetText()}")
        };

        return expr;
    }

    public static AstExpression BuildAst(Parser.Brackets_expressionContext context)
    {
        var expr = context switch
        {
            Parser.AtomExpressionContext atom         => BuildAtom(atom.atom()),
            Parser.BracketsExpressionContext brackets => BuildAst(brackets.expression()),
            Parser.InExpressionContext @in            => new AstExpressionIn(@in.VARIABLE().GetText(), @in.NOT() != null, @in.expression().Select(BuildAst)),
            Parser.FuncExpressionContext func         => new AstExpressionFunctionCall(func.VARIABLE().GetText(), BuildUnnamedArgumentsList(func.unnamed_args_list()).Concat( BuildNamedArgumentsList(func.named_args_list())) ),
            Parser.ProjectionExpressionContext proj   => new AstExpressionProjection(proj.VARIABLE().GetText(), JsonListenerHelper.Convert(proj.json())),
            Parser.ExistsExpressionContext exists     => new AstExpressionExists(exists.VARIABLE().GetText(), exists.NOT() == null),

            _ => throw new($"Invalid expression: {context?.GetType().Name}: {context}")
        };

        return expr;

        static AstExpression BuildAtom(Parser.AtomContext atom)
        {
            if (atom.STRING() != null)
                return new AstExpressionString(atom.STRING().GetText());
            else if (atom.NUMBER() != null)
                return new AstExpressionNumber(atom.NUMBER().GetText());
            else if (atom.GetText() == "true")
                return new AstExpressionBool(true);
            else if (atom.GetText() == "false")
                return new AstExpressionBool(false);
            else if (atom.GetText() == "null")
                return new AstExpressionNull();
            else if (atom.VARIABLE() != null)
                return new AstExpressionVariable(atom.VARIABLE().GetText());
            throw new($"Invalid atom: {atom?.GetType().Name}: {atom}");
        }

    }
}
