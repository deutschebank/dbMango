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
﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Authorization;
using Rms.Service.Bootstrap.Security;
using Rms.Risk.Mango.Services;

namespace Rms.Risk.Mango.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DownloadController(
    ISingleUseTokenService _singleUseTokenService,
    IPasswordManager _passwordManager
    ) : ControllerBase
{
    private const string DownloadDataUrl = "data/[TOKEN]/[SECRET]?fileName=[FILE_NAME]";

    public static async Task<string> GetDownloadLink(
        ITempFileStorage storage, 
        IPasswordManager passwordManager, 
        ISingleUseTokenService singleUseTokenService,
        Func<string, Task> writeDataToFile, 
        string destFileName
        )
    {
        var fileName = storage.GetTempFileName("CsvData");
        await writeDataToFile(fileName);

        return GetDownloadLink(passwordManager, singleUseTokenService, fileName, destFileName);
    }

    public static string GetDownloadLink(
        IPasswordManager       passwordManager, 
        ISingleUseTokenService singleUseTokenService,
        string                 srcFilePath, 
        string                 destFileName
    )
    {
        var fileName = srcFilePath;

        var secret   = passwordManager.EncryptPassword(fileName);
        var url      = DownloadDataUrl
               .Replace("[TOKEN]",     singleUseTokenService.GetSingleUseToken())
               .Replace("[SECRET]",    Uri.EscapeDataString(secret))
               .Replace("[FILE_NAME]", Uri.EscapeDataString(destFileName))
            ;

        return $"api/download/{url}";
    }


    public class TempFileStreamResult(string fileName, string mime) : PhysicalFileResult(fileName, mime)
    {
        public required string TempFileName { get; init; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            try {
                await base.ExecuteResultAsync(context);
            }
            finally {
                System.IO.File.Delete(TempFileName);
            }
        }
    }

    [HttpGet( "data/{token}/{secret}" )]
    public Task<PhysicalFileResult> GetCsv( 
        [FromRoute] string token,
        [FromRoute] string secret,
        [FromQuery] string? fileName
    )
    {
        CheckToken( token );

        var tempFileName = _passwordManager.DecryptPassword(Uri.UnescapeDataString(secret));
        if ( !System.IO.File.Exists(tempFileName) )
            throw new ApplicationException("Invalid secret");

        if ( !new FileExtensionContentTypeProvider().TryGetContentType(fileName ?? "data.bin", out var contentType) )
            contentType = "application/octet-stream";

        return Task.FromResult((PhysicalFileResult)new TempFileStreamResult( tempFileName, contentType )
        {
            FileDownloadName = fileName ?? "data.csv",
            TempFileName     = tempFileName
        });
    }

    private void CheckToken( string token )
    {
        if ( _singleUseTokenService.CheckSingleUseToken( token ) )
            return;

        throw new ApplicationException( "Download token is invalid or expired" );
    }


}