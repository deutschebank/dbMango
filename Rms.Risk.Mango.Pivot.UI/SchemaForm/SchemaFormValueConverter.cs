using MongoDB.Bson;

namespace Rms.Risk.Mango.Pivot.UI.SchemaForm;

public static class SchemaFormValueConverter
{
    public static bool IsObjectIdSchema(BsonDocument schema, string fieldName, BsonValue? currentValue)
    {
        if (currentValue is BsonObjectId)
        {
            return true;
        }

        if (schema.TryGetValue("bsonType", out var bsonType) && bsonType.IsString && string.Equals(bsonType.AsString, "objectId", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (fieldName == "_id" && schema.TryGetValue("type", out var typeValue) && typeValue.IsString && typeValue.AsString == "string")
        {
            return true;
        }

        return false;
    }

    public static BsonValue? ParseScalarValue(BsonDocument schema, string fieldName, string? value, BsonValue? currentValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (IsObjectIdSchema(schema, fieldName, currentValue))
        {
            if (ObjectId.TryParse(normalized, out var objectId))
            {
                return objectId;
            }

            return currentValue;
        }

        var types = SchemaFormSchemaParser.GetTypes(schema);

        if (types.Contains("boolean"))
        {
            if (bool.TryParse(normalized, out var b))
            {
                return b;
            }

            return currentValue;
        }

        if (types.Contains("integer"))
        {
            if (long.TryParse(normalized, out var n))
            {
                return n;
            }

            return currentValue;
        }

        if (types.Contains("number"))
        {
            if (double.TryParse(normalized, out var n))
            {
                return n;
            }

            return currentValue;
        }

        if (types.Contains("null") && string.Equals(normalized, "null", StringComparison.OrdinalIgnoreCase))
        {
            return BsonNull.Value;
        }

        return normalized;
    }

    public static string ToDisplayString(BsonValue? value)
    {
        if (value is null || value.IsBsonNull)
        {
            return string.Empty;
        }

        return value.BsonType switch
        {
            BsonType.String => value.AsString,
            BsonType.ObjectId => value.AsObjectId.ToString(),
            BsonType.Boolean => value.AsBoolean ? "true" : "false",
            BsonType.DateTime => value.ToUniversalTime().ToString("O"),
            _ => value.ToString() ?? string.Empty
        };
    }
}
