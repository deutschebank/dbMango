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
using System.Text;

namespace Rms.Risk.Mango.Pivot.Core;

public static class PivotedDataHelpers
{
    public static string CopyToCsv(this IPivotedData data)
    {
        var csv = new StringBuilder(1024);

        // ordered headers
        var headers = data.Headers;

        // write header
        csv.AppendLine(string.Join(",", headers));

        var sb = new StringBuilder(1024);
        for (var i = 0; i < data.Count; i++)
        {
            sb.Clear();
            for (var j = 0; j < headers.Count; j++)
            {
                if (j > 0)
                    sb.Append(',');
                sb.Append(ShieldElement(data.Get(j, i) ?? ""));
            }

            csv.AppendLine(sb.ToString());
        }

        return csv.ToString();
    }

    public static int WriteToCsv( this IPivotedData data, string fileName, bool writeHeader=true, bool append = false, Dictionary<string, string>? formats = null )
    {
        using var writer = new StreamWriter( fileName, append );
        var count = WriteToCsv(data, writer, writeHeader, formats);
        writer.Close();

        return count;
    }

    public static int WriteToCsv(this IPivotedData data, TextWriter writer, bool writeHeader = true, Dictionary<string, string>? formats = null)
    {
        var       count  = 0;
        // ordered headers
        var headers = data.Headers;

        var colPositions    = data.GetColumnPositions();
        var positionFormats = new string[headers.Count];
        if (formats != null)
        {
            foreach (var formattedColumn in formats.Keys)
            {
                if (colPositions.TryGetValue(formattedColumn, out var pos))
                    positionFormats[pos] = formats[formattedColumn];
            }
        }

        // write header
        if (writeHeader)
            writer.WriteLine( string.Join( ",", headers ) );

        var sb = new StringBuilder( 1024 );
        for ( var i = 0; i < data.Count; i++ )
        {
            sb.Clear();
            for ( var j = 0; j < headers.Count; j++ )
            {
                if ( j > 0 )
                    sb.Append( ',' );
                sb.Append( ShieldElement( data.Get(j, i) ?? "", positionFormats[j] ) );
            }

            writer.WriteLine( sb.ToString() );
            count += 1;
        }

        return count;
    }

    private static string ShieldElement(object e, string? format = null)
    {
        switch ( e )
        {
            case double d:
                return d.ToString(format);

            case int:
            case long:
            case bool:
                return e.ToString() ?? "";

//                case DateTime d:
//                    return "\"" + d.ToLocalTime() + "\"";

            default:
                return "\"" + e + "\"";
        }
    }

}