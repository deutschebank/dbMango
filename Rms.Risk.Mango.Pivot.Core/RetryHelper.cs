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
﻿using log4net;
using System.Reflection;

namespace Rms.Risk.Mango.Pivot.Core;

/// <summary>
/// Utility class used to run a method using a retry strategy
/// IF the method throws, the utility waits for a period of time and reruns it again, for a specific max number of times
/// </summary>
internal static class RetryHelper
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    public static void DoWithRetries( Action doWhat, int retries, TimeSpan delay, Action<int, int, Exception>? logFunc = null)
    {
        logFunc ??= DefaultLogFunc;

        Exception? firstException = null;

        for ( var i = 0; i < retries; i++ )
        {
            try
            {
                doWhat();
                return;
            }
            catch ( Exception e )
            {
                logFunc(i, retries, e);

                firstException ??= e;
            }

            if ( i == retries - 1 )
                break; // If this is the last iteration, don't wait

            Thread.Sleep( delay );
        }

        throw new ApplicationException($"{firstException?.Message} (after {retries} retries)", firstException);
    }

    public static T DoWithRetries<T>(Func<T> doWhat, int retries, TimeSpan delay, Action? resetFunc = null, Action<int, int, Exception>? logFunc = null)
    {
        logFunc ??= DefaultLogFunc;

        Exception? firstException = null;
            
        for (var i = 0; i < retries; i++)
        {
            try
            {
                return doWhat();
            }
            catch (Exception e)
            {
                logFunc(i, retries, e);

                firstException ??= e;
            }

            resetFunc?.Invoke();

            if ( i == retries - 1 )
                break; // If this is the last iteration, don't wait

            Thread.Sleep(delay);
        }

        throw new ApplicationException($"{firstException?.Message} (after {retries} retries)", firstException);
    }

    public static async Task<T> DoWithRetriesAsync<T>(
        Func<Task<T>>                doAsyncWhat,
        int                          retries,
        TimeSpan                     delay,
        Action?                      resetFunc = null,
        Action<int, int, Exception>? logFunc   = null,
        CancellationToken            token = default)
    {
        logFunc ??= DefaultLogFunc;

        Exception? firstException = null;

        for (var i = 0; i < retries; i++)
        {
            try
            {
                return await doAsyncWhat();
            }
            catch (Exception e)
            {
                logFunc(i, retries, e);

                firstException ??= e;
            }
            
            resetFunc?.Invoke();
            await Task.Delay(delay, token);
        }

        if (firstException != null)
            throw firstException;
        throw new ApplicationException("Retries exhausted");
    }

    public static async Task DoWithRetriesAsync(
        Func<Task>                   doAsyncWhat,
        int                          retries,
        TimeSpan                     delay,
        Action?                      resetFunc          = null,
        Action<int, int, Exception>? logFunc            = null,
        string?                      exceptionSubstring = null,
        CancellationToken            token = default)

    {
        logFunc ??= DefaultLogFunc;

        Exception? firstException = null;

        for (var i = 0; i < retries; i++)
        {
            try
            {
                await doAsyncWhat();
                return;
            }
            catch (Exception e)
            {
                if ( exceptionSubstring != null && !e.Message.Contains(exceptionSubstring, StringComparison.OrdinalIgnoreCase) )
                {
                    _log.Warn($"Exception does not contain substring {exceptionSubstring} Not retrying. {e.Message}");
                    throw;
                }

                logFunc(i, retries, e);

                firstException ??= e;
            }

            resetFunc?.Invoke();
            await Task.Delay(delay, token);
        }

        if (firstException != null)
            throw firstException;
    }

    /// <summary>
    /// This is the default logger callback that will log failed attempts, its recommended that the user supplies their own so
    /// that they can log more contextual information about the operation
    /// </summary>
    /// <param name="iteration"></param>
    /// <param name="maxRetries"></param>
    /// <param name="e"></param>
    private static void DefaultLogFunc(int iteration, int maxRetries, Exception e)
    {
        if (iteration < maxRetries - 1)
            _log.Warn($"Call failed, retrying  RetriesLeft={maxRetries - iteration - 1}", e);
        else
            _log.Error("Call failed, retries exhausted", e);
    }

}