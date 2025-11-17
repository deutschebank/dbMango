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
﻿using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Rms.Service.Bootstrap.Health;
using Rms.Service.Bootstrap.Logging;
using Rms.Service.Bootstrap.Metrics;
using Rms.Service.Bootstrap.Security;
using System.Reflection;

namespace Rms.Service.Bootstrap;

/// <summary>
/// Easy service initialization. Applies most of UseXX and AddXXX calls.
/// </summary>
public static class ServiceBootstrap
{
    /// <summary>
    /// Configure standard endpoint. This enables Swagger, OpenTelemetry, gRPC, web endpoints etc.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    public static WebApplicationBuilder ConfigureStandardEndpoint<T>(this WebApplicationBuilder builder) where T : class
        => builder.ConfigureStandardEndpoint<T>(new());

    /// <summary>
    /// Configure standard endpoint. This enables Swagger, OpenTelemetry, gRPC, web endpoints etc.
    /// </summary>
    public static WebApplicationBuilder ConfigureStandardEndpoint<T>(this WebApplicationBuilder builder, ServiceBootstrapOptions<T> options) where T : class
    {
        var apiVersion  = options.ApiVersion;
        var asm         = typeof(T).Assembly;
        var serviceName = Path.GetFileNameWithoutExtension(asm.Location);
        var settings    = GetSecuritySettings<T>();

        builder.AddStandardOptions( asm );

        // reset mTLS flag if none of the service URLs are HTTPs
        if ( !builder.IsHttps() )
            options.EnableMTLS = false;

        builder
           .ConfigureStandardHealthChecks(options)
           .ConfigureStandardLogging()
           ;

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

        // Add services to the container.
        if ( options.EnableGrpc )
        {
            var grpc = builder.Services.AddGrpc(); // <-- use this if you want gRPC
            if ( options.EnableGrpcTranscoding )
                grpc.AddJsonTranscoding(); // <-- add this for JSON API to gRPC translation layer (expose gRPC via REST)
        }

        builder.Services
               .AddHttpClient()
               .AddMvc(o => // <-- this is to use REST API via Controllers
                {
                    
                    if ( options.EnableOpenIdConnect )
                    {
                        // The RequireAuthenticatedUser policy locks down the entire site by default. 
                        // this disables metrics, ping, swagger and ability to use AllowAnonymous
//                        var policy = new AuthorizationPolicyBuilder()
//                                    .RequireAuthenticatedUser()
//                                    .Build();
//                        o.Filters.Add(new AuthorizeFilter(policy));
                    }
                    else
                    {
                        o.EnableEndpointRouting = false;
                    }
                }); 

        // add support for OpenAPI
        builder.Services
               .AddGrpcSwagger()
               .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc(apiVersion,
                                 new()
                                 {
                                     Title   = $"{serviceName} gRPC transcoding",
                                     Version = apiVersion
                                 });

                    var filePath = Path.Combine(AppContext.BaseDirectory, $"{serviceName}.xml");
                    c.IncludeXmlComments(filePath);
                    c.IncludeGrpcXmlComments(filePath, includeControllerXmlComments: true);
                });

        // support for OpenTelemetry
        builder.ConfigureStandardMetrics(options);

        // 
        builder.Services
               .AddStandardSecurity(settings, options);

        return builder;
    }

    internal static IOptions<SecuritySettings> GetSecuritySettings<T>() where T : class
    {
        var tempBuilder = WebApplication.CreateBuilder();
        tempBuilder.AddStandardOptions( typeof(T).Assembly );
        tempBuilder.Services
                   .AddSingleton<IPasswordManager, SimplePasswordManager>()
            ;
        var tempServiceProvider = tempBuilder.Build();
        var settings            = tempServiceProvider.Services.GetRequiredService<IOptions<SecuritySettings>>();
        return settings;
    }

    private static IHostApplicationBuilder AddStandardOptions( this IHostApplicationBuilder builder, Assembly asm )
    {
        var env         = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development;
        var serviceName = Path.GetFileNameWithoutExtension(asm.Location);

        builder.Configuration.Sources.Clear();
        var cfg = builder.Configuration
               .SetBasePath(AppContext.BaseDirectory)
               ;

        var settingsLocation = Environment.GetEnvironmentVariable("SETTINGS_LOCATION") ?? "";

        if ( !string.IsNullOrWhiteSpace(settingsLocation) && Directory.Exists(settingsLocation) )
        {
            cfg
                .AddJsonFile(Path.Combine(settingsLocation, $"{serviceName}.Settings.json"),       optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine(settingsLocation, $"{serviceName}.Settings.{env}.json"), optional: true, reloadOnChange: true)
                ;
        }
        else
        {
            cfg
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, $"{serviceName}.Settings.json"),       optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, $"{serviceName}.Settings.{env}.json"), optional: true, reloadOnChange: true)
                ;
        }
        cfg
            .AddUserSecrets(asm, true)
            .AddEnvironmentVariables()
            ;


        // https://stackoverflow.com/questions/74346677/using-options-pattern-for-settings-net-core-app-not-working-settings-file-alwa
        builder.Services
               .ConfigureProtected<SecuritySettings>(builder.Configuration.GetSection("SecuritySettings"))
               .Configure<HealthCheckServiceOptions>(builder.Configuration.GetSection("HealthCheckServiceOptions"))
               .AddOptions()
            ;
        return builder;
    }

    /// <summary>
    /// Configure Kestrel to use server certificate etc.
    /// </summary>
    /// <param name="kestrelServerOptions"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static KestrelServerOptions ConfigureStandardKestrel<T>(
        this KestrelServerOptions kestrelServerOptions,
        WebApplicationBuilder     builder
    ) where T : class
        => ConfigureStandardKestrel<T>(kestrelServerOptions, builder, new());

    /// <summary>
    /// Check if service URLs contain at least one HTTPs URL
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static bool IsHttps(this WebApplicationBuilder builder)
    {
        var u = builder.Configuration.GetSection("ASPNETCORE_URLS").Get<string>();
        
        var isHttps= (u ?? builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey) ?? string.Empty)
                  .Split(";", StringSplitOptions.RemoveEmptyEntries)
                  .Any(x => x.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                  ;
        return isHttps;
    }

    /// <summary>
    /// Configure Kestrel to use server certificate etc.
    /// </summary>
    /// <param name="kestrelServerOptions"></param>
    /// <param name="builder"></param>
    /// <param name="bootstrapOptions"></param>
    /// <returns></returns>
    public static KestrelServerOptions ConfigureStandardKestrel<T>(
        this KestrelServerOptions kestrelServerOptions, 
        WebApplicationBuilder builder, 
        ServiceBootstrapOptions<T> bootstrapOptions
        ) where T : class
    {

        if (builder.IsHttps())
        {
            kestrelServerOptions
               .ConfigureServerCertificate<T>(builder)
               .ConfigureHttpsDefaults(o =>
                {
                    builder.ConfigureStandardHttpsDefaults(o, bootstrapOptions.EnableMTLS);
                });
        }
        return kestrelServerOptions;
    }

    /// <summary>
    /// Initializes standard endpoint. This enables Swagger, OpenTelemetry, gRPC, web endpoints etc.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseStandardEndpoint<T>(this WebApplication app) where T : class
        => app.UseStandardEndpoint<T>(new());

    /// <summary>
    /// Initializes standard endpoint. This enables Swagger, OpenTelemetry, gRPC, web endpoints etc.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static WebApplication UseStandardEndpoint<T>(this WebApplication app, ServiceBootstrapOptions<T> options) where T : class
    {
        Logging.Logging.LoggerFactory = app.Services.GetService<ILoggerFactory>()!;

        var serviceName = Path.GetFileNameWithoutExtension(typeof(T).Assembly.Location);
        var apiVersion  = options.ApiVersion;

        app
           .UseRouting()
           ;
        //app.UseCors();
        app
           .UseStandardSecurity(options);

        app
           .UseSwagger()
           .UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", $"{serviceName} API {apiVersion}");
            })

           .UseOpenTelemetryPrometheusScrapingEndpoint()
           ;

//        app.MapControllerRoute(
//            "default",
//            "{controller}/{action}");
//
        app.MapControllers();

        app.MapStandardHealthChecks(options);

        return app;
    }
}