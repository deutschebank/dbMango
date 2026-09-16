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
using System;
using System.Threading;
using System.Threading.Tasks;

internal static class ConsoleLogger
{
    private static readonly object _lock = new();
    private static CancellationTokenSource? _queryingCts;
    private static int _queryingLineLength;

    // ── Querying ticker ──────────────────────────────────────────────────────

    public static void BeginQuerying(string packageName)
    {
        ClearQuerying();

        var cts = new CancellationTokenSource();
        _queryingCts = cts;

        _ = Task.Run(async () =>
        {
            var start = DateTime.UtcNow;
            while (!cts.Token.IsCancellationRequested)
            {
                var elapsed = (int)(DateTime.UtcNow - start).TotalSeconds;
                var dots    = new string('.', 9);
                var line    = $"  Querying {packageName}{dots} {elapsed}s";
                lock (_lock)
                {
                    if (!cts.Token.IsCancellationRequested)
                    {
                        _queryingLineLength = line.Length;
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("\r" + line);
                        Console.ResetColor();
                    }
                }
                try { await Task.Delay(1000, cts.Token); }
                catch (OperationCanceledException) { break; }
            }
        }, cts.Token);
    }

    public static void ClearQuerying()
    {
        var cts = Interlocked.Exchange(ref _queryingCts, null);
        if (cts == null) return;

        cts.Cancel();
        cts.Dispose();

        lock (_lock)
        {
            if (_queryingLineLength > 0)
            {
                Console.Write("\r" + new string(' ', _queryingLineLength) + "\r");
                _queryingLineLength = 0;
            }
        }
    }

    // ── All other log methods clear the querying line first ──────────────────

    public static void Info(string message)
    {
        ClearQuerying();
        Console.ResetColor();
        Console.WriteLine(message);
    }

    public static void Updated(string packageName, string oldVersion, string newVersion)
    {
        ClearQuerying();
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(packageName);
        Console.ResetColor();
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(oldVersion);
        Console.ResetColor();
        Console.Write(" → ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(newVersion);
        Console.ResetColor();
    }

    public static void Skipped(string packageName, string version)
    {
        ClearQuerying();
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(packageName);
        Console.Write("  ");
        Console.WriteLine(version);
        Console.ResetColor();
    }

    public static void Fixed(string packageName, string version)
    {
        ClearQuerying();
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(packageName);
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(version);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  (fixed, skipped)");
        Console.ResetColor();
    }

    public static void Error(string packageName, string message)
    {
        ClearQuerying();
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("[error] ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(packageName);
        Console.ResetColor();
        Console.Write(": ");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void Section(string title)
    {
        ClearQuerying();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(title);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('─', Math.Min(title.Length + 2, 60)));
        Console.ResetColor();
    }

    public static void Summary(int updated, int skipped, int fixed_, string outputFile)
    {
        ClearQuerying();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Done.  ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"{updated} updated");
        Console.ResetColor();
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"{skipped} up-to-date");
        Console.ResetColor();
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{fixed_} fixed");
        Console.ResetColor();
        Console.WriteLine();
        Console.Write("Output: ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(outputFile);
        Console.ResetColor();
    }

    public static void NothingToDo()
    {
        ClearQuerying();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("All packages are up-to-date.");
        Console.ResetColor();
    }
}
