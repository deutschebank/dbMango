# PivotDrilldownComponent  

The `PivotDrilldownComponent` is a Blazor component designed to manage and configure drilldown rules for pivot definitions. It provides an interface for selecting fields, defining drilldown conditions, and associating them with specific pivot tables.  

## Features  
- **Drilldown Field Selection**: Allows users to select fields for drilldown operations.  
- **Dynamic Drilldown Configuration**: Add, edit, and delete drilldown rules dynamically.  
- **Pivot Association**: Associate drilldown rules with specific pivot tables.  
- **Customizable Conditions**: Define conditions and filters for drilldown operations.  

## Parameters  

| Parameter                  | Type                                      | Default Value | Description                                                                 |
|----------------------------|-------------------------------------------|---------------|-----------------------------------------------------------------------------|
| `Class`                    | `string`                                 | `""`          | CSS class to apply to the root container of the component.                 |
| `Pivot`                    | `PivotDefinition?`                       | `null!`       | The pivot definition to configure drilldown rules for.                     |
| `AllPivots`                | `List<GroupedPivot>`                     | `[]`          | A list of all available pivots for drilldown association.                  |

## Properties  

| Property                  | Type                                      | Description                                                                 |
|---------------------------|-------------------------------------------|-----------------------------------------------------------------------------|
| `CurrentDrilldownDef`     | `PivotDefinition.DrilldownDef?`           | The currently selected drilldown definition.                               |
| `CurrentField`            | `string`                                 | The name of the currently selected drilldown field.                        |
| `IsDeleteDisabled`        | `bool`                                   | Indicates whether the delete button is disabled.                           |

## Methods  

| Method                  | Description                                                                 |
|--------------------------|-----------------------------------------------------------------------------|
| `SelectDrilldown`       | Selects a drilldown definition and updates the UI.                         |
| `SelectDrilldownPivot`  | Associates a pivot table with the current drilldown definition.            |
| `OnDelete`              | Deletes the currently selected drilldown definition.                      |
| `OnAdd`                 | Adds a new drilldown definition to the pivot.                             |

## UI Elements  

### Drilldown Field Selection  
- A dropdown menu to select the field for drilldown.  
- Buttons to add or delete drilldown rules.  

### Drilldown Rule Configuration  
- Input fields for specifying the field name and conditions.  
- Dropdown menu to associate a pivot table with the drilldown rule.  
- Textareas for defining filters and conditions.  

## Usage  

### Example: Adding a Drilldown Rule  
1. Select a field from the dropdown menu.  
2. Click the "Add" button to create a new drilldown rule.  
3. Configure the field name, associated pivot, and conditions.  

### Example: Deleting a Drilldown Rule  
1. Select an existing drilldown rule.  
2. Click the "Delete" button to remove the rule.  

## Notes  
- The component dynamically updates the UI when drilldown rules are added, edited, or deleted.  
- Ensure that the `Pivot` parameter is set to a valid `PivotDefinition` instance before using the component.  