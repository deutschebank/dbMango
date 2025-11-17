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
﻿using System.Text.Json.Serialization.Metadata;
using Rms.Risk.Mango.Language;
using Rms.Risk.Mango.Language.Ast;
using Rms.Risk.Mango.Pivot.Core;
using Rms.Risk.Mango.Pivot.Core.Models;

namespace Rms.Risk.Mango.Services;

public static class AfhHelpers
{
    private static readonly System.Text.Json.JsonSerializerOptions _prettyPrint = new()
    {
        WriteIndented    = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };


    private static Func<PivotDefinition, FilterExpressionTree.ExpressionGroup? /*extra filter*/, Dictionary<string, Type> /*fieldTypes*/, Func<string,string> /*getAggregationOperator*/, string>? _prevConvertToJson;

    public static void Init()
    {
        _prevConvertToJson            = PivotDefinition.ConvertToJson;
        PivotDefinition.ConvertToJson = ConvertToJson;
    }

    public static string ConvertToJson(
        PivotDefinition       pivot,
        FilterExpressionTree.ExpressionGroup? extraFilter,
        Dictionary<string, Type> fieldTypes,
        Func<string, string> getAggregationOperator)
    {
        if (_prevConvertToJson  == null)
            throw new ApplicationException("Call AfhHelpers.Init() first");

        if ( pivot.PivotType != PivotTypeEnum.AggregationForHumans )
            return _prevConvertToJson(pivot, extraFilter, fieldTypes, getAggregationOperator );

        var filterText = "";
        if ( extraFilter?.IsEmpty == false )
        {
            var afhFilter = ConvertToExpression(extraFilter!, fieldTypes);
            filterText = $" AND ( {afhFilter.AsText()} )";
        }

        var keys         = ConvertKeys(pivot.KeyFields);
        var preserveKeys = ConvertKeysPreserve(pivot.KeyFields);
        var idToRootKeys = ConvertKeysFromIdToRoot(pivot.KeyFields);

        var query = pivot.CustomQuery
               .Replace("/*[EXTRA_FILTER]*/",         filterText)
               .Replace("/*[KEYS]*/",                 keys)
               .Replace("/*[KEYS_PRESERVE]*/",        preserveKeys)
               .Replace("/*[KEYS_FROM_ID_TO_ROOT]*/", idToRootKeys)
            ;

        var ast  = LanguageParser.ParseScriptToAST(query);
        var json = ast.AsJson() ?? throw new ApplicationException("Failed to convert AST to JSON");

        return json.ToJsonString(_prettyPrint);
    }

    private static string? ConvertKeys(string[] keys) => string.Join(", ", keys.Select(x => $"'${x}'"));
    private static string? ConvertKeysPreserve(string[] keys) => string.Join(", ", keys.Select(x => $"'${x}' AS '${x}'"));
    private static string? ConvertKeysFromIdToRoot(string[] keys) => string.Join(", ", keys.Select(x => $"'$_id.{x}' AS '${x}'"));

    private static AstExpressionOperation ConvertToExpression(FilterExpressionTree.ExpressionGroup filter, Dictionary<string, Type> fieldTypes)
    {
        var afhFilter = new AstExpressionOperation(
            filter.Condition == FilterExpressionTree.ExpressionGroup.ConditionType.And
                ? AstExpressionOperation.OperationType.AND
                : AstExpressionOperation.OperationType.OR
          , filter.Children.Select( x =>
                x switch
                {
                    FilterExpressionTree.FieldExpression fe => ConvertToExpression(fe, fieldTypes),
                    FilterExpressionTree.ExpressionGroup eg => ConvertToExpression(eg, fieldTypes),
                    _ => throw new NotSupportedException()
                })
            );

        return afhFilter;
    }

    private static AstExpression ConvertToExpression(FilterExpressionTree.FieldExpression filter, Dictionary<string, Type> fieldTypes)
    {
        switch (filter.Condition)
        {
            case FilterExpressionTree.FieldConditionType.EqualTo:
                return new AstExpressionOperation(
                        AstExpressionOperation.OperationType.EQ,
                        new AstExpressionVariable(filter.Field),
                        ConvertConstant(filter.Argument, fieldTypes));

            case FilterExpressionTree.FieldConditionType.NotEqualTo:
                return new AstExpressionOperation(
                    AstExpressionOperation.OperationType.NEQ,
                    new AstExpressionVariable(filter.Field),
                    ConvertConstant(filter.Argument, fieldTypes));

            case FilterExpressionTree.FieldConditionType.GreaterThan:
                return new AstExpressionOperation(
                    AstExpressionOperation.OperationType.GT,
                    new AstExpressionVariable(filter.Field),
                    ConvertConstant(filter.Argument, fieldTypes));

            case FilterExpressionTree.FieldConditionType.LessThan:
                return new AstExpressionOperation(
                    AstExpressionOperation.OperationType.LT,
                    new AstExpressionVariable(filter.Field),
                    ConvertConstant(filter.Argument, fieldTypes));

            case FilterExpressionTree.FieldConditionType.GreaterThanOrEqualTo:
                return new AstExpressionOperation(
                    AstExpressionOperation.OperationType.GTE,
                    new AstExpressionVariable(filter.Field),
                    ConvertConstant(filter.Argument, fieldTypes));

            case FilterExpressionTree.FieldConditionType.LessThanOrEqualTo:
                return new AstExpressionOperation(
                    AstExpressionOperation.OperationType.LTE,
                    new AstExpressionVariable(filter.Field),
                    ConvertConstant(filter.Argument, fieldTypes));

            case FilterExpressionTree.FieldConditionType.IsEmpty:
                return new AstExpressionExists(filter.Field, false);

            case FilterExpressionTree.FieldConditionType.NotIsEmpty:
                return new AstExpressionExists(filter.Field, true);

            case FilterExpressionTree.FieldConditionType.IsNull:
                return new AstExpressionOperation(
                    AstExpressionOperation.OperationType.EQ,
                    new AstExpressionVariable(filter.Field),
                    new AstExpressionNull());

            case FilterExpressionTree.FieldConditionType.NotIsNull:
                return new AstExpressionOperation(
                    AstExpressionOperation.OperationType.NEQ,
                    new AstExpressionVariable(filter.Field),
                    new AstExpressionNull());

            case FilterExpressionTree.FieldConditionType.Contains:
            case FilterExpressionTree.FieldConditionType.StartsWith:
            case FilterExpressionTree.FieldConditionType.EndsWith:
            case FilterExpressionTree.FieldConditionType.Matches:
            case FilterExpressionTree.FieldConditionType.DoesNotMatch:
            case FilterExpressionTree.FieldConditionType.DoesNotContain:
            case FilterExpressionTree.FieldConditionType.DoesNotStartWith:
            case FilterExpressionTree.FieldConditionType.DoesNotEndWith:
            default:
                throw new NotImplementedException();
        }
    }

    private static AstExpression ConvertConstant(string? filterArgument, Dictionary<string, Type> fieldTypes)
    {
        if (filterArgument == null)
            return new AstExpressionNull();
        if ( long.TryParse(filterArgument, out var l))
            return new AstExpressionNumber(l);
        if ( double.TryParse(filterArgument, out var d))
            return new AstExpressionNumber(d);
        return new AstExpressionString(filterArgument);
    }
}