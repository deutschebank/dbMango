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
/// Helpers to adapt strongly typed collections to <see cref="IPivotedData"/> without using
/// <c>dynamic</c>. Columns are derived from the public readable properties of the element type
/// (see <see cref="EnumerablePivotData{T}"/>).
/// </summary>
public static class PivotDataHelper
{
    /// <summary>
    /// Adapt any <see cref="IEnumerable{T}"/> to <see cref="IPivotedData"/>.
    /// </summary>
    public static IPivotedData ToPivotData<T>(this IEnumerable<T> items, string id = "") =>
        new EnumerablePivotData<T>(items, id);

    /// <summary>
    /// Adapt a <see cref="List{T}"/> to <see cref="IPivotedData"/>.
    /// </summary>
    public static IPivotedData ToPivotData<T>(this List<T> items, string id = "") =>
        new EnumerablePivotData<T>(items, id);

    /// <summary>
    /// Adapt an <see cref="IList{T}"/> to <see cref="IPivotedData"/>.
    /// </summary>
    public static IPivotedData ToPivotData<T>(this IList<T> items, string id = "") =>
        new EnumerablePivotData<T>(items, id);

    /// <summary>
    /// Adapt an <see cref="IReadOnlyCollection{T}"/> to <see cref="IPivotedData"/>.
    /// </summary>
    public static IPivotedData ToPivotData<T>(this IReadOnlyCollection<T> items, string id = "") =>
        new EnumerablePivotData<T>(items, id);

    /// <summary>
    /// Adapt an <see cref="IReadOnlyList{T}"/> to <see cref="IPivotedData"/>.
    /// </summary>
    public static IPivotedData ToPivotData<T>(this IReadOnlyList<T> items, string id = "") =>
        new EnumerablePivotData<T>(items, id);

    /// <summary>
    /// Adapt a dictionary to <see cref="IPivotedData"/> producing <c>Key</c> / <c>Value</c> columns.
    /// </summary>
    public static IPivotedData ToPivotData<TKey, TValue>(this IDictionary<TKey, TValue> items, string id = "") =>
        new EnumerablePivotData<KeyValuePair<TKey, TValue>>(items, id);

    /// <summary>
    /// Adapt a read only dictionary to <see cref="IPivotedData"/> producing <c>Key</c> / <c>Value</c> columns.
    /// </summary>
    public static IPivotedData ToPivotData<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> items, string id = "") =>
        new EnumerablePivotData<KeyValuePair<TKey, TValue>>(items, id);

    /// <summary>
    /// Adapt a sequence of key/value pairs to <see cref="IPivotedData"/> producing <c>Key</c> / <c>Value</c> columns.
    /// </summary>
    public static IPivotedData ToPivotData<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items, string id = "") =>
        new EnumerablePivotData<KeyValuePair<TKey, TValue>>(items, id);

    /// <summary>
    /// Recover the original source object for a row when the supplied data is backed by a typed
    /// collection (see <see cref="ISourceRowProvider"/>). Returns <c>default</c> when the row cannot
    /// be resolved or is not of the requested type.
    /// </summary>
    public static T? Source<T>(IPivotedData data, int row) =>
        data is ISourceRowProvider provider && provider.GetSourceRow(row) is T value ? value : default;
}
