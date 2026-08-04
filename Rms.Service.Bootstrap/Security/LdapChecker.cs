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
using System.Text.RegularExpressions;
using Novell.Directory.Ldap;

namespace Rms.Service.Bootstrap.Security;

public class LdapChecker
{
    private readonly  LdapSettings _settings;

    private readonly Regex[] _ignoreAccount;

    public LdapChecker(LdapSettings settings)
    {
        _settings      = settings;
        _ignoreAccount = settings.IgnoreAccounts
            .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            .ToArray();

        Task.Run(CleanUp);
    }

    private async Task CleanUp()
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(30));
            _nestedGroups   .Clear();
            _groupMembers   .Clear();
            _userGroups     .Clear();
            _userCn         .Clear();
            _fastUserGroups .Clear();
        }
        catch (Exception)
        {
            // ignore
        }
        _ =Task.Run(CleanUp); // reschedule
    }

    private static readonly ConcurrentDictionary<string, HashSet<string>> _nestedGroups   = new();
    private static readonly ConcurrentDictionary<string, HashSet<string>> _groupMembers   = new();
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userGroups     = new();
    private static readonly ConcurrentDictionary<string, string>          _userCn         = new();
    private static readonly ConcurrentDictionary<string, HashSet<string>> _fastUserGroups = new();

    private static readonly string[] _requiredAttributes =
    [
        "dc", "o", "ou",
        "cn", "uid", "mail",
        "member", "uniqueMember", "memberOf",
        "sAMAccountName",
        "primaryGroupToken", "primaryGroupID"
    ];

    public async Task<bool> IsGroupMember(string email, string group, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(group))
            return false;

        // fast method via memberOf attribute

        if (_fastUserGroups.TryGetValue(email, out var groups))
        {
            if (groups.Contains(group))
                return true;
        }

        if (!_userCn.TryGetValue(email, out var cn))
        {
            (cn, groups)           = await DoOnLdap(email, GetCnAndGroups, token);
            _fastUserGroups[email] = groups;
            _userCn[email]         = cn;

            if (groups.Contains(group))
                return true;
        }

        // slower method via nested groups membership

        if (!_groupMembers.TryGetValue(group, out var members))
        {
            members              = await DoOnLdap(group, GetGroupMembers, token);
            _groupMembers[group] = members;
        }

        return members.Contains(cn);
    }

    public async Task<HashSet<string>> GetUserGroups(string email, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(email))
            return [];

        if (!_userGroups.TryGetValue(email, out var groups))
        {
            groups             = await DoOnLdap(email, GetGroups, token);
            _userGroups[email] = groups;
        }
        return groups;
    }


    private async Task<T> DoOnLdap<T>(string arg, Func<LdapConnection, string, CancellationToken, Task<T>> action, CancellationToken token)
    {
        using var cn = new LdapConnection();

        // connect
        var uri  = new Uri(_settings.Url);
        var port = uri.Port > 0 ? uri.Port : uri.Scheme == "ldaps" ? 636 : 389;

        cn.SecureSocketLayer = uri.Scheme == "ldaps";
        await cn.ConnectAsync(uri.Host, port, token);

        // bind with a username and password
        // this how you can verify the password of a user
        await cn.BindAsync(_settings.Username, _settings.Password, token);

        var res = await action(cn, arg, token);
        
        cn.Disconnect();

        return res;
    }

    private async Task<HashSet<string>> GetGroups(LdapConnection connection, string email, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(email))
            return [];

        var escapedEmail = EscapeLdapFilterValue(email);
        var result = await connection.SearchAsync(
            _settings.EntryPoint,
            LdapConnection.ScopeSub,
            $"(&(objectClass=person)(mail={escapedEmail}))",
            _requiredAttributes,
            false,
            token);

        var groups = new HashSet<string>();
        while (await result.HasMoreAsync(token))
        {
            var group = await result.NextAsync(token);
            var set = group.GetAttributeSet();
            if (!set.TryGetValue("memberOf", out var memberOfAttribute))
                continue;
            foreach (var attr in memberOfAttribute.StringValueArray)
            {
                var dict = Extract(attr);
                groups.Add(dict["CN"]);
            }
        }

        foreach (var group in groups.ToList())
        {
            var expanded = await GetNestedGroups(connection, group, token);
            foreach (var g in expanded)
            {
                groups.Add(g);
            }
        }

        return groups;
    }

    private async Task<(string, HashSet<string>)> GetCnAndGroups(LdapConnection connection, string email, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new("No email supplied to GetCnAndGroups");

        var escapedEmail = EscapeLdapFilterValue(email);
        var result = await connection.SearchAsync(
            _settings.EntryPoint,
            LdapConnection.ScopeSub,
            $"(&(objectClass=person)(mail={escapedEmail}))",
            _requiredAttributes,
            false, 
            token);

        var accountName = "";
        var groups = new HashSet<string>();

        while (await result.HasMoreAsync(token))
        {
            var entry      = await result.NextAsync(token);
            var aSet = entry.GetAttributeSet();
            
            if (!aSet.TryGetValue("cn", out var cnAttr))
                continue;
            
            var skip = _ignoreAccount.Any(regex => regex.IsMatch(cnAttr.StringValue));
            if (skip)
                continue;

            if (!aSet.TryGetValue("memberOf", out var memberOf)) 
                continue;

            if ( string.IsNullOrWhiteSpace(accountName) )
                accountName = cnAttr.StringValue;

            foreach (var value in memberOf.StringValues)
            {
                var dict = Extract(value);
                if ( dict.TryGetValue("CN", out var cnValue))
                    groups.Add(cnValue);
            }
        }

        if ( groups.Count == 0 )
            throw new($"Can't get CN for {email}");
        
        return (accountName, groups);
    }

    private async Task<HashSet<string>> GetGroupMembers(LdapConnection connection, string groupName, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return [];

        if (_groupMembers.TryGetValue(groupName, out var hashSet))
            return hashSet;

        var escapedGroupName = EscapeLdapFilterValue(groupName);
        var result = await connection.SearchAsync(
            _settings.EntryPoint,
            LdapConnection.ScopeSub,
            $"(&(objectClass=group)(cn={escapedGroupName}))",
            _requiredAttributes,
            false,
            token);

        var members = new HashSet<string>();
        var groups  = new HashSet<string>();

        while (await result.HasMoreAsync(token))
        {
            var entry = await result.NextAsync(token);
            var set   = entry.GetAttributeSet();
            if (!set.TryGetValue("member", out var memberAttribute))
                continue;

            foreach (var attr in memberAttribute.StringValueArray)
            {
                var dict = Extract(attr);
                if (!attr.Contains("OU=Users"))  // there are multiple OU=
                    groups.Add(dict["CN"]);
                else
                    members.Add(dict["CN"]);
            }
        }

        // add members of the nested groups
        foreach (var group in groups)
        {
            var gm = await GetGroupMembers(connection, group, token);
            foreach (var groupMember in gm)
            {
                members.Add(groupMember);
            }
        }

        _groupMembers[groupName] = members;
        return members;
    }

    private async Task<HashSet<string>> GetNestedGroups(LdapConnection connection, string groupName, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return [];

        if (_nestedGroups.TryGetValue(groupName, out var hashSet))
            return hashSet;

        var escapedGroupName = EscapeLdapFilterValue(groupName);
        var result = await connection.SearchAsync(
            _settings.EntryPoint,
            LdapConnection.ScopeSub,
            $"(&(objectClass=group)(cn={escapedGroupName}))",
            _requiredAttributes,
            false, 
            token);

        var nestedGroups = new HashSet<string>();
        var groups       = new HashSet<string>();

        while (await result.HasMoreAsync(token))
        {
            var entry = await result.NextAsync(token);
            var set   = entry.GetAttributeSet("member");
            if (!set.TryGetValue("member", out var memberAttribute))
                continue;

            foreach (var attr in memberAttribute.StringValueArray)
            {
                var dict = Extract(attr);
                if (!attr.Contains("OU=Users"))  // there are multiple OU=
                    nestedGroups.Add(dict["CN"]);
                groups.Add(dict["CN"]);
            }
        }

        // add members of the nested groups
        foreach (var group in nestedGroups)
        {
            var gm = await GetNestedGroups(connection, group, token);
            foreach (var groupMember in gm)
            {
                groups.Add(groupMember);
            }
        }

        _nestedGroups[groupName] = groups;
        return groups;
    }

    private static string EscapeLdapFilterValue(string value)
    {
        return value
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }

    private static Dictionary<string,string> Extract(string attr)
    {
        var s   = attr.Split(",", StringSplitOptions.RemoveEmptyEntries);
        var res = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in s)
        {
            var pos = item.IndexOf('=');
            if ( pos < 0 )
                continue;
            res[item[..pos]] = item[(pos + 1)..];
        }

        return res;
    }
}