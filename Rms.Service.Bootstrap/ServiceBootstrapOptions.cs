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
﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;

namespace Rms.Service.Bootstrap;

/// <summary>
/// Which authorization type to use
/// </summary>
public enum AuthorizationType
{
    /// <summary>
    /// use FxAdmin. User profiles read from the database
    /// </summary>
    FxAdmin, 
    /// <summary>
    /// Use Abacus. Abacus calls used for authorization.
    /// </summary>
    Abacus,
    /// <summary>
    /// Do nor add any authorization. Application will handle it itself.
    /// </summary>
    Skip
}

/// <summary>
/// Options for configuring standard endpoint
/// </summary>
/// <typeparam name="T"></typeparam>
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
// ReSharper disable once UnusedTypeParameter
public class ServiceBootstrapOptions<T> where T : class
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global

    /// <summary>
    /// API service. Enable gRPC endpoints. 
    /// </summary>
    public bool              EnableGrpc            { get; set; } = true;
    /// <summary>
    /// API service. Enable gRPC transcoding to Json REST calls. 
    /// </summary>
    public bool              EnableGrpcTranscoding { get; set; } = true;
    /// <summary>
    /// Service API version (example: v1) to use for Swagger
    /// </summary>
    public string            ApiVersion            { get; set; } = "v1";
    /// <summary>
    /// API service. Enable Oidc integration. Requires Oidc application onboarding.
    /// </summary>
    public bool              EnableOidc            { get; set; } = true;
    /// <summary>
    /// API service. Enable mTLS authentication. Requires service certificate.
    /// </summary>
    public bool              EnableMTLS            { get; set; } = true;
    /// <summary>
    /// Web application. Enable OAuth2. Requires Oidc application onboarding.
    /// </summary>
    public bool              EnableOpenIdConnect   { get; set; }
    /// <summary>
    /// API service. Enable builtin health checks.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;
    /// <summary>
    /// Authorization source: FxAdmin or Abacus.
    /// </summary>
    public AuthorizationType AuthorizeBy           { get; set; } = AuthorizationType.Abacus;
    /// <summary>
    /// Callback for additional OpenTelemetry configuration.
    /// </summary>
    public Action<MeterProviderBuilder>? ConfigureOpenTelemetry { get; set; }
    /// <summary>
    /// If you are using custom health checks this is your chance to configure them.
    /// See https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?source=recommendations
    /// </summary>
    public Action<IHealthChecksBuilder>? ConfigureHealthChecks { get; set; }
    /// <summary>
    /// Method for formatting health check result
    /// </summary>
    public Func<HttpContext, HealthReport, Task>? HealthChecksWriter { get; set; }
}