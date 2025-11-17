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
namespace Rms.Risk.Mango.Pivot.Core;

public interface IPivotedData
{

    public string Id { get; }

    /// <summary>
    /// name : column
    /// </summary>
    IReadOnlyCollection<string> Headers { get; }

    /// <summary>
    /// Get column positions. Beware that duplicate column will be missing.
    /// Only first unique occurrence of column name will be counted.
    /// </summary>
    Dictionary<string, int> GetColumnPositions()
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // this is safer than calling ToDictionary as it handles duplicate headers
        foreach ( var (key, pos) in Headers.Select((x, i) => (Key: x, Value: i)) )
        {
            dict.TryAdd(key, pos);
        }

        return dict;
    }

    /// <summary>
    /// Number of rows
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Get element at (col, row)
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    object? Get( int col, int row );

    Type GetColumnType( int col );

    /// <summary>
    /// Create filtered copy of data. Filter func must return true if you want row to remain in the output copy.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    IPivotedData Filter(Func<int, bool> filter);
}