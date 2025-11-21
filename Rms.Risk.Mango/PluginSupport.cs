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
﻿using Rms.Risk.Mango.Interfaces;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Rms.Risk.Mango;

public static partial class PluginSupport
{
    [GeneratedRegex(@"^.*Mango.*Plugin\.dll$")]
    private static partial Regex PluginNameMask();

    private static readonly List<Assembly>  _loaded = [];
    private static          IDbMangoPlugin? _plugin;

    public static List<Assembly> GetPluginAssemblies(ILogger? logger)
    {
        if ( _loaded.Count > 0 )
            return _loaded;

        var basePath = AppContext.BaseDirectory;

        if (!Directory.Exists(basePath))
            return _loaded;

        var pluginFiles = Directory.GetFiles(basePath, "*.dll", SearchOption.TopDirectoryOnly)
            .Where(file => PluginNameMask().IsMatch(file));

        _loaded.AddRange(pluginFiles.Select(LoadPlugin).OfType<Assembly>());

        if ( _loaded.Count > 0 )
            logger?.LogInformation("Plugin assemblies loaded:\n{Join}", string.Join("\n\t", _loaded.Select(x => x.FullName)));
        else
            logger?.LogInformation("No plugin assemblies loaded");

        return _loaded;
    }

    public static IDbMangoPlugin? GetPlugin(ILogger? logger = null)
    {
        if ( _plugin != null )
            return _plugin;
        string? pluginClassName = GetPluginClassName();
        var assemblies = GetPluginAssemblies(logger);
        var type = assemblies
            .SelectMany(x => x.GetTypes().Where(t => ( string.IsNullOrWhiteSpace(pluginClassName) || t.Name == pluginClassName) && t.GetInterface(nameof(IDbMangoPlugin)) != null ))
            .FirstOrDefault()
            ;

        if ( type == null )
            return null;

        _plugin = (IDbMangoPlugin?)Activator.CreateInstance(type);

        if ( _loaded.Count > 0 )
            logger?.LogInformation("Plugin selected:\n{type}", type);
        else
            logger?.LogInformation("No plugin selected");

        return _plugin;
    }

    private static string? GetPluginClassName()
    {
        var args = Environment.GetCommandLineArgs();
        // Check for "--plugin" argument and extract the plugin class name
        string? pluginClassName = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--plugin-class-name" && i + 1 < args.Length)
            {
                pluginClassName = args[i + 1];
                break;
            }
        }
        if (!string.IsNullOrWhiteSpace(pluginClassName))
        {
            return pluginClassName.Trim();
        }

        pluginClassName = Environment.GetEnvironmentVariable("DBMANGO_PLUGIN_CLASS_NAME");
        if (!string.IsNullOrWhiteSpace(pluginClassName))
        {
            return pluginClassName.Trim();
        }

        return null;
    }

    private static Assembly? LoadPlugin(string pluginPath)
    {
        if (!File.Exists(pluginPath)) 
            return null;

        try
        {
            var assembly = Assembly.LoadFrom(pluginPath);
            return assembly;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
