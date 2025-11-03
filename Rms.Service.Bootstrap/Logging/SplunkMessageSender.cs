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
using System.Net.Sockets;
using System.Text;

namespace Rms.Service.Bootstrap.Logging;

internal class SplunkMessageSender(Func<SplunkLoggerConfiguration> getOptions, ILogger? errorHandler)
{
    private const int TimeoutSec = 3;

    private readonly BlockingCollection<string> _jobs        = new();
    private readonly CancellationTokenSource    _cancelToken = new();

    private          TcpClient? _client;
    private          Thread?    _jobThread;
    private          DateTime   _connectionTime;
    private readonly Lock     _syncObj = new ();

    public void Log(string message)
    {
        try
        {
            var opt = getOptions();
            if ( opt.RemotePort <= 0 || string.IsNullOrWhiteSpace(opt.RemoteHost) )
                return;

            if (_jobThread == null)
            {
                _jobThread = new(ThreadProc) { IsBackground = true, Name = "TcpAppender" };
                _jobThread.Start();
            }

            //if the queue gets to the limit (100k) then this will drop the event
            if (_jobs.Count > 1000)
                errorHandler?.LogError("Job queue reached capacity dropping event");
            else
                _jobs.Add(message);
                
        }
        catch (Exception ex)
        {
            errorHandler?.LogError($"Unable to send logging event to {SplunkAddress}", ex);
        }
    }

    private string SplunkAddress => string.IsNullOrWhiteSpace(getOptions().RemoteHost) || getOptions().RemotePort <= 0
        ? "<No Splunk configured>"
        : $"tcp://{getOptions().RemoteHost}:{getOptions().RemotePort}";

    /// <summary>
    /// Thread proc which loops sending messages to the endpoint
    /// </summary>
    private void ThreadProc()
    {
        try
        {
            while (!_cancelToken.IsCancellationRequested)
            {
                //will block until an event is available
                var loggingEvent = _jobs.Take(_cancelToken.Token);

                while (!CheckConnection() && !_cancelToken.IsCancellationRequested)
                {
                    //keep spinning until we can reconnect, with 5 mins between attempts
                    Thread.Sleep((int)TimeSpan.FromMinutes(1).TotalMilliseconds);
                }

                if (!_cancelToken.IsCancellationRequested)
                    SendLoggingEvent(loggingEvent);
            }
        }
        catch (OperationCanceledException /*e*/)
        {
            //thrown when jobs.take is cancelled, do nothing, just shut down
        }
        catch (Exception ex)
        {
            errorHandler?.LogError($"Exception in thread proc {ex.Message}");
        }
    }

    /// <summary>
    /// Send a message via the tcp connection
    /// </summary>
    /// <param name="loggingData"></param>
    /// <returns></returns>
    private void SendLoggingEvent(string loggingData)
    {
        try
        {
            lock (_syncObj)
            { 
                if (_client is not { Connected: true }) 
                    return;
            }
            
            var buffer = Encoding.UTF8.GetBytes(loggingData);
            lock (_syncObj)
            { 
                _client.GetStream().Write(buffer, 0, buffer.Length);
            }
        }
        catch (Exception ex)
        {
            errorHandler?.LogError($"Unable to send logging event to {SplunkAddress}", ex);
        }
    }

    /// <summary>
    /// Encure the connection to the endpoint is connected
    /// Will retry 5 times if there's a failure
    /// </summary>
    /// <returns></returns>
    private bool CheckConnection() 
    { 
        lock (_syncObj)
        {
            if (_client is not { Connected: true } || DateTime.UtcNow - _connectionTime > TimeSpan.FromMinutes(1))
            {
                _client = new()
                {
                    SendTimeout = (int)TimeSpan.FromSeconds(TimeoutSec).TotalMilliseconds
                };
            }

            //try to connect to the end point, will retry 5 times, with a backoff timeout between each attempt
            //2,4,6,8,10 seconds between attempts
            var retries = 0;
            while (!_client.Connected && !_cancelToken.IsCancellationRequested && retries++ < 5)
            {
                if ( getOptions().RemotePort <= 0 || string.IsNullOrWhiteSpace(getOptions().RemoteHost))
                {
                    Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
                    continue;
                }

                try
                {
                    var result = _client.BeginConnect(getOptions().RemoteHost, getOptions().RemotePort, null, null);

                    result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(TimeoutSec));
                    // we have connected
                    _client.EndConnect(result);
                    //remember the time we connected, we will regularly reconnect to make sure splunk doesnt keeping dropping us.
                    _connectionTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    Thread.Sleep((int)TimeSpan.FromSeconds(2 * retries).TotalMilliseconds);
                    errorHandler?.LogError($"Unable to connect to {SplunkAddress} attempt={retries}", ex);
                }
            }

            if (!_client.Connected)
            {
                errorHandler?.LogError($"Failed all attempts to connect to {SplunkAddress}");
                return false;
            }
            return true;
        }
    }

}