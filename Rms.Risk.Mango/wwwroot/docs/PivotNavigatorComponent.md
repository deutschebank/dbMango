# PivotNavigatorComponent  

The `PivotNavigatorComponent` is a Blazor component designed to provide a user interface for navigating and managing pivot data. It integrates with collections and pivots, allowing users to filter, refresh, and export data dynamically.  

## Features  
- **Collection and Pivot Selection**: Allows users to select collections and pivots dynamically.  
- **Row Configuration**: Provides options to configure the number of rows displayed.  
- **Extra Filters**: Supports additional filters through a `RenderFragment`.  
- **Navigation and Actions**: Includes navigation controls and actions such as refresh, copy CSV, and export CSV.  
- **Dynamic State Management**: Dynamically updates button states based on the component's parameters.  

## Parameters  

| Parameter              | Type                              | Default Value | Description                                                                 |
|------------------------|-----------------------------------|---------------|-----------------------------------------------------------------------------|
| `Class`                | `string`                         | `"form-row"`  | CSS class to apply to the root container of the component.                 |
| `ShowCollection`       | `bool`                           | `true`        | Determines whether the collection selector is visible.                     |
| `ShowPivot`            | `bool`                           | `true`        | Determines whether the pivot selector is visible.                          |
| `ShowRows`             | `bool`                           | `true`        | Determines whether the row configuration is visible.                       |
| `ExtraFilter`          | `RenderFragment?`                | `null`        | Additional filter UI provided as a render fragment.                        |
| `Rows`                 | `int`                            | `40`          | Number of rows to display.                                                 |
| `Collections`          | `List<GroupedCollection>`         | `[]`          | List of available collections.                                             |
| `UseCache`             | `bool`                           | `true`        | Indicates whether cached data should be used.                              |
| `RowsChanged`          | `EventCallback<int>`             | `null`        | Callback invoked when the `Rows` value changes.                            |
| `UseCacheChanged`      | `EventCallback<bool>`            | `null`        | Callback invoked when the `UseCache` value changes.                        |
| `RefreshPivotTriggered`| `EventCallback`                  | `null`        | Callback invoked when the "Refresh" action is triggered.                   |
| `CopyCsvTriggered`     | `EventCallback`                  | `null`        | Callback invoked when the "Copy CSV" action is triggered.                  |
| `ExportCsvTriggered`   | `EventCallback`                  | `null`        | Callback invoked when the "Export CSV" action is triggered.                |
| `Collection`           | `string?`                        | `null`        | The currently selected collection.                                         |
| `CollectionChanged`    | `EventCallback<string?>`         | `null`        | Callback invoked when the `Collection` value changes.                      |
| `Pivot`                | `string?`                        | `null`        | The currently selected pivot.                                              |
| `PivotChanged`         | `EventCallback<string>`          | `null`        | Callback invoked when the `Pivot` value changes.                           |
| `IsExportEnabled`      | `bool`                           | `false`       | Determines whether the export buttons are enabled.                         |
| `IsRefreshEnabled`     | `bool`                           | `true`        | Determines whether the "Refresh" button is enabled.                        |
| `Navigation`           | `Navigation<NavigationUnit>`     | `null!`       | Provides navigation functionality for managing pivot states.               |

## Properties  

| Property              | Type                              | Description                                                                 |
|-----------------------|-----------------------------------|-----------------------------------------------------------------------------|
| `SelectedCollectionNode` | `GroupedCollection?`           | The currently selected collection node.                                    |
| `SelectedPivotNode`      | `GroupedPivot?`                | The currently selected pivot node.                                         |
| `IsRefreshEnabledLocal`  | `bool`                         | Determines whether the "Refresh" button is enabled based on the current state. |

## Methods  

| Method                | Description                                                                 |
|-----------------------|-----------------------------------------------------------------------------|
| `OnAfterRenderAsync`  | Handles initialization logic after the component is rendered.              |

## Behavior  

- **Dynamic State Management**: The enabled/disabled state of buttons is determined by the `IsRefreshEnabled`, `IsExportEnabled`, and the selected collection/pivot.  
- **Collection and Pivot Initialization**: Automatically selects the first available collection and pivot if none are selected.  
- **Event Callbacks**: Triggers callbacks for actions such as refreshing, exporting, and copying data.  
- **Cache Toggle**: The `UseCache` parameter allows toggling the use of cached data, with changes triggering the `UseCacheChanged` callback.  

## Usage  

### Example: Basic Usage  
