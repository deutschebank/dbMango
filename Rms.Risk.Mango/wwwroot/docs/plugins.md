# Plugins support

> Implementation is optional! 
Upon startup dbMango will look for the plugins in its folder and if none found, 
it will continue normal operation without loading any.

## Purpose

Plugins for dbMango serve as a mechanism to extend its functionality in a 
modular and flexible way. They are particularly useful for incorporating 
organization-specific features or proprietary extensions that cannot be 
open-sourced due to confidentiality or business requirements. By using plugins, 
developers can tailor dbMango to meet specific needs without altering its core,
making it easier to maintain and upgrade. Additionally, plugins simplify the 
process of adding custom functionality during development, enabling teams to 
experiment and iterate without impacting the main application.

## Initialisation sequence

In the Program.cs and PluginSupport.cs, the initialization of plugins is a dynamic process 
designed to load and configure external assemblies that implement the `IDbMangoPlugin` interface. 
This process begins in Program.cs where the `PluginSupport.GetPlugin` method is invoked to 
retrieve an instance of a plugin. The `GetPlugin` method in `PluginSupport.cs` is responsible 
for dynamically discovering, loading, and instantiating the plugin.

The `GetPlugin` method first calls `GetPluginAssemblies` to identify and load assemblies
that match a specific naming pattern defined by the `PluginNameMask` regex (`^.*Mango.*Plugin\.dll$`). 
This regex ensures that only assemblies with names containing "Mango" and ending in "Plugin.dll" are 
considered. The `GetPluginAssemblies` method scans the application's base directory for 
matching DLL files, loads them into memory using the LoadPlugin method, and stores the 
loaded assemblies in a static list to avoid redundant loading.

Once the assemblies are loaded, `GetPlugin` searches for a type within these assemblies that 
implements the `IDbMangoPlugin` interface. If such a type is found, an instance of the plugin 
is created using Activator.CreateInstance. This instance is cached in a static field to ensure 
that the plugin is only initialized once during the application's lifecycle.
Back in Program.cs, if a plugin instance is successfully retrieved, it is registered as a 
singleton service in the application's dependency injection container. Additionally, the 
ConfigureServices method of the plugin is invoked, allowing the plugin to further configure 
the application's services. If no plugin is found, a default implementation (NoChangeNumberChecker)
is registered instead.

This approach provides a flexible mechanism for extending the application's functionality 
through external plugins, enabling dynamic discovery and integration of new features without 
modifying the core application code. The use of dependency injection ensures that the plugin's 
services are seamlessly integrated into the application's service pipeline.

You can force plugin class name via either environment variable `DBMANGO_PLUGIN_CLASS_NAME` or 
by setting it in command line arguments `--plugin-class-name <class name>`.

> Only one plugin may contain implementation of `IDbMangoPlugin`. If more than one plugin contains 
such class and class name is not set via command line or environment variable, only one **random** class 
will be created. Make sure that only one plugin contains implementation.

## Extending dbMango

To extend dbMango with a plugin, you need to create a class that implements the `IDbMangoPlugin` interface. 
This interface defines the contract for plugins, ensuring they integrate seamlessly with dbMango's architecture. 
Plugins are designed to extend the application's functionality in a modular and flexible way. They allow developers 
to add organization-specific features or proprietary extensions without modifying the core application. 
This modularity ensures that the core remains maintainable and upgradable while enabling teams to experiment 
with custom functionality. Plugins are particularly useful for adding confidential or business-specific features,
tailoring dbMango to meet unique organizational needs, and simplifying the process of adding and testing new 
functionality during development.

Once the class is implemented, compile the project to generate a .dll file. Ensure the assembly name matches 
the naming pattern expected by dbMango, which is defined by the regex `^.*Mango.*Plugin\.dll$`. Place the 
compiled .dll file in the folder where dbMango scans for plugins, typically the application's base directory.

It is important to note that only one plugin implementing `IDbMangoPlugin` should exist in the plugins folder.
However you can have as many plugin dlls implementing Blazor pages as you want.
If multiple plugins implement the interface, dbMango will load one at random, which can lead to unpredictable
behavior. Use the `ConfigureServices` method to integrate your plugin's services into dbMango's dependency 
injection pipeline, ensuring your services are available throughout the application. By following these steps,
you can create and deploy plugins to extend dbMango's functionality in a clean, modular, and maintainable way.

Example:

```csharp
using Rms.Risk.Mango.Interfaces;
using Microsoft.Extensions.Hosting;

public class DemoMangoPlugin : IDbMangoPlugin
{
    public IHostApplicationBuilder ConfigureServices(IHostApplicationBuilder builder)
    {
        // Minimal service registration for demonstration
        builder.Services.AddSingleton<IDemoService, DemoService>();
        return builder;
    }

    public IAuditService? CreateSecureAuditService(OracleConnectionSettings settings)
    {
        // Return null as this is a minimal implementation
        return null;
    }
}

// Example service for demonstration purposes
public interface IDemoService { }

public class DemoService : IDemoService { }
```

### Extending dbMango menus

To extend dbMango menus, you can leverage the `MenuService` class, which provides a structured way to manage 
and dynamically add menu items. The `MenuService` class acts as a centralized repository for menu definitions,
allowing you to define new menus or extend existing ones without directly modifying the `NavMenu.razor` file.
This approach ensures that the menu structure remains modular and maintainable.

The `MenuService` class exposes two primary methods: `AddMenuItem` and `Get`. The `AddMenuItem` method 
allows you to add new menu items by specifying the menu name, the title of the menu item, and its URL. 
This method appends the new menu item to an internal list of `MenuItem` objects. The `Get` method retrieves 
all menu items associated with a specific menu name, enabling dynamic rendering of menu items in the 
`NavMenu.razor` file.

In the `NavMenu.razor` file, the `MenuService` is injected and used to dynamically populate menu items. 
For example, the `Admin` and `User` menus include a `foreach` loop that iterates over the items returned by
`MenuService.Get("Admin")` or `MenuService.Get("User")`. This dynamic approach allows you to extend these
menus by simply adding new items to the `MenuService` at runtime.

To add a new menu or extend an existing one, you can call the `AddMenuItem` method during the application's 
initialization or at any point where the `MenuService` is accessible. For instance, you might add new menu 
items in the `Program.cs` file, a plugin, or any service that has access to the `MenuService`. Once added, 
the new menu items will automatically appear in the navigation bar, provided the corresponding `foreach` loop 
in `NavMenu.razor` is configured to render items for that menu.

This design ensures that the menu structure is both extensible and decoupled from the UI logic, making 
it easier to maintain and customize. By using the `MenuService`, you can dynamically adapt the navigation
structure to meet the needs of different users, roles, or organizational requirements without modifying 
the core navigation component.

Example:

```csharp
using Microsoft.Extensions.Hosting;
using Rms.Risk.Mango.Interfaces;
using Rms.Risk.Mango.Services;

public class CustomMangoPlugin : IDbMangoPlugin
{
    public IHostApplicationBuilder ConfigureServices(IHostApplicationBuilder builder)
    {
        // Resolve the MenuService from the service collection
        var menuService = builder.Services.BuildServiceProvider().GetRequiredService<IMenuService>();

        // Call the method to extend menus
        ExtendMenus(menuService);

        return builder;
    }

    public IAuditService? CreateSecureAuditService(OracleConnectionSettings settings)
    {
        // Return null as this is a minimal implementation
        return null;
    }

    private void ExtendMenus(IMenuService menuService)
    {
        // Add a new item to the existing "Admin" menu
        menuService.AddMenuItem("Admin", "Audit Logs", "admin/audit-logs");

        // Create a completely new top-level menu called "Reports" with two items
        menuService.AddMenuItem("Reports", "Monthly Report", "reports/monthly");
        menuService.AddMenuItem("Reports", "Yearly Report", "reports/yearly");
    }
}
```


### Creating custom pages

All Blazor pages located within plugin assembles are available to dbMango application. Create your page
as normal and access it via custom menu (see above).

```csharp
@page "/plugin/example"

@attribute [Authorize]

<h3>Example page</h3>

```

### Authorization controls

When creating custom pages within your dbMango plugin, you can leverage the existing authorization.
There are 3 polocies defined in dbMango:

- ReadAccess
- WriteAccess
- AdminAccess

Use them in the standard `AuthorizeView` control specifying database configuration name as `Resource`.

> Note that Resoure should contain the name of database configuration as within **Onboarding** page, not the
MongoDb database name.

Example:

```csharp

@page "/plugin/example"
@page "/plugin/example/{DatabaseStr}/{DatabaseInstanceStr}"

@attribute [Authorize]

@inject NavigationManager NavigationManager
@inject IUserSession      UserSession
@inject IJSRuntime        JsRuntime

<AuthorizeView Policy="WriteAccess" Resource="@Database">
    <Authorized Context="ctx">
        <div> You have write access to "@Database" @DatabaseInstance </div>
    </Authorized>
    <NotAuthorized Context="ctx">
        <div> You don't have write access to "@Database" @DatabaseInstance </div>
    </NotAuthorized>
</AuthorizeView>

@code {
    [Parameter] public string? DatabaseStr         { get; set; }
    [Parameter] public string? DatabaseInstanceStr { get; set; }

    private string Database
    {
        get => UserSession.Database;
        set
        {
            if (UserSession.Database == value)
                return;
            UserSession.Database = value;
            SyncUrl();
            InvokeAsync(StateHasChanged);
        }
    }

    private string DatabaseInstance
    {
        get => UserSession.DatabaseInstance;
        set
        {
            if (UserSession.DatabaseInstance == value)
                return;
            UserSession.DatabaseInstance = value;
            SyncUrl();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender)
            return;

        if (string.IsNullOrWhiteSpace(DatabaseStr))
            DatabaseStr = Database;
        else
            Database = DatabaseStr;

        if (string.IsNullOrWhiteSpace(DatabaseInstanceStr))
            DatabaseInstanceStr = DatabaseInstance;
        else
            DatabaseInstance = DatabaseInstanceStr;

        SyncUrl();
    }

    private void SyncUrl()
    {
        var url = NavigationManager.BaseUri + $"plugin/example/{Database}";
        if (!string.IsNullOrWhiteSpace(DatabaseInstance))
            url += $"/{DatabaseInstance}";
        JsRuntime.InvokeAsync<string>("DashboardUtils.ChangeUrl", url);
    }
}
```