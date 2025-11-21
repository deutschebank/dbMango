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
﻿
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System.Globalization;

namespace Rms.Risk.Mango.Pivot.UI.Services;

public static class Night
{
    public const string Background             = "#27394f";
    public const string BackgroundLight        = "#2e4057";
    public const string Headers                = "#76c5fd";
    public const string Border                 = "#556880";
    public const string Link                   = "#45cbfd";
    public const string LinkHover              = "#ffffff";
    public const string ButtonBg               = "#1966b5";

    public const string blue                   = "#1d5dc7";
    public const string indigo                 = "#6610f2";
    public const string purple                 = "#6f42c1";
    public const string pink                   = "#e83e8c";
    public const string red                    = "#840808";
    public const string orange                 = "#904918";
    public const string yellow                 = "#904918";
    public const string green                  = "#015402";
    public const string teal                   = "#147285";
    public const string cyan                   = "#2155a8";
    public const string white                  = "#1d2a3b";
    public const string gray                   = "#09121f";
    public const string gray_dark              = "#1a2633";
    public const string primary                = "#1d5dc7";
    public const string secondary              = "#586d85";
    public const string success                = "#016603";
    public const string info                   = "#138397";
    public const string warning                = "#994d15";
    public const string danger                 = "#a10b0b";
    public const string light                  = "#FFFFFF";
    public const string dark                   = "#121c29";
    public const string breakpoint_xs          = "0";
    public const string breakpoint_sm          = "576px";
    public const string breakpoint_md          = "768px";
    public const string breakpoint_lg          = "992px";
    public const string breakpoint_xl          = "1200px";
    public const string font_family_sans_serif = "Arial, sans_serif, \"Apple Color Emoji\", \"Segoe UI Emoji\", \"Segoe UI Symbol\"";
    public const string font_family_monospace  = "SFMono-Regular, Menlo, Monaco, Consolas, \"Liberation Mono\", \"Courier New\", monospace\"";

    /// <summary>
    /// Produces a string of the form 'rgba(r, g, b, alpha)' with random values for rgb and alpha > 0.3
    /// </summary>
    /// <returns></returns>
    public static string RandomColorString()
    {
        var random = new Random();

        var r = 1 + random.Next(byte.MaxValue);
        var g = 1 + random.Next(byte.MaxValue);
        var b = 1 + random.Next(byte.MaxValue);
        var a = 0.3 + random.NextDouble() * 0.7;
        
        if (a > 1.0)
            a = 1.0;
        if (a < 0.3)
            a = 0.3;

        return $"rgba({r}, {g}, {b}, {a.ToString(CultureInfo.InvariantCulture)})";
    }
}
