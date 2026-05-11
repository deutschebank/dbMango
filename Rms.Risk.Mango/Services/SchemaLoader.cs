using MongoDB.Bson;
using Rms.Risk.Mango.Interfaces;

namespace Rms.Risk.Mango.Services;

public class SchemaLoader(
    // ReSharper disable InconsistentNaming
    IUserSession _userSession,
    ILogger<SchemaLoader> _logger
    // ReSharper restore InconsistentNaming
    ) : ISchemaLoader
{
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromHours(2);

    private static readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private static readonly Lock _cacheLock = new();

    private sealed record CacheEntry(BsonDocument Schema, DateTimeOffset ExpiresAt);

    public async Task<BsonDocument?> LoadSchema(string collection, CancellationToken token = default)
    {
        using var timeoutCts = token == CancellationToken.None
                ? new CancellationTokenSource(TimeSpan.FromSeconds(30))
                : null
            ;

        var effectiveToken = timeoutCts?.Token ?? token;

        var cacheKey = GetCacheKey();
        var now = DateTimeOffset.UtcNow;

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(cacheKey, out var cacheEntry))
            {
                if (cacheEntry.ExpiresAt > now)
                    _cache.Remove(cacheKey);
                else
                    return cacheEntry.Schema;
            }
        }

        try
        {
            _userSession.Collection = collection; // Ensure the collection is set for schema inference
            var schema = await JsonSchemaExtractor.InferJsonSchema(
                _userSession.MongoDb,
                token: effectiveToken);

            lock (_cacheLock)
            {
                _cache[cacheKey] = new CacheEntry(schema, DateTimeOffset.UtcNow.Add(_cacheTtl));
            }

            return schema;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log the error and return null if schema inference fails
            _logger.LogError(ex, "Failed to load schema for collection '{collectionName}': {message}", _userSession.Collection, ex.Message);
            return null;
        }
    }

    private string GetCacheKey() =>
        string.Join("|", _userSession.Database, _userSession.DatabaseInstance, _userSession.Collection);
}

