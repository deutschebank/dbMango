# ReadOnlyPivotTable Component  

The `ReadOnlyPivotTable` component is a Blazor component designed to display pivoted data in a tabular format with optional charting capabilities. It supports dynamic data rendering, customizable styles, and interactive features such as sorting and cell click handling.  

## Features  
- Displays pivoted data in a table format.  
- Supports line chart visualization for pivoted data.  
- Customizable cell styles and classes.  
- Interactive cell click handling with event callbacks.  
- Export pivot data to CSV or copy to clipboard.  
- Dynamic updates based on filtered rows.  

## Parameters  

| Parameter                  | Type                                      | Default Value | Description                                                                 |
|----------------------------|-------------------------------------------|---------------|-----------------------------------------------------------------------------|
| `Class`                    | `string`                                 | `""`          | CSS class for the component container.                                     |
| `PivotData`                | `IPivotedData`                           | `null`        | The pivoted data to display.                                               |
| `PivotDef`                 | `PivotDefinition?`                       | `null`        | The pivot definition for charting and data configuration.                  |
| `SelectedCollectionNode`   | `GroupedCollection?`                     | `null`        | The selected collection node for column and field descriptors.             |
| `Rows`                     | `int`                                    | `35`          | The number of rows to display per page.                                    |
| `GetColumnDescriptor`      | `Func<string, PivotColumnDescriptor?>`   | `null`        | Callback to retrieve column descriptors.                                   |
| `GetFieldDescriptor`       | `Func<string, PivotFieldDescriptor?>`    | `null`        | Callback to retrieve field descriptors.                                    |
| `HandleCellClick`          | `Func<DynamicObject, string, Task<bool>>`| `null`        | Callback invoked when a cell is clicked.                                   |
| `Title`                    | `string`                                 | `""`          | The title displayed above the table.                                       |
| `ShowIfEmpty`              | `bool`                                   | `true`        | Determines whether to show the table if the data is empty.                 |
| `HeaderStyles`             | `Dictionary<string, string>`             | `{}`          | Custom styles for table headers.                                           |
| `IsExportEnabled`          | `bool`                                   | `false`       | Indicates whether exporting data is enabled.                               |
| `IsExportEnabledChanged`   | `EventCallback<bool>`                    | `null`        | Callback invoked when the export enabled state changes.                    |

## Methods  

| Method           | Description                                                                 |
|-------------------|-----------------------------------------------------------------------------|
| `CopyCsv()`       | Copies the pivot data to the clipboard in CSV format.                      |
| `ExportCsv(string destFileName)` | Exports the pivot data to a CSV file with the specified name. |

## Usage  

### Basic Example  
