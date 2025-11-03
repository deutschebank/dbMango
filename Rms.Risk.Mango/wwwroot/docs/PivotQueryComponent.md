# PivotQueryComponent  

The `PivotQueryComponent` is a Blazor component designed to provide a user interface for configuring and querying pivot table definitions. It supports multiple pivot types and allows users to define custom queries, aggregation logic, and grouping options.  

## Features  

- **Pivot Type Selection**: Allows users to select between different pivot types such as Simple Aggregation, Custom Query, and Aggregation for Humans.  
- **Dynamic Query Configuration**: Provides text areas for defining `Before Grouping`, `Additional Grouping`, and `After Grouping` logic.  
- **Custom Query Support**: Enables users to define and edit custom queries directly.  
- **Query Preview**: Allows users to reveal the generated query for the current pivot configuration.  
- **Modal Integration**: Displays query results or exceptions in a modal dialog.  

## Parameters  

| Parameter       | Type                                                                 | Default Value | Description                                                                 |
|------------------|----------------------------------------------------------------------|---------------|-----------------------------------------------------------------------------|
| `Class`         | `string`                                                             | `""`          | CSS class to apply to the root container of the component.                 |
| `Pivot`         | `PivotDefinition?`                                                  | `null`        | The pivot definition object to configure.                                  |
| `PivotService`  | `IPivotTableDataSource?`                                            | `null`        | The data source service for pivot operations.                              |
| `Collection`    | `string?`                                                           | `null`        | The name of the collection to query.                                       |
| `ExtraFilter`   | `FilterExpressionTree.ExpressionGroup?`                             | `null`        | Additional filter expressions to apply to the query.                       |
| `GetQueryText`  | `Func<string, PivotDefinition, FilterExpressionTree.ExpressionGroup?, CancellationToken, Task<string>>` | A default function returning an empty string. | A delegate to retrieve the query text for the current pivot configuration. |

## Properties  

| Property        | Type     | Description                                                                 |
|------------------|----------|-----------------------------------------------------------------------------|
| `SimpleClass`   | `string` | CSS class to toggle visibility of Simple Aggregation fields.                |
| `CustomClass`   | `string` | CSS class to toggle visibility of Custom Query fields.                      |
| `PivotTypeName` | `string` | The display name of the currently selected pivot type.                      |

## Behavior  

- **Dynamic State Updates**: The component dynamically updates its state when the `Pivot` parameter changes.  
- **Pivot Type Visibility**: Fields for `Simple Aggregation` and `Custom Query` are conditionally displayed based on the selected pivot type.  
- **Query Execution**: The `RevealTheQuery` method generates and displays the query for the current pivot configuration.  
- **JavaScript Interop**: Supports JavaScript-based code editors for advanced query editing.  

## Methods  

| Method                     | Description                                                                 |
|-----------------------------|-----------------------------------------------------------------------------|
| `SelectPivotType`          | Updates the pivot type and refreshes the component state.                   |
| `RevealTheQuery`           | Retrieves and displays the query for the current pivot configuration.       |
| `ShowQueryDialog`          | Displays the query or error message in a modal dialog.                      |
| `UpdateBeforeGroupingField`| Updates the `BeforeGrouping` field dynamically.                             |
| `UpdateAdditionalGroupingField` | Updates the `WithinGrouping` field dynamically.                        |
| `UpdateCustomQueryField`   | Updates the `CustomQuery` field dynamically.                                |

## Usage  

### Example: Basic Usage  
