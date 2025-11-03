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
﻿using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Rms.Service.Bootstrap.Security;

public static partial class InternalPasswordManager
{
    public static ILogger? _log;

    /// <summary>
    /// Decrypt password. 
    /// If encrypted password starts with '*' it should be in base64 format.
    /// If encrypted password starts with '@' it should contain a file name. File should be binary.
    /// </summary>
    /// <example>
    ///    var pass1 = PasswordManager.DecryptPassword( @"@FileName.pwd",  cert);
    ///    var pass2 = PasswordManager.DecryptPassword( @"*abZf/dh+",      cert);
    ///    var pass3 = PasswordManager.DecryptPassword( @"clear text pass",cert);
    /// </example>
    /// <param name="encryptedPassword"></param>
    /// <param name="cert"></param>
    /// <returns></returns>
    public static string DecryptPassword( string encryptedPassword, X509Certificate2 cert )
    {
        var decrypted = DecryptPasswordUnchecked( encryptedPassword, cert );
        CheckGoodPassword( decrypted );

        return decrypted;
    }

    private static string DecryptPasswordUnchecked(string encryptedPassword, X509Certificate2 cert)
    {
        if ( string.IsNullOrWhiteSpace( encryptedPassword ) )
            return encryptedPassword;

        if (encryptedPassword.StartsWith("*")) // encoded right here
        {
            return Decrypt(encryptedPassword[1..], cert);
        }

        if (encryptedPassword.StartsWith("#")) // clear text password located in secrets-mounted file
        {
            var fileName = Environment.ExpandEnvironmentVariables( encryptedPassword[1..] );
            if ( File.Exists(fileName) )
                return File.ReadAllText(fileName).TrimEnd('\n').TrimEnd('\r');
            return encryptedPassword;
        }

        if (encryptedPassword.StartsWith("@")) // file name. contents encrypted
        {
            var fileName = Environment.ExpandEnvironmentVariables( encryptedPassword[1..] );
            var password = Encoding.ASCII.GetString(File.ReadAllBytes(fileName));
            return Decrypt(password, cert);
        }

        // it's not actually encrypted
        return encryptedPassword;
    }

    /// <summary>
    /// Encrypt password. 
    /// Encrypted password starts with '*' and it's in base64 format.
    /// </summary>
    /// <example>
    ///    var pass2 = PasswordManager.EncryptPassword( @"myPass", cert);
    /// </example>
    /// <param name="clearTextPassword"></param>
    /// <param name="cert"></param>
    /// <returns></returns>
    public static string EncryptPassword(string clearTextPassword, X509Certificate2 cert)
    {
        if ( string.IsNullOrWhiteSpace( clearTextPassword ) )
            return clearTextPassword;

        return "*"+Encrypt(clearTextPassword, cert);
    }

    private static T Repeat<T>( Func<T> func, string message )
    {
        Exception? firstException = null;
        var       stop = DateTime.Now + TimeSpan.FromSeconds( 5 );
        while ( DateTime.Now < stop )
        {
            try
            {
                return func();
            }
            catch ( Exception e )
            {
                firstException ??= e;
                Thread.Sleep( (int)(333 + new Random().NextDouble()*333.0) );
            }
        }
        _log?.LogError( firstException, message );
        throw firstException ?? new ApplicationException(message);
    }

    public static X509Certificate2 LoadPrivateKey( string fileName, string password )
    {
        var keyColl = LoadCertificateCollection(fileName, password, true);

        _log?.LogDebug( $"Certificate collection loaded from File=\"{fileName}\" Count={keyColl.Count}\n\t"
                   +string.Join("\n\t", keyColl.Select(x => $"Subject=\"{x.Subject}\" Valid=\"{x.NotAfter:s}\" Issuer=\"{x.Issuer}\"")));

        var key = SelectTheRightKey(keyColl);

        _log?.LogDebug( $"Private key loaded from File=\"{fileName}\" Subject=\"{key.Subject}\" Issuer={key.Issuer} Expiry=\"{key.NotAfter}\"" );
        return key;
    }

    public static X509Certificate2 SelectTheRightKey(X509Certificate2Collection keyColl, string? keyName = null )
    {
        if ( keyName != null )
            return keyColl.First(x => x.Subject == keyName);

        var key = keyColl.FirstOrDefault(key => key.Subject.EndsWith(":Forge", StringComparison.OrdinalIgnoreCase));
        if ( key == null && keyColl.Count == 1 )
            key = keyColl[0];
        return key ?? keyColl[^1]; // return the last one as CA keys should be upfront
    }

    public static X509Certificate2Collection LoadCertificateCollection(string fileName, string password, bool withPrivateKey = false)
    {
        if ( fileName == null )
            throw new ArgumentNullException(nameof(fileName));

        fileName = Environment.ExpandEnvironmentVariables(fileName);

        if ( !File.Exists(fileName) )
            throw new ApplicationException($"File=\"{fileName}\" does not exists");

        var flags = withPrivateKey
                ? X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
                : X509KeyStorageFlags.DefaultKeySet
            ;

        var keyColl = Repeat(() =>
                             {
                                 X509Certificate2Collection keyCollection;
                                 try
                                 {
                                     keyCollection = X509CertificateLoader.LoadPkcs12Collection(
                                         File.ReadAllBytes(fileName),
                                         string.IsNullOrWhiteSpace(password) ? "" : password,
                                         flags
                                     );
                                     // keyCollection.Import( 
                                     //     fileName, 
                                     //     string.IsNullOrWhiteSpace( password ) ? "" : password, 
                                     //     flags 
                                     // );
                                 }
                                 catch (Exception)
                                 {
                                     try
                                     {
                                         keyCollection = X509CertificateLoader.LoadPkcs12Collection(
                                             File.ReadAllBytes(fileName),
                                             password?.ToUpper(),
                                             flags
                                         );
                                         // keyCollection.Import(fileName, password?.ToUpper(), flags);
                                     }
                                     catch (Exception)
                                     {
                                         keyCollection = X509CertificateLoader.LoadPkcs12Collection(
                                             File.ReadAllBytes(fileName),
                                             null,
                                             flags
                                         );
                                         // keyCollection.Import(fileName, null, flags);
                                         _log?.LogWarning($"Keystore=\"{fileName}\" is not password protected");
                                     }
                                 }

                                 return keyCollection;
                             }, $"Error loading private key from File=\"{fileName}\"");
        return keyColl;
    }

    /// <summary>
    /// Decrypt text. Source is base64 form or encrypted text.
    /// </summary>
    /// <param name="encryptedText">Text to decrypt in base64</param>
    /// <param name="certificate">decryptionKey</param>
    /// <returns>Decrypted text</returns>
    private static string Decrypt(string encryptedText, X509Certificate2 certificate)
    {
        if ( string.IsNullOrWhiteSpace(encryptedText) )
            return encryptedText;
        if ( certificate == null )
            throw new ArgumentNullException(nameof(certificate));

        try
        {
            if (!certificate.HasPrivateKey)
                throw new CryptographicException($"Private key not contained within certificate {certificate.Subject}");

            byte[] decryptedBytes;
            using ( var rsa = certificate.GetRSAPrivateKey() )
            {
                decryptedBytes = rsa!.Decrypt( Convert.FromBase64String( encryptedText ), RSAEncryptionPadding.OaepSHA1 );
            }

            //    Check to make sure we decrypted the string 
            return decryptedBytes.Length == 0 ? string.Empty : Encoding.ASCII.GetString( decryptedBytes );
        }
        catch ( Exception e )
        {
            var m = $"Unable to decrypt data by {certificate.Subject}";
            _log?.LogError( m, e );
            throw new CryptographicException( m, e );
        }
    }

    /// <summary>
    /// Decrypt text. Source is base64 form or encrypted text.
    /// </summary>
    /// <param name="encryptedText">Text to decrypt in base64</param>
    /// <param name="recipientCertificate">decryptionKey</param>
    /// <returns>Decrypted text</returns>
    public static string Pkcs7Decrypt(string encryptedText, X509Certificate2 recipientCertificate)
        => Encoding.UTF8.GetString(Pkcs7DecryptBinary(encryptedText, recipientCertificate));

    /// <summary>
    /// Decrypt text. Source is base64 form or encrypted text.
    /// </summary>
    /// <param name="encryptedText">Text to decrypt in base64</param>
    /// <param name="recipientCertificate">decryptionKey</param>
    /// <returns>Decrypted text</returns>
    public static byte[] Pkcs7DecryptBinary(string encryptedText, X509Certificate2 recipientCertificate)
    {
        if ( string.IsNullOrWhiteSpace(encryptedText) )
            throw new ArgumentNullException(nameof(encryptedText));

        if ( recipientCertificate == null )
            throw new ArgumentNullException(nameof(recipientCertificate));

        try
        {
            if (!recipientCertificate.HasPrivateKey)
                throw new CryptographicException($"Private key not contained within certificate {recipientCertificate.Subject}");

            var envelopedCms = new EnvelopedCms();
            var bytes        = Convert.FromBase64String(encryptedText);
            envelopedCms.Decode(bytes);

            var recipientInfo = new X509Certificate2Collection { recipientCertificate };
            envelopedCms.Decrypt(recipientInfo);
            var decryptedBytes = envelopedCms.ContentInfo.Content;
                
            return decryptedBytes;
        }
        catch ( Exception e )
        {
            try
            {
                var m = $"Unable to decrypt data by {recipientCertificate.Subject}";
                _log?.LogError( m, e );
                throw new CryptographicException( m, e );
            }
            catch ( Exception )
            {
                const string m = "Unable to decrypt data";
                _log?.LogError( e,m );
                throw new CryptographicException( m, e );
            }
        }
    }

    /// <summary>
    /// Encrypt text and get result in base64 form.
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="certificate">Use LoadEncryptionKey to get encryptionKey</param>
    /// <param name="bytes"></param>
    /// <returns>Encrypted text in base64 form</returns>
    private static string Encrypt( string plainText, X509Certificate2 certificate, byte[]? bytes = null )
    {
        if ( certificate == null )
            throw new ArgumentNullException(nameof(certificate));

        using var rsa = certificate.GetRSAPrivateKey();
        try
        {
            bytes ??= Encoding.ASCII.GetBytes(plainText);

            var decryptedBytes = rsa!.Encrypt( bytes, RSAEncryptionPadding.OaepSHA1 );
            //    Check to make sure we decrypted the string 
            return decryptedBytes.Length == 0 ? string.Empty : Convert.ToBase64String( decryptedBytes );
        }
        catch ( Exception e )
        {
            try
            {
                var m = $"Unable to encrypt data by {certificate.Subject}";
                _log?.LogError( m, e );
                throw new CryptographicException( m, e );
            }
            catch ( Exception )
            {
                const string m = "Unable to encrypt data";
                _log?.LogError( e, m );
                throw new CryptographicException( m, e );
            }
        }
    }

    /// <summary>
    /// Encrypt text and get result in PKCS#7 form.
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="recipientCertificate">Public key container</param>
    /// <returns>Encrypted text in base64 form</returns>
    public static string Pkcs7Encrypt( string plainText, X509Certificate2 recipientCertificate )
        => Pkcs7Encrypt( Encoding.UTF8.GetBytes( plainText ), recipientCertificate );

    /// <summary>
    /// Encrypt binary data and get result in PKCS#7 form.
    /// </summary>
    /// <param name="recipientCertificate">Public key container</param>
    /// <param name="bytes"></param>
    /// <returns>Encrypted text in base64 form</returns>
    public static string Pkcs7Encrypt( byte[] bytes, X509Certificate2 recipientCertificate )
    {
        if ( recipientCertificate == null )
            throw new ArgumentNullException(nameof(recipientCertificate));

        var recipient    = new CmsRecipient(recipientCertificate);
        var contentInfo  = new ContentInfo(bytes);
        var envelopedCms = new EnvelopedCms(contentInfo);
            
        envelopedCms.Encrypt(recipient);

        return SplitBy80(Convert.ToBase64String(envelopedCms.Encode()));
    }

    private static string SplitBy80(string str, int chunkSize = 80)
    {
        var nChunks = (str.Length + chunkSize - 1) / chunkSize;
        return string.Join("\r\n", Enumerable.Range(0, nChunks)
                                             .Select(i => str.Substring(i * chunkSize, Math.Min(chunkSize, str.Length-chunkSize*i))));
    }

    [GeneratedRegex("^[ A-Za-z0-9_@\\./\\\\$%#=\\\"\\'\\`\\^\\&\\+\\-\\(\\)\\{\\}\\[\\]!~\\?\\:;<>|]+$", RegexOptions.Compiled)]
    private static partial Regex GoodPasswordRegex();

    private static void CheckGoodPassword( string decrypted )
    {
        try
        {
            if ( string.IsNullOrWhiteSpace( decrypted ) )
                _log?.LogWarning( "Decrypted password is empty" );
            else
            {
                var rx = GoodPasswordRegex();
                var m  = rx.Match( decrypted );
                if ( !m.Success )
                {
                    _log?.LogDebug( "Decrypted password does not match the 'good password regex'" );
                }
            }
        }
        catch( Exception ex )
        {
            _log?.LogWarning( "Decrypted password causes 'good password regex' to fail: " + ex.Message );
        }
    }


}