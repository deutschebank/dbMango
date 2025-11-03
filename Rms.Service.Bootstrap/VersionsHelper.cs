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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Rms.Service.Bootstrap;

public static class VersionsHelper
{
    /// <summary>
    /// Returns the version of the Forge binaries.
    /// Currently the Designer/Pivot/Workflow exes are stampted at build time via globalassemblyinfo.cs(Version/FileVersion/InformationalVersion)
    /// The core microservices are also stamped but use a slightly different model via VErsion in the csproj
    /// This logic is all done via TC as the first build step (powershell script)
    /// </summary>
    [field: AllowNull, MaybeNull]
    public static string Version
    {
        get
        {
            if (field != null)
                return field;

            field = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(field))
                return field;

            var path = Path.GetDirectoryName(typeof(VersionsHelper).Assembly.Location) ?? ".";
            field = ParseVersionFile(Path.Combine(path, "App.Ver.txt"));
            return field;
        }
    }

    public static string GetLocalExePath(string exe)
    {
        var myPath = Path.GetDirectoryName( typeof( VersionsHelper ).Assembly.Location );
        if ( myPath == null || !Directory.Exists( myPath ) )
            throw new ApplicationException( "Can't detect this assembly location" );

        var pathsToTry = new[]
        {
            ".",
            "../net8.0",
            "../net8.0/win-x64",
            "../net8.0-windows",
            "../net8.0-windows/win-x64",
        };

        string? LookupExe(string exeName) =>
            pathsToTry
               .Select( searchPath => Path.Combine( myPath, searchPath, exeName ) )
               .Where( File.Exists )
               .Select( Path.GetFullPath )
               .FirstOrDefault();

        myPath = LookupExe(exe) 
         ?? LookupExe(Path.ChangeExtension(exe, ".dll"))
            ;

        return myPath != null
                ? Path.GetDirectoryName( myPath )!
                : throw new ApplicationException("Can't find " + exe)
            ;
    }

    public static bool IsLinux => Environment.OSVersion.Platform == PlatformID.Unix;

    public static string PlatformId => (IsLinux ? "linux-" : "win-") +
        System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString().ToLower();

    public static bool IsInContainer() =>
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
     || Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_PORT_HTTPS") != null;

    public static string NetRuntimeVersion => Environment.Version.ToString();
    public static string OSVersion         => Environment.OSVersion.ToString();

#region Private methods/fields

    private static string ParseVersionFile(string fileName)
    {
        try
        {
            if (!File.Exists(fileName))
                return "unknown";

            var s = File.ReadAllLines(fileName);
            if (s.Length > 0)
                return s[0];
        }
        // ReSharper disable once EmptyGeneralCatchClause
        catch
        {
        }

        return "unknown";
    }
#endregion
}