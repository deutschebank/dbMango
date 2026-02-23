using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Rms.Risk.Mango.Services;

public class PatchRecord
{
    [BsonId]
    public BsonObjectId      Id               { get; set; } = new(ObjectId.GenerateNewId());
    public string            Name             { get; set; } = "";
    public string            Comments         { get; set; } = "";
    public bool              Active           { get; set; }
    public string            Timeout          { get; set; } = "20";
    public bool              OnlyCRUDCommands  { get; set; }
    public List<DatabaseRec> DatabasesToApply { get; set; } = [];
    public List<StageRec>    Patch            { get; set; } = [new()];
}

public class DatabaseRec
{
    public string DatabaseName { get; set; } = "";
    public string InstanceName { get; set; } = "";
    public bool   IsSelected   { get; set; }

    public DatabaseRec Clone() => new() 
    { 
        DatabaseName = DatabaseName, 
        InstanceName = InstanceName, 
        IsSelected   = IsSelected 
    };
}

public class StageRec
{
    public string Text { get; set; } = "{\n}";
    public bool   Use  { get; set; } = true;
}

public interface IPatchService
{
    Task<List<PatchRecord>> LoadPatches(bool              activeOnly);
    Task                    SavePatch(PatchRecord         rec);
    Task                    DeletePatch(PatchRecord       rec);
    Task<List<DatabaseRec>> LoadDatabasesList(string      accessLevel);
    void                    CheckCRUDCommands(PatchRecord rec);
    bool                    IsCRUDCommand(BsonDocument    bson);

    bool IsCRUDCommand(string json)
    {
        var bson    = BsonDocument.Parse( json );
        return IsCRUDCommand(bson);
    }

    bool IsCRUDPatch(PatchRecord rec)
    {
        if ( !rec.OnlyCRUDCommands )
            return false;

        foreach (var stage in rec.Patch)
        {
            if (!stage.Use)
                continue;
            if ( !IsCRUDCommand(stage.Text) )
                return false;
        }

        return true;
    }
}