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
﻿using System.Dynamic;
using Rms.Risk.Mango.Pivot.Core;

namespace Rms.Risk.Mango.Pivot.UI.Pivot;

public class PivotRow(IPivotedData                         data,
                      int                                  row,
                      Dictionary<string, int>              _columnPositions,
                      Func<string, PivotColumnDescriptor?> _getDescriptor,
                      Func<string, PivotFieldDescriptor?>  _getFieldDescriptor) : DynamicObject
{
    public IPivotedData PivotData { get; } = data;
    public int          Row       { get; } = row;

    public PivotColumnDescriptor? GetColumnDescriptor(string columnName) => _getDescriptor(columnName);
    public PivotFieldDescriptor?  GetFieldDescriptor (string columnName) => _getFieldDescriptor(columnName);

    public override IEnumerable<string> GetDynamicMemberNames() => PivotData.Headers;

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var found = _columnPositions.TryGetValue(binder.Name, out var col);

        result = found ? PivotData.Get(col, Row) : null;
        return found;
    }

    public string GetFormat(string column) => _getDescriptor(column)?.Format ?? "";

    public bool ShouldShowTotals(string column)
    {
        var desc = _getFieldDescriptor(column);
        if ( desc != null && desc.Purpose != PivotFieldPurpose.Data )
            return false;

        var colDesc = _getDescriptor(column);
        return colDesc?.ShowTotals ?? true;
    }
}