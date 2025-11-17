# PivotFilterComponent  

The `PivotFilterComponent` is a Blazor component designed to manage and configure filter expressions for pivot definitions. It provides an interface for dynamically binding and updating filter conditions associated with a pivot table.  

## Features  
- **Dynamic Filter Management**: Allows users to define and update filter expressions dynamically.  
- **Two-Way Binding**: Supports two-way binding for filter expressions.  
- **Pivot Integration**: Automatically updates the associated pivot definition when filters are modified.  

## Parameters  

| Parameter                  | Type                                      | Default Value | Description                                                                 |
|----------------------------|-------------------------------------------|---------------|-----------------------------------------------------------------------------|
| `Class`                    | `string`                                 | `""`          | CSS class to apply to the root container of the component.                 |
| `RowClass`                 | `string`                                 | `""`          | CSS class to apply to rows within the component.                           |
| `AllFields`                | `Dictionary<string, Type>`               | `null!`       | A dictionary of all available fields and their types for filter expressions. |
| `Pivot`                    | `PivotDefinition?`                       | `null`        | The pivot definition to configure filter expressions for.                  |
| `FilterChanged`            | `EventCallback<string>`                  | `null`        | Callback invoked when the filter expression is updated.                    |

## Properties  

| Property                  | Type                                      | Description                                                                 |
|---------------------------|-------------------------------------------|-----------------------------------------------------------------------------|
| `Filter`                  | `FilterExpressionTree.ExpressionGroup`   | The current filter expression group.                                       |

## Methods  

| Method                  | Description                                                                 |
|--------------------------|-----------------------------------------------------------------------------|
| `UpdateFilter`          | Updates the pivot definition with the current filter expression and invokes the `FilterChanged` callback. |

## Behavior  

- When the `Pivot` parameter is set, the component initializes the `Filter` property based on the `Pivot.Filter` value.  
- Changes to the `Filter` property automatically trigger the `UpdateFilter` method, which updates the `Pivot.Filter` and invokes the `FilterChanged` callback.  
- The component ensures that the UI reflects the latest filter state by calling `StateHasChanged` when necessary.  

## Usage  

### Example: Basic Usage  
