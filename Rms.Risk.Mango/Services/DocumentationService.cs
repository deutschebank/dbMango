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
﻿using log4net;
using Microsoft.Extensions.Options;
using Rms.Service.Bootstrap.Security;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;

namespace Rms.Risk.Mango.Services;

/// <summary>
/// Provides functionality for retrieving and caching documentation for MongoDB commands.
/// </summary>
/// <remarks>
/// This service fetches documentation for MongoDB commands from a remote source, caches the results, 
/// and handles gzip-compressed files. It ensures that documentation is retrieved efficiently and avoids  redundant
/// network calls by caching both existing and non-existing documentation.
/// </remarks>
public class DocumentationService : IDocumentationService
{
    private readonly        IHttpClientFactory    _factory;
    private static readonly ILog                  _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    private readonly        DbMangoSettings _dbMangoSettings;
    private readonly        string          _tempFolderPath;

    private readonly ConcurrentDictionary<string, string?> _documentationCache = new(StringComparer.OrdinalIgnoreCase);

    public DocumentationService(
        IOptions<DbMangoSettings> dbMangoSettings, 
        IHttpClientFactory factory
        )
    {
        _factory                   = factory;
        _dbMangoSettings           = dbMangoSettings.Value;

        _tempFolderPath = Path.Combine(Path.GetTempPath(), "MongoDbDocumentation");
        if (!Directory.Exists(_tempFolderPath))
        {
            Directory.CreateDirectory(_tempFolderPath);
        }
    }

    public async Task<string?> TryGetHint(string commandName, CancellationToken token = default)
    {
        try
        {
            if ( string.IsNullOrWhiteSpace(_dbMangoSettings.MongoDbDocUrl))
                return null;

            commandName = commandName.Trim();
            if ( commandName.Length < 4)
                return null;

            if ( _documentationCache.TryGetValue(commandName, out var documentation)) 
                return documentation;

            documentation = await LoadCommandDocumentation(commandName, token);
            _log.Info( !string.IsNullOrWhiteSpace(documentation)
                ? $"Loaded documentation for command '{commandName}'" 
                : $"No documentation found for command '{commandName}'"
                );

            _documentationCache[commandName] = documentation; // even if empty. this prevents re-fetching of non-existing documentation
            return documentation;
        }
        catch (Exception ex)
        {
            _log.Warn($"Failed to load documentation for command '{commandName}': {ex.Message}", ex);
        }
        return null;
    }

    public async Task<string?> TryGetMarkdown(string commandName, CancellationToken token = default)
    {
        try
        {
            if ( string.IsNullOrWhiteSpace(_dbMangoSettings.MongoDbDocUrl))
                return null;

            commandName = commandName.Trim();
            if ( commandName.Length < 4)
                return null;

            var lines = await DownloadDocumentation(commandName, token);
            if ( lines.Length == 0 )
                return null;

            var converter = new RstToMarkdownConverter();
            var markdown = converter.Convert(lines);
            return markdown;
        }
        catch (Exception ex)
        {
            _log.Warn($"Failed to load documentation for command '{commandName}': {ex.Message}", ex);
        }

        return null;
    }

    private string GetMongoDbDocUrl(string commandName) => $"{_dbMangoSettings.MongoDbDocUrl}{commandName}.txt";

    private async Task<string?> LoadCommandDocumentation(string commandName, CancellationToken token)
    {
        var lines = await DownloadDocumentation(commandName, token);
        if ( lines.Length == 0 )
            return null;

        var syntaxIndex = Array.FindIndex(lines, line => line.Trim() == "Syntax");
        if (syntaxIndex == -1)
        {
            throw new InvalidDataException("Syntax section not found in documentation.");
        }

        var codeBlockIndex = Array.FindIndex(lines, syntaxIndex, line => line.Trim() == ".. code-block:: javascript");
        if (codeBlockIndex == -1)
        {
            throw new InvalidDataException("Code block section not found in documentation.");
        }

        var codeBlock = new List<string>();
        for (var i = codeBlockIndex + 1; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("  ") && !String.IsNullOrWhiteSpace(lines[i]))
                break;

            codeBlock.Add(lines[i]);
        }

        return string.Join(Environment.NewLine, codeBlock);
    }

    private async Task<string[]> DownloadDocumentation(string commandName, CancellationToken token)
    {
        var mongoDbDocUrl = _dbMangoSettings.MongoDbDocUrl;
        if ( string.IsNullOrWhiteSpace(mongoDbDocUrl))
            return [];

        if ( mongoDbDocUrl.EndsWith(".zip"))
            return LoadFromZip(commandName, token);

        return await LoadFromWeb(commandName, token);
    }

    private string[] LoadFromZip(string commandName, CancellationToken token)
    {
        if ( string.IsNullOrWhiteSpace(_dbMangoSettings.MongoDbDocUrl) || 
             !_dbMangoSettings.MongoDbDocUrl.EndsWith(".zip"))
        {
            return [];
        }

        var zipFilePath = _dbMangoSettings.MongoDbDocUrl;
        if (!File.Exists(zipFilePath))
            zipFilePath = Path.Combine(AppContext.BaseDirectory, _dbMangoSettings.MongoDbDocUrl);

        if (!File.Exists(zipFilePath))
            return [];

        using var zipArchive = ZipFile.OpenRead(zipFilePath);
        var entry = zipArchive.GetEntry($"{commandName}.txt");
        if (entry == null)
            return [];

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        return content.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    private async Task<string[]> LoadFromWeb(string commandName, CancellationToken token)
    {
        var url      = GetMongoDbDocUrl(commandName);
        var filePath = Path.Combine(_tempFolderPath, $"{commandName}.txt.gz");

        if (!File.Exists(filePath))
        {
            using var httpClient = CreateClient();
            await DownloadFileFromUrlAsync( httpClient, url, filePath, token);
        }
        else
            _log.Debug($"Documentation file already exists: {filePath}");

        var lines = await ReadGzipCompressedFileAsync(filePath, token);
        return lines;
    }

    private static async Task DownloadFileFromUrlAsync(HttpClient httpClient, string url, string destinationFilePath, CancellationToken token)
    {
        _log.Debug($"Downloading file from URL: {url}");

        var response = await httpClient.GetAsync(url, token);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to download file from URL: {url}. Status code: {response.StatusCode}");

        token.ThrowIfCancellationRequested();

        var fileContent = await response.Content.ReadAsStringAsync(token);
        if (File.Exists(destinationFilePath))
            return;

        await WriteFileGzipCompressedAsync(destinationFilePath, fileContent, token);
    }

    private HttpClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_dbMangoSettings.MongoDbDocProxyUrl))
            return _factory.CreateClient();

        var handler   = CertificateHelper.ConfigureHttpsHandler("MongoDBDoc");
        handler.Proxy = new WebProxy(_dbMangoSettings.MongoDbDocProxyUrl);

        return new (handler);
    }


    private static async Task WriteFileGzipCompressedAsync(string destinationFilePath, string fileContent, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
        var bytes = Encoding.UTF8.GetBytes(fileContent);
        await gzipStream.WriteAsync(bytes, token);
    }
    private static async Task<string[]> ReadGzipCompressedFileAsync(string filePath, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var streamReader = new StreamReader(gzipStream);

        var lines = new List<string>();
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var line = await streamReader.ReadLineAsync(token);
            if (line is null)
            {
                break;
            }

            lines.Add(line);
        }

        return lines.ToArray();
    }

}