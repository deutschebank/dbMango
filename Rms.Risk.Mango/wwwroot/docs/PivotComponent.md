# PivotComponent  

The `PivotComponent` is a Blazor component designed to manage and display pivot tables with advanced features such as filtering, navigation, sharing, and exporting. It provides a user-friendly interface for interacting with pivoted data and supports dynamic updates, caching, and user-specific configurations.  

## Features  
- **Pivot Navigation**: Navigate between collections and pivots dynamically.  
- **Filtering**: Show or hide filters and apply extra filters to pivot data.  
- **Export and Copy**: Export pivot data to CSV or copy it to the clipboard.  
- **Sharing**: Share pivots with other users via a generated URL.  
- **Save and Delete**: Save pivots, create new copies, or delete existing ones.  
- **Dynamic Updates**: Automatically update the UI when pivot data changes.  
- **Authorization**: Restrict certain actions based on user roles and policies.  

## Parameters  

| Parameter                  | Type                                      | Default Value | Description                                                                 |
|----------------------------|-------------------------------------------|---------------|-----------------------------------------------------------------------------|
| `PivotType`                | `IPivotTableDataSource.PivotType`         | `Predefined`  | The type of pivot (e.g., Predefined, User).                                |
| `ExtraFilter`              | `RenderFragment`                         | `null`        | Additional filter UI to display.                                           |
| `PivotData`                | `IPivotedData?`                          | `null`        | The pivoted data to display.                                               |
| `PivotDataChanged`         | `EventCallback<IPivotedData>`            | `null`        | Callback invoked when `PivotData` changes.                                 |
| `CurrentPivot`             | `PivotDefinition?`                       | `null`        | The currently displayed pivot definition.                                  |
| `CurrentPivotChanged`      | `EventCallback<PivotDefinition>`          | `null`        | Callback invoked when `CurrentPivot` changes.                              |
| `PivotService`             | `IPivotTableDataSource`                  | `null`        | The service used to fetch and manage pivot data.                           |
| `ShowCollection`           | `bool`                                   | `true`        | Whether to show the collection selector.                                   |
| `Collection`               | `string?`                                | `null`        | The currently selected collection.                                         |
| `CollectionChanged`        | `EventCallback<string>`                  | `null`        | Callback invoked when `Collection` changes.                                |
| `Pivot`                    | `string?`                                | `null`        | The currently selected pivot.                                              |
| `PivotChanged`             | `EventCallback<string>`                  | `null`        | Callback invoked when `Pivot` changes.                                     |
| `LastRefresh`              | `DateTime`                               | `default`     | The timestamp of the last refresh.                                         |
| `LastRefreshChanged`       | `EventCallback<DateTime>`                | `null`        | Callback invoked when `LastRefresh` changes.                               |
| `LastRefreshElapsed`       | `TimeSpan`                               | `TimeSpan.Zero`| The duration of the last refresh.                                          |
| `LastRefreshElapsedChanged`| `EventCallback<TimeSpan>`                | `null`        | Callback invoked when `LastRefreshElapsed` changes.                        |
| `Navigation`               | `Navigation<NavigationUnit>?`            | `null`        | Navigation object for managing pivot navigation.                           |
| `NavigationChanged`        | `EventCallback<Navigation<NavigationUnit>>`| `null`      | Callback invoked when `Navigation` changes.                                |
| `AllPivotsAccessPolicyName`| `string?`                                | `null`        | The policy name for accessing all pivots.                                  |
| `GetExtraFilter`           | `Func<FilterExpressionTree.ExpressionGroup?>`| `null`    | Function to retrieve additional filters.                                   |
| `UseCache`                 | `bool`                                   | `true`        | Whether to use cached data.                                                |
| `UseCacheChanged`          | `EventCallback<bool>`                    | `null`        | Callback invoked when `UseCache` changes.                                  |
| `Rows`                     | `int`                                    | `40`          | The number of rows to display.                                             |
| `RowsChanged`              | `EventCallback<int>`                     | `null`        | Callback invoked when `Rows` changes.                                      |
| `Collections`              | `List<GroupedCollection>`                | `[]`          | The list of available collections.                                         |

## Methods  

| Method                  | Description                                                                 |
|--------------------------|-----------------------------------------------------------------------------|
| `NavigateTo`            | Navigates to a specific collection and pivot.                              |
| `OnCopyCsv`             | Copies the pivot data to the clipboard in CSV format.                      |
| `OnExportCsv`           | Exports the pivot data to a CSV file.                                      |
| `ShowHideFilter`        | Toggles the visibility of the filter panel.                                |
| `OnRefreshPivot`        | Refreshes the pivot data.                                                  |
| `OnSave`                | Saves the current pivot.                                                   |
| `OnSaveAs`              | Saves the current pivot as a new pivot.                                    |
| `OnShare`               | Shares the current pivot via a generated URL.                              |
| `OnDelete`              | Deletes the current pivot.                                                 |
| `NavigateToSharedPivot` | Navigates to a shared pivot using a unique identifier.                     |

## Usage  

### Basic Example  
