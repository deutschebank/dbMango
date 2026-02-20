using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Rms.Risk.Mango.Interfaces;
using Rms.Risk.Mango.Services.Context;

namespace Rms.Risk.Mango.Services;

// ReSharper disable InconsistentNaming
public class PatchService(IUserSession _userSession, IAuthorizationService _auth, IDatabaseConfigurationService _databaseConfigurationService) : IPatchService
// ReSharper restore InconsistentNaming
{
    private const string PatchCollectionName = "dbMango-Patches";

    public async Task<List<PatchRecord>> LoadPatches(bool activeOnly)
    {
        var patches = new List<PatchRecord>();

        var service = _userSession.GetCustomMongoDbService(_userSession.Database, _userSession.DatabaseInstance, PatchCollectionName);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var results = service.FindAsync("{}",  token: cts.Token);
        await foreach( var doc in results)
        {
            try
            {
                var patch = BsonSerializer.Deserialize<PatchRecord>(doc);
                if (activeOnly && !patch.Active)
                    continue;
                patches.Add(patch);
            }
            catch (Exception)
            {
                // ignore deserialization errors for individual documents
            }
        }

        return patches;
    }

    public async Task SavePatch(PatchRecord rec)
    {
        var service = _userSession.MongoDbAdmin;
        var cts     = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var json =
$@"{{
    update: ""{PatchCollectionName}"",
    updates: [
       {{
         q: {{ _id : {rec.Id.ToJson() } }},
         u: {rec.ToJson(new() { Indent = true } )},
         upsert: true,
         multi: false,
       }}
    ],
    maxTimeMS: 10000
}}";

        var doc = BsonDocument.Parse(json);

        _ = await service.RunCommand(doc, cts.Token);
    }

    public async Task DeletePatch(PatchRecord rec)
    {
            var service = _userSession.MongoDbAdmin;
            var cts     = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var json =
$@"{{
    delete: ""{PatchCollectionName}"",
    deletes: [
       {{
         q: {{ _id : {rec.Id.ToJson() } }},
         limit: 1
       }}
    ],
    maxTimeMS: 10000
}}";

            var doc = BsonDocument.Parse(json);

            _ = await service.RunCommand(doc, cts.Token);
    }

    public async Task<List<DatabaseRec>> LoadDatabasesList(string accessLevel)
    {
        var databases = new List<DatabaseRec>();

        foreach (var name in _databaseConfigurationService.Databases.Keys)
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                if (!await _userSession.CanAccess(_auth, accessLevel, name))
                    continue;

                var admin = _userSession.GetCustomAdmin(name, "admin");

                var reply = await admin.RunCommand(new ("listDatabases", 1), cts.Token);

                var dbs = reply["databases"].AsBsonArray.Select(x => x["name"].AsString).ToList();
                databases.AddRange(dbs.Select(db => new DatabaseRec() { DatabaseName = name, InstanceName = db, IsSelected = false }));
            }
            catch (Exception)
            {
                // ignore
            }
        }

        return databases;
    }

}