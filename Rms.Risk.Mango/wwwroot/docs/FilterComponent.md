# FilterComponent  

The `FilterComponent` is a Blazor component designed to provide a dynamic and user-friendly interface for creating and managing filter expressions. It supports hierarchical filter structures, allowing users to define complex filtering logic for data operations.  

## Features  
- **Dynamic Filter Creation**: Add, edit, and delete filter groups and field conditions dynamically.  
- **Hierarchical Filters**: Supports nested filter groups for advanced filtering logic.  
- **Customizable Fields**: Dynamically populate filter fields based on the provided data source.  
- **Condition Types**: Supports a variety of condition types for field expressions.  
- **Event Callbacks**: Notifies parent components of filter changes.  

## Parameters  

| Parameter                  | Type                                      | Default Value | Description                                                                 |
|----------------------------|-------------------------------------------|---------------|-----------------------------------------------------------------------------|
| `Class`                    | `string`                                 | `""`          | CSS class to apply to the root container of the component.                 |
| `RowClass`                 | `string`                                 | `""`          | CSS class to apply to each row in the filter tree.                         |
| `AllFields`                | `Dictionary<string, Type>`               | `null!`       | A dictionary of all available fields for filtering.                       |
| `Filter`                   | `FilterExpressionTree.ExpressionGroup`   | `new()`       | The root filter expression group.                                          |
| `FilterChanged`            | `EventCallback<FilterExpressionTree.ExpressionGroup>` | `null` | Callback invoked when the filter changes.                                  |

## Methods  

| Method                  | Description                                                                 |
|--------------------------|-----------------------------------------------------------------------------|
| `AddGroup`              | Adds a new filter group to the specified parent group.                     |
| `AddCondition`          | Adds a new field condition to the specified parent group.                  |
| `DelCondition`          | Deletes a filter group or field condition from the tree.                  |
| `ChangeOperation`       | Updates the condition type of a filter group or field condition.           |
| `ChangeField`           | Updates the field of a field condition.                                   |
| `ChangeArgument`        | Updates the argument value of a field condition.                          |
| `UpdateFilter`          | Invokes the `FilterChanged` callback to notify parent components.          |

# ExtraFilterDefinition  

The `ExtraFilterDefinition` class is a utility designed to manage and manipulate filter definitions and their corresponding values. It provides methods to parse, create, and transform filter data into query parameters or hierarchical filter structures.  

## Features  
- **Filter Parsing**: Converts query parameters or hierarchical filter structures into `FilterValue` objects.  
- **Query Parameter Generation**: Converts `FilterValue` objects into query parameters for use in data operations.  
- **Filter Expression Tree Integration**: Supports creating and parsing hierarchical filter structures using `FilterExpressionTree.ExpressionGroup`.  
- **Dynamic Filter Configuration**: Allows dynamic configuration of filter controls, including dropdowns and date pickers.  

## Classes  

### `FilterControl`  
Represents the definition of a filter control, including its type, display name, and configuration.  

| Property              | Type              | Default Value | Description                                                                 |
|-----------------------|-------------------|---------------|-----------------------------------------------------------------------------|
| `ControlType`         | `string`         | `""`          | The type of control (e.g., `DropDown`, `DatePicker`).                       |
| `AllowMultiselect`    | `bool`           | `false`       | Indicates whether multiple selections are allowed.                         |
| `DisplayName`         | `string`         | `""`          | The display name of the filter control.                                    |
| `FieldName`           | `string`         | `""`          | The field name associated with the filter control.                         |
| `DefaultValue`        | `string?`        | `null`        | The default value for the filter control.                                  |
| `Format`              | `string`         | `""`          | The format string for the filter control.                                  |
| `SelectorCollection`  | `string?`        | `null`        | The collection used for selector-based controls.                           |
| `SelectorQuery`       | `string?`        | `null`        | The query used for selector-based controls.                                |
| `Values`              | `List<string>`   | `[]`          | The list of values available for the filter control.                       |  

### `FilterValue`  
Represents the value of a filter, including its selected values or range.  

| Property              | Type              | Default Value | Description                                                                 |
|-----------------------|-------------------|---------------|-----------------------------------------------------------------------------|
| `MultipleSelected`    | `bool`           | `false`       | Indicates whether multiple values are selected.                            |
| `Value`               | `HashSet<string>?`| `null`        | The selected values for the filter.                                        |
| `RangeStart`          | `string?`        | `null`        | The start of the range for range-based filters.                            |
| `RangeEnd`            | `string?`        | `null`        | The end of the range for range-based filters.                              |  

## Methods  

### `ParseQueryParameters`  
Parses a dictionary of query parameters into a list of `FilterValue` objects.  

**Parameters**:  
- `queryParameters`: `Dictionary<string, string>` - The query parameters to parse.  

**Returns**:  
- `List<FilterValue>` - The parsed filter values.  

### `CreateQueryParameters`  
Creates a dictionary of query parameters from a list of `FilterValue` objects.  

**Parameters**:  
- `values`: `List<FilterValue>` - The filter values to convert.  

**Returns**:  
- `Dictionary<string, string>` - The generated query parameters.  

### `ParseExtraFilter`  
Parses a `FilterExpressionTree.ExpressionGroup` into a list of `FilterValue` objects.  

**Parameters**:  
- `extraFilter`: `FilterExpressionTree.ExpressionGroup` - The filter expression group to parse.  

**Returns**:  
- `List<FilterValue>` - The parsed filter values.  

### `CreateExtraFilter`  
Creates a `FilterExpressionTree.ExpressionGroup` from a list of `FilterValue` objects.  

**Parameters**:  
- `values`: `List<FilterValue>` - The filter values to convert.  

**Returns**:  
- `FilterExpressionTree.ExpressionGroup` - The generated filter expression group.  

## Constants  

| Constant Name               | Value         | Description                                                                 |
|-----------------------------|---------------|-----------------------------------------------------------------------------|
| `ControlTypeDropDown`       | `"DropDown"`  | Represents a dropdown control type.                                        |
| `ControlTypeDatePicker`     | `"DatePicker"`| Represents a date picker control type.                                     |
| `CurrentCollectionSignature`| `"[COLLECTION]"` | Represents the current collection signature.                              |
| `Any`                       | `"<Any>"`     | Represents a wildcard value for filters.                                   |  

## Usage  

### Example: Parsing Query Parameters  