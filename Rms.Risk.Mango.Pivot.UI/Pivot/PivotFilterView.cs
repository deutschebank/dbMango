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
    private readonly IPivotedData      _source;
    private readonly IReadOnlyList<int> _rows;

    public PivotFilteredView(IPivotedData source, IReadOnlyList<int> rows)
    {
        if ((rows?.Count ?? 0) == 0)
            throw new ApplicationException($"{nameof(rows)} must be a non empty list");

        _source = source ?? throw new ArgumentNullException(nameof(source));
        _rows   = rows!;
    }

    public string Id { get; set; } = "";

    public IReadOnlyCollection<string> Headers                   => _source.Headers;
    public int                   Count                           => _rows.Count;
    public object?               Get(int           col, int row) => _source.Get(col, _rows[row]);
    public Type                  GetColumnType(int col)          => _source.GetColumnType(col);

    public IPivotedData Filter(Func<int, bool> filter) => throw new NotImplementedException();
}