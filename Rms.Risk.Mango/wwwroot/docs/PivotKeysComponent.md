# PivotKeysComponent  

The `PivotKeysComponent` is a Blazor component designed to manage and display a list of selectable keys. It provides a drag-and-drop interface for reordering items and allows users to toggle the selection state of each key.  

## Features  
- **Drag-and-Drop Support**: Enables reordering of items in the list.  
- **Two-Way Binding**: Supports two-way binding for the selected keys.  
- **Dynamic Key Management**: Automatically synchronizes the displayed items with the provided key list.  
- **Selection Toggle**: Allows users to toggle the selection state of individual keys.  

## Parameters  

| Parameter      | Type                          | Default Value | Description                                                                 |
|----------------|-------------------------------|---------------|-----------------------------------------------------------------------------|
| `AllKeys`      | `IReadOnlyCollection<string>` | `null!`       | The complete list of available keys.                                       |
| `Value`        | `string[]`                   | `[]`          | The currently selected keys.                                               |
| `ValueChanged` | `EventCallback<string[]>`     | `null`        | Callback invoked when the selected keys are updated.                       |
| `Class`        | `string`                     | `""`          | CSS class to apply to the root container of the component.                 |
| `LabelClass`   | `string`                     | `""`          | CSS class to apply to the labels of the items.                             |

## Properties  

| Property       | Type                          | Description                                                                 |
|----------------|-------------------------------|-----------------------------------------------------------------------------|
| `Items`        | `List<ItemWithSelection>`     | The list of items displayed in the component, including their selection state. |

## Behavior  

- **Dynamic Item Synchronization**: The component synchronizes its internal list of items (`Items`) with the `AllKeys` and `Value` parameters.  
- **Selection State Management**: The `IsSelected` property of each item is bound to a checkbox, allowing users to toggle the selection state.  
- **Change Notification**: Changes to the selection state or item order trigger the `ValueChanged` callback.  
- **Drag-and-Drop**: Items can be reordered using drag-and-drop functionality provided by the `DragDropList` component.  

## Methods  

| Method           | Description                                                                 |
|------------------|-----------------------------------------------------------------------------|
| `OnChanged`      | Handles changes to the selection state of an item and updates the `Value`. |
| `SyncToItems`    | Synchronizes the internal list of items with the `Value` and `AllKeys`.    |
| `SyncToValue`    | Generates the `Value` array based on the selected items.                   |
| `Same`           | Compares two lists or arrays for equality.                                |

## Usage  

### Example: Basic Usage  
