using Blazored.Modal.Services;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using Rms.Risk.Mango.Interfaces;
using Rms.Risk.Mango.Pages.Admin;
using Rms.Risk.Mango.Pivot.UI.Controls;
using Rms.Risk.Mango.Services;
using Rms.Risk.Mango.Services.Security;
using System.Collections;
using System.Dynamic;
using Rms.Risk.Mango.Pivot.UI.Pivot;

namespace Rms.Risk.Mango.Pages.User;

public class CellClickHelper(
    IAuthorizationService         auth,
    ISchemaLoader                 schemaLoader,
    IUserSession                  userSession,
    IModalService                 modal,
    Func<Func<Task>, Task>        invokeAsync,
    Func<BsonValue, string, Task> showError
    )
{
    public string Timeout { get; set; } = "20";

    public async Task<bool> OnCellClick(string collection, List<BsonDocument>  resultBson, DynamicObject row, string columnName)
    {
        try
        {
            BsonDocument bson;
            var enableWrite = false;
            BsonDocument? schema = null;
            BsonValue? oldId = null;

            var id = TableControl.GetDynamicMember(row, "_id");
            var r = id == null ? null : resultBson.FirstOrDefault(x => x["_id"].ToString() == id.ToString());
            var title = ToBsonValue(id).ToString() ?? "Document";

            if (r == null)
            {
                if (row is not PivotRow pivotRow)
                    return true;

                bson = PivotRowToBson(pivotRow);
            }
            else
            {
                bson = r;
                oldId = bson["_id"];

                var authRes = await auth.AuthorizeAsync(
                    userSession.User.GetUser(),
                    userSession.Database,
                    [new WriteAccessRequirement()]);

                enableWrite = authRes.Succeeded;

                if (enableWrite)
                {
                    try
                    {
                        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        schema = await schemaLoader.LoadSchema(userSession.Collection, cts.Token);
                    }
                    catch
                    {
                        schema = null;
                    }
                }

            }

            var res = await Find.ShowBsonDialog(modal, title, bson, schema, enableWrite);
            if (res == null)
                return true;

            if (enableWrite && oldId != null)
            {
                if (res.Value.ShouldUpdate)
                {
                    bson = BsonDocument.Parse(res.Value.Json);
                    await OnUpdate(collection, oldId, bson);
                }
                else
                    await OnDelete(collection, oldId);
            }
        }
        catch (Exception e)
        {
            await ModalDialogUtils.ShowExceptionDialog(modal, "Error", e);
        }
        return true;
    }

    private static BsonDocument PivotRowToBson(PivotRow pivotRow)
    {
        var bson = new BsonDocument();

        foreach (var (header, col) in pivotRow.PivotData.Headers.Select((h, i) => (h, i)))
        {
            var value = pivotRow.PivotData.Get(col, pivotRow.Row);
            bson[header] = ToBsonValue(value);
        }

        return bson;
    }

    private static BsonValue ToBsonValue(object? value)
    {
        if (value == null)
            return BsonNull.Value;

        if (value is BsonValue bsonValue)
            return bsonValue;

        if (value is IDictionary dictionary)
        {
            var doc = new BsonDocument();
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key.ToString();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                doc[key] = ToBsonValue(entry.Value);
            }
            return doc;
        }

        if (value is IEnumerable enumerable and not string)
        {
            var arr = new BsonArray();
            foreach (var item in enumerable)
            {
                arr.Add(ToBsonValue(item));
            }
            return arr;
        }

        return BsonValue.Create(value);
    }

    private async Task OnDelete(string collection, BsonValue id)
    {
        var ticket = await Shell.CanExecuteCommand(userSession, invokeAsync, modal);
        if (string.IsNullOrWhiteSpace(ticket))
            return;

        var r = await ModalDialogUtils.ShowConfirmationDialog(modal, $"Delete {id}", "Are you sure to delete document?");
        if (r.Cancelled)
            return;

        var command = BsonDocument.Parse($@"{{
     ""delete"": ""{collection}"",
     ""deletes"": [{{
        ""q"": {{  }},
        ""limit"": 1
     }}]
}}");

        var idDoc = new BsonDocument
        {
            ["_id"] = id
        };

        command["deletes"][0]["q"] = idDoc;

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(int.Parse(Timeout)));
        Shell.UpdateComment(command, ticket, userSession.User.GetEmail());

        var res = await userSession.MongoDbAdmin.RunCommand(command, cts.Token);
        var deleted = res["n"].ToInt32();
        if (deleted != 1)
        {
            await showError( id,
                res.ToJson(new() { Indent = true }) +
                "\n------------------------------------------------------------------\n" +
                command.ToJson(new() { Indent = true })
                );
        }
        else
            await ModalDialogUtils.ShowInfoDialog(modal, $"Delete {id}", "Success");
    }

    private async Task OnUpdate(string collection, BsonValue id, BsonDocument newBson)
    {
        var ticket = await Shell.CanExecuteCommand(userSession, invokeAsync, modal);
        if (string.IsNullOrWhiteSpace(ticket))
            return;

        var r = await ModalDialogUtils.ShowConfirmationDialog(modal, $"Update {id}", "Are you sure to update document?");
        if (r.Cancelled)
            return;

        var command = BsonDocument.Parse($@"{{
    update: ""{collection}"",
    updates: [
       {{
         q: {{ }},
         u: {{ }},
         upsert: false,
         multi: false,
       }}
    ],
    ordered: false,
    bypassDocumentValidation: false
}}");

        var idDoc = new BsonDocument
        {
            ["_id"] = id
        };

        command["updates"][0]["q"] = idDoc;
        command["updates"][0]["u"] = newBson;

        Shell.UpdateComment(command, ticket, userSession.User.GetEmail());

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(int.Parse(Timeout)));
        var res = await userSession.MongoDbAdmin.RunCommand(command, cts.Token);

        var modified = res["nModified"].ToInt32();
        if (modified != 1)
        {
            await showError(id,
                res.ToJson(new() { Indent = true }) +
                "\n------------------------------------------------------------------\n" +
                command.ToJson(new() { Indent = true })
                );

        }
        else
            await ModalDialogUtils.ShowInfoDialog(modal, $"Update {id}", "Success");
    }

}

