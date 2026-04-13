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
﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Rms.Service.Bootstrap.Security;

internal static class SecurityExtensions
{
    public static IServiceCollection AddStandardSecurity<T>(this IServiceCollection services, IOptions<SecuritySettings> settings, ServiceBootstrapOptions<T> options)
        where T : class
    {
        // For API services: authentication via mTLS and Oidc JWT tokens (both supported)
        // For Web applications: Oidc code authentication

        services
           .AddSingleton<IPasswordManager, SimplePasswordManager>()
           ;

        if (!string.IsNullOrWhiteSpace(settings.Value.Ldap.Url))
        {
            services
               .AddSingleton<LdapChecker>( s => new (s.GetRequiredService<IOptions<SecuritySettings>>().Value.Ldap))
                ;
        }

        var authBuilder = services
           .AddAuthentication(cfg =>
            {
                if ( options.EnableOpenIdConnect )
                {
                    cfg.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    cfg.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
                    cfg.DefaultChallengeScheme    = OpenIdConnectDefaults.AuthenticationScheme;
                }
                else if ( options.EnableOidc )
                {
                    cfg.DefaultScheme          = JwtBearerDefaults.AuthenticationScheme;
                    cfg.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }
            });

        if ( options.EnableOidc )
        {
            //TokenHelper.ConfigureLocalJwtBearer(builder.Configuration, x) 
            authBuilder
               .AddJwtBearer(x => OidcHelper.ConfigureOidcJwtBearer(settings, x))
                ;
        }

        if ( options.EnableOpenIdConnect )
        {
            services
               .AddScoped<UserService>()
               .AddScoped<LogoutHandler>()
               .AddSingleton<IServerSideTokenStore, ServerSideTokenStore>()
                ;

            services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, UserCircuitHandler>());

            authBuilder
               .AddCookie( x => OidcHelper.ConfigureCookieForOpenIdConnect(settings, x))
               .AddOpenIdConnect(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    x => OidcHelper.ConfigureOpenIdConnect(settings, x)
                );
        }

        if ( options.EnableMTLS )
        {
            authBuilder
               .AddCertificate(x => CertificateHelper.ConfigureCertificateAuthentication(settings, x));
        }

        return services;
    }

    public static WebApplication UseStandardSecurity<T>(this WebApplication app, ServiceBootstrapOptions<T> options) where T : class
    {
        app
           .UseAuthentication()
           .UseAuthorization()
            ;

        if ( options.EnableOpenIdConnect )
            app.UseMiddleware<UserServiceMiddleware>(); // to provide UserService with tokens
        
        return app;
    }
}