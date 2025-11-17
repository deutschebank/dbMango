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
﻿using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rms.Service.Bootstrap.Security;

namespace Rms.Service.Bootstrap.Health;

public static class HealthChecksExtensionMethods
{
    public static WebApplicationBuilder ConfigureStandardHealthChecks<T>(this WebApplicationBuilder builder, ServiceBootstrapOptions<T> options)
        where T : class
    {
        if (!options.EnableHealthChecks)
            return builder;

        var securitySection  = builder.Configuration.GetSection("SecuritySettings");
        var securitySettings = securitySection.Get<SecuritySettings>() ?? new();

        // Health checks
        var hcBuilder = builder.Services
                               .Configure<HealthCheckPublisherOptions>(builder.Configuration.GetSection("HealthCheckPublisher"))
                               .AddSingleton<StartupHealthCheck>()
                               .AddHealthChecks()
                               .AddCheck<StartupHealthCheck>(
                                    "Startup",
                                    tags: ["ready"])
            ;

        options.ConfigureHealthChecks?.Invoke(hcBuilder);

        return builder;
    }

    public static WebApplication MapStandardHealthChecks<T>(this WebApplication app, ServiceBootstrapOptions<T> options) where T : class
    {
        var hcOptionsReady = new HealthCheckOptions
        {
            Predicate             = healthCheck => healthCheck.Tags.Contains("ready"),
            AllowCachingResponses = false,
            ResponseWriter        = HealthCheckResponseWriter.WriteResponse
        };

        if ( options.HealthChecksWriter != null )
            hcOptionsReady.ResponseWriter = options.HealthChecksWriter;

        var hcOptionsLive = new HealthCheckOptions
        {
            Predicate             = _ => false,
            AllowCachingResponses = false,
            ResponseWriter        = HealthCheckResponseWriter.WriteResponse

        };
        if ( options.HealthChecksWriter != null )
            hcOptionsLive.ResponseWriter = options.HealthChecksWriter;

        app.MapHealthChecks("/healthz/ready", hcOptionsReady);
        app.MapHealthChecks("/healthz/live",  hcOptionsLive);

        return app;
    }
}