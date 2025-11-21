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
namespace Rms.Risk.Mango.Services;

public static class TempFolderHelper
{
    private static          string? _tempFolder;
    private static readonly Lock _syncObject = new();

    /// <summary>
    /// Unfortunately on DB application servers TEMP folder located on very small C: drives.
    /// So we can't use Path.GetTempPath(). We are using %RMS_RISKSTORE% instead.
    /// </summary>
    /// <returns>Temp folder path on the large disk</returns>
    public static string GetTempFolder()
    {
        var path = _tempFolder;
        if (path != null) 
            return path;
            
        lock (_syncObject)
        {
            if (_tempFolder != null) 
                return _tempFolder;

            if ( path == null || !Directory.Exists( path ) )
            {
                path = Environment.GetEnvironmentVariable("RMS_RISKSTORE"); //appserver = D:\data\Rms_RiskStore
                if (path != null && Directory.Exists(path))
                {
                    path = Path.Combine(path, "Forge", "Temp");
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
            }
            if ( path == null || !Directory.Exists( path ) )
                path = Path.GetTempPath();
            if ( !Directory.Exists( path ) )
                throw new ApplicationException("Can't get temporary folder");
            _tempFolder = path;
        }

        return _tempFolder;
    }

    private static long _counter;

    public static string GetTempFileName(string key = "tmp")
    {
        var folder = GetTempFolder();
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder!);

        for (var i = 0; i < 100_000; i++)
        {
            var counter = Interlocked.Increment(ref _counter);

            var name = Path.Combine(folder, $"{DateTime.Now:hh-mm-ss}-{FileUtils.Shield(key)}-{counter}.tmp");
            if ( !File.Exists(name))
                return name;
        }

        throw new ApplicationException($"Can't obtain temp file name in Folder=\"{folder}\" Key=\"{key}\". Too many temp files in the folder.");
    }
}