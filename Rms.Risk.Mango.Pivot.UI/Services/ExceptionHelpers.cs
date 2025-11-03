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
﻿using System.Text;
using System.Text.RegularExpressions;

namespace Rms.Risk.Mango.Pivot.UI.Services;

public static class ExceptionHelpers
{
    public static string ToMarkdown(this Exception ex)
    {
        var sb = new StringBuilder();
        Append(ex, sb);
        return sb.ToString();
    }

    private static readonly Regex _stackTraceRegex = new(
        @"^\s*at\s+(?<ClassName>[\w\.']+)\.(?<MethodName>[\w`]+)(?<Args>(\[[^)]*\])?(\([^)]*\))?)(\s+in\s+(?<FileName>.*)\:line\s+(?<LineNumber>\d+))?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private record ResultRec(string ClassName, string MethodName, string Args, string? FileName, string? LineNumber);

    private static ResultRec? ExtractStackTraceInfo(string stackTraceLine)
    {
        var match = _stackTraceRegex.Match(stackTraceLine);
        if (match.Success)
        {
            var className  = match.Groups["ClassName"].Value;
            var methodName = match.Groups["MethodName"].Value;
            var args = match.Groups["Args"].Value;
            var fileName   = match.Groups["FileName"].Value;
            var lineNumber = match.Groups["LineNumber"].Value;
            return new (className, methodName, args, fileName, lineNumber);
        }
        return null;
    }

    private static void Append(Exception ex, StringBuilder sb)
    {
        while (true)
        {
            if (ex is AggregateException aggEx)
            {
                foreach (var inner in aggEx.InnerExceptions)
                {
                    Append(inner, sb);
                }
            }
            else
            {
                sb.AppendLine($"### {ex.Message}");
                sb.AppendLine($"#### {ex.GetType()}");

                var stack = (ex.StackTrace ?? "")
                    .Replace("`", "'")
                    .Replace("\r", "")
                    .Split("\n", StringSplitOptions.RemoveEmptyEntries);

                foreach( var line in stack)
                {
                    AppendStackLine(line, sb);
                }
            }

            if (ex.InnerException != null)
            {
                ex = ex.InnerException;
                continue;
            }

            break;
        }
    }

    public static void AppendStackLine(string line, StringBuilder sb)
    {
        var res = ExtractStackTraceInfo(line);
        if (res == null)
        {
            sb.AppendLine($"* {line}");
            return;
        }

        if (string.IsNullOrWhiteSpace(res.FileName))
            sb.AppendLine($"* *{res.ClassName}.{res.MethodName}{res.Args}*");
        else
            sb.AppendLine($"* *{res.ClassName}*.`{res.MethodName}`*{res.Args}* `{res.FileName}` {res.LineNumber}");
    }
}
