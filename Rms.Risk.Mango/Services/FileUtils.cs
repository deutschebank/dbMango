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
using System.Diagnostics;
using System.Reflection;
using log4net;

namespace Rms.Risk.Mango.Services;

public static class FileUtils
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    public static string ExecutionEnvironment = "";

    /// <summary>
    /// Use this call BEFORE creating a file.
    /// Deletion done synchronously.
    /// </summary>
    /// <param name="fileName">File to delete</param>
    /// <param name="throwOnFailure">Throw an exception on failure (default true)</param>
    /// <param name="logErrorOnFailure">Log error on failure (default true)</param>
    /// <param name="timeout"></param>
    public static void ForceDelete(string fileName, bool throwOnFailure = true, bool logErrorOnFailure = true, TimeSpan? timeout = null)
    {
        if (fileName == null || !File.Exists(fileName))
            return;

        timeout ??= TimeSpan.FromSeconds(1);
        DeletionCycle( fileName, timeout.Value, out var firstException);
        if ( !File.Exists( fileName ) )
            return;

        var msg = $"Unable to delete File=\"{fileName}\"";

        if (firstException != null)
        {
            msg += $" : exception \"{firstException.Message}\"";
        }
        if (logErrorOnFailure)
            _log.Error( msg );
        if (throwOnFailure)
            throw new ApplicationException(msg);
    }

    /// <summary>
    /// Use this method AFTER you finished working with a file.
    /// Deletion may be delayed and executed asynchronously.
    /// </summary>
    /// <param name="fileName">File to delete</param>
    public static void SafeDelete(string fileName)
    {
        if (fileName == null || !File.Exists(fileName))
            return;

        try
        {
            // try to delete synchronously
            File.Delete(fileName);
        }
        catch
        {
            // start a delayed task
               
            Task.Factory.StartNew( () => 
            {
                DeletionCycle( fileName, TimeSpan.FromSeconds( 5 ) , out var firstException);
                if (firstException != null)
                {
                    _log.Debug($"Unable to delete file {fileName} : {firstException.Message}");
                }
            });
        }
    }

    private static void DeletionCycle( string fileName, TimeSpan timeout , out Exception? firstException)
    {
        var sw = new Stopwatch();
        sw.Start();
        firstException = null;
        try
        {
            do
            {
                try
                {
                    if (File.Exists(fileName))
                        File.Delete( fileName );
                    return;
                }
                catch ( Exception ex )
                {
                    firstException ??= ex;
                }

                Task.Delay(TimeSpan.FromMilliseconds(333)); // 1/3 second

            } while ( sw.Elapsed < timeout );
        }
        catch ( Exception ex )
        {
            _log.Debug( $"Can't delete File=\"{fileName}\"", ex );
            firstException ??= ex;
            // swallow
        }
    }
    public static string Shield(string key, string replace = ".") =>
        //remove any illegal filename chars (:,/,\,<,> etc) and replace with a replacement string
        string.Join(replace, key.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));


    /// <summary>
    /// Copy file with .tmp renaming
    /// </summary>
    /// <param name="dest"></param>
    /// <param name="src"></param>
    /// <param name="logErrorInForceDelete">Log error on failure in ForceDelete (default true)</param>
    public static void SafeCopy(string dest, string src, bool logErrorInForceDelete = true)
    {
        var tempDest = dest + ".tmp";
        try
        {
            var directory = new FileInfo(dest).Directory;
            if (directory is { Exists: false })
                directory.Create();

            ForceDelete( tempDest, logErrorOnFailure: logErrorInForceDelete );
            File.Copy( src, tempDest, true );
            ForceDelete( dest, logErrorOnFailure: logErrorInForceDelete );
            File.Move( tempDest, dest );
        }
        catch ( Exception )
        {
            SafeDelete( tempDest );
            throw;
        }
    }

    public static T DoSafe<T>( Func<T> action, string message )
    {
        Exception? firstException = null;
        var       endAt          = DateTime.Now + TimeSpan.FromSeconds( 10 );
            
        while( DateTime.Now < endAt )
        {
            try
            {
                return action();
            }
            catch ( Exception e )
            {
                firstException ??= e;
                _log.Warn( $"{message} (retry pending): {e.Message}", e );
            }

            Thread.Sleep( 333 );
        }

        throw new ApplicationException( $"{message}: {firstException?.Message}", firstException );
    }

    public static StreamReader SafeOpenText( string fileName )
        => DoSafe( () => File.OpenText( fileName ), $"Can't open File=\"{fileName}\"" );

    public static void SafeRename( string srcFileName, string destFileName )
        => DoSafe( () =>
                   {
                       ForceDelete(destFileName);
                       File.Move( srcFileName, destFileName );
                       return true;
                   }, $"Can't rename File=\"{srcFileName}\" To=\"{destFileName}\"" );

    public static bool SafeDeleteFolder( string folder )
    {
        if ( string.IsNullOrWhiteSpace( folder ) || !Directory.Exists( folder ) ) 
            return true;

        try
        {
            Directory.Delete(folder, true);
            if (!Directory.Exists(folder))
                return true;
            _log.Warn( $"Switching to slow method as Folder=\"{folder}\" is still exists");
        }
        catch (Exception ex)
        {
            _log.Warn( $"Switching to slow method as we can't remove Folder=\"{folder}\" {ex.Message}", ex);
        }

        var files = GetFiles(folder);
        foreach (var file in files)
        {
            ForceDelete(file, false);
        }

        try
        {
            Directory.Delete(folder, true);
            if (!Directory.Exists(folder))
                return true;
            _log.Error( $"Folder=\"{folder}\" should have been deleted, but it still exists");
        }
        catch (Exception ex)
        {
            _log.Error( $"Can't delete Folder=\"{folder}\": {ex.Message}", ex);
        }

        return false;
    }

    public static IEnumerable<string> GetFiles(string path, bool throwOnError = false)
    {
        var queue = new Queue<string>();
        queue.Enqueue(path);
        while (queue.Count > 0)
        {
            path = queue.Dequeue();
            try
            {
                foreach (var subDir in Directory.GetDirectories(path))
                {
                    queue.Enqueue(subDir);
                }
            }
            catch (Exception ex)
            {
                _log.Error( $"Error listing directories for Folder=\"{path}\": {ex.Message}", ex);
                if (throwOnError)
                    throw;
            }
        }
        string[]? files = null;
        try
        {
            files = Directory.GetFiles(path);
        }
        catch (Exception ex)
        {
            _log.Error($"Error listing files within Folder=\"{path}\": {ex.Message}", ex);
            if (throwOnError)
                throw;
        }

        if (files == null)
            yield break;

        foreach (var t in files)
            yield return t;
    }

    // If folderName is relative (incl. if it is empty), then it is appended to baseFolder.
    // Otherwise, if folderName is an absolute path, then baseFolder is ignored.
    // If fileName is relative (incl. if it is empty), then it is appended to the above combination.
    // Otherwise if fileName is an absolute path, then it replaces everything.
    // (Note these are all just desirable consequences of using Path.Combine().)
    //
    // The result of the above gets the following substitutions:
    //   %DATEDIR% => yyyy\MM (e.g. 2021\07)
    //   %ENV% => PROD or UAT or DEV according to ExecutionEnvironment
    //   % => "-yyyy-MM-dd"
    // Note: Date-dependent substitutions only take place if date argument is not null.
    //
    // Null folder/file names are converted to "", for which Path.Combine is well defined.
    //
    // Nothing is saved or created here! Just building path name with (optional) substitutions.
    //
    public static string? InterpolatePath(string    baseFolder,  string folderName, string fileName,
                                         DateTime? date = null, bool   useFullPath = true)
    {
        string fullPathName;
        try
        {
            fullPathName = useFullPath
                ? Path.GetFullPath(Path.Combine(Path.Combine(baseFolder ?? "", folderName ?? ""),
                                                             fileName ?? ""))
                : Path.Combine(Path.Combine(baseFolder ?? "", folderName ?? ""),
                                            fileName ?? "");

            var envName = ExecutionEnvironment.Replace("Cloud-", "");

            fullPathName = fullPathName.Replace("%ENV%", envName);
            if (date != null)
            {
                fullPathName = fullPathName.Replace("%DATEDIR%", date.Value.Year.ToString() +
                                                    Path.DirectorySeparatorChar + date.Value.Month.ToString("D2"));
                fullPathName = fullPathName.Replace("%", "-" + date.Value.ToString("yyyy-MM-dd"));
            }
        }
        catch (Exception e)
        {
            _log.Error($"Failed to build path from '{baseFolder}' + '{folderName}' + '{fileName}' + '{date?.ToString()}': {e.Message}");
            return null;
        }
        return fullPathName;
    }

    /// <summary>
    /// Delete a directory and its subdirs and files.
    /// Fixes the issue were Directory.Delete doesn't work in the hidden .git folder
    /// </summary>
    public static void ObliterateDirectory(string targetDir)
    {
        if (!Directory.Exists(targetDir))
            return;

        try
        {
            File.SetAttributes(targetDir, FileAttributes.Normal);
        }
        catch (Exception e)
        {
            _log.Warn($"Cant set attributes for {targetDir} directory", e);
        }

        var files = Directory.GetFiles(targetDir);

        foreach (var file in files)
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                ForceDelete(file, throwOnFailure:false);
            }
            catch (Exception e)
            {
                _log.Warn($"Cant delete {targetDir} directory", e);
            }
        }

        try
        {
            foreach (var dir in Directory.EnumerateDirectories(targetDir))
            {
                ObliterateDirectory(dir);
            }
        }
        catch (Exception e)
        {
            _log.Warn($"Cant enumerate {targetDir} directories", e);
        }

        Directory.Delete(targetDir, false);
    }

    public static void CopyDirectory(string source, string target)
    {
        if ( !Directory.Exists(target))
            Directory.CreateDirectory(target!);
        CopyRecursively(new (source), new (target));
    }

    private static void CopyRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (var dir in source.GetDirectories())
            CopyRecursively(dir, target.CreateSubdirectory(dir.Name));
        foreach (var file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name));
    }
}