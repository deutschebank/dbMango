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
﻿using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Rms.Service.Bootstrap.Security;

public class SimplePasswordManager : IPasswordManager
{
    private readonly string            _decryptionKeyFileName;
    private readonly string            _decryptionKeyPassword;
    private readonly bool              _allowClearText;
    private          X509Certificate2? _cert;

    private readonly ILogger _log;

    /// <summary>
    /// Constructor. DO NOT USE IOptions(SecuritySettings) here. Otherwise, you'll get an infinite loop
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="logger"></param>
    public SimplePasswordManager(IConfiguration settings, 
                                 ILogger<SimplePasswordManager> logger)
    {
        _log                         =   logger;
        InternalPasswordManager._log ??= logger;

        _decryptionKeyFileName = settings.GetSection("SecuritySettings")["MasterKeyFileName"] ?? "";
        _decryptionKeyPassword = settings.GetSection("SecuritySettings")["MasterKeyPassword"] ?? "";

        _log.Log(LogLevel.Debug, $"Loading master key from {_decryptionKeyFileName}");

        if (_decryptionKeyPassword.StartsWith('#') && File.Exists(_decryptionKeyPassword[1..]))
        {
            _log.Log(LogLevel.Debug, $"Loading master key password from {_decryptionKeyPassword[1..]}");
            _decryptionKeyPassword = File.ReadAllText(_decryptionKeyPassword[1..]);
        }

        _allowClearText        = true;

        LoadPrivateKey();
    }

    public SimplePasswordManager(string                         decryptionKeyFileName, 
                                 string                         decryptionKeyPassword, 
                                 ILogger<SimplePasswordManager> logger,
                                 bool                           allowClearText = false)
    {
        _log                         =   logger;
        InternalPasswordManager._log ??= logger;
        _decryptionKeyFileName       =   decryptionKeyFileName;
        _decryptionKeyPassword       =   decryptionKeyPassword;
        _allowClearText              =   allowClearText;

        LoadPrivateKey();
    }

    public bool Enabled => !string.IsNullOrWhiteSpace(_decryptionKeyFileName);

    private void LoadPrivateKey()
    {
        if ( !Enabled )
            return;

        var decryptionKeyFileName = _decryptionKeyFileName;

        if ( !File.Exists(decryptionKeyFileName) )
        {
            var path = Path.GetDirectoryName(GetType().Assembly.Location) 
             ?? Path.GetFullPath(".");
            var name = Path.Combine(path, Path.GetFileName(decryptionKeyFileName) 
                                 ?? throw new ApplicationException("no file name found"));

            if ( !File.Exists(name) )
            {
                ReportFatalError($"Private key file is not found. Tried \"{decryptionKeyFileName}\" and \"{name}\"", null);
            }

            decryptionKeyFileName = name;
        }


        var p = string.IsNullOrWhiteSpace(_decryptionKeyPassword)
            ? "F" + "orge"
            : _decryptionKeyPassword
            ;

        try
        {
            _cert = InternalPasswordManager.LoadPrivateKey(decryptionKeyFileName, p);
        }
        catch (Exception e)
        {
            ReportFatalError($"Can't load private key from \"{decryptionKeyFileName}\"", e);
        }
    }

    private void ReportFatalError(string message, Exception? e)
    {
        if ( !Enabled )
        {
            _log.LogWarning("No decryption file specified, passwords are not supposed to be stored in the clear text!");
        }
        else
        {
            if ( File.Exists(_decryptionKeyFileName))
                _log.LogDebug($"Decryption key FileName=\"{_decryptionKeyFileName}\" exists");
            else
                _log.LogError($"Decryption key FileName=\"{_decryptionKeyFileName}\" does not exist");
        }

        if ( Directory.Exists(_decryptionKeyFileName) )
            _log.LogDebug($"DecryptionKeyFileName=\"{_decryptionKeyFileName}\" is a directory");

        var dataDir = Array.Empty<string>();
        if ( Enabled )
        {
            var dirName = Directory.Exists(_decryptionKeyFileName) 
                    ? _decryptionKeyFileName
                    : Path.GetDirectoryName(_decryptionKeyFileName)
                ;

            if ( !string.IsNullOrWhiteSpace(dirName) )
                dataDir = Directory.GetFiles(dirName);
        }

        var msg = $"{message}\n" +
                $"Data file    : {_decryptionKeyFileName}\n" +
                $"\t{string.Join("\n\t", dataDir)}\n" 
            ;

        _log.LogCritical(msg, e);

        throw new CryptographicException(msg, e);
    }

    private T WithRetry<T>(Func<T> action, [CallerMemberName] string? method = null ) where T : class
    {
        try
        {
            return action();
        }
        catch (Exception e)
        {
            _log.LogWarning($"{method}: {e.Message}", e );
            LoadPrivateKey();
            return action();
        }
    }

    public string DecryptPassword(string encryptedPassword) => WithRetry(() =>
    {
        if (!Enabled)
            return encryptedPassword;
        try
        {
            return InternalPasswordManager.DecryptPassword(encryptedPassword, _cert ?? throw new InvalidOperationException());
        }
        catch (Exception)
        {
            if ( _allowClearText && encryptedPassword.StartsWith("*") )
                return Uri.UnescapeDataString(encryptedPassword[1..]);
            throw;
        }
    });

    public string EncryptPassword(string clearTextPassword) => WithRetry(() =>
    {
        if (!Enabled)
            return clearTextPassword;
        try
        {
            return InternalPasswordManager.EncryptPassword(clearTextPassword, _cert ?? throw new InvalidOperationException());
        }
        catch (Exception)
        {
            if ( _allowClearText )
                return "*" + Uri.EscapeDataString(clearTextPassword);
            throw;
        }
    });

    public string DecryptFileContents(string encryptedFileContents)      => WithRetry(() =>
    {
        if (!Enabled)
            return encryptedFileContents;
        return InternalPasswordManager.Pkcs7Decrypt(encryptedFileContents,
                                                    _cert ?? throw new InvalidOperationException());
    });

    public string EncryptFileContents( string clearTextFileContents )    => WithRetry(() =>
    {
        if (!Enabled)
            return clearTextFileContents;
        return InternalPasswordManager.Pkcs7Encrypt(clearTextFileContents,
                                                    _cert ?? throw new InvalidOperationException());
    });

    public string EncryptBinaryFileContents( byte[] binaryFileContents ) => WithRetry(() =>
    {
        if (!Enabled)
            return Convert.ToBase64String(binaryFileContents);
        return InternalPasswordManager.Pkcs7Encrypt(binaryFileContents, _cert ?? throw new InvalidOperationException());
    });
    public byte [] DecryptBinaryFileContents( string contents )          => WithRetry(() =>
    {
        if (!Enabled)
            return Convert.FromBase64String(contents);
        return InternalPasswordManager.Pkcs7DecryptBinary(contents, _cert ?? throw new InvalidOperationException());
    });
}