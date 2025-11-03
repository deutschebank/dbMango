# PivotGenericExtraFilterControl  

The `PivotGenericExtraFilterControl` is a Blazor component designed to provide a dynamic and configurable interface for managing extra filters in pivot tables. It supports multiple filter types, including dropdowns and date pickers, and integrates seamlessly with pivot table data sources.  

## Features  
- **Dynamic Filter Rendering**: Automatically renders filters based on the provided filter definitions.  
- **Support for Multiple Control Types**: Includes dropdowns (single and multi-select) and date pickers.  
- **Two-Way Binding**: Supports two-way binding for filter values.  
- **Integration with Pivot Data Source**: Dynamically loads filter values from the pivot data source.  
- **Query Parameter Parsing**: Parses query parameters to initialize filter values.  

## Parameters  

| Parameter                  | Type                                      | Default Value | Description                                                                 |
|----------------------------|-------------------------------------------|---------------|-----------------------------------------------------------------------------|
| `Class`                    | `string`                                 | `""`          | CSS class to apply to the root container of the component.                 |
| `Collection`               | `string`                                 | `""`          | The collection name used to fetch filter values dynamically.               |
| `FilterDef`                | `ExtraFilterDefinition`                  | `new()`       | The filter definition containing filter configurations.                    |
| `ExtraFilter`              | `FilterExpressionTree.ExpressionGroup`   | `new()`       | The current extra filter expression group.                                 |
| `ExtraFilterChanged`       | `EventCallback<FilterExpressionTree.ExpressionGroup>` | `null` | Callback invoked when the extra filter is updated.                         |
| `PivotService`             | `IPivotTableDataSource?`                 | `null`        | The pivot table data source used to fetch filter values.                   |
| `LoadFilterDef`            | `bool`                                   | `false`       | Indicates whether to load the filter definition dynamically.               |

## Properties  

| Property                  | Type                                      | Description                                                                 |
|---------------------------|-------------------------------------------|-----------------------------------------------------------------------------|
| `_values`                 | `List<ExtraFilterDefinition.FilterValue>` | The current values of the filters.                                         |
| `_queryParameters`        | `Dictionary<string, string>`              | Query parameters parsed from the URL.                                      |
| `_extraFilterJson`        | `string`                                  | JSON representation of the current extra filter.                           |

## Methods  

| Method                     | Description                                                                 |
|----------------------------|-----------------------------------------------------------------------------|
| `OnInitialized`            | Initializes the component and parses query parameters from the URL.        |
| `OnParametersSetAsync`     | Ensures the extra filter is parsed and updated when parameters are set.     |
| `OnAfterRenderAsync`       | Reloads the filter definition on the first render if required.              |
| `ReloadFilterDefinition`   | Reloads the filter definition from the pivot data source.                  |
| `ParseExtraFilter`         | Parses the extra filter from query parameters or the provided filter group. |
| `UpdateExtraFilter`        | Updates the extra filter and invokes the `ExtraFilterChanged` callback.    |
| `UpdateAvailableSelectorValues` | Dynamically loads selector values for filters.                        |
| `LoadSelectorValues`       | Fetches distinct values for a filter from the pivot data source.           |
| `IsDateAvailable`          | Checks if a date is available based on filter values.                     |
| `OnSelectedValueChanged`   | Handles changes to selected values in multi-select dropdowns.             |
| `SelectElement`            | Handles selection of a single value in dropdowns.                         |
| `SelectStart`              | Updates the start date for date range filters.                            |
| `SelectEnd`                | Updates the end date for date range filters.                              |

## Behavior  

- **Dynamic Filter Rendering**: The component dynamically renders filters based on the `FilterDef` parameter. Supported control types include dropdowns (single and multi-select) and date pickers.  
- **Filter Value Updates**: Changes to filter values automatically update the `ExtraFilter` property and invoke the `ExtraFilterChanged` callback.  
- **Dynamic Value Loading**: If a filter's values are not pre-defined, the component fetches them dynamically from the `PivotService`.  
- **Query Parameter Parsing**: The component initializes filter values based on query parameters in the URL.  

## Usage  

### Example: Basic Usage  
