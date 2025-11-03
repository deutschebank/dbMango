# TableControl Component Documentation  

The `TableControl` component is a highly customizable Blazor table control designed for displaying, filtering, sorting, and paginating tabular data.  

## Features  
- **Dynamic Columns**: Define columns dynamically with support for templates.  
- **Filtering**: Apply column-based filters with exact or partial matches.  
- **Sorting**: Sort data by columns, including absolute sorting.  
- **Pagination**: Navigate through data with configurable page sizes.  
- **Totals Row**: Display a totals row for numeric columns.  
- **Clipboard Support**: Copy all table data to the clipboard.  

---

## Parameters  

| Parameter                  | Type                                      | Description                                                                                     | Default Value |  
|----------------------------|-------------------------------------------|-------------------------------------------------------------------------------------------------|---------------|  
| `Items`                    | `IReadOnlyCollection<dynamic>`            | The data source for the table.                                                                 | `[]`          |  
| `PageSize`                 | `int`                                    | Number of rows per page.                                                                        | `10`          |  
| `PagerSize`                | `int`                                    | Number of pages to display in the pagination control.                                           | `5`           |  
| `Class`                    | `string?`                                | CSS class for the table container.                                                             | `null`        |  
| `TableClass`               | `string?`                                | CSS class for the table element.                                                               | `null`        |  
| `Filterable`               | `bool`                                   | Enables or disables the filter row.                                                            | `false`       |  
| `Selectable`               | `bool`                                   | Enables row selection.                                                                          | `false`       |  
| `SelectedItem`             | `dynamic?`                               | The currently selected row.                                                                    | `null`        |  
| `SelectedColumn`           | `string?`                                | The currently selected column.                                                                 | `null`        |  
| `SelectionChanged`         | `EventCallback<(dynamic row, string col)>` | Callback triggered when a row or column is selected.                                           | `null`        |  
| `RowClass`                 | `Func<dynamic, string>`                  | Function to apply custom CSS classes to rows.                                                  | `""`          |  
| `AbsoluteSort`             | `bool`                                   | Enables absolute sorting for numeric columns.                                                  | `true`        |  
| `Totals`                   | `IMinMaxCache?`                          | Provides totals for numeric columns.                                                           | `null`        |  
| `CellClass`                | `Func<dynamic, TableColumnControl, string>` | Function to apply custom CSS classes to cells.                                                | Default logic |  
| `CellStyle`                | `Func<dynamic, TableColumnControl, string>` | Function to apply custom inline styles to cells.                                              | `""`          |  

---

## Usage Examples  

### Basic Table  
