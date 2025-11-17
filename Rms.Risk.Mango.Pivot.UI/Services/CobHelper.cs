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
﻿using System.Globalization;

namespace Rms.Risk.Mango.Pivot.UI.Services;

public static class CobHelper
{
    public const string CobFormat = "yyyy-MM-dd";

    public static DateTime ConvertToDefault(DateTimeOffset? cob) => cob?.Date ?? GetLatestCob();

    public static DateTimeOffset? ConvertStringToOffset(string? cobStr)
    {
        if ( !string.IsNullOrWhiteSpace(cobStr) &&
            (
                DateTime.TryParseExact(cobStr, "yyyy-MM-dd",          CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var res)
             || DateTime.TryParseExact(cobStr, "dd-MM-yyyy",          CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out res)
             || DateTime.TryParseExact(cobStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out res)
             || DateTime.TryParseExact(cobStr, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out res)
            )
           )
        {
            return res;
        }

        return null;
    }

    public static DateTime ConvertToDefault(string? cobStr)
    {
        var cob = ConvertStringToOffset( cobStr );
        if ( cob != null )
            return cob.Value.DateTime;
        return GetLatestCob();
    }

    public static DateTime GetLatestCob()
    {
        var timeInLondon = DateUtil.UTCToLondon( DateTime.UtcNow );

        var mostRecentCob = (timeInLondon + TimeSpan.FromHours(7)).Date - TimeSpan.FromDays(1);
        var cob = mostRecentCob;

        // ReSharper disable once PossibleInvalidOperationException
        if (cob.DayOfWeek == DayOfWeek.Sunday)
            cob = cob - TimeSpan.FromDays(2);
        if (cob.DayOfWeek == DayOfWeek.Saturday)
            cob = cob - TimeSpan.FromDays(1);

        return cob;
    }

    public static List<DateTime> GetLastNCobDates(int n)
    {
        var latestCob = GetLatestCob();
        var cobDates  = new List<DateTime>
        {
            latestCob
        };
        for ( var day = 1; day < n; day++ )
        {
            cobDates.Add(WeekdaysBackward(DateOnly.FromDateTime(latestCob), day).ToDateTime(TimeOnly.MinValue));
        }

        return cobDates;
    }

    public static bool IsWeekendDay(DateOnly date)
        => date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

    public static DateOnly WeekdaysBackward(DateOnly baseDate, int days)
    {
        while (days > 0)
        {
            baseDate = baseDate.AddDays(-1);
            if (!IsWeekendDay(baseDate))
                --days;
        }
        return baseDate;
    }
}
