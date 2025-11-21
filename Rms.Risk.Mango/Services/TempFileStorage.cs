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
using log4net;
using System.Diagnostics;
using System.Reflection;

namespace Rms.Risk.Mango.Services;

public class TempFileStorage : ITempFileStorage, IDisposable
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    private const int TtlHours = 24;
    private const string TempFolderPrefix = "dbMango-";

    private static int _counter;


    public TempFileStorage( string? useThisFolder = null )
    {
        LocalPersistentFolder = Path.Combine(
                Environment.GetEnvironmentVariable("RMS_RISKSTORE")
             ?? Environment.GetEnvironmentVariable("TEMP")
             ?? Environment.GetEnvironmentVariable("TMP")
             ?? Path.GetTempPath()
             , "dbMango"
            );

        var tempFolderBase = useThisFolder;

        if (tempFolderBase != null)
        {
            if (!Directory.Exists(tempFolderBase))
                Directory.CreateDirectory(tempFolderBase);
            _log.Debug($"Using specific temporary Folder=\"{tempFolderBase}\" base");
        }

        if (tempFolderBase == null || !Directory.Exists(tempFolderBase))
        {
            tempFolderBase = TempFolderHelper.GetTempFolder();
            _log.Debug($"Using default temporary Folder=\"{tempFolderBase}\" base");
        }

        ClearOutdatedFiles(tempFolderBase, TimeSpan.FromHours(TtlHours));

        var counter = Interlocked.Increment(ref _counter);
        var name    = $"{TempFolderPrefix}{DateTime.Now.Date:yyyy-MM-dd}-{counter}-{Process.GetCurrentProcess().Id}";

        for ( var i = 0; i < 100; i++ )
        {
            var tempName = (i == 0 ? name : name + "(" + i + ")")+".temp";

            if ( Directory.Exists( Path.Combine( tempFolderBase, tempName ) ) ) 
                continue;

            TempFolder = Path.Combine( tempFolderBase, tempName );
            Directory.CreateDirectory( TempFolder );
            break;
        }

        if (TempFolder == null)
            throw new ApplicationException($"Failed to create temporary folder in {tempFolderBase}");

        _log.Debug( $"Using temporary Folder=\"{TempFolder}\"" );
    }

    ~TempFileStorage()
    {
        Dispose( false );
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

// ReSharper disable once UnusedParameter.Local
    private void Dispose(bool disposing)
    {
        if ( Directory.Exists( TempFolder ) )
        {
            FileUtils.SafeDeleteFolder( TempFolder );
        }
    }

    private static void ClearOutdatedFiles(string folder, TimeSpan timeToLive)
    {
        var now = DateTime.UtcNow;

        var allFolders = Directory.EnumerateDirectories(folder);
        var myTempFolders = allFolders
           .Where(x => Path.GetFileName(x).StartsWith(TempFolderPrefix, StringComparison.OrdinalIgnoreCase) && x.EndsWith(".temp", StringComparison.OrdinalIgnoreCase))
           ;
        var outdatedFolders = myTempFolders
           .Where( name => now - File.GetCreationTimeUtc(name) > timeToLive )
           ;

        foreach ( var name in outdatedFolders )
        {
            _log.Debug( $"Deleting temporary Folder=\"{name}\"" );
            var sw = Stopwatch.StartNew();

            FileUtils.SafeDeleteFolder(name);
            _log.Debug( $"Temporary Folder=\"{name}\" deleted. Elapsed=\"{sw.Elapsed:g}\"" );
        }
    }

    public string TempFolder            { get; }
    public string LocalPersistentFolder { get; }

    public string GetTempFileName( Type t ) => GetTempFileName( t.Name );

    public string GetTempFileName<T>() => GetTempFileName(typeof(T));

    public string GetTempFileName(string key)
    {
        var counter = Interlocked.Increment(ref _counter);
            
        if (!Directory.Exists(TempFolder))
            Directory.CreateDirectory(TempFolder);

        var name = Path.Combine( TempFolder, $"{DateTime.Now:hh-mm-ss}-{FileUtils.Shield(key)}-{counter}.tmp");
        _log.Debug( $"Temporary file name obtained. FileName=\"{name}\"" );
        return name;
    }
}