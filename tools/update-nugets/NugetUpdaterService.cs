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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

internal class NugetUpdaterService
{
    private class NugetPackage
    {
        public required string Name    { get; init; }
        public required string Version { get; init; }

        public override string ToString() => $"{Name} {Version}";

        public override bool Equals(object? obj)
        {
            if (obj is NugetPackage other)
            {
                return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode() => Name?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
    }

    public async Task UpdateNugetPackages()
    {
        var nugets        = ReadDirectoryPackages();
        var fixedVersions = ReadFixedVersions();

        if (fixedVersions.Count > 0)
        {
            ConsoleLogger.Section("Fixed versions (skipped)");
            foreach (var p in fixedVersions.OrderBy(x => x.Name))
                ConsoleLogger.Fixed(p.Name, p.Version);
        }

        var newVersions = await SearchForUpdatesOnNugetOrg(nugets.Except(fixedVersions).ToList());

        if (newVersions.Count == 0)
        {
            ConsoleLogger.NothingToDo();
            return;
        }

        var updatedList = nugets
            .Except(fixedVersions)
            .Except(newVersions)
            .Concat(fixedVersions)
            .Concat(newVersions)
            .OrderBy(x => x.Name)
            .ToList()
            ;

        var outputFile = WritePackagesProps(updatedList, "Directory.Packages.props.new");
        ConsoleLogger.Summary(newVersions.Count, nugets.Except(fixedVersions).Count() - newVersions.Count, fixedVersions.Count, outputFile);
    }

    private string WritePackagesProps(List<NugetPackage> updatedList, string directoryPackagesPropsNew)
    {
        var folder = Path.GetDirectoryName(FindPackagesPropsFileName("Directory.Packages.props"));
        var fileName = Path.Combine(folder!, directoryPackagesPropsNew);
        using var writer = new StreamWriter(fileName);
        writer.WriteLine("<Project>");
        writer.WriteLine("  <PropertyGroup>");
        writer.WriteLine("    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>");
        writer.WriteLine("  </PropertyGroup>");
        writer.WriteLine("  <ItemGroup>");
        foreach (var package in updatedList)
        {
            writer.WriteLine($"    <PackageVersion Include=\"{package.Name}\" Version=\"{package.Version}\" />");
        }
        writer.WriteLine("  </ItemGroup>");
        writer.WriteLine("</Project>");
        return fileName;
    }

    private async Task<List<NugetPackage>> SearchForUpdatesOnNugetOrg(List<NugetPackage> src)
    {
        var httpClient = new HttpClient();
        var updatedPackages = new List<NugetPackage>();

        ConsoleLogger.Section($"Checking {src.Count} packages on NuGet.org");

        foreach (var package in src)
        {
            var url = $"https://api.nuget.org/v3-flatcontainer/{package.Name.ToLowerInvariant()}/index.json";

            try
            {
                ConsoleLogger.BeginQuerying(package.Name);
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = System.Text.Json.JsonDocument.Parse(content);

                    if (json.RootElement.TryGetProperty("versions", out var versionsElement))
                    {
                        var currentVersion = NuGet.Versioning.NuGetVersion.Parse(package.Version);
                        var versions = versionsElement.EnumerateArray()
                            .Select(v => v.GetString())
                            .Where(v => !string.IsNullOrEmpty(v))
                            .Select(v => NuGet.Versioning.NuGetVersion.Parse(v!))
                            // Keep stable packages stable; follow pre-release updates only for packages already using one.
                            .Where(v => currentVersion.IsPrerelease == v.IsPrerelease)
                            .ToList();

                        if (versions.Any())
                        {
                            var latestVersion = versions
                                .OrderByDescending(v => v)
                                .FirstOrDefault();

                            if (latestVersion != null && latestVersion > currentVersion)
                            {
                                ConsoleLogger.Updated(package.Name, package.Version, latestVersion.ToString());
                                updatedPackages.Add(new()
                                {
                                    Name    = package.Name,
                                    Version = latestVersion.ToString()
                                });
                            }
                            else
                            {
                                ConsoleLogger.Skipped(package.Name, package.Version);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.Error(package.Name, ex.Message);
            }
        }

        return updatedPackages;
    }

    private List<NugetPackage> ReadDirectoryPackages()
    {
        var fileName = FindPackagesPropsFileName("Directory.Packages.props")!;
        if (fileName == null)
        {
            ConsoleLogger.Info($"Cannot find `Directory.Packages.props` in the current directory or parent directories.");
            throw new FileNotFoundException($"Cannot find Directory.Packages.props in the current directory or parent directories.");
        }

        ConsoleLogger.Info($"Found `{fileName}`.");
        var packages = ReadPackagesProps(fileName);

        return packages;
    }

    private List<NugetPackage> ReadFixedVersions()
    {
        var fileName = FindPackagesPropsFileName("FixedPackageVersions.xml");
        if (fileName == null)
        {
            ConsoleLogger.Info($"No `FixedPackageVersions.xml` found.");
            return []; 
        }

        ConsoleLogger.Info($"Found `{fileName}`.");
        var packages = ReadPackagesProps(fileName);

        return packages;
    }


    private static List<NugetPackage> ReadPackagesProps(string fileName)
    {
        var packages = new List<NugetPackage>();

        // Read the contents of the Directory.Packages.props file
        var fileContent = File.ReadAllText(fileName);

        // Parse the XML content to extract NuGet package information
        var xmlDoc = new System.Xml.XmlDocument();
        xmlDoc.LoadXml(fileContent);

        var packageNodes = xmlDoc.SelectNodes("//PackageVersion");
        if (packageNodes != null)
        {
            foreach (System.Xml.XmlNode node in packageNodes)
            {
                if (node.Attributes != null)
                {
                    var name    = node.Attributes["Include"]?.InnerText;
                    var version = node.Attributes["Version"]?.InnerText;

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(version))
                    {
                        packages.Add(new NugetPackage
                        {
                            Name    = name,
                            Version = version
                        });
                    }
                }
            }
        }

        return packages;
    }

    private static string? FindPackagesPropsFileName(string fileName)
    {
        while (!File.Exists(fileName))
        {
            var p = Path.Combine("..", fileName!);
            if ( Path.GetDirectoryName(Path.GetFullPath(fileName)) == Path.GetPathRoot(Path.GetFullPath(p)) )
                break;
            fileName = p;
        }

        if ( File.Exists(fileName))
            return fileName;
        return null;
    }
}
