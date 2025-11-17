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
using Rms.Risk.Mango.Controllers;
using Rms.Risk.Mango.Pivot.UI.Services;
using Rms.Service.Bootstrap.Security;
using System.Reflection;

namespace Rms.Risk.Mango.Services;

public class PivotSharingService(
    ITempFileStorage tempFileStorage,
    IPasswordManager passwordManager,
    ISingleUseTokenService singleUseTokenService
    ) : IPivotSharingService
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    public async Task<string> ExportToCsv(string destFileName, string fileContents)
    {
        var url = await DownloadController.GetDownloadLink(
            tempFileStorage,
            passwordManager,
            singleUseTokenService,
            async fileName => await File.WriteAllTextAsync(fileName, fileContents),
            destFileName
        );

        return url;
    }

    public async Task<string> SharePivot(SharedPivotDef def)
    {
        // Serialize SharedPivotDef to JSON using Newtonsoft.Json
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(def);
        
        var guid = Guid.NewGuid().ToString("D");

        var fileName = Path.Combine(tempFileStorage.TempFolder, guid + ".json");
        await File.WriteAllTextAsync(fileName, json);

        return guid;
    }

    public async Task<SharedPivotDef?> GetSharedPivot(string guid)
    {

        var fileName = Path.Combine(tempFileStorage.TempFolder, FileUtils.Shield(guid).Replace("..", ".") + ".json");
        if (!File.Exists(fileName))
            return null;

        var json = await File.ReadAllTextAsync(fileName);

        try
        {
            // Deserialize JSON to SharedPivotDef using Newtonsoft.Json
            var def = Newtonsoft.Json.JsonConvert.DeserializeObject<SharedPivotDef>(json);
            return def;
        }
        catch (Exception ex)
        {
            // Log the error if needed
            _log.Error($"Error deserializing SharedPivotDef: {ex.Message}");
            return null;
        }
    }
}