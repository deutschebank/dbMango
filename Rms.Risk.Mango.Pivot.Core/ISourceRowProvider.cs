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
namespace Rms.Risk.Mango.Pivot.Core;

/// <summary>
/// Implemented by <see cref="IPivotedData"/> sources that are backed by a collection of
/// strongly typed objects. Allows callers (e.g. table templates and callbacks) to recover
/// the original source object for a given row without resorting to <c>dynamic</c>.
/// </summary>
public interface ISourceRowProvider
{
    /// <summary>
    /// Get the original source object that produced the supplied row, or <c>null</c> when the
    /// row is out of range or no backing object exists.
    /// </summary>
    /// <param name="row">Zero based row index.</param>
    object? GetSourceRow(int row);
}
