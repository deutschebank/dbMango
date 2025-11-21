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
﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Rms.Risk.Mango.Pivot.UI.Services;

public static class NavigationManagerExtensions
{
    public static T? TryGetQueryString<T>(this NavigationManager navManager, string key, T? defaultValue = default)
    {
        var uri = navManager.ToAbsoluteUri(navManager.Uri);

        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue(key, out var valueFromQueryString))
        {
            if (typeof(T) == typeof(int) && int.TryParse(valueFromQueryString, out var valueAsInt))
            {
                return (T)(object)valueAsInt;
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)valueFromQueryString.ToString();
            }

            if (typeof(T) == typeof(decimal) && decimal.TryParse(valueFromQueryString, out var valueAsDecimal))
            {
                return (T)(object)valueAsDecimal;
            }
        }

        return defaultValue;
    }

    public static Dictionary<string, string> GetQueryParameters( this NavigationManager navManager )
    {
        var uri    = navManager.ToAbsoluteUri(navManager.Uri);
        var parsed = QueryHelpers.ParseQuery( uri.Query );
        var res = parsed.ToDictionary(
            x => x.Key,
            x => string.Join( ",", (IEnumerable<string>)x.Value ) 
        );

        return res;
    }

}