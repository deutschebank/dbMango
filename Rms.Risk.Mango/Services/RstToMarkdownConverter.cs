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
﻿using System.Text.RegularExpressions;

namespace Rms.Risk.Mango.Services;

/// <summary>
/// Converts ReStructuredText (RST) to Markdown (MD).
/// Unknown tags are omitted during conversion.
/// </summary>
public class RstToMarkdownConverter
{

    private static readonly Dictionary<Regex, string> _conversionRules = new ()
    {
        { new (@"([^\*]|^)\*([^\*]+)\*([^\*]|$)"), "_$2_"       }, // Italic text
        { new (@"\*\*([^\*]+)\*\*"              ), "**$1**"     }, // Bold text
        { new (@"^\.\.\s+code-block::\s*(\w+)"  ), "```$1"      }, // Code block start
        { new (@"^\.\.\s+collflag::\s*(\w+)"    ), "\n**$1**\n" }, // ?
        { new (@"^\.\.\s+data::\s*(.+)"         ), "  * $1\n\n" }, // Data annotation
        { new (@":\w+:`([^`]+)`"                ), "$1"         }, // Replace :tag:`word` with `word`
 
        { new (@"&"                             ), "&amp;"      },
        { new (@"<"                             ), "&lt;"       },
        { new (@">"                             ), "&gt;"       },

        { new (@"\s*:local:.*"                  ), ""           },
        { new (@"\s*:backlinks:\s*\w+.*"        ), ""           },
        { new (@"\s*:depth:\s*[0-9]+.*"         ), ""           },
        { new (@"\s*:class:\s*\w+.*"            ), ""           },
    };

    private static readonly Regex _h1 = new(@"^=+\s*$");
    private static readonly Regex _h2 = new(@"^-+\s*$");
    private static readonly Regex _h3 = new(@"^\~+\s*$");
    private static readonly Regex _copyable = new (@"\s*:copyable:\s*(true|false).*" );

    /// <summary>
    /// Converts RST content to Markdown.
    /// </summary>
    /// <param name="lines">The RST content to convert.</param>
    /// <returns>The converted Markdown content.</returns>
    public string Convert(string[] lines)
    {
        if (lines.Length == 0)
            return string.Empty;

        // Conversion rules
        var markdownLines = new List<string>();
        var insideCodeBlock = false;
        var insideTable = false;
        var tableRows = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var convertedLine = line;

            if (i == 0 && _h1.IsMatch(line))
                continue;

            convertedLine = ConvertLine(convertedLine);

            if (insideCodeBlock)
            {
                if ( _copyable.IsMatch(line) )
                    continue;

                if (!line.StartsWith("  ") && !string.IsNullOrWhiteSpace(line))
                {
                    markdownLines.Add("```");
                    markdownLines.Add("");
                    insideCodeBlock = false;
                    // continue processing this line
                }
                else
                {
                    markdownLines.Add(line);
                    continue;
                }
            }

            // Handle tables
            if (line.TrimStart().StartsWith(".. list-table::"))
            {
                insideTable = true;
                tableRows.Clear();
                continue;
            }

            if (convertedLine.StartsWith("```"))
            {
                markdownLines.Add(convertedLine);
                insideCodeBlock = true;
                continue;
            }

            if (insideTable)
            {
                if (!line.StartsWith(" ") && !string.IsNullOrWhiteSpace(line))
                {
                    // End of table
                    if (tableRows.Count > 0)
                        markdownLines.Add(ConvertTableToMarkdownList(tableRows));

                    insideTable = false;
                    // continue processing this line
                }
                else
                {
                    tableRows.Add(line);
                    continue;
                }
            }

            // Handle headers underlined with "=", "-", "~"
            if (i < lines.Length - 1)
            {
                var nextLine = lines[i + 1];
                if (_h1.IsMatch(nextLine))
                {
                    markdownLines.Add($"# {line}");
                    i++;
                    continue;
                }

                if (_h2.IsMatch(nextLine))
                {
                    markdownLines.Add($"## {line}");
                    i++;
                    continue;
                }

                if (_h3.IsMatch(nextLine))
                {
                    markdownLines.Add($"### {line}");
                    i++;
                    continue;
                }
            }

            // Skip unknown tags and ".. include::" lines
            if (convertedLine.TrimStart().StartsWith(".."))
                continue;

            if (convertedLine.StartsWith("  ") && convertedLine.Length > 2 && convertedLine[2] != ' ')
                markdownLines.Add(convertedLine[2..]);
            else
                markdownLines.Add(convertedLine);
        }

        if (insideCodeBlock)
        {
            markdownLines.Add("```");
        }

        return string.Join(Environment.NewLine, markdownLines);
    }

    private static string ConvertLine(string convertedLine)
    {
        foreach (var rule in _conversionRules)
        {
            convertedLine = rule.Key.Replace(convertedLine, rule.Value);
        }

        return convertedLine;
    }

    private static string ConvertTableToMarkdownList(List<string> tableRows)
    {
        var converted = new List<string>();
        // Stack to keep track of the current position of "-" character
        var tabs = new Stack<int>();

        var level = 0;

        // current position of "-" character in the row
        var startPos = 0;

        foreach (var row in tableRows)
        {
            if ( row.TrimStart().StartsWith('*') )
            {
                var pos = row.IndexOf('*', StringComparison.Ordinal);
                if ( pos > 0 )
                {
                    level = 1;
                    startPos = pos;
                    tabs.Clear();
                    tabs.Push(startPos);

                    pos = row.IndexOf('-', StringComparison.Ordinal);
                    converted.Add(row[pos..]);
                }

                continue;
            }

            if ( level == 0 )
                continue;

            // Converting logic for list items:
            // list level starts with N*2 spaces and "-" character
            // if line is empty or whitespace, add an empty line to the output
            // if row[pos] != '-' and row[..pos] == spaces, continue the current list item (add a new line to the converted list starting with N*2+2 spaces)

            if (string.IsNullOrWhiteSpace(row))
            {
                converted.Add(string.Empty);
                continue;
            }

            // if !string.IsNullOrWhiteSpace(row[..pos]) end current list item, decrease level (level -= 1; pos -= 2) and recheck the line with new values (i.e. here must be the inner loop)
            while ( !string.IsNullOrWhiteSpace(row[..Math.Min(row.Length, startPos)]) && level > 0 )
            {
                level -= 1;
                startPos = tabs.Pop();
            }

            // this should not ever happen because of the previous check at the caller level
            if ( level <= 0 )
                break;

            // if row[pos] == '-' continue current level and start a new item
            if ( row.Length > startPos && row[startPos] == '-')
            {
                converted.Add(new string(' ', (level-1) * 2) + "- " + row[(startPos+1)..].Trim());
                continue;
            }

            // if row[pos..].Trim().StartsWith( '-' ) start a new level and item
            if ( row.Length > startPos && row[startPos..].Trim().StartsWith( '-' ) )
            {
                level    += 1;
                startPos =  row.IndexOf('-', StringComparison.Ordinal);
                tabs.Push(startPos);

                converted.Add(new string(' ', (level-1) * 2) + "- " + row[(startPos+1)..].Trim());
                continue;
            }

            converted.Add(new string(' ', level * 2) + row[(startPos+1)..].Trim());
        }

        return string.Join(Environment.NewLine, converted.Select(ConvertLine));
    }
}