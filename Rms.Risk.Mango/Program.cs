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
using Blazored.Modal;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Rms.Risk.Mango.Interfaces;
using Rms.Risk.Mango.Pivot.Core.MongoDb;
using Rms.Risk.Mango.Pivot.UI.Services;
using Rms.Risk.Mango.Services;
using Rms.Risk.Mango.Services.Context;
using Rms.Risk.Mango.Services.Logging;
using Rms.Risk.Mango.Services.Security;
using Rms.Service.Bootstrap;
using Rms.Service.Bootstrap.Health;
using Rms.Service.Bootstrap.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

[assembly: UserSecretsId("BF0D1460-2D1E-40CC-8F10-127369F684FE")]
[assembly: InternalsVisibleTo("Tests.Rms.Risk.Mango")]

namespace Rms.Risk.Mango;

public class Program
{
    private static ILogger<Program>? _logger;

    public static void Main(string[] args)
    {
        TaskScheduler.UnobservedTaskException += ( _, a ) => 
        {
            //HACK: Because of a BUG we ignore all unhandled task exceptions
            a.SetObserved();
            var m = $"Unobserved task exception ignored: {a.Exception.InnerException?.Message ?? a.Exception.Message}";
            _logger?.LogError(a.Exception, m);
        };

        AppDomain.CurrentDomain.UnhandledException += ( _, a ) =>
        {
            var m = $"Unexpected execution error IsTerminating={a.IsTerminating} ExceptionObject=\"{a.ExceptionObject}\"";
            _logger?.LogError(a.ExceptionObject as Exception, m);
        };

        // Load assembly dynamically if it exists
        var plugin = PluginSupport.GetPlugin(_logger);

        var builder = WebApplication.CreateBuilder(args);

        var options = new ServiceBootstrapOptions<Program>
        {
            EnableGrpc          = false,
            EnableOidc          = false,
            EnableMTLS          = false,
            AuthorizeBy         = AuthorizationType.Skip,
            EnableOpenIdConnect = true,
        };

        builder.ConfigureStandardEndpoint(options);
        builder.Services
            .AddLog4NetRedirection();

        MongoDbHelper.RemoteCertificateCheck = (_, s,c,e) => CertificateHelper.CheckClientCertificateChain((X509Certificate2?)s,c,e);

        IdentityModelEventSource.ShowPII = true; // show strings in error messages

        builder.Services
           .ConfigureProtected<DatabasesConfig>(builder.Configuration.GetSection("DatabasesConfig"))
           .ConfigureProtected<DbMangoSettings>(builder.Configuration.GetSection("DbMangoSettings"))
            ;

        // Add services to the container.
        
        builder.Services
           .AddRazorComponents()
           .AddInteractiveServerComponents();

        builder.Services
           .AddSignalR(x => x.MaximumReceiveMessageSize = 100_000_000);

        builder.Services
               .TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddBlazoredModal();

        if ( plugin != null )
        {
            builder.Services.AddSingleton(plugin);
        }
        else
        {
            builder.Services
                .AddSingleton<IChangeNumberChecker, NoChangeNumberChecker>()
                ;
        }

        builder.Services
           .AddMongoDbAccess()
           .AddSingleton<IMenuService                 , MenuService>()
           .AddSingleton<IMigrationEngine             , MigrationEngine>()
           .AddSingleton<IMongoDbServiceFactory       , MongoDbServiceFactory>()
           .AddSingleton<ISingleUseTokenService       , SingleUseTokenService>()
           .AddSingleton<ITempFileStorage             , TempFileStorage>()
           .AddSingleton<IPivotSharingService         , PivotSharingService>()
           .AddSingleton<IDatabaseConfigurationService, DatabaseConfigurationService>()
           .AddSingleton<IDocumentationService        , DocumentationService>()
           .AddSingleton<ICommandListService          , CommandListService>()
           .AddSingleton<IMongoCollectionStatsService , MongoCollectionStatsService>()
           .AddScoped<IUserService                    , UserServiceProxy>()
           .AddScoped<IUserSession                    , UserSession>()
           .AddScoped<IConnectedUser                  , ConnectedUser>()
           .AddScoped<IPatchService                   , PatchService>()
           .AddSingleton<IConnectedUserList           , ConnectedUserList>()
            ;

        plugin?.ConfigureServices(builder);

        // Required for split-repo/project-reference static web assets in development.
        // In published/container runs this can interfere with publish-time static asset resolution.
        if (builder.Environment.IsDevelopment())
            builder.WebHost.UseStaticWebAssets();

        builder.WebHost
               .UseKestrel((_, kestrelServerOptions) => { kestrelServerOptions.ConfigureStandardKestrel(builder, options); });

        AfhHelpers.Init();

        var app = builder.Build();
        
        _logger = app.Services.GetService<ILogger<Program>>();
        var settings = app.Services.GetService<IOptions<DbMangoSettings>>();
        if (settings?.Value.DefaultTimeout != null )
            DatabaseConfigurationService.DefaultTimeout = settings.Value.DefaultTimeout.Value;

        // Configure the HTTP request pipeline.

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        else
        {
            app.UseDeveloperExceptionPage();
        }

        if ( builder.IsHttps() )
            app.UseHttpsRedirection();

        app.UseStandardEndpoint(options);
        app.UseAntiforgery();

        //if (!app.Environment.IsDevelopment())
        //    app.UseStatusCodePagesWithRedirects("/StatusCode/{0}");

        app.UseStaticFiles();
        //app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // ----------------------------------------------- run the server ---------------------------------------------

        StartupHealthCheck.StartupCompleted = true;

        app.Run();
    }
}