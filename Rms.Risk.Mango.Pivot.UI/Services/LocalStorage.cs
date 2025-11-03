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
﻿using Microsoft.JSInterop;

namespace Rms.Risk.Mango.Pivot.UI.Services;

public static class LocalStorage
{
    public static async Task<string?> LoadFromLocalStorage(this IJSRuntime runtime, string dataName, CancellationToken token = default)
    {
        try
        {
            var text = await runtime.InvokeAsync<string>("localStorage.getItem", token, dataName);
            return text;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task<T?> LoadFromLocalStorage<T>(this IJSRuntime runtime, string dataName, CancellationToken token = default)
        where T : class
    {
        var json = await runtime.LoadFromLocalStorage(dataName, token);
        if ( string.IsNullOrWhiteSpace(json) ) 
            return null;
        var t = JsonUtils.FromJson<T>(json);
        return t;
    }


    public static async Task SaveToLocalStorage(this IJSRuntime runtime, string dataName, string data, CancellationToken token = default)
    {
        try
        {
            await runtime.InvokeAsync<string>("localStorage.setItem", token, dataName, data);
        }
        catch (Exception)
        {
            // ignore
        }
    }

    public static async Task SaveToLocalStorage<T>(this IJSRuntime runtime, string dataName, T? data, CancellationToken token = default)
    {
        if ( data == null )
        {
            await runtime.SaveToLocalStorage(dataName, "", token);
            return;
        }

        var json = JsonUtils.ToJson(data, new () { WriteIndented = true } );
        await runtime.SaveToLocalStorage(dataName, json, token);
    }
}