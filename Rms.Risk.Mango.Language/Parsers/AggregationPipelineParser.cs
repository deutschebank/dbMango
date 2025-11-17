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
namespace Rms.Risk.Mango.Language.Parsers;

internal static class AggregationPipelineParser
{
    public static AstAggregation Parse(string collection, string json)
    {
        var arr = (JsonArray?)JsonNode.Parse(json);
        if ( arr == null )
            throw new ("Json array expected");
        return Parse(collection, arr);
    }

    public static AstAggregation Parse(string collection, JsonArray json)
    {
        var agg = new AstAggregation(collection);
        var pipeline = new AstPipeline();

        foreach (var stageJson in json )
        {
            if ( stageJson is not JsonObject jo )
                throw new ("Json object expected as stage");

            var stage = ParseStage(jo);
            pipeline.Add(stage);
        }

        agg.Add(pipeline);
        return agg;
    }

    private static AstStage ParseStage(JsonObject json)
    {
        var name = json.ElementAt(0).Key;

        if (json.ElementAt(0).Value is JsonValue jv)
        {
            if (name == "$unwind")
                return ParseUnwind(jv);
            else
                return new AstStageDo(json);
        }

        if (json.ElementAt(0).Value is not JsonObject body)
            throw new($"Json object expected as body of {name} stage");

        switch (name)
        {
            case "$addFields":   return ParseAddFields(body);
            case "$bucket":      return ParseBucket(body);
            case "$bucketAuto":  return ParseBucketAuto(body);
            case "$facet":       return ParseFacet(body);
            case "$project":     return ParseProject(body);
            case "$match":       return ParseMatch(body);
            case "$group":       return ParseGroup(body);
            case "$sort":        return ParseSort(body);
            case "$unwind":      return ParseUnwind(body);
            case "$lookup":      return ParseLookup(body);
            case "$replaceWith": return ParseReplaceWith(body);
            // case "$merge":              return ParseMerge(body);
            // case "$out":                return ParseOut(body);
            // case "$limit":              return ParseLimit(body);
            // case "$skip":               return ParseSkip(body);
            // case "$count":              return ParseCount(body);
            default:
                return new AstStageDo(json);

        }
    }

    private static AstStage ParseLookup(JsonObject body)
    {
        if ( !body.TryGetPropertyValue("from", out var collection)
        || !body.TryGetPropertyValue("localField", out var local) 
        || !body.TryGetPropertyValue("foreignField", out var foreign) 
        || !body.TryGetPropertyValue("as", out var asField) 
         )
            throw new($"Invalid lookup stage: {body.ToJsonString()}");

        var eqv = new AstEquivalence( new(local!.GetValue<string>()), new(foreign!.GetValue<string>()) );
        return new AstStageJoin(collection!.GetValue<string>(), asField!.GetValue<string>(), [eqv], [], null);
    }

    private static AstStage ParseReplaceRoot(JsonObject body)
    {
        throw new NotImplementedException();
    }

    private static AstStage ParseUnwind(JsonObject body)
    {
        var path = body.ElementAt(0).Value?.GetValue<string>() ?? throw new($"Expected path: {body}");
        string? index = null;
        if (body.TryGetPropertyValue("includeArrayIndex", out var indexNode))
            index= indexNode!.GetValue<string>();
        
        return new AstStageUnwind(path, index);
    }

    private static AstStage ParseUnwind(JsonValue body)
    {
        var     path  = body.GetValue<string>() ?? throw new($"Expected path: {body}");
        return new AstStageUnwind(path);
    }

    private static AstStage ParseMerge(JsonObject body)
    {
        throw new NotImplementedException();
    }

    private static AstStage ParseSort(JsonObject body)
    {
        var order = new List<AstSortField>();
        foreach (var field in body)
        {
            var name = field.Key;
            var sortOrder = field.Value?.GetValue<int>() ?? 1;

            order.Add(new(name, sortOrder != -1 ? AstSortField.SortOrder.Ascending : AstSortField.SortOrder.Descending));
        }
        return new AstStageSortBy(order);
    }

    private static AstStage ParseGroup(JsonObject body)
    {
        var fields = new List<AstLet>();
        var id = new List<AstLet>();

        foreach (var field in body)
        {
            if ( field.Key == "_id")
            {
                if (field.Value is JsonValue jv && jv.GetValueKind() == JsonValueKind.String)
                {
                    var let = new AstLetExpression(new AstExpressionVariable(jv.GetValue<string>()));
                    id.Add(let);
                }
                else
                {
                    var idObj = field.Value as JsonObject ??
                                throw new($"_id must be an object: {field.Value?.ToJsonString()}");

                    foreach (var idField in idObj)
                    {
                        var (let, _) = ParseLet(idField);
                        id.Add(let);
                    }
                }
            }
            else
            {
                var (let, _) = ParseLet(field);
                fields.Add(let);
            }
        }

        var stage = new AstStageGroupBy(id, fields);
        return stage;
    }

    private static AstStage ParseMatch(JsonObject body)
    {
        // special case: there is no logical function at the top
        if ( body.Count == 1 )
        {
            var field = body.ElementAt(0);
            if ( !field.Key.StartsWith("$") && field.Value is JsonObject jo )
            {
                var expr = ParseLogicalFuncArgument(jo);
                var eq = new AstExpressionOperation(AstExpressionOperation.OperationType.EQ, new AstExpressionVariable(field.Key), expr);
                return new AstStageWhere(eq);
            }
        }
        var expression = ParseExpression(body);
        return new AstStageWhere(expression);
    }

    private static HashSet<string> _operations = [
        "$and",
        "$or",
        "$eq",
        "$ne",
        "$gt",
        "$gte",
        "$lt",
        "$lte",
        "$add",
        "$subtract",
        "$divide",
        "$multiply"
    ];

    private static HashSet<string> _projectionLogicalOperations = [
        "$eq",
        "$ne",
        "$gt",
        "$gte",
        "$lt",
        "$lte"
    ];


    private static AstExpression ParseExpression(JsonNode? json)
    {
        switch (json)
        {
            case null: return new AstExpressionNull();
            case JsonArray ja: 
                throw new ($"Unexpected array {ja}");
            case JsonObject jo: 
            {
                if (_operations.Contains(jo.ElementAt(0).Key))
                    return ParseOperation(jo);
                else
                    return ParseFunctionCall(jo);
            }
            case JsonValue jv: 
            {
                switch (jv.GetValueKind())
                {
                    case JsonValueKind.String: 
                        if (jv.GetValue<string>().StartsWith('$') && !jv.GetValue<string>().StartsWith("$$"))
                            return new AstExpressionVariable(jv.GetValue<string>());
                        else
                            return new AstExpressionString(jv.GetValue<string>());
                    case JsonValueKind.Number:
                        if (jv.TryGetValue<long>(out var l))
                            return new AstExpressionNumber(l);
                        if (jv.TryGetValue<int>(out var i))
                            return new AstExpressionNumber((long)i);
                        if (jv.TryGetValue<double>(out var d))
                            return new AstExpressionNumber(d);
                        throw new($"Invalid number {jv}");
                    case JsonValueKind.True: return new AstExpressionBool(true);
                    case JsonValueKind.False: return new AstExpressionBool(false);
                    case JsonValueKind.Null: return new AstExpressionNull();
                    default: throw new($"Invalid json value {jv}");
                }
            }
        }
        throw new($"Invalid json expression {json}");
    }

    private static AstExpressionOperation? TryParseProjectionOperation(JsonNode? json)
    {
        if ( json is not JsonObject jo )
            return null;

        if ( jo.Count != 1 )
            return null;

        var fieldName = jo.ElementAt(0).Key;
        if ( fieldName.StartsWith("$") )
            return null;

        var operationFunc = jo.ElementAt(0).Value as JsonObject;
        if ( operationFunc?.Count != 1 )
            return null;

        var operationName = operationFunc.ElementAt(0).Key;
        if ( !_projectionLogicalOperations.Contains(operationName) )
            return null;

        // HACK: { field: { $gt: {} } is equivalent to { field: { $exists: true } }
        if ( operationName == "$gt" && operationFunc.ElementAt(0).Value is JsonObject { Count: 0 } )
        {
            return new(
                AstExpressionOperation.OperationType.EQ, 
                new AstExpressionVariable(fieldName), 
                new AstExpressionFunctionCall(
                    "exists",
                     [new (null, new AstExpressionBool(true))]));
        }

        var operationParam = ParseExpression(operationFunc.ElementAt(0).Value);

        return new(operationName, new AstExpressionVariable(fieldName), operationParam);
    }

    private static AstExpression ParseOperation(JsonObject json)
    {
        var funcName = json.ElementAt(0).Key;
        if (!funcName.StartsWith("$"))
            throw new ($"Operation name {funcName} must start with $");

        if ( json.ElementAt(0).Value is not JsonArray funcParams || funcParams.Count == 0 )
            throw new($"Operation {funcName} parameters must be an array: {json?.ToJsonString()}");

        if ( funcName == "$and" || funcName == "$or")
            return ParseLogicalOperation(funcName, funcParams);

        if ( funcParams.Count != 2 )
            throw new($"Operation {funcName} must have 2 parameters");

        var arg1 = ParseExpression(funcParams[0]);
        var arg2 = ParseExpression(funcParams[1]);

        return new AstExpressionOperation(funcName, arg1, arg2);
    }

    private static AstExpression ParseLogicalOperation(string funcName, JsonArray funcParams)
    {
        var args = funcParams.Select(ParseLogicalFuncArgument).ToList();
        if ( args.Count == 1 )
            return args[0];
        if ( args.Count == 2)
        {
            var isLogical0 = (args[0] as AstExpressionOperation) is { Operator: AstExpressionOperation.OperationType.AND } or { Operator: AstExpressionOperation.OperationType.OR };
            var isLogical1 = (args[1] as AstExpressionOperation) is { Operator: AstExpressionOperation.OperationType.AND } or { Operator: AstExpressionOperation.OperationType.OR };

            var arg0 = isLogical0 ? new AstExpressionBrackets(args[0]) : args[0];
            var arg1 = isLogical1 ? new AstExpressionBrackets(args[1]) : args[1];
            return new AstExpressionOperation(funcName, arg0, arg1);
        }
        return new AstExpressionBrackets(Join(funcName, args));
    }

    private static AstExpressionOperation Join( string funcName, List<AstExpression> args)
    {
        if ( args.Count < 2 )
            throw new($"Expecting at least 3 parameters for joining {funcName} operations");
        if ( args.Count == 2 )
            return new(funcName, args[0], args[1]);

        var op = new AstExpressionOperation(funcName, args[0], Join(funcName, [.. args.Skip(1)]));
        return op;
    }

    private static AstExpressionFunctionCall ParseFunctionCall(JsonObject json)
    {
        var funcName = json.ElementAt(0).Key;
        if (!funcName.StartsWith("$"))
            throw new ($"Function name \"{funcName}\" must start with $: {json?.ToJsonString()}");

        var funcParams = json.ElementAt(0).Value;

        List<AstFunctionArgument> namedParams   = [];
        List<AstFunctionArgument> unnamedParams = [];

        if (funcParams is JsonValue jv)
            unnamedParams.Add(new (null, ParseExpression(jv)));
        if (funcParams is JsonArray ja)
            unnamedParams.AddRange(ja.Select(x => new AstFunctionArgument(null, ParseExpression(x))));
        if (funcParams is JsonObject jo)
        {
            foreach ( var arg in jo)
            {
                if ( arg.Value is JsonArray arrArg )
                {
                    var arrayMembers = new List<AstExpression>();
                    foreach (var member in arrArg)
                        arrayMembers.Add(ParseExpression(member));
                    
                    namedParams.Add(new(arg.Key, new AstExpressionArray(arrayMembers)));
                }
                else
                {
                    if (arg.Key.StartsWith("$") )
                    {
                        var funcObj = new JsonObject([new(arg.Key, arg.Value?.DeepClone())]);
                        unnamedParams.Add(new(null, ParseFunctionCall(funcObj)));
                        continue;
                    }

                    if (arg.Value is JsonObject inner)
                    {
                        if (inner.Count == 1 && inner.ElementAt(0).Key.StartsWith('$') )
                        {
                            //var funcObj = new JsonObject([new(inner.ElementAt(0).Key, inner.ElementAt(0).Value?.DeepClone())]);
                            namedParams.Add(new(arg.Key, ParseExpression(inner)));
                            continue;

                        }
                    }

                    var expr = ParseExpression(arg.Value);
                    if ( expr is AstExpressionFunctionCall )
                        unnamedParams.Add(new (null, expr));
                    else
                        namedParams.Add(new(arg.Key, expr));
                }
            }
        }

        return new(funcName, unnamedParams.Concat( namedParams ));
    }

    /// <summary>
    /// Special case for $and and $or - their arguments can be like { aaa : 1} which means "a == 1".
    /// </summary>
    private static AstExpression ParseLogicalFuncArgument(JsonNode? json)
    {
        if (json is JsonObject jo && !jo.ElementAt(0).Key.StartsWith('$'))
        {
            var projectionOperation = TryParseProjectionOperation(json);
            if ( projectionOperation != null )
                return projectionOperation;

            if ( jo.ElementAt(0).Value is JsonValue )
            {
                // simple equality
                var right = ParseExpression(jo.ElementAt(0).Value);
                return new AstExpressionOperation(AstExpressionOperation.OperationType.EQ, new AstExpressionVariable(jo.ElementAt(0).Key), right);
            }
            else if ( jo.ElementAt(0).Value is JsonObject joRight )
            {
                // projection https://www.mongodb.com/docs/manual/reference/operator/query/
                // only $exists is supported.
                if (joRight.ElementAt(0).Key == "$exists" &&  joRight.ElementAt(0).Value is JsonValue)
                    return new AstExpressionExists(jo.ElementAt(0).Key, joRight.ElementAt(0).Value!.GetValue<bool>());

                // full range of projections is not supported.
                //return new AstExpressionProjection(jo.ElementAt(0).Key, joRight);
                var right = ParseExpression(jo.ElementAt(0).Value);
                return new AstExpressionOperation(AstExpressionOperation.OperationType.EQ, new AstExpressionVariable(jo.ElementAt(0).Key), right);
            }
        }
        return ParseExpression(json);
    }

    private static AstStage ParseProject(JsonObject body)
    {
        var fields = new List<AstLet>();
        var idFields = new List<AstLet>();

        AstLet? let;

        var exclude = false;
        foreach (var field in body)
        {
            if ( field is { Key: "_id", Value: JsonObject idObj })
            {
                foreach (var idField in idObj)
                {
                    (let, _) = ParseLet(idField);
                    idFields.Add(let);
                }
                exclude = false;
                continue;
            }

            bool exc;
            
            (let, exc) = ParseLet(field);
            exclude = exc;
            fields.Add(let);
        }

        var stage = new AstStageProject(idFields, fields, exclude);
        return stage;
    }

    private static AstStage ParseReplaceWith(JsonObject body)
    {
        var fields = new List<AstLet>();
        var idFields = new List<AstLet>();

        AstLet? let;

        foreach (var field in body)
        {
            if ( field is { Key: "_id", Value: JsonObject idObj })
            {
                foreach (var idField in idObj)
                {
                    (let, _) = ParseLet(idField);
                    idFields.Add(let);
                }
                continue;
            }

            (let, _) = ParseLet(field);
            fields.Add(let);
        }

        var stage = new AstStageReplace(idFields, fields);
        return stage;
    }

    private static (AstLet, bool) ParseLet(KeyValuePair<string, JsonNode?> field)
    {
        var exclude = false;

        if ( field.Value is JsonArray ja )
            return (ParseArrayProjection(field.Key, ja),false);
        if ( field.Value is JsonObject { Count: > 0 } jo && !jo.ElementAt(0).Key.StartsWith('$') )
            return (ParseObjectProjection(field.Key, jo),false);

        var expression = ParseExpression(field.Value);
        AstLet let;
        
        // if any argument is like aaa : 0 this means PROJECT EXCLUDE aaa
        if (expression is AstExpressionNumber { IsLong: true } en)
        {
            if (en.LongValue == 0)
                exclude = true;
            let = new AstLetExpression(new AstExpressionVariable(field.Key));
        }
        else
            let = new AstLetExpression(expression, field.Key);
        return (let, exclude);
    }

    private static AstLetArray ParseArrayProjection(string name, JsonArray ja)
    {
        var fields = new List<AstLet>();

        foreach ( var o in ja.OfType<JsonObject>() )
        {
            var singleLet = ParseLet(new("", o)).Item1;
            fields.Add(singleLet);
        }
        var res = new AstLetArray(name, fields, true);
        return res;
    }

    private static AstLetArray ParseObjectProjection(string name, JsonObject jo)
    {
        var fields = new List<AstLet>();

        foreach (var pair in jo)
        {
            var singleLet = ParseLet(pair).Item1;
            fields.Add(singleLet);
        }
        var res = new AstLetArray(name, fields, false);
        return res;
    }

    private static AstStage ParseAddFields(JsonObject body)
    {
        var fields = new List<AstLet>();

        var exclude = false;
        foreach (var field in body)
        {
            if ( field.Value is JsonArray ja )
            {
                var arrayProjection = ParseArrayProjection(field.Key, ja);
                fields.Add(arrayProjection);
            }
            else
            {
                var (let, exc) = ParseLet(field);
                exclude = exc;
                fields.Add(let);
            }
        }

        var stage = new AstStageAddFields(fields);
        return stage;
    }

    private static AstStage ParseBucket(JsonObject body)
    {
        var stage = new AstStageBucket();

        foreach (var field in body)
        {
            switch (field)
            {
                case { Key: "groupBy",  }:
                    stage.GroupBy = ParseExpression(field.Value);
                    break;
                case { Key: "default", Value: JsonValue val }:
                    stage.DefaultBucket = val.ToString();
                    break;
                case { Key: "boundaries", Value: JsonArray arr }:
                {
                    foreach (var v in arr.Select(x => x?.ToString()).Where(x => x != null))
                        stage.AddBucket(v!.Trim('"'));
                    break;
                }
                case { Key: "output", Value: JsonObject output }:
                {
                    foreach (var let in output)
                    {
                        if (let.Value is JsonArray ja)
                        {
                            var arrayProjection = ParseArrayProjection(let.Key, ja);
                            stage.Add(arrayProjection);
                        }
                        else
                        {
                            var (let1, _) = ParseLet(let);
                            stage.Add(let1);
                        }
                    }

                    break;
                }
            }
        }

        return stage;
    }

    private static AstStage ParseBucketAuto(JsonObject body)
    {
        var stage = new AstStageBucket()
        {
            Auto = true
        };

        foreach (var field in body)
        {
            switch (field)
            {
                case { Key: "groupBy",  }:
                    stage.GroupBy = ParseExpression(field.Value);
                    break;
                case { Key: "buckets", Value: JsonValue val }:
                    stage.NumberOfBuckets = val.GetValue<int>();
                    break;
                case { Key: "granularity", Value: JsonValue val }:
                    stage.Granularity = val.ToString();
                    break;
                case { Key: "output", Value: JsonObject output }:
                {
                    foreach (var let in output)
                    {
                        if (let.Value is JsonArray ja)
                        {
                            var arrayProjection = ParseArrayProjection(let.Key, ja);
                            stage.Add(arrayProjection);
                        }
                        else
                        {
                            var (let1, _) = ParseLet(let);
                            stage.Add(let1);
                        }
                    }

                    break;
                }
            }
        }

        return stage;
    }

    private static AstStage ParseFacet(JsonObject body)
    {
        var stage = new AstStageFacet();

        foreach (var (name, value) in body)
        {
            var pipeLine = ParsePipeline(value as JsonArray);

            stage.Add( new AstNamedPipeline(name, new (pipeLine)));
        }

        return stage;
    }

    private static List<AstStage> ParsePipeline(JsonArray? json)
    {
        var pipeline = new List<AstStage>();
        if (json == null)
            return pipeline;

        foreach ( var stageJson in json.OfType<JsonObject>())
        {
            var stage = ParseStage(stageJson);
            pipeline.Add(stage);
        }
        return pipeline;
    }
}
