# PivotOptionsComponent

The `PivotOptionsComponent` is a Blazor component designed to provide a user interface for configuring pivot table options. It allows users to customize various aspects of a pivot table, such as column order, renaming, highlighting, and visualization settings.

## Features

- **User Visibility Toggle**: Allows toggling the visibility of the pivot for users.
- **Column Customization**: Supports reordering and renaming columns.
- **Highlighting**: Enables highlighting the top/bottom N% of data.
- **Totals Row**: Provides an option to show or hide the totals row.
- **2D Pivot Configuration**: Allows creating and configuring 2D pivots with row headers, column headers, and data fields.
- **Line Chart Configuration**: Enables creating line charts with options for labels, data, legends, and visual styles.

## Parameters

| Parameter  | Type                | Default Value | Description                                                                 |
|------------|---------------------|---------------|-----------------------------------------------------------------------------|
| `Class`    | `string`            | `""`          | CSS class to apply to the root container of the component.                 |
| `Pivot`    | `PivotDefinition?`  | `null`        | The pivot definition object to configure.                                  |

## Properties

| Property                  | Type                | Description                                                                 |
|---------------------------|---------------------|-----------------------------------------------------------------------------|
| `RenameColumns`           | `string`           | A newline-separated string for renaming columns in the format `key=value`. |
| `ColumnOrder`             | `string`           | A newline-separated string for specifying the order of columns.            |
| `Pivot2DRows`             | `string`           | A newline-separated string for specifying row headers in a 2D pivot.       |
| `Pivot2DCol`              | `string`           | The column header for a 2D pivot.                                          |
| `Pivot2DData`             | `string`           | The data field for a 2D pivot.                                             |
| `Pivot2DDataTypeColumn`   | `string`           | The data type column for a 2D pivot.                                       |
| `LineChartXAxis`          | `string`           | The X-axis labels for a line chart.                                        |
| `LineChartYAxis`          | `string`           | A newline-separated string for the Y-axis data of a line chart.            |
| `LineChartAdditionalKeys` | `string`           | A newline-separated string for additional dataset keys in a line chart.    |
| `LineChartShowLegend`     | `bool`             | Indicates whether to show the legend in a line chart.                      |
| `LineChartSteppedLine`    | `bool`             | Indicates whether the line chart uses stepped lines.                       |
| `LineChartFill`           | `bool`             | Indicates whether the line chart area is filled.                           |

## Behavior

- **Dynamic State Updates**: The component dynamically updates its state when the `Pivot` parameter changes.
- **2D Pivot Options**: Additional options for 2D pivots are displayed only when the `Make2DPivot` property is enabled.
- **Line Chart Options**: Additional options for line charts are displayed only when the `MakeLineChart` property is enabled.
- **Validation**: Ensures valid input for properties like column renaming and ordering.

## Usage

### Example: Basic Usage
