# PivotDefinition  

The `PivotDefinition` class is a core component for defining and managing pivot table configurations. It supports various pivot types, including simple aggregations, custom queries, and human-readable aggregations.  

## Features  

- **Pivot Types**: Supports multiple pivot types via the `PivotTypeEnum` enumeration.  
- **Cloning**: Provides deep cloning functionality for creating independent copies of pivot definitions.  
- **JSON Conversion**: Converts pivot definitions to JSON for MongoDB aggregation pipelines.  
- **Drilldown Support**: Allows defining drilldown configurations for detailed data exploration.  
- **Custom Query Handling**: Supports dynamic replacement of placeholders in custom queries.  

## Enums  

### PivotTypeEnum  

| Value                  | Description                                                                 |
|-------------------------|-----------------------------------------------------------------------------|
| `Unknown`              | Default value for undefined pivot types.                                   |
| `SimpleAggregation`    | Represents a simple aggregation pivot.                                     |
| `CustomQuery`          | Represents a pivot based on a custom query.                                |
| `AggregationForHumans` | Represents a human-readable aggregation pivot.                             |  

## Properties  

| Property                  | Type                        | Default Value       | Description                                                                 |
|---------------------------|-----------------------------|---------------------|-----------------------------------------------------------------------------|
| `Name`                   | `string`                   | `"New pivot"`       | The name of the pivot definition.                                          |
| `Group`                  | `string`                   | `"User pivots"`     | The group to which the pivot belongs.                                      |
| `UserVisible`            | `bool`                     | `true`              | Indicates whether the pivot is visible to users.                           |
| `KeyFields`              | `string[]`                 | `[]`                | The fields used as keys in the pivot.                                      |
| `DataFields`             | `string[]`                 | `[]`                | The fields used as data in the pivot.                                      |
| `Filter`                 | `string`                   | `""`                | The filter expression for the pivot.                                       |
| `DrilldownFilter`        | `string`                   | `""`                | The filter expression for drilldowns.                                      |
| `BeforeGrouping`         | `string`                   | `""`                | Additional operations before grouping.                                     |
| `WithinGrouping`         | `string`                   | `""`                | Additional operations within grouping.                                     |
| `AfterGrouping`          | `string`                   | `""`                | Additional operations after grouping.                                      |
| `CustomQuery`            | `string`                   | `""`                | The custom query for the pivot.                                            |
| `Highlighting`           | `Highlighting`             | `new Highlighting()`| Highlighting configuration for the pivot.                                  |
| `AllowDiskUsage`         | `bool`                     | `false`             | Indicates whether disk usage is allowed for the pivot.                     |
| `ShowTotals`             | `bool`                     | `true`              | Indicates whether to display totals in the pivot.                          |  

## Methods  

| Method                     | Return Type               | Description                                                                 |
|-----------------------------|---------------------------|-----------------------------------------------------------------------------|
| `Clone()`                  | `PivotDefinition`         | Creates a deep copy of the pivot definition.                               |
| `ToJson()`                 | `string`                 | Converts the pivot definition to a JSON representation.                    |
| `GetFilterExpression()`    | `string`                 | Generates the filter expression for the pivot.                             |
| `CompareTo()`              | `int`                    | Compares the current pivot definition with another for sorting.            |  

## Nested Classes  

### DrilldownDef  

Represents the configuration for drilldown functionality in a pivot.  

| Property                  | Type                        | Default Value       | Description                                                                 |
|---------------------------|-----------------------------|---------------------|-----------------------------------------------------------------------------|
| `ColumnName`             | `string`                   | `""`                | The column to drill down to.                                               |
| `DrilldownCondition`     | `string`                   | `""`                | The condition for the drilldown.                                           |
| `AppendToBeforeGrouping` | `string`                   | `""`                | Additional operations to append before grouping.                           |
| `DrilldownPivot`         | `string`                   | `""`                | The pivot definition to use for the drilldown.                             |  

## Behavior  

- **Dynamic JSON Conversion**: The `ConvertToJson` delegate allows dynamic customization of JSON conversion logic.  
- **Drilldown Handling**: Supports defining and applying drilldown configurations for detailed data exploration.  
- **Custom Query Replacement**: Dynamically replaces placeholders in custom queries with actual values.  

## Usage  

### Example: Creating a Simple Aggregation Pivot  
