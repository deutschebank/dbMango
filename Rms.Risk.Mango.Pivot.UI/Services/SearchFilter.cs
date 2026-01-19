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

using System.Text.RegularExpressions;

namespace Rms.Risk.Mango.Pivot.UI.Services;

public class StringFilter
{
    private List<string>? _searchItems;
    private Regex?        _regex;

    public StringFilter(string searchString = "")
    {
        SearchString = searchString;
    }

    public string SearchString
    {
        get;
        set
        {
            _searchItems = null;
            _regex       = null;
            field        = value;

            if (string.IsNullOrWhiteSpace(value))
                return;

            if ( value.Contains('*') || value.Contains('?'))
            {
                var pattern = Regex.Escape(value.Trim())
                   .Replace(@"\*", ".*")
                   .Replace(@"\?", ".");

                _regex = new(pattern, RegexOptions.IgnoreCase);
            }

            if ( _regex == null)
                _searchItems = value.Trim()
                   .Split([' '], StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .ToList();
        }
    }

    public bool IsMatch(string input)
    {
        if (_regex != null)
        {
            return _regex.IsMatch(input);
        }
        if (_searchItems?.Count > 0)
        {
            return _searchItems.All(searchItem => input.Contains(searchItem, StringComparison.OrdinalIgnoreCase));
        }
        return true;
    }

    public IEnumerable<string> Filter(IEnumerable<string> inputs) => inputs.Where(IsMatch);
    public IEnumerable<T> Filter<T>(IEnumerable<T> inputs, Func<T, string> getText) => inputs.Where(x => IsMatch(getText(x)));
    public IEnumerable<T> Filter<T>(IEnumerable<T> inputs, Func<T, IEnumerable<string>> getText) => inputs.Where(x => getText(x).Any(IsMatch));
}