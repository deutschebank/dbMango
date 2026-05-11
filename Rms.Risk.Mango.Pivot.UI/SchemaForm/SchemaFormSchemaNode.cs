using MongoDB.Bson;

namespace Rms.Risk.Mango.Pivot.UI.SchemaForm;

public sealed class SchemaFormSchemaNode
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = "$";
    public BsonDocument Schema { get; init; } = [];
    public HashSet<string> Types { get; init; } = [];
    public bool IsRequired { get; init; }
    public Dictionary<string, SchemaFormSchemaNode> Properties { get; init; } = new(StringComparer.Ordinal);
    public SchemaFormSchemaNode? Items { get; init; }

    public bool IsArray => Types.Contains("array");
    public bool IsObject => Types.Contains("object");
    public bool IsComplex => IsArray || IsObject;
}
