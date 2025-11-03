# Rms.Risk.Mango.Pivot.UI

## Overview

This package provides a collection of Blazor UI components designed for Pivot functionality and general form support. It aims to deliver a rich user interface experience for data pivoting and interactive forms, primarily for use within the Rms.Risk.Mango ecosystem.

As indicated by its package tags (`pivot forge ui blazor razor shared form components`), this library is focused on:
- Pivot table and data visualization UI.
- UI elements for "Forge" platform.
- General Blazor and Razor shared components.
- Components to facilitate form building.

## Features

*   **Pivot UI Components**: Specialized Blazor components for creating and interacting with pivot tables and related data visualizations.
*   **Form Support Components**: A comprehensive set of reusable Blazor components to build forms, including inputs, validation helpers, and more.
*   **Shared UI Elements**: Common UI elements and utilities to ensure a consistent look and feel.
*   **Static Assets**: Includes necessary static assets (CSS/JS) in its `wwwroot` folder, accessible via `_content/Rms.Risk.Mango.Pivot.UI/`.

## Installation

To use Rms.Risk.Mango.Pivot.UI in your Blazor project, install the NuGet package.

Using the .NET CLI:
```bash
dotnet add package Rms.Risk.Mango.Pivot.UI
```

Or via the NuGet Package Manager Console in Visual Studio:
```
Install-Package Rms.Risk.Mango.Pivot.UI
```

## Usage

After installing the package, you will typically need to:

1.  **Import Namespaces**:
    Add the relevant `@using` statements to your `_Imports.razor` file or directly in your `.razor` components where the UI elements are used. For example:
    ```razor
    @using Rms.Risk.Mango.Pivot.UI.Components // Example, actual namespaces may vary
    @using Rms.Risk.Mango.Pivot.UI.Forms     // Example, actual namespaces may vary
    ```

2.  **Register Services (if applicable)**:
    If the library exposes services that need to be registered (e.g., for Blazored.Modal), add them to your `Program.cs` (for .NET 6+ Blazor apps) or `Startup.cs` (for older versions).
    ```csharp
    // In Program.cs
    // builder.Services.AddBlazoredModal(); // Example for a dependency
    // Potentially other services from this library
    ```

3.  **Include CSS/JS**:
    If the library has global CSS or JS files necessary for its components, you might need to link them in your main layout file (`_Layout.cshtml` or `MainLayout.razor`) or `index.html`. Static assets from this RCL are served from `_content/Rms.Risk.Mango.Pivot.UI/`.
    For example:
    ```html
    <link href="_content/Rms.Risk.Mango.Pivot.UI/styles.css" rel="stylesheet" />
    ```
    (Note: The specific file names like `styles.css` are illustrative.)

## Dependencies

This package utilizes the following external NuGet packages:

*   **BlazorDateRangePicker**: For date range selection UI.
*   **Blazored.Modal**: For modal dialog functionality.
*   **ChartJs.Blazor.Fork**: For rendering charts using Chart.js.

## Bundled Libraries

The following project dependencies are included directly within this NuGet package (due to `PrivateAssets="All"` setting) and do not need to be installed separately by the consuming project:

*   `Rms.Risk.Mango.Language.csproj`
*   `Rms.Risk.Mango.Pivot.Core.csproj`

This packaging strategy ensures that the specific versions of these core and language libraries used during the development of `Rms.Risk.Mango.Pivot.UI` are the ones used at runtime, simplifying dependency management for consumers.

## Building the Package

This project is configured to automatically generate a NuGet package (`.nupkg`) and a symbols package (`.snupkg`) upon building the project in Release configuration.

Key MSBuild properties for packaging:
```xml
<GeneratePackageOnBuild>true</GeneratePackageOnBuild>
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>
<DebugType>portable</DebugType>
```

The project also includes custom MSBuild targets (`IncludeProjectReferencesWithPrivateAssetsAttributeInPackage` and `IncludeReferenceAssemblies`) to manage how project references and reference assemblies are included in the package. This ensures that dependencies marked with `PrivateAssets="All"` are bundled, and a `ref` assembly is correctly packaged to control the public API surface.

## Contributing

Please refer to the contribution guidelines of the Rms.Risk.Mango project if you wish to contribute.
Drop a email to mailto:forge@list.db.com.

