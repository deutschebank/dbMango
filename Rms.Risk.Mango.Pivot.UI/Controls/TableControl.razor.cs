using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;

namespace Rms.Risk.Mango.Pivot.UI.Controls;

public partial class TableControl
{
    private object? _lastItemsSource;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!ReferenceEquals(_lastItemsSource, Items))
        {
            _lastItemsSource = Items;
            ResetStateForNewItems();
        }
    }

    private void ResetStateForNewItems()
    {
        SelectedRow = null;
        SelectedColumn = null;

        ActiveFilters?.Clear();
        ActiveFiltersExactMatch?.Clear();

        CurrentSortColumn = null;

        _currentPage = 1;
        _startPage = 1;
        _itemCount = Items?.Count ?? 0;
        _totalPages = PageSize > 0
            ? Math.Max(1, (int)Math.Ceiling(_itemCount / (double)PageSize))
            : 1;
        _endPage = Math.Min(PagerSize, _totalPages);

        FilteredRowIndices = Items is null
            ? new List<int>()
            : new List<int>(_itemCount);

        if (Items is not null)
        {
            for (var i = 0; i < Items.Count; i++)
                FilteredRowIndices.Add(i);
        }
    }
}
