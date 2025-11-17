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
﻿using System.Diagnostics;

namespace Rms.Risk.Mango.Pivot.UI.Services;

public class DateUtil
{
    public static int DateToIntDate(DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var tokens  = dateStr.Split('-');
        var joined  = tokens[0] + tokens[1] + tokens[2];
        return int.Parse(joined);
    }

    public static readonly DateTime UnixEpoch = new(
        1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static DateTime FromMillisecondsSinceUnixEpoch(long milliseconds) => UnixEpoch.AddMilliseconds(milliseconds);

    public static ulong ToMillisecondsSinceUnixEpoch(DateTime dateTimeUtc) => (ulong)(dateTimeUtc - UnixEpoch).TotalMilliseconds;

    public static readonly DateTime LocalEpoch = new(
        1970, 1, 1, 0, 0, 0, DateTimeKind.Local);

    public static DateTime FromMillisecondsSinceLocalEpoch(long milliseconds) => LocalEpoch.AddMilliseconds(milliseconds);

    public static long ToMillisecondsSinceLocalEpoch(DateTime dateTime) => (long)(dateTime - LocalEpoch).TotalMilliseconds;

    private static readonly TimeZoneInfo _londonTzInfo = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

    public static DateTime LondonToUTC( DateTime timeInLondonTz )
    {
        Debug.Assert( timeInLondonTz.Kind == DateTimeKind.Unspecified );
        return TimeZoneInfo.ConvertTimeToUtc( timeInLondonTz, _londonTzInfo );
    }

    public static DateTime UTCToLondon( DateTime utcTime )
    {
        Debug.Assert( utcTime.Kind == DateTimeKind.Utc );
            
        var timeInLondonTz = TimeZoneInfo.ConvertTimeFromUtc( utcTime, _londonTzInfo );
        return timeInLondonTz;
    }

    /// <summary>
    /// Round down to the nearest second
    /// </summary>
    public static DateTime RoundTime( DateTime d ) => new(d.Year,d.Month,d.Day,d.Hour,d.Minute,d.Second, d.Kind);

}