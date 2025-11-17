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
﻿using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Rms.Service.Bootstrap.Health;

internal static class HealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = JsonSerializer.Serialize(new
        {
            status = healthReport.Status.ToString(),
            details = healthReport.Entries.Select(e => new
            {
                key         = e.Key,
                description = e.Value.Description,
                status      = e.Value.Status.ToString(),
                data        = e.Value.Data
            })
        }, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        return context.Response.WriteAsync(result);
    }
}