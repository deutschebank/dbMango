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
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;

namespace Rms.Risk.Mango.Pivot.Core.Models;

internal class ColorConverter
{
    private static readonly ConcurrentDictionary<string, Color> _colorConverter = new();

    public static Color ConvertFromString( string colorStr )
    {
        if ( _colorConverter.TryGetValue( colorStr, out var color ) )
            return color;

        if ( colorStr.StartsWith( "#" ) )
        {
            color = Color.FromArgb( int.Parse( colorStr[1..], NumberStyles.HexNumber ) );
            _colorConverter.TryAdd( colorStr, color );
            return color;
        }

        color = Color.FromName( colorStr );
        if ( color.IsEmpty )
            color = Color.DimGray;
        _colorConverter.TryAdd( colorStr, color );
        return color;
    }
}