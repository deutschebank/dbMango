using MongoDB.Bson;

namespace Rms.Risk.Mango.Pivot.UI.SchemaForm;

public static class SchemaFormSchemaParser
{
    public static BsonDocument ResolveSchema(BsonDocument rootSchema, BsonDocument schema)
    {
        ArgumentNullException.ThrowIfNull(rootSchema);
        ArgumentNullException.ThrowIfNull(schema);

        return ResolveSchemaInternal(rootSchema, schema, new HashSet<string>(StringComparer.Ordinal));
    }

    public static SchemaFormSchemaNode ParseRoot(BsonDocument schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var resolved = ResolveSchema(schema, schema);
        return Parse("$", "$", resolved, true);
    }

    public static HashSet<string> GetTypes(BsonDocument schema)
    {
        if (schema.TryGetValue("bsonType", out var bsonType))
        {
            return ParseTypeValue(bsonType);
        }

        if (schema.TryGetValue("type", out var type))
        {
            return ParseTypeValue(type);
        }

        return ["object"];
    }

    public static bool IsRequired(BsonDocument schema, string propertyName)
    {
        if (!schema.TryGetValue("required", out var requiredValue) || requiredValue is not BsonArray requiredArray)
        {
            return false;
        }

        return requiredArray.Any(x => x.IsString && x.AsString == propertyName);
    }

    private static SchemaFormSchemaNode Parse(string name, string path, BsonDocument schema, bool required)
    {
        var types = GetTypes(schema);
        var properties = new Dictionary<string, SchemaFormSchemaNode>(StringComparer.Ordinal);

        if (schema.TryGetValue("properties", out var propertiesValue) && propertiesValue is BsonDocument propertiesDoc)
        {
            foreach (var element in propertiesDoc)
            {
                if (element.Value is not BsonDocument propertySchema)
                {
                    continue;
                }

                var childPath = path == "$" ? $"$.{element.Name}" : $"{path}.{element.Name}";
                properties[element.Name] = Parse(element.Name, childPath, propertySchema, IsRequired(schema, element.Name));
            }
        }

        SchemaFormSchemaNode? items = null;
        if (schema.TryGetValue("items", out var itemsValue) && itemsValue is BsonDocument itemsSchema)
        {
            items = Parse("[]", $"{path}[*]", itemsSchema, false);
        }

        return new SchemaFormSchemaNode
        {
            Name = name,
            Path = path,
            Schema = schema,
            Types = types,
            IsRequired = required,
            Properties = properties,
            Items = items
        };
    }

    private static BsonDocument ResolveSchemaInternal(BsonDocument rootSchema, BsonDocument schema, HashSet<string> inProgressRefs)
    {
        var working = schema;

        if (schema.TryGetValue("$ref", out var refValue) && refValue.IsString)
        {
            var refPath = refValue.AsString;
            if (TryResolveRef(rootSchema, refPath, out var referenced))
            {
                if (inProgressRefs.Contains(refPath))
                {
                    return new BsonDocument
                    {
                        ["type"] = "object"
                    };
                }

                inProgressRefs.Add(refPath);
                var resolvedRef = ResolveSchemaInternal(rootSchema, referenced, inProgressRefs);
                inProgressRefs.Remove(refPath);

                var merged = resolvedRef.DeepClone().AsBsonDocument;
                foreach (var element in schema)
                {
                    if (element.Name == "$ref")
                    {
                        continue;
                    }

                    merged[element.Name] = element.Value;
                }

                working = merged;
            }
        }

        var result = new BsonDocument();
        foreach (var element in working)
        {
            if (element.Name == "properties" && element.Value is BsonDocument properties)
            {
                var resolvedProps = new BsonDocument();
                foreach (var prop in properties)
                {
                    resolvedProps[prop.Name] = prop.Value is BsonDocument propDoc
                        ? ResolveSchemaInternal(rootSchema, propDoc, inProgressRefs)
                        : prop.Value;
                }

                result[element.Name] = resolvedProps;
                continue;
            }

            if (element.Name is "items" or "contains" or "if" or "then" or "else" && element.Value is BsonDocument childDoc)
            {
                result[element.Name] = ResolveSchemaInternal(rootSchema, childDoc, inProgressRefs);
                continue;
            }

            if (element.Name is "allOf" or "anyOf" or "oneOf" && element.Value is BsonArray arr)
            {
                var resolvedArr = new BsonArray();
                foreach (var item in arr)
                {
                    resolvedArr.Add(item is BsonDocument itemDoc
                        ? ResolveSchemaInternal(rootSchema, itemDoc, inProgressRefs)
                        : item);
                }

                result[element.Name] = resolvedArr;
                continue;
            }

            result[element.Name] = element.Value;
        }

        return result;
    }

    private static bool TryResolveRef(BsonDocument rootSchema, string refPath, out BsonDocument schema)
    {
        schema = [];
        const string defsPrefix = "#/$defs/";
        if (!refPath.StartsWith(defsPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var defName = refPath[defsPrefix.Length..];
        if (!rootSchema.TryGetValue("$defs", out var defsValue) || defsValue is not BsonDocument defs)
        {
            return false;
        }

        if (!defs.TryGetValue(defName, out var defSchema) || defSchema is not BsonDocument defDoc)
        {
            return false;
        }

        schema = defDoc;
        return true;
    }

    private static HashSet<string> ParseTypeValue(BsonValue typeValue)
    {
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (typeValue.IsString)
        {
            types.Add(typeValue.AsString);
            return types;
        }

        if (typeValue is BsonArray typeArray)
        {
            foreach (var value in typeArray)
            {
                if (value.IsString)
                {
                    types.Add(value.AsString);
                }
            }
        }

        if (types.Count == 0)
        {
            types.Add("object");
        }

        return types;
    }
}
