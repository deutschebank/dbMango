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
namespace Rms.Risk.Mango.Language.Parsers;

/// <summary>
/// Script parsing syntax error
/// </summary>
public class SyntaxErrorException : Exception
{
    /// <summary>
    /// Line number
    /// </summary>
    public int Line { get; }
    /// <summary>
    /// Position within the line
    /// </summary>
    public int Position { get; }
    /// <summary>
    /// Offending symbol
    /// </summary>
    public object? Symbol { get; }

    /// <summary>
    /// Source of error
    /// </summary>
    public string Component { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="component"></param>
    /// <param name="line"></param>
    /// <param name="pos"></param>
    /// <param name="message"></param>
    /// <param name="symbol"></param>
    /// <param name="innerException"></param>
    public SyntaxErrorException( string component, int line, int pos, string message, object? symbol = null, Exception? innerException = null )
        : base($"Syntax error in {component}: line {line} position {pos}: {message}", innerException)
    {
        Line      = line;
        Position  = pos;
        Symbol    = symbol;
        Component = component;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="component"></param>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public SyntaxErrorException( string component, string message, Exception? innerException = null )
        : base($"Syntax error in {component}: {message}", innerException)
    {
        Line      = 0;
        Position  = 0;
        Symbol    = null;
        Component = component;
    }
}