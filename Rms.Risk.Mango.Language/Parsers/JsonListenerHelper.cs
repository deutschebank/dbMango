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

public static class JsonListenerHelper
{
    public static JsonNode Convert(MongoAggregationForHumansParser.JsonContext context)
    {
        if (context.@object() != null)
        {
            return ConvertToJsonObject(context.@object());
        }
        else if (context.array() != null)
        {
            return ConvertToJsonArray(context.array());
        }
        throw new($"Invalid json: {context?.GetType().Name}: {context}");
    }

    private static JsonNode? ConvertToJsonValue(MongoAggregationForHumansParser.ValueContext context)
    {
        if (context.VARIABLE() != null)
        {
            return JsonValue.Create(context.VARIABLE().GetText().Trim('"'));
        }
        else if (context.STRING() != null)
        {
            return JsonValue.Create(context.STRING().GetText().Trim('"'));
        }
        else if (context.NUMBER() != null)
        {
            var d = double.Parse(context.NUMBER().GetText());
            if ( d == Math.Floor(d) )
            {
                if ( d >= int.MinValue || d <= int.MaxValue )
                    return JsonValue.Create((int)d);
                return JsonValue.Create((long)d);
            }
            else
                return JsonValue.Create(d);
        }
        else if (context.GetText() == "true")
        {
            return JsonValue.Create(true);
        }
        else if (context.GetText() == "false")
        {
            return JsonValue.Create(false);
        }
        else if ( context.@object() != null )
        {
            return ConvertToJsonObject(context.@object());
        }
        else if ( context.array() != null )
        {
            return ConvertToJsonArray(context.array());
        }
        else if (context.GetText() == "null")
        {
            return null;
        }
        throw new($"Invalid json value: {context?.GetType().Name}: {context}");
    }

    private static JsonArray ConvertToJsonArray(MongoAggregationForHumansParser.ArrayContext context)
    {
        var array = new JsonArray();
        foreach (var value in context.value())
        {
            array.Add(ConvertToJsonValue(value));
        }
        return array;
    }

    private static JsonObject ConvertToJsonObject(MongoAggregationForHumansParser.ObjectContext context)
    {
        var pairs = new JsonObject();
        foreach (var pair in context.pair())
        {
            pairs.Add(ConvertToJsonPair(pair));
        }
        return pairs;
    }

    private static KeyValuePair<string, JsonNode?> ConvertToJsonPair(MongoAggregationForHumansParser.PairContext context)
    {
        var name = context.object_name().GetText().Trim('"');
        var value = ConvertToJsonValue(context.value());
        return new(name, value);
    }


}
