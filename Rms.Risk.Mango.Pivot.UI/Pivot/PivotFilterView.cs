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
﻿using Rms.Risk.Mango.Pivot.Core;

namespace Rms.Risk.Mango.Pivot.UI.Pivot;

public class PivotFilteredView : IPivotedData
{
    private readonly List<PivotRow> _filteredRows;

    public PivotFilteredView(List<PivotRow> filteredRows)
    {
        if ((filteredRows?.Count ?? 0) == 0)
            throw new ApplicationException($"{nameof(filteredRows)} must be a non empty List");

        _filteredRows = filteredRows!;
    }

    public string Id { get; set; } = "";

    public IReadOnlyCollection<string> Headers                   => _filteredRows[0].PivotData.Headers;
    public int                   Count                           => _filteredRows.Count;
    public object?               Get(int           col, int row) => _filteredRows[row].PivotData.Get(col, _filteredRows[row].Row);
    public Type                  GetColumnType(int col)          => _filteredRows[0].PivotData.GetColumnType(col);

    public IPivotedData Filter(Func<int, bool> filter) => throw new NotImplementedException();
}