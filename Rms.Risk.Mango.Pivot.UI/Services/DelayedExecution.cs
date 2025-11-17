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
﻿using System.Reflection;
using log4net;

namespace Rms.Risk.Mango.Pivot.UI.Services;

/// <summary>
/// Execute action only after a delay. Any subsequent calls will cancel the previous action and restart the timer.
/// </summary>
public class DelayedExecution
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once ConvertToConstant.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public static int DefaultDelayMSec = 3000;

    private readonly TimeSpan                 _delay;
    private readonly Lock                     _syncObject = new();
    private          CancellationTokenSource? _tokenSource;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="delay"></param>
    public DelayedExecution( TimeSpan delay = default)
    {
        if ( delay == TimeSpan.Zero )
            delay = TimeSpan.FromMilliseconds( DefaultDelayMSec );
        _delay = delay;
    }

    /// <summary>
    /// Execute action only after a delay. Any subsequent calls will cancel the previous action and restart the timer.
    /// </summary>
    public void Run( Action<CancellationToken> action, string description, CancellationToken token = default )
    {
        lock ( _syncObject )
        {
            if ( _tokenSource != null )
            {
                _tokenSource.Cancel();
                _tokenSource = null;
            }

            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            var newToken = _tokenSource.Token;
            Task.Run( async () =>
            {
                try
                {
                    await Task.Delay( _delay, newToken );
                    if ( newToken.IsCancellationRequested )
                        return;
                    action(newToken);
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    _log.Error($"Delayed execution Task=\"{description}\" failed with: {ex.Message}", ex);
                }
            }, newToken );
        }
    }

    /// <summary>
    /// Execute action only after a delay. Any subsequent calls will cancel the previous action and restart the timer.
    /// </summary>
    public void Run( Func<CancellationToken, Task> action, string description, CancellationToken token = default )
    {
        lock ( _syncObject )
        {
            if ( _tokenSource != null )
            {
                _tokenSource.Cancel();
                _tokenSource = null;
            }

            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            var newToken = _tokenSource.Token;
            Task.Run( async () =>
            {
                try
                {
                    await Task.Delay(_delay, newToken);
                    if (newToken.IsCancellationRequested)
                        return;
                    await action(newToken);
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    _log.Error($"Delayed execution Task=\"{description}\" failed with: {ex.Message}", ex);
                }
            }, newToken );
        }
    }

}