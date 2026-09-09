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
using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace Rms.Risk.Mango.Pivot.UI.SchemaForm;

public static class SchemaFormValidator
{
    public static List<SchemaFormValidationError> Validate(BsonDocument schema, BsonDocument document)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(document);

        var resolvedSchema = SchemaFormSchemaParser.ResolveSchema(schema, schema);
        var errors = new List<SchemaFormValidationError>();
        ValidateValue(resolvedSchema, document, "$", errors);
        return errors;
    }

    private static void ValidateValue(BsonDocument schema, BsonValue? value, string path, List<SchemaFormValidationError> errors)
    {
        var types = SchemaFormSchemaParser.GetTypes(schema);

        if (value is null || value.IsBsonNull)
        {
            if (!types.Contains("null") && types.Count > 0)
            {
                errors.Add(new(path, "Value cannot be null."));
            }
            return;
        }

        var actualType = GetJsonType(value);
        if (types.Count > 0 && !types.Contains(actualType))
        {
            errors.Add(new(path, $"Expected type '{string.Join("|", types)}' but got '{actualType}'."));
            return;
        }

        if (schema.TryGetValue("const", out var constValue) && !constValue.Equals(value))
        {
            errors.Add(new(path, "Value must match const."));
        }

        if (schema.TryGetValue("enum", out var enumValue) && enumValue is BsonArray enumArray)
        {
            if (!enumArray.Any(x => x.Equals(value)))
            {
                errors.Add(new(path, "Value must be one of enum values."));
            }
        }

        if (actualType is "integer" or "number")
        {
            var n = value.ToDouble();
            if (schema.TryGetValue("minimum", out var minVal) && minVal.IsNumeric && n < minVal.ToDouble())
            {
                errors.Add(new(path, $"Value must be >= {minVal}."));
            }

            if (schema.TryGetValue("maximum", out var maxVal) && maxVal.IsNumeric && n > maxVal.ToDouble())
            {
                errors.Add(new(path, $"Value must be <= {maxVal}."));
            }
        }

        if (actualType == "string")
        {
            var str = value.ToString() ?? "";

            if (schema.TryGetValue("minLength", out var minLength) && minLength.IsNumeric && str.Length < minLength.ToInt32())
            {
                errors.Add(new(path, $"String length must be >= {minLength}."));
            }

            if (schema.TryGetValue("maxLength", out var maxLength) && maxLength.IsNumeric && str.Length > maxLength.ToInt32())
            {
                errors.Add(new(path, $"String length must be <= {maxLength}."));
            }

            if (schema.TryGetValue("pattern", out var patternValue) && patternValue.IsString)
            {
                try
                {
                    if (!Regex.IsMatch(str, patternValue.AsString))
                    {
                        errors.Add(new(path, "String does not match expected pattern."));
                    }
                }
                catch
                {
                    // ignore invalid pattern in schema
                }
            }
        }

        if (actualType == "array")
        {
            var arr = value.AsBsonArray;

            if (schema.TryGetValue("minItems", out var minItems) && minItems.IsNumeric && arr.Count < minItems.ToInt32())
            {
                errors.Add(new(path, $"Array size must be >= {minItems}."));
            }

            if (schema.TryGetValue("maxItems", out var maxItems) && maxItems.IsNumeric && arr.Count > maxItems.ToInt32())
            {
                errors.Add(new(path, $"Array size must be <= {maxItems}."));
            }

            if (schema.TryGetValue("uniqueItems", out var uniqueItems) && uniqueItems.IsBoolean && uniqueItems.AsBoolean)
            {
                var hs = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in arr)
                {
                    hs.Add(item.ToJson());
                }

                if (hs.Count != arr.Count)
                {
                    errors.Add(new(path, "Array values must be unique."));
                }
            }

            if (schema.TryGetValue("items", out var itemsValue) && itemsValue is BsonDocument itemSchema)
            {
                for (var i = 0; i < arr.Count; i++)
                {
                    ValidateValue(itemSchema, arr[i], $"{path}[{i}]", errors);
                }
            }
        }

        if (actualType == "object")
        {
            var doc = value.AsBsonDocument;

            if (schema.TryGetValue("minProperties", out var minProperties) && minProperties.IsNumeric && doc.ElementCount < minProperties.ToInt32())
            {
                errors.Add(new(path, $"Object properties count must be >= {minProperties}."));
            }

            if (schema.TryGetValue("maxProperties", out var maxProperties) && maxProperties.IsNumeric && doc.ElementCount > maxProperties.ToInt32())
            {
                errors.Add(new(path, $"Object properties count must be <= {maxProperties}."));
            }

            if (schema.TryGetValue("required", out var requiredValue) && requiredValue is BsonArray required)
            {
                foreach (var requiredName in required.Where(x => x.IsString).Select(x => x.AsString))
                {
                    if (!doc.Contains(requiredName))
                    {
                        var requiredPath = path == "$" ? $"$.{requiredName}" : $"{path}.{requiredName}";
                        errors.Add(new(requiredPath, "Field is required."));
                    }
                }
            }

            if (schema.TryGetValue("properties", out var propertiesValue) && propertiesValue is BsonDocument properties)
            {
                foreach (var property in properties)
                {
                    if (property.Value is not BsonDocument propertySchema)
                    {
                        continue;
                    }

                    if (!doc.TryGetValue(property.Name, out var child))
                    {
                        continue;
                    }

                    var childPath = path == "$" ? $"$.{property.Name}" : $"{path}.{property.Name}";
                    ValidateValue(propertySchema, child, childPath, errors);
                }
            }
        }
    }

    public static string GetJsonType(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.Null => "null",
            BsonType.Boolean => "boolean",
            BsonType.Int32 => "integer",
            BsonType.Int64 => "integer",
            BsonType.Double => "number",
            BsonType.Decimal128 => "number",
            BsonType.Array => "array",
            BsonType.Document => "object",
            BsonType.DateTime => "string",
            BsonType.ObjectId => "string",
            _ => "string"
        };
    }
}
