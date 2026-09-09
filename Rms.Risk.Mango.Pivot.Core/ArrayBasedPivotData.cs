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
using System.Diagnostics;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Attributes;

namespace Rms.Risk.Mango.Pivot.Core;

[BsonSerializer( typeof(ArrayBasedPivotDataSerializer))]
[JsonConverter( typeof(ArrayBasedPivotDataJsonSerializer))]
public class ArrayBasedPivotData : IPivotedData
{
    private readonly List<string>                                     _headers;
    private          List<(string OrigHeader, string DisplayHeader)>? _headersMap;
    private readonly List<object?[]>                                  _realData         = [];
    private readonly Dictionary<int, Type>                            _columnTypesCache = [];
    private readonly List<string>                                     _displayHeaders   = [];
    private          int[]?                                           _columnMap; // displayPos -> physicalPos in _headers


    public static readonly ArrayBasedPivotData NoData = new(["No data"]) { Id = "no_data" };


    public IReadOnlyCollection<string> Headers
    {
        get
        {
            if ( _displayHeaders.Count > 0 && _displayHeaders.Count == _headers.Count)
                return _displayHeaders;

            if ( !(_columnMap?.Length > 0) )
                return _headers;

            _displayHeaders.Clear();

            _displayHeaders.AddRange(
                _columnMap
                   .Select((physicalPos, displayPos) => (physicalPos, displayPos))
                   .OrderBy(x => x.displayPos)
                   .Select( x => _headers[x.physicalPos])
            );

//            _displayHeaders.AddRange(
//                _headers
//                   .Select((x, i) => (Name: x, Order: Array.IndexOf(_columnMap, i, 0)))
//                   .OrderBy(x => x.Order)
//                   .Select(x => x.Name)
//                   .ToList()
//            );

            return _displayHeaders;
        }
    }

    public IReadOnlyCollection<(string OrigHeader, string DesplayHeader)> HeadersMap => _headersMap ?? [];

    public void UpdateHeaders(Func<string, string> changeColumnName)
    {
        // make a copy or original headers
        if ( _headersMap != null )
            throw new ApplicationException("UpdateHeaders can only be called once");
        _headersMap = [];

        for (var i = 0; i < _headers.Count; i++ )
        {
            var newName = changeColumnName(_headers[i]);
            _headersMap.Add((OrigHeader:_headers[i], DisplayHeader: newName));
            _headers[i] = newName;
        }

        _displayHeaders.Clear();
    }

    public string   Id       { get; set; } = "";
    public DateTime ExpireAt { get; set; }

    public int Count => _realData.Count;

    public object? Get(int displayCol, int row) => this[displayCol, row];

    public object? this[int displayCol, int row]
    {
        get
        {
            if (row >= _realData.Count)
                return null;

            var physicalCol = _columnMap != null && displayCol < _columnMap.Length
                ? _columnMap[displayCol]
                : displayCol;

            return physicalCol >= _realData[row].Length
                ? null
                : _realData[row][physicalCol];
        }
        set
        {
            if (row >= _realData.Count)
                throw new ArgumentException($"Row={row} is greater than Count={_realData.Count}");

            var physicalCol = _columnMap != null && displayCol < _columnMap.Length
                ? _columnMap[displayCol]
                : displayCol;

            if (physicalCol >= _realData[row].Length)
                throw new ArgumentException($"Col={row} is greater than Count={_realData[row].Length} or Row={row}");
            _realData[row][physicalCol] = value;
        }
    }

    public ArrayBasedPivotData(IEnumerable<string> headers)
    {
        _headers = headers.ToList();
    }

    public ArrayBasedPivotData(IPivotedData other)
        : this( other.Headers )
    {
        var len = other.Headers.Count;
        var o   = new object?[len];

        for ( var row = 0; row < other.Count; row++)
        {
            for (var col = 0; col < len; col++)
            {
                o[col] = other.Get(col, row);
            }
            Add(o);
        }
    }

    private ArrayBasedPivotData(
        IEnumerable<string> headers,         
        int[]?              columnMap
    )
    {
        _headers   = headers.ToList();
        _columnMap = columnMap == null ? null : [.. columnMap]; // make a copy
    }

    public void Add( IEnumerable<object?> data )
    {
        var row = new object?[_headers.Count];
        var i   = 0;
        foreach ( var o in data )
        {
            if ( i >= _headers.Count )
                throw new ArgumentException($"Length of supplied data must be at least {_headers.Count}", nameof(data));
            row[i++] = o;
        }

        _realData.Add( row );
    }

    public void AddHeader(string header)
    {
        _headers.Add(header);
        _displayHeaders.Clear();
        _columnMap = null;
    }

    public bool Contains( string header ) => _headers.Any( x => x == header );

    public Type GetColumnType(int displayCol)
    {
        if ( _columnTypesCache.TryGetValue( displayCol, out var t ) )
            return t;
        t                             = DetectColumnType( displayCol );
        _columnTypesCache[displayCol] = t;
        return t;
    }

    private Type DetectColumnType(int displayCol)
    {
        var guessDouble = 0;
        var guessLong   = 0;
        var guessInt    = 0;
        var guessDec    = 0;

        for ( var i = 0; i < Math.Min( 200, Count); i++ )
        {
            var o = this[displayCol, i];
            if (o == null)
                continue;

            if ( o is double )
                guessDouble += 1;
            else if (o is int)
                guessInt += 1;
            else if (o is long)
                guessLong += 1;
            else if (o is decimal)
                guessDec += 1;
        }

        if (guessDec > guessDouble && guessDec > guessLong && guessDec > guessInt)
            return typeof(decimal);
        if (guessDouble  > guessLong && guessDouble > guessInt)
            return typeof(double);
        if (guessLong > guessInt)
            return typeof(long);
        if (guessInt > 0)
            return typeof(int);

        return typeof(string);
    }

    public void ReorderColumns( IReadOnlyCollection<string> columnsOrder )
    {
        _displayHeaders.Clear();

        if ( !(columnsOrder?.Count > 0) )
        {
            _columnMap = [];
            return;
        }

        var src    = new List<string>( _headers );
        var colMap = new List<int>(); // position = display col / value = real col number

        foreach ( var regex in columnsOrder.Select( x => new Regex( x, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline ) ).ToArray() )
        {
            var alpha = new List<string>(); // for alpha sorting
            foreach ( var name in src )
            {
                var m = regex.Match( name );
                if ( !m.Success )
                    continue;
                alpha.Add( name );
            }

            alpha.Sort();

            foreach ( var name in alpha )
            {
                src.Remove( name );
                colMap.Add( _headers.IndexOf( name ) );
            }
        }

        foreach (var name in src) // something what is not fall under any regex
            colMap.Add(_headers.IndexOf(name));

        Debug.Assert( colMap.Count == _headers.Count );

        _columnMap = colMap.ToArray(); // set column mapping
    }

    /// <summary>
    /// Create filtered copy of data. Filter func must return true if you want row to remain in the output copy.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public IPivotedData Filter(Func<int, bool> filter)
    {
        var res = new ArrayBasedPivotData(_headers, _columnMap );
        for( var i = 0; i < _realData.Count; i++ )
        {
            if (!filter(i))
                continue;
            res.Add(_realData[i]);
        }

        return res;
    }

    /// <summary>
    /// Create filtered copy of data. Resulting columns present in the data in order of appearance in newColumns. If column is missing it's omitted.
    /// </summary>
    public IPivotedData FilterColumns(IReadOnlyCollection<string> newColumns, Func<int, bool>? filter = null)
    {
        var newColMap = new List<(string, int)>();
        foreach (var colName in newColumns)
        {
            var index = _headers.IndexOf(colName);
            if ( index < 0 )
                continue;
            newColMap.Add((colName, index));
        }

        var res = new ArrayBasedPivotData( newColMap.Select(x => x.Item1).ToList() );
        for( var i = 0; i < _realData.Count; i++ )
        {
            if (filter != null && !filter(i))
                continue;

            var newData = new object?[newColMap.Count];
            foreach (var (oldIndex, newIndex) in newColMap.Select((x,pos) => (OldIndex: x.Item2, NewIndex: pos)))
            {
                newData[newIndex] = _realData[i].Length <= oldIndex 
                    ? null 
                    : _realData[i][oldIndex]
                    ;
            }
            res.Add(newData);
        }

        return res;
    }
    private class RowComparer(IPivotedData pivot, Func<object?[], object?[], Dictionary<string, int>, int> comparer) : IComparer<object?[]>
    {
        private readonly Dictionary<string, int> _columnMap = pivot.Headers.Select((x, i) => (x, i)).ToDictionary(x => x.x, x => x.i);
        private readonly Func<object?[], object?[], Dictionary<string, int>, int> _comparer = comparer;

        public int Compare( object?[]? x, object?[]? y )
            => x == null ? -1 : y == null ? 1 : _comparer( x, y, _columnMap );
    }

    /// <summary>
    /// Custom sort for real data. Be careful with views.
    /// </summary>
    /// <param name="comparer"></param>
    public void Sort( Func<object?[], object?[], Dictionary<string, int>, int> comparer ) => _realData.Sort(new RowComparer( this, comparer ));
}