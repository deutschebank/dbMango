# SimplePivotComponent  

The `SimplePivotComponent` is a Blazor component designed to render and manage a pivot table. It provides functionality for dynamic updates, filtering, and transposing pivot data.  

## Features  

- **Dynamic Updates**: Automatically updates the pivot table when relevant parameters change.  
- **Filtering**: Supports additional filtering using the `ExtraFilter` parameter.  
- **Data Transposition**: Allows transposing pivot data for alternate views.  
- **Event Handling**: Handles cell click events and column descriptor retrieval.  

## Parameters  

| Parameter             | Type                                      | Default Value | Description                                                                 |
|------------------------|-------------------------------------------|---------------|-----------------------------------------------------------------------------|
| `PivotService`        | `IPivotTableDataSource?`                  | `null`        | The service used to interact with pivot data.                              |
| `HandleCellClick`     | `Func<DynamicObject, string, Task<bool>>` | `(_, _) => Task.FromResult(false)` | Callback for handling cell click events.                                   |
| `GetColumnDescriptor` | `Func<string, PivotColumnDescriptor?>`    | `_ => null`   | Function to retrieve column descriptors.                                   |
| `EnableAutoUpdate`    | `bool`                                    | `true`        | Determines whether the pivot table updates automatically.                  |
| `Transpose`           | `bool`                                    | `false`       | Indicates whether to transpose the pivot data.                             |
| `Class`               | `string`                                  | `""`          | CSS class for the component's container.                                   |
| `CollectionName`      | `string?`                                 | `null`        | The name of the collection to use for the pivot.                           |
| `PivotName`           | `string?`                                 | `null`        | The name of the pivot to display.                                          |
| `ExtraFilter`         | `FilterExpressionTree.ExpressionGroup?`   | `null`        | Additional filter expression for the pivot.                                |
| `Collections`         | `List<GroupedCollection>`                 | `[]`          | The list of grouped collections available for the pivot.                   |
| `PivotData`           | `IPivotedData?`                           | `null`        | The pivoted data to display.                                               |
| `PivotDataChanged`    | `EventCallback<IPivotedData>`             | `null`        | Event callback triggered when the pivot data changes.                      |

## Properties  

| Property                | Type                        | Description                                                                 |
|--------------------------|-----------------------------|-----------------------------------------------------------------------------|
| `_noFilter`             | `FilterExpressionTree.ExpressionGroup` | Represents an empty filter expression.                                     |
| `PivotTable`            | `PivotTableComponent?`      | Reference to the underlying pivot table component.                         |

## Methods  

| Method              | Description                                                                 |
|----------------------|-----------------------------------------------------------------------------|
| `UpdatePivot`       | Updates the pivot table state and refreshes the data if `EnableAutoUpdate` is true. |
| `Refresh`           | Asynchronously refreshes the pivot data based on the current parameters.    |

## Behavior  

- **Dynamic State Updates**: The component automatically updates its state when parameters like `PivotService`, `CollectionName`, `PivotName`, or `ExtraFilter` change.  
- **Error Handling**: Logs errors using `log4net` when exceptions occur during pivot updates.  
- **Pivot Selection**: Dynamically selects the appropriate pivot and collection nodes based on the provided parameters.  
- **Data Transposition**: Transposes the pivot data if the `Transpose` parameter is set to `true`.  

## Usage  

### Example: Basic Usage  
