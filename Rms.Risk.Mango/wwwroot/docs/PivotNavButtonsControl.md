# PivotNavButtonsControl  

The `PivotNavButtonsControl` is a Blazor component that provides a set of navigation and action buttons for managing pivot operations. It includes functionality for refreshing, navigating backward and forward, and exporting pivot data. Additionally, it allows toggling the use of cached data.  

## Features  
- **Navigation Controls**: Buttons for navigating backward and forward through pivot states.  
- **Action Buttons**: Includes options to refresh the pivot, copy data as CSV, and export data as a CSV file.  
- **Cache Toggle**: Allows users to enable or disable the use of cached data.  
- **Dynamic State Management**: Button states (enabled/disabled) are dynamically updated based on the component's parameters.  

## Parameters  

| Parameter              | Type                              | Default Value | Description                                                                 |
|------------------------|-----------------------------------|---------------|-----------------------------------------------------------------------------|
| `Class`                | `string`                         | `"form-row"`  | CSS class to apply to the root container of the component.                 |
| `Navigation`           | `Navigation<NavigationUnit>`     | `null!`       | Provides navigation functionality for managing pivot states.               |
| `IsRefreshEnabled`     | `bool`                           | `false`       | Determines whether the "Refresh" button is enabled.                        |
| `IsExportEnabled`      | `bool`                           | `false`       | Determines whether the export buttons are enabled.                         |
| `RefreshPivotTriggered`| `EventCallback`                  | `null`        | Callback invoked when the "Refresh" button is clicked.                     |
| `CopyCsvTriggered`     | `EventCallback`                  | `null`        | Callback invoked when the "Copy CSV" button is clicked.                    |
| `ExportCsvTriggered`   | `EventCallback`                  | `null`        | Callback invoked when the "Export CSV" button is clicked.                  |
| `UseCache`             | `bool`                           | `true`        | Indicates whether cached data should be used.                              |
| `UseCacheChanged`      | `EventCallback<bool>`            | `null`        | Callback invoked when the `UseCache` value changes.                        |

## Properties  

| Property              | Type                              | Description                                                                 |
|-----------------------|-----------------------------------|-----------------------------------------------------------------------------|
| `IsBackwardDisabled`  | `bool`                           | Indicates whether the "Backward" button is disabled.                       |
| `IsForwardDisabled`   | `bool`                           | Indicates whether the "Forward" button is disabled.                        |
| `IsRefreshDisabled`   | `bool`                           | Indicates whether the "Refresh" button is disabled.                        |
| `IsExportDisabled`    | `bool`                           | Indicates whether the export buttons are disabled.                         |

## Methods  

| Method                | Description                                                                 |
|-----------------------|-----------------------------------------------------------------------------|
| `OnBackward`          | Handles the "Backward" button click and navigates to the previous pivot.   |
| `OnForward`           | Handles the "Forward" button click and navigates to the next pivot.        |
| `OnRefreshPivot`      | Invokes the `RefreshPivotTriggered` callback.                              |
| `OnCopyCsv`           | Invokes the `CopyCsvTriggered` callback.                                   |
| `OnExportCsv`         | Invokes the `ExportCsvTriggered` callback.                                 |

## Behavior  

- **Dynamic Button States**: The enabled/disabled state of the buttons is determined by the `IsRefreshEnabled`, `IsExportEnabled`, and `Navigation` parameters.  
- **Navigation Management**: The `Navigation` parameter provides the logic for navigating backward and forward through pivot states.  
- **Event Callbacks**: Clicking action buttons triggers the corresponding event callbacks, allowing parent components to handle the actions.  
- **Cache Toggle**: The `UseCache` parameter is bound to a checkbox, enabling users to toggle the use of cached data. Changes to this parameter trigger the `UseCacheChanged` callback.  

## Usage  

### Example: Basic Usage  
