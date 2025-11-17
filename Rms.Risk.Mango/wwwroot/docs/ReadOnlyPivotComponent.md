# ReadOnlyPivotComponent  

The `ReadOnlyPivotComponent` is a Blazor component designed to render a read-only pivot table. It provides functionality for displaying pivoted data in a tabular format with support for sorting, styling, and totals visibility.  

## Features  

- **Dynamic Data Rendering**: Automatically updates the table when the `PivotData` parameter changes.  
- **Sorting**: Supports sorting by columns.  
- **Custom Styling**: Allows custom cell and column styling.  
- **Totals Visibility**: Optionally displays totals for the pivoted data.  

## Parameters  

| Parameter       | Type                        | Default Value | Description                                                                 |
|------------------|-----------------------------|---------------|-----------------------------------------------------------------------------|
| `PivotData`     | `IPivotedData?`            | `null`        | The pivoted data to display.                                               |
| `Rows`          | `int`                      | `35`          | The number of rows to display per page.                                    |
| `ShowTotals`    | `bool`                     | `false`       | Determines whether to display totals for the pivoted data.                 |

## Properties  

| Property          | Type                        | Description                                                                 |
|--------------------|-----------------------------|-----------------------------------------------------------------------------|
| `_pivotRows`      | `PivotRow[]`               | The rows of the pivot table generated from the `PivotData`.                |
| `_totals`         | `MinMaxCache`             | Cache for storing min and max values for columns.                          |
| `_shadowHeaders`  | `Dictionary<string, int>` | Stores column positions for the pivoted data.                              |
| `SortMode`        | `TableControl.SortModeType`| The current sort mode for the table.                                       |
| `SortColumn`      | `string?`                 | The column currently being sorted.                                         |
| `VisibleTotals`   | `IMinMaxCache?`           | The totals to display if `ShowTotals` is enabled.                          |

## Methods  

| Method                     | Description                                                                 |
|-----------------------------|-----------------------------------------------------------------------------|
| `GetColumnDescriptorInternal` | Retrieves the column descriptor for a given column name.                  |
| `GetFieldDescriptorInternal`  | Retrieves the field descriptor for a given column name.                   |
| `GetCellClassCallback`        | Callback to determine the CSS class for a specific cell.                  |

## Behavior  

- **Dynamic Updates**: The component updates its state and re-renders when the `PivotData` parameter changes.  
- **Column Styling**: Automatically applies a "nowrap" class to columns based on their names and lengths.  
- **Totals Handling**: Updates totals when the `PivotData` changes and displays them if `ShowTotals` is enabled.  

## Usage  

### Example: Basic Usage  
