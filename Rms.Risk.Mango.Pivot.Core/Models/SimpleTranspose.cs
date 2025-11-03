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
namespace Rms.Risk.Mango.Pivot.Core.Models;

public static class SimpleTranspose
{

    public static IPivotedData? Transpose(IPivotedData? src)
    {
        if (   src == null
         || src.Count < 1
         || src.Headers.Count < 1
         || src is { Count: 1, Headers.Count: 1 }
           )
        {
            return src;
        }

        var columnHeaders = GetColHeaders(src);
        if ( columnHeaders == null || columnHeaders.Count != src.Count+1 )
            return src;

        var headers = columnHeaders
                     .OrderBy(x => x.Value)
                     .Select(x => x.Key)
                     .ToArray();

        var dest = new ArrayBasedPivotData(headers);

        var srcHeaders = src.Headers;


        // row and col corresponds to dest
        for ( var row = 0; row < srcHeaders.Count-1; row++ )
        {
            var r = new object?[src.Count+1];
            // first column is src.Headers
            r[0] = srcHeaders.Skip(row+1).First(); // [row+1]
            // the rest (1st src column becomes dest row header)
            for ( var col = 1; col < headers.Length; col++ )
                r[col] = src.Get(row+1, col-1); // note reverse row/col
            dest.Add(r);
        }

        // double check
        if ( dest.Count != src.Headers.Count-1
         || dest.Headers.Count != src.Count+1 )
        {
            return src;
        }

        return dest;
    }

    private static Dictionary<string, int>? GetColHeaders(IPivotedData src)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [src.Headers.First() ?? string.Empty] = 0
        };

        for ( var i = 0; i < src.Count; i++ )
        {
            var key = src.Get(0, i)?.ToString() ?? string.Empty;
            if ( key == string.Empty )
                key = "-";

            if ( !headers.TryAdd(key, i+1) )
                return null;
        }

        return headers;
    }
}