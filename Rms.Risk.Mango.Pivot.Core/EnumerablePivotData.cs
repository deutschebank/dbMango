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

namespace Rms.Risk.Mango.Pivot.Core;

/// <summary>
/// Exposes a collection of strongly typed objects as <see cref="IPivotedData"/>.
/// Columns are derived from the public readable instance properties of <typeparamref name="T"/>
/// (in declaration order). <see cref="KeyValuePair{TKey,TValue}"/> elements are projected to
/// <c>Key</c> / <c>Value</c> columns.
/// </summary>
/// <typeparam name="T">Element type of the backing collection.</typeparam>
public sealed class EnumerablePivotData<T> : IPivotedData, ISourceRowProvider
{
    private readonly IReadOnlyList<T>        _items;
    private readonly string[]                _headers;
    private readonly Func<T, object?>[]      _accessors;
    private readonly Type[]                  _columnTypes;
    private readonly Dictionary<string, int> _columnPositions;

    public EnumerablePivotData(IEnumerable<T> items, string id = "")
    {
        _items = items as IReadOnlyList<T> ?? items.ToList();
        Id     = id;

        (_headers, _accessors, _columnTypes) = BuildColumns(_items);

        _columnPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _headers.Length; i++)
            _columnPositions.TryAdd(_headers[i], i);
    }

    private EnumerablePivotData(
        IReadOnlyList<T>        items,
        string[]                headers,
        Func<T, object?>[]      accessors,
        Type[]                  columnTypes,
        Dictionary<string, int> columnPositions,
        string                  id)
    {
        _items           = items;
        _headers         = headers;
        _accessors       = accessors;
        _columnTypes     = columnTypes;
        _columnPositions = columnPositions;
        Id               = id;
    }

    public string Id { get; }

    public IReadOnlyCollection<string> Headers => _headers;

    public int Count => _items.Count;

    public Dictionary<string, int> GetColumnPositions() =>
        new(_columnPositions, StringComparer.OrdinalIgnoreCase);

    public object? Get(int col, int row)
    {
        if (col < 0 || col >= _accessors.Length || row < 0 || row >= _items.Count)
            return null;

        return _accessors[col](_items[row]);
    }

    public Type GetColumnType(int col) =>
        col >= 0 && col < _columnTypes.Length ? _columnTypes[col] : typeof(string);

    public object? GetSourceRow(int row) =>
        row >= 0 && row < _items.Count ? _items[row] : null;

    public IPivotedData Filter(Func<int, bool> filter)
    {
        var filtered = new List<T>();
        for (var i = 0; i < _items.Count; i++)
        {
            if (filter(i))
                filtered.Add(_items[i]);
        }

        return new EnumerablePivotData<T>(filtered, _headers, _accessors, _columnTypes, _columnPositions, Id);
    }

    private static (string[] Headers, Func<T, object?>[] Accessors, Type[] Types) BuildColumns(IReadOnlyList<T> items)
    {
        var type = typeof(T);

        // When the static type is not informative (e.g. object/dynamic) fall back to the runtime
        // type of the first non null element.
        if (type == typeof(object))
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    type = item.GetType();
                    break;
                }
            }
        }

        if (IsKeyValuePair(type, out var keyType, out var valueType))
        {
            var keyProp   = type.GetProperty("Key")!;
            var valueProp = type.GetProperty("Value")!;

            return (
                ["Key", "Value"],
                [
                    x => x is null ? null : keyProp.GetValue(x),
                    x => x is null ? null : valueProp.GetValue(x)
                ],
                [keyType, valueType]
            );
        }

        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && (p.GetMethod?.IsPublic ?? false))
            .OrderBy(p => p.MetadataToken)
            .ToArray();

        var headers   = new string[props.Length];
        var accessors = new Func<T, object?>[props.Length];
        var types     = new Type[props.Length];

        for (var i = 0; i < props.Length; i++)
        {
            var p        = props[i];
            headers[i]   = p.Name;
            accessors[i] = x => x is null ? null : p.GetValue(x);
            types[i]     = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        }

        return (headers, accessors, types);
    }

    private static bool IsKeyValuePair(Type type, out Type keyType, out Type valueType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            var args  = type.GetGenericArguments();
            keyType   = args[0];
            valueType = args[1];
            return true;
        }

        keyType   = typeof(object);
        valueType = typeof(object);
        return false;
    }
}
