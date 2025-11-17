# IPivotTableDataSource

The `IPivotTableDataSource` interface defines the contract for interacting with pivot table data sources. It provides methods for retrieving metadata, performing data aggregation, managing pivot definitions, and handling drilldowns.

## Features

- **Metadata Retrieval**: Fetch grouped collections and metadata for pivot tables.
- **Data Aggregation**: Perform pivot operations with customizable filters and caching options.
- **Drilldown Support**: Generate drilldown formulas for detailed data exploration.
- **Pivot Management**: Create, update, and delete pivot definitions.
- **Query Preprocessing**: Generate query text from pivot definitions.

## Properties

| Property   | Type   | Description                              |
|------------|--------|------------------------------------------|
| `SourceId` | string | Unique identifier for the data source.   |
| `Prefix`   | string | Prefix associated with the data source.  |
| `User`     | string | User associated with the data source.    |

## Methods

### `GetAllMeta`
Retrieves all metadata collections.

| Parameter | Type              | Default Value | Description                          |
|-----------|-------------------|---------------|--------------------------------------|
| `force`   | `bool`            | `false`       | Forces a refresh of metadata.        |
| `token`   | `CancellationToken` | `default`   | A cancellation token.                |

**Returns**: `Task<List<GroupedCollection>>`

---

### `GetDrilldownAsync`
Retrieves a drilldown formula for a specific column.

| Parameter        | Type              | Default Value | Description                                                                 |
|------------------|-------------------|---------------|-----------------------------------------------------------------------------|
| `collectionName` | `string`         |               | The name of the collection.                                                |
| `name`           | `string`         |               | The column name.                                                           |
| `value`          | `string`         | `""`          | The value to compare against. Only records not matching this value will be shown. |
| `equals`         | `bool`           | `false`       | If true, uses "name = value"; otherwise, uses "name != value".             |
| `token`          | `CancellationToken` | `default`   | A cancellation token.                                                      |

**Returns**: `Task<string>`

---

### `PivotAsync`
Aggregates data based on the provided pivot definition.

| Parameter        | Type                                   | Default Value | Description                              |
|------------------|----------------------------------------|---------------|------------------------------------------|
| `collectionName` | `string`                              |               | The name of the collection.              |
| `def`            | `PivotDefinition`                     |               | The pivot definition.                    |
| `extraFilter`    | `FilterExpressionTree.ExpressionGroup?` | `null`       | An optional extra filter to apply.       |
| `skipCache`      | `bool`                                |               | If true, skips cached results.           |
| `userName`       | `string?`                             | `null`        | The user performing the operation.       |
| `maxFetchSize`   | `int`                                 | `-1`          | The maximum number of records to fetch.  |
| `token`          | `CancellationToken`                   | `default`     | A cancellation token.                    |

**Returns**: `Task<IPivotedData>`

---

### `UpdatePredefinedPivotsAsync`
Updates predefined pivots in the collection.

| Parameter        | Type                        | Default Value | Description                              |
|------------------|-----------------------------|---------------|------------------------------------------|
| `collectionName` | `string`                   |               | The name of the collection.              |
| `pivots`         | `IEnumerable<PivotDefinition>` |               | The pivots to update.                    |
| `predefined`     | `bool`                     | `false`       | If true, marks the pivots as predefined. |
| `userName`       | `string?`                  | `null`        | The user performing the operation.       |
| `token`          | `CancellationToken`        | `default`     | A cancellation token.                    |

**Returns**: `Task`

---

### `UpdatePivotAsync`
Updates a single pivot in the collection.

| Parameter        | Type              | Default Value | Description                              |
|------------------|-------------------|---------------|------------------------------------------|
| `collectionName` | `string`         |               | The name of the collection.              |
| `pivot`          | `PivotDefinition` |               | The pivot to update.                     |
| `userName`       | `string?`        | `null`        | The user performing the operation.       |
| `token`          | `CancellationToken` | `default`   | A cancellation token.                    |

**Returns**: `Task`

---

### `GetQueryTextAsync`
Preprocesses a pivot definition to generate a query text.

| Parameter        | Type                                   | Default Value | Description                              |
|------------------|----------------------------------------|---------------|------------------------------------------|
| `collectionName` | `string`                              |               | The name of the collection.              |
| `def`            | `PivotDefinition`                     |               | The pivot definition.                    |
| `extraFilter`    | `FilterExpressionTree.ExpressionGroup?` | `null`       | An optional extra filter to apply.       |
| `token`          | `CancellationToken`                   | `default`     | A cancellation token.                    |

**Returns**: `Task<string>`

---

### `GetDocumentAsync` (Overload 1)
Retrieves a single document based on primary key fields.

| Parameter        | Type                                   | Default Value | Description                              |
|------------------|----------------------------------------|---------------|------------------------------------------|
| `collectionName` | `string`                              |               | The name of the collection.              |
| `keys`           | `KeyValuePair<string, object>[]`      |               | The primary key fields.                  |
| `extraFilter`    | `FilterExpressionTree.ExpressionGroup?` | `null`       | An optional extra filter to apply.       |
| `token`          | `CancellationToken`                   | `default`     | A cancellation token.                    |

**Returns**: `Task<string>`

---

### `GetDocumentAsync` (Overload 2)
Retrieves a single document based on a filter expression.

| Parameter        | Type                                   | Default Value | Description                              |
|------------------|----------------------------------------|---------------|------------------------------------------|
| `collectionName` | `string`                              |               | The name of the collection.              |
| `filterText`     | `FilterExpressionTree.ExpressionGroup` |               | The filter expression.                   |
| `token`          | `CancellationToken`                   | `default`     | A cancellation token.                    |

**Returns**: `Task<string>`

---

### `DeletePivotAsync`
Deletes a user pivot from the collection.

| Parameter        | Type              | Default Value | Description                              |
|------------------|-------------------|---------------|------------------------------------------|
| `collectionName` | `string`         |               | The name of the collection.              |
| `pivotName`      | `string`         |               | The name of the pivot to delete.         |
| `groupName`      | `string`         |               | The group the pivot belongs to.          |
| `userName`       | `string`         |               | The user who owns the pivot.             |
| `token`          | `CancellationToken` | `default`   | A cancellation token.                    |

**Returns**: `Task`

## Enums

### `PivotType`

| Value               | Description                              |
|---------------------|------------------------------------------|
| `Predefined`        | Represents predefined pivots.           |
| `User`              | Represents user-defined pivots.         |
| `UserAndPredefined` | Represents both user and predefined pivots. |
| `All`               | Represents all pivots.                  |