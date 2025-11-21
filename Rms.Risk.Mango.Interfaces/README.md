### This is a public interface for dbMango plugin.

## Purpose

> Implementation is optional! 
Upon startup dbMango will look for the plugin and if it is not found, 
it will continue without it.

On startup dbMango will look for assembly called `Rms.Risk.Mango.Db.Plugin.dll`. If it exists,
it will be loaded and the plugin will be initialized. The plugin is the class with the full name of
`Rms.Risk.Mango.Db.Plugin.Plugin`. This name must match the plugin name mask what is hardcoded in dbMango code.

For more information about creating plugins see [dbMango Plugins](../Rms.Risk.Mango/wwwroot/docs/plugins.md).

## Functionality

Currently it only provides interfaces for persistent storage for 

- configuration parameters overrides stored externally (Oracle database)
- persistent storage for onboarding information
- secure storage for audit records

The only implementation is in Rms.Risk.Mango.Db.Plugin for Deutsche Bank specific implementation.