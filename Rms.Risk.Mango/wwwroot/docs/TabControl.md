# TabControl Component  

The `TabControl` component is a Blazor component that provides a tabbed interface for organizing content. It allows users to switch between different views or sections of content within the same page.  

## Features  
- Dynamically add or remove tabs.  
- Support for vertical or horizontal tab layouts.  
- Customizable tab headers and styles.  
- Option to persist all tabs or only the active tab.  
- Event callback for active tab changes.  

## Parameters  

| Parameter        | Type                  | Default Value | Description                                                                 |
|------------------|-----------------------|---------------|-----------------------------------------------------------------------------|
| `ChildContent`   | `RenderFragment?`     | `null`        | The content to be rendered inside the `TabControl`.                        |
| `Class`          | `string`             | `""`          | CSS class for the `TabControl` container.                                  |
| `TabGroupClass`  | `string`             | `""`          | CSS class for non-selectable tab headers.                                  |
| `ShowHeaders`    | `bool`               | `true`        | Determines whether tab headers are displayed.                              |
| `Vertical`       | `bool`               | `false`       | If `true`, tabs are displayed vertically.                                  |
| `PersistAllTabs` | `bool`               | `false`       | If `true`, all tabs are persisted in memory even when inactive.            |
| `ActivePage`     | `string`             | `""`          | The text of the currently active tab.                                      |
| `ActivePageChanged` | `EventCallback<string>` | `null`    | Callback invoked when the active tab changes.                              |  

## Methods  

| Method           | Description                                                                 |
|-------------------|-----------------------------------------------------------------------------|
| `AddPage(TabPage)` | Adds a new `TabPage` to the `TabControl`.                                  |
| `ActivatePage(TabPage)` | Activates the specified `TabPage`.                                    |  

# TabPage Component  

The `TabPage` component is a Blazor component that represents an individual tab within a `TabControl`. It is used to define the content and behavior of a single tab.

## Features  
- Supports dynamic addition to a `TabControl`.
- Allows customization of tab content.
- Can be selectively displayed based on the active tab.
- Supports optional persistence of all tabs.

## Parameters  

| Parameter        | Type                  | Default Value | Description                                                                 |
|------------------|-----------------------|---------------|-----------------------------------------------------------------------------|
| `ChildContent`   | `RenderFragment?`     | `null`        | The content to be rendered inside the `TabPage`.                           |
| `Text`           | `string`             | `"Tab"`       | The text identifier for the tab.                                           |
| `IsSelectable`   | `bool`               | `true`        | Determines whether the tab can be selected.                                |

## Cascading Parameters  

| Parameter        | Type                  | Description                                                                 |
|------------------|-----------------------|-----------------------------------------------------------------------------|
| `Parent`         | `TabControl`         | The parent `TabControl` that this `TabPage` belongs to.                     |

## Methods  

| Method           | Description                                                                 |
|-------------------|-----------------------------------------------------------------------------|
| `OnInitialized`   | Ensures the `TabPage` is added to the parent `TabControl` during initialization. |

## Usage  

### Basic Example  
