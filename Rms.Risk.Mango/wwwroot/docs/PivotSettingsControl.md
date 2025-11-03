# PivotSettingsControl  

The `PivotSettingsControl` is a Blazor component designed to provide a user interface for configuring pivot settings. It organizes settings into tabs for filtering, keys, data, queries, drilldowns, and options.  

## Features  

- **Tab-Based Layout**: Provides a tabbed interface for organizing pivot settings.  
- **Dynamic Updates**: Automatically updates the UI when the `Pivot` or `SelectedCollectionNode` parameters change.  
- **Filter Management**: Supports dynamic filtering with customizable fields.  
- **Key and Data Field Selection**: Allows users to select key and data fields for the pivot.  
- **Query Configuration**: Enables users to configure and preview queries.  
- **Drilldown and Options**: Includes tabs for drilldown configuration and additional pivot options.  

## Parameters  

| Parameter             | Type                                      | Default Value | Description                                                                 |
|------------------------|-------------------------------------------|---------------|-----------------------------------------------------------------------------|
| `SelectedCollectionNode` | `GroupedCollection?`                   | `null`        | The currently selected collection node.                                    |
| `Pivot`               | `PivotDefinition?`                        | `null`        | The pivot definition being configured.                                     |
| `GetExtraFilter`      | `Func<FilterExpressionTree.ExpressionGroup?>` | `() => null` | A function to retrieve additional filters for the query.                   |
| `PivotService`        | `IPivotTableDataSource`                   | `null!`       | The service used to interact with pivot data.                              |
| `Vertical`            | `bool`                                   | `false`       | Determines whether the tabs are displayed vertically.                      |

## Properties  

| Property                | Type                        | Description                                                                 |
|--------------------------|-----------------------------|-----------------------------------------------------------------------------|
| `Filter`                | `string`                   | The filter string for the pivot.                                           |
| `AllDataFields`         | `HashSet<string>`           | The set of all data fields available in the selected collection.           |
| `AllKeyFields`          | `HashSet<string>`           | The set of all key fields available in the selected collection.            |
| `AllPivots`             | `List<GroupedPivot>`        | The list of all pivots in the selected collection.                         |
| `CurrentPivotKeyFields` | `string[]`                 | The currently selected key fields for the pivot.                           |
| `CurrentPivotDataFields`| `string[]`                 | The currently selected data fields for the pivot.                          |
| `SelectedCollectionName`| `string?`                  | The name of the currently selected collection, if applicable.              |

## Methods  

| Method              | Description                                                                 |
|----------------------|-----------------------------------------------------------------------------|
| `GetAllFields`      | Retrieves all fields from the selected collection, including their types.   |
| `GetQueryText`      | Asynchronously generates the query text for the pivot configuration.        |

## Behavior  

- **Dynamic State Updates**: The component automatically updates its state when the `Pivot` or `SelectedCollectionNode` parameters change.  
- **Field Type Resolution**: Dynamically resolves field types for filtering and query generation.  
- **Tab Persistence**: Maintains the state of all tabs to ensure a seamless user experience.  

## Usage  

### Example: Basic Usage  
