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
﻿using MongoDB.Bson;
using Rms.Risk.Mango.Interfaces;

namespace Rms.Risk.Mango.Services.Audit;

public class ChainedAuditService(IReadOnlyCollection<IAuditService> _audit) : IAuditService
{
    public void PreCheck(BsonDocument command)
    {
        foreach (var auditService in _audit)
        {
            auditService.PreCheck(command);
        }
    }

    public async Task Record(AuditRecord rec, CancellationToken token = default)
    {
        if (MongoDbCommandHelper.IsReadOnlyCommand(rec.Command))
            return;

        foreach (var auditService in _audit)
        {
            await auditService.Record(rec, token);
        }
    }

    public Task<List<AuditRecord>> Audit(DateTime startDate, DateTime endDate, CancellationToken token = default)
        => _audit.First().Audit(startDate, endDate, token);
}