using MongoDB.Bson;

namespace Rms.Risk.Mango.Services;

public interface ISchemaLoader
{
    Task<BsonDocument?> LoadSchema(string collection, CancellationToken token = default);
}

