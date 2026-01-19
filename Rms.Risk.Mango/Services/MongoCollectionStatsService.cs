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

using Rms.Risk.Mango.Pivot.Core.Models;
using Rms.Risk.Mango.Pivot.Core.MongoDb;
using Rms.Risk.Mango.Interfaces;

namespace Rms.Risk.Mango.Services;

public class MongoCollectionStatsService : IMongoCollectionStatsService
{
    private record Args(IMongoDbDatabaseAdminService Admin, Func<CollectionStats, CancellationToken, Task> Callback);

    // databaseName -> ( collectionName -> stats )
    private static ExpiringObjectPool<string, Dictionary<string, CollectionStats>, Args>? _collectionsCache;

    public MongoCollectionStatsService()
    {
        _collectionsCache ??= new(LoadCollStats, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(1));
    }

    public async Task<Dictionary<string, CollectionStats>> LoadCollStats(IUserSession user, Func<CollectionStats, CancellationToken, Task> callback, CancellationToken token)
    {
        var args = new Args(
            Admin: user.MongoDbAdmin,
            Callback: callback
        );
        
        await Task.Delay(1, token);
        return await _collectionsCache!.Get($"{user.Database}/{user.DatabaseInstance}", args, token);
    }

    private static async Task<Dictionary<string, CollectionStats>> LoadCollStats(string database, Args args, CancellationToken token)
    {
        var collections = await args.Admin.ListCollections(token); // make a copy to avoid modifying the original list during iteration

        var stats = new Dictionary<string, CollectionStats>();
        foreach (var name in collections)
        {
            try
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token, token);

                var doc = await args.Admin.CollStats(name, cts.Token);
                doc.Details = null; // Remove details to avoid large output

                stats[name] = doc;
                await args.Callback(doc, cts.Token);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        return stats;
    }
}