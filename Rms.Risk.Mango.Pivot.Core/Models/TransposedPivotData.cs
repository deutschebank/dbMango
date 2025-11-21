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
namespace Rms.Risk.Mango.Pivot.Core.Models;

public class TransposedPivotData : NotifyPropertyChangedBase, IPivotedData
{
    private readonly IPivotedData  _data;
    private          IPivotedData? _transposedData;

    public TransposedPivotData( IPivotedData data,       string columnHeaderColumn, string [] rowHeaderColumns,
                                string       dataColumn, CancellationToken token )
    {
        _data               = data;
        _columnHeaderColumn = columnHeaderColumn;
        _rowHeaderColumns   = rowHeaderColumns;
        _dataColumn         = dataColumn;
        _transposedData     = CreateTransposedTable( token );
        Id                  = $"{data.Id}-Transposed";
    }

    public string Id { get; set; }

    private string [] _rowHeaderColumns;
    public string [] RowHeaderColumn
    {
        get => _rowHeaderColumns;
        set
        {
            if (_rowHeaderColumns == value)
                return;
            _rowHeaderColumns = value;
            Refresh();
            OnPropertyChanged(() => RowHeaderColumn);
        }
    }
        
    private string _columnHeaderColumn;
    public string ColumnHeaderColumn
    {
        get => _columnHeaderColumn;
        set
        {
            if (_columnHeaderColumn == value)
                return;
            _columnHeaderColumn = value;
            Refresh();
            OnPropertyChanged(() => ColumnHeaderColumn);
        }
    }

    private string _dataColumn;
    public string DataColumn
    {
        get => _dataColumn;
        set
        {
            if (_dataColumn == value)
                return;
            _dataColumn = value;
            Refresh();
            OnPropertyChanged(() => DataColumn);
        }
    }

#region IPivotedData

    public IReadOnlyCollection<string> Headers => _transposedData?.Headers ?? _data.Headers;
    public int                         Count   => _transposedData?.Count ?? _data.Count;

    public Dictionary<string, int> GetColumnPositions()
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // this is safer than calling ToDictionary as it handles duplicate headers
        foreach ( var (key, pos) in Headers.Select((x, i) => (Key: x, Value: i)) )
        {
            dict.TryAdd(key, pos);
        }

        return dict;
    }

    public object? Get( int col, int row )
    {
        if ( _transposedData != null )
        {
            if ( col >= _transposedData.Headers.Count
             || row >= _transposedData.Count )
                return null;
            return _transposedData.Get( col, row );
        }

        if (col >= _data.Headers.Count
         || row >= _data.Count)
            return null;

        return _data.Get( col, row );
    }

    public Type GetColumnType( int col ) =>
        _transposedData != null 
            ? _transposedData.GetColumnType( col ) 
            : _data.GetColumnType( col );

    public IPivotedData Filter(Func<int, bool> filter)
    {
        var data = _data.Filter(filter);
        return new TransposedPivotData(data,
                                       _columnHeaderColumn,
                                       _rowHeaderColumns,
                                       _dataColumn,
                                       CancellationToken.None);
    }

#endregion

    private CancellationTokenSource _cancellationTokenSource = new();

    private async void Refresh()
    {
        try
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource = new();

            var token = _cancellationTokenSource.Token;
            var data = await Task.Run( () => CreateTransposedTable(token), token );
            if ( data == null )
                return;
            _transposedData = data;
            OnPropertyChanged( () => Headers );
            OnPropertyChanged( () => Count );
        }
        catch ( Exception )
        {
            //
        }
    }

    private ArrayBasedPivotData? CreateTransposedTable( CancellationToken token )
    {
        if ( token.IsCancellationRequested )
            return null;

        if ( _data == null 
         || _data.Headers.Count == 0 
         || _data.Count == 0 )
            return null;

        var rhc = RowHeaderColumn;
        var chc = ColumnHeaderColumn;
        var dc  = DataColumn;

        if ( rhc == null 
         || rhc.Length == 0
         || string.IsNullOrWhiteSpace( chc )
         || string.IsNullOrWhiteSpace( dc ) 
           )
            return null;

        if ( rhc.Any( x => !_data.Headers.Contains( x ) )
         || !_data.Headers.Contains( chc )
         || !_data.Headers.Contains( dc )
           )
            return null;

        var headersDict = _data.Headers.Select((s, i) => (s, i)).ToDictionary(x => x.s, x => x.i);

        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var colPos  = headersDict[chc];
        var count   = rhc.Length;

        for ( var row = 0; row < _data.Count; row++ )
        {
            var v = ConvertToString( _data.Get( colPos, row ) );

            if ( columns.ContainsKey( v ) )
                continue;

            columns[v] = count++;
        }

        if ( token.IsCancellationRequested )
            return null;

        var rows   = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rowPos = rhc.Select( x => headersDict[x]).ToArray();
        count = 0;

        for ( var row = 0; row < _data.Count; row++ )
        {
            var r = row;
            var v = string.Join("|", rowPos.Select( x => ConvertToString( _data.Get( x, r ) )));

            if ( rows.ContainsKey( v ) )
                continue;

            rows[v] = count++;
        }

        if ( token.IsCancellationRequested )
            return null;

        var d = new ArrayBasedPivotData( 
            rhc
               .Concat( columns
                       .OrderBy(  x => x.Value ) 
                       .Select( x => x.Key  )
                )
        );

        // preallocate space

        for ( var row = 0; row < rows.Count; row++ )
        {
            d.Add( new object[columns.Count+rhc.Length] );
            var k = rows.First( x => x.Value == row ).Key.Split( '|' );
            for ( var col = 0; col < k.Length; col++ )
                d[col, row] = k[col];
        }

        if ( token.IsCancellationRequested )
            return null;

        var dataPos = headersDict[dc];

        for ( var row = 0; row < _data.Count; row++ )
        {
            if ( row % 2048 == 0)
                if ( token.IsCancellationRequested )
                    return null;

            var destCol = columns[ConvertToString(_data.Get( colPos, row ))];

            var r       = row;
            var v       = string.Join("|", rowPos.Select( x => ConvertToString( _data.Get( x, r ) )));
            var destRow = rows[v];


            var dest = d[destCol, destRow];
            var src  = _data.Get( dataPos, row );
                
            if ( src == null )
                continue;
            if (dest == null)
                d[destCol, destRow] = src;
            else
            {
                switch ( dest )
                {
                    case double dbl:
                        dest = dbl + Convert.ToDouble( src );
                        break;
                    case int i:
                        dest = i   + Convert.ToInt32( src );
                        break;
                    case long l:
                        dest = l   + Convert.ToInt64( src );
                        break;
                    case decimal dec:
                        dest = dec + Convert.ToInt64( src );
                        break;
                    default:
                        dest = src;
                        break;
                }
                d[destCol, destRow] = dest;
            }
        }

        return token.IsCancellationRequested ? null : d;
    }

    private static string ConvertToString( object? val )
    {
        var v = val is DateTime time ? time.ToString( "yyyy-MM-dd" ) : val?.ToString() ?? "";
        return v;
    }
}