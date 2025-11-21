# PivotSelectorComponent  

The `PivotSelectorComponent` is a Blazor component designed to provide a user interface for selecting a collection and its associated pivot. It supports dynamic updates based on user selection and provides cascading dropdowns for hierarchical data selection.  

## Features  

- **Collection Selection**: Allows users to select a collection from a list of grouped collections.  
- **Pivot Selection**: Dynamically updates the list of pivots based on the selected collection.  
- **Cascading Parameters**: Supports cascading parameters for modal integration.  
- **Dynamic State Updates**: Automatically synchronizes the selected collection and pivot with the provided parameters.  

## Parameters  

| Parameter         | Type                        | Default Value | Description                                                                 |
|--------------------|-----------------------------|---------------|-----------------------------------------------------------------------------|
| `ShowCollection`  | `bool`                      | `true`        | Determines whether the collection dropdown is displayed.                   |
| `ShowPivot`       | `bool`                      | `true`        | Determines whether the pivot dropdown is displayed.                        |
| `Class`           | `string`                   | `"form-row"`  | CSS class to apply to the root container of the component.                 |
| `Collection`      | `string?`                  | `null`        | The name of the currently selected collection.                             |
| `Pivot`           | `string?`                  | `null`        | The name of the currently selected pivot.                                  |
| `Collections`     | `List<GroupedCollection>`  | `[]`          | The list of available grouped collections.                                 |
| `CollectionChanged` | `EventCallback<string?>` | `null`        | Callback invoked when the selected collection changes.                     |
| `PivotChanged`    | `EventCallback<string>`    | `null`        | Callback invoked when the selected pivot changes.                          |

## Properties  

| Property                | Type                 | Description                                                                 |
|--------------------------|----------------------|-----------------------------------------------------------------------------|
| `SelectedCollectionNode`| `GroupedCollection?` | The currently selected collection node.                                    |
| `SelectedPivotNode`      | `GroupedPivot?`      | The currently selected pivot node.                                         |

## Behavior  

- **Cascading Dropdowns**: The pivot dropdown dynamically updates based on the selected collection.  
- **State Synchronization**: The component synchronizes its internal state with the `Collection` and `Pivot` parameters.  
- **Selectable Filtering**: Filters out non-selectable groups for both collections and pivots.  
- **Modal Integration**: Supports cascading modal services for additional UI interactions.  

## Methods  

| Method              | Description                                                                 |
|----------------------|-----------------------------------------------------------------------------|
| `SyncSelectedNodes` | Synchronizes the selected collection and pivot nodes based on the parameters. |
| `IsSelectableGroup` | Determines if a collection group is selectable.                             |
| `IsSelectablePivot` | Determines if a pivot group is selectable.                                  |

## Usage  

### Example: Basic Usage  
