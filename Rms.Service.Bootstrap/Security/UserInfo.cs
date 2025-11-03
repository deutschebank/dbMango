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

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Basic user information
/// </summary>
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class UserInfo
{
    /// <summary>
    /// Windows account name
    /// </summary>
    public string NtAccount
    {
        get;
        set => field = value.Trim().ToUpper();
    } = "";

    /// <summary>
    /// Oracle account name
    /// </summary>
    public string OracleAccount
    {
        get;
        set => field = value.Trim().ToUpper();
    } = "";

    /// <summary>
    /// User name
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Email address
    /// </summary>
    public string Email
    {
        get;
        set => field = value.Trim().ToLower();
    } = "";

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles      { get; set; } = [];
    /// <summary>
    /// Function
    /// </summary>
    public string       Function   { get; set; } = "";
    /// <summary>
    /// Department
    /// </summary>
    public string       Department { get; set; } = "";
}