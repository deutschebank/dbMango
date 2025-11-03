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
﻿using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;
using System.Text;

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Static class for certificate handling helpers
/// </summary>
public static class CertificateHelper
{
    private static readonly ILogger _log = Logging.Logging.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType ?? typeof(int));

    private static List<(string Fingerprint, string CN)> _allowedRootCA = [];

    /// <summary>
    /// Accept only selected certificates. See <see cref="SecuritySettings"/> for the list.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="o"></param>
    /// <param name="enableMTLS"></param>
    public static void ConfigureStandardHttpsDefaults(
        this WebApplicationBuilder    builder, 
        HttpsConnectionAdapterOptions o, 
        bool enableMTLS
        )
    {

        if (_allowedRootCA.Count == 0)
            LoadAllowedRootCA(builder.Configuration);

        o.ClientCertificateMode = enableMTLS ? ClientCertificateMode.AllowCertificate : ClientCertificateMode.NoCertificate;
        o.ClientCertificateValidation = CheckClientCertificateChain;
    }

    private static void LoadAllowedRootCA(IConfigurationManager configuration)
    {
        var section  = configuration.GetSection("SecuritySettings");
        var settings = section.Get<SecuritySettings>() ?? new();

        var store = new X509Store(StoreName.Root);
        if (!store.IsOpen)
            store.Open(OpenFlags.ReadOnly);

        _allowedRootCA = store.Certificates
                              .Where(certificate => settings.CASubjectToAccept.Contains(certificate.Subject))
                              .Select(certificate => (Fingerprint: certificate.Thumbprint, CN: certificate.Subject))
                              .ToList();

        if (_allowedRootCA.Count == 0)
        {
            _log.LogWarning("None of the allowed CAs are on the the trusted list. All CAs are allowed!\n" +
                            $"\tAllowed CAs: {string.Join("; ", settings.CASubjectToAccept)}\n" +
                            $"\tTrusted CAs: Count={store.Certificates.Count}");
        }
        if (_allowedRootCA.Count != settings.CASubjectToAccept.Count)
        {
            _log.LogWarning("Allowed CA list contains some CAs that are not on the trusted list:\n" +
                            $"\tAllowed CAs: {string.Join("; ", settings.CASubjectToAccept)}\n" +
                            $"\tTrusted CAs: {string.Join("; ", _allowedRootCA.Select(x => x.CN))}");
        }
    }

    public static HttpClientHandler ConfigureHttpsHandler(IConfigurationManager configurationManager, string name)
    {
        if (_allowedRootCA.Count == 0)
            LoadAllowedRootCA(configurationManager);
        return ConfigureHttpsHandler(name);
    }

    public static HttpClientHandler ConfigureHttpsHandler(string name)
    {

        var handler = new HttpClientHandler
        {
            CheckCertificateRevocationList = false,
            AllowAutoRedirect              = false,
            ClientCertificateOptions       = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => CheckClientCertificateChain(cert, chain, errors)
            //ClientCertificates             = { ForgeCertificate.Certificate }
        };

        if(handler.SupportsAutomaticDecompression)
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

        return handler;
    }


    /// <summary>
    /// Setup server (own) certificate. See <see cref="SecuritySettings"/> for details.
    /// </summary>
    /// <param name="kestrelServerOptions"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static KestrelServerOptions ConfigureServerCertificate<T>(this KestrelServerOptions kestrelServerOptions, WebApplicationBuilder builder) where T : class
    {
        //var section  = builder.Configuration.GetSection("SecuritySettings");
        //var settings = section.Get<SecuritySettings>() ?? new();

        var settings = ServiceBootstrap.GetSecuritySettings<T>().Value;

        if (   string.IsNullOrWhiteSpace(settings.CertificateFileName) 
            || string.IsNullOrWhiteSpace(settings.CertificatePassword) )
        {
            return kestrelServerOptions;
        }

        if ( !File.Exists(settings.CertificateFileName) )
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,//Path.GetFullPath(Assembly.GetEntryAssembly()?.Location ?? ".",
                Path.GetFileName(settings.CertificateFileName)
                );

            if ( !File.Exists(path) )
            {
                var m = $"Certificate file is not found: {settings.CertificateFileName}";
                _log.LogCritical(m);

                throw new ApplicationException(m);
            }
            else
            {
                settings.CertificateFileName = path;
            }
        }

        var cert = X509CertificateLoader.LoadPkcs12( File.ReadAllBytes(settings.CertificateFileName), settings.CertificatePassword);
        //var cert = new X509Certificate2( settings.CertificateFileName, settings.CertificatePassword );

        if ( !cert.HasPrivateKey )
        {
            var m = $"Certificate must have private key attached. FileName=\"{settings.CertificateFileName}\"";
            _log.LogCritical(m);

            throw new ApplicationException(m);
        }

        kestrelServerOptions.ConfigureEndpointDefaults(listenOptions => { listenOptions.UseHttps( cert ); });

        _log.LogDebug($"Certificate setup {cert.Subject} with\n" +
                      $"\tSubject    : {cert.Subject}\n" +
                      $"\tIssuer     : {cert.Issuer}\n" +
                      $"\tExpiry     : {cert.GetExpirationDateString()}\n" +
                      $"\tSNo        : {cert.GetSerialNumberString()}\n" +
                      $"\tThumbprint : {cert.Thumbprint}\n" +
                      $"\tFile       : {settings.CertificateFileName}"
        );

        return kestrelServerOptions;
    }

    /// <summary>
    /// Check if the current environment is "Development"
    /// </summary>
    /// <returns></returns>
    public static bool IsDevelopment()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development;
        return env.Equals(Environments.Development, StringComparison.OrdinalIgnoreCase) || env.Equals("DEV", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Configure mTLS authentication
    /// </summary>
    public static void ConfigureCertificateAuthentication(IOptions<SecuritySettings> settings,
                                                          CertificateAuthenticationOptions options)
    {
        options.AllowedCertificateTypes = CertificateTypes.Chained;
        options.RevocationMode          = X509RevocationMode.NoCheck;

        options.Events = new()
        {
            OnAuthenticationFailed = ctx =>
            {
                if ( IsDevelopment() )
                {
                    _log.LogWarning($"Certificate rejected, but we'll still accept it: {ctx.Exception.Message}");
                    ctx.Success();
                }
                return Task.CompletedTask;
            },
            OnCertificateValidated = context =>
            {
                if ( !CheckClientCertificate(context.ClientCertificate, settings.Value) )
                {
                    context.Fail("Invalid certificate");
                    return Task.CompletedTask;
                }

                var claims = new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        context.ClientCertificate.Subject,
                        ClaimValueTypes.String, context.Options.ClaimsIssuer),
                    new Claim(
                        ClaimTypes.Name,
                        context.ClientCertificate.Subject,
                        ClaimValueTypes.String, context.Options.ClaimsIssuer)
                };

                context.Principal = new(new ClaimsIdentity(claims, context.Scheme.Name));
                context.Success();

                return Task.CompletedTask;
            }
        };
    }

    public static WebApplicationBuilder AddConfiguredHttpClient(this WebApplicationBuilder builder, string name)
    {
        builder.Services
                      .AddHttpClient(name)
                      .ConfigurePrimaryHttpMessageHandler(() => ConfigureHttpsHandler(builder.Configuration, name));
        return builder;
    }


    private static bool CheckClientCertificate(X509Certificate2 cert, SecuritySettings settings)
    {
        if ( !settings.ClientCertWhiteList.Contains(cert.Subject) )
        {
            if ( IsDevelopment() )
            {
                _log.LogWarning($"Certificate Subject=\"{cert.Subject}\" Fingerprint=\"{cert.Thumbprint}\" rejected, but we'll still accept it. Reason=\"User not registered\"");
                return true;
            }
            _log.LogInformation($"Certificate Subject=\"{cert.Subject}\" Fingerprint=\"{cert.Thumbprint}\" rejected. Reason=\"User not registered\"");
            return false;
        }
        return true;
    }

    public static bool CheckClientCertificateChain(
        X509Certificate2?                     cert, 
        X509Chain?                            chain, 
        SslPolicyErrors                       errors 
    )
    {
        var res = true;
        if ( errors != SslPolicyErrors.None )
        {
            _log.LogInformation($"Certificate Subject=\"{cert?.Subject}\" Issuer=\"{cert?.Issuer}\" Fingerprint=\"{cert?.Thumbprint}\" rejected. Reason=\"Check errors: {errors}\"{PrintChain(chain)}");
            res = false;
        }
        else if ( chain == null )
        {
            _log.LogInformation($"Certificate Subject=\"{cert?.Subject}\" Fingerprint=\"{cert?.Thumbprint}\" rejected. Reason=\"Certificate chain is not present\"");
            res = false;
        }
        else if ( _allowedRootCA.Count > 0 && !chain.ChainElements.Any(x => _allowedRootCA.Contains(new (x.Certificate.Thumbprint,x.Certificate.Subject)) ))
        {
            _log.LogInformation($"Certificate Subject=\"{cert?.Subject}\" Fingerprint=\"{cert?.Thumbprint}\" rejected. Reason=\"CA not in chain: {string.Join(",",_allowedRootCA.Select(x => x.CN))}\"{PrintChain(chain)}");
            res = false;
        }

        if ( res || !IsDevelopment() ) 
            return res;

        _log.LogInformation($"Certificate Subject=\"{cert?.Subject}\" Fingerprint=\"{cert?.Thumbprint}\" rejected. Reason=\"Invalid certificate chain\"{PrintChain(chain)}");
        return true;

    }

    private static string PrintChain(X509Chain? chain)
    {
        if (chain == null)
            return "";

        var sb = new StringBuilder();
        
        sb.AppendLine();
        foreach (var cert in chain.ChainElements)
        {
            sb.AppendLine($"    {cert.Certificate.Subject} -> {string.Join(", ", cert.ChainElementStatus.Select(x => $"{x.Status} {x.StatusInformation}"))}");
        }

        return sb.ToString();
    }
}