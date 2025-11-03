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
﻿using System.Collections.Concurrent;
using Rms.Risk.Mango.Pivot.Core;

namespace Rms.Risk.Mango.Pivot.UI.Controls;

internal class MinMaxCache : IMinMaxCache
{
    private readonly ConcurrentDictionary<string, MinMax> _minMaxCache = new();

    public void Clear() => _minMaxCache.Clear();

    public static bool IsNumeric(object? value) => value is double or int or long or uint or ulong;

    public void Update(IPivotedData data, IReadOnlyCollection<int> rows)
    {
        Clear();

        if ( data == null )
            return;

        foreach ( var (columnName, columnPosition) in data.GetColumnPositions() )
        {
            if (string.IsNullOrWhiteSpace(columnName))
                continue;

            // works for double columns only

            var r     = new MinMax();
            var found = false;
            foreach ( var row in rows )
            {
                found |= UpdateOne(data, columnPosition, row, r);
            }

            if ( found )
                _minMaxCache[columnName] = r;
        }
    }

    public void Update(IPivotedData data)
    {
        Clear();

        if ( data == null )
            return;

        foreach ( var (columnName, columnPosition) in data.GetColumnPositions() )
        {
            if (string.IsNullOrWhiteSpace(columnName))
                continue;

            // works for double columns only

            var r = new MinMax();
            var found = false;
            for( var row = 0; row < data.Count; row++ )
            {
                found |= UpdateOne(data, columnPosition, row, r);
            }

            if ( found )
                _minMaxCache[columnName] = r;
        }
    }

    private static bool UpdateOne(IPivotedData data, int col, int row, MinMax r)
    {
        var val = data.Get( col, row );
        if ( !IsNumeric(val) )
            return false;

        var d    = Convert.ToDouble(val);
        var absD = Math.Abs( d );

        if (r.MinValue > d)
            r.MinValue = d;
        if (r.MaxValue < d)
            r.MaxValue = d;

        if (r.AbsMinValue > absD)
            r.AbsMinValue = absD;
        if (r.AbsMaxValue < absD)
            r.AbsMaxValue = absD;


        r.Total    += d;
        r.AbsTotal += Math.Abs( d );

        return true;
    }

    public MinMax? TryGet( string columnName ) =>
        _minMaxCache.GetValueOrDefault(columnName);
}
