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

using System.Reflection;
using log4net;

namespace Rms.Risk.Mango.Services;

public class ConnectedUserList : IConnectedUserList
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    // ReSharper disable FieldCanBeMadeReadOnly.Global
    public static int UsersReportingIntervalMinutes = 60;
    // ReSharper restore FieldCanBeMadeReadOnly.Global

    private readonly List<IConnectedUser>  _users       = [];
    private readonly Dictionary<string, bool> _activeUsers = [];

    public ConnectedUserList()
    {
        Task.Run(ReportingLoop);
    }

    private async Task ReportingLoop()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(UsersReportingIntervalMinutes));

                var users = string.Join("\n\t", _activeUsers.Keys);
                _activeUsers.Clear();
                _log.Debug($"Active users within last hour interval:\n\t{users}");
            }
            catch (Exception)
            {
                // ignore
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public IReadOnlyCollection<IConnectedUser> Users
    {
        get
        {
            lock (_users)
            {
                return _users;
            }
        }
    }

    public void Add(IConnectedUser user)
    {
        lock (_users)
        {
            _users.Add(user);
            _activeUsers[user.Name] = true;
        }
    }

    public void Remove(IConnectedUser user)
    {
        lock (_users)
            _users.Remove(user);
    }
}