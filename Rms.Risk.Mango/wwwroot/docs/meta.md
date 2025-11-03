# Meta collections

dbMango uses meta collections to store pivot definitions and user-specific settings.

Meta collections include:

- **CalculatedFields** - formulas for calculated fields
- **FieldsMap** - various field information 
- **FieldDescriptors** - various UI related information bout pivot columns
- **Lookups** - lookup definitions
- **PredefinedPivots** - the main storage for pivot definitions
- **PredefinedPivots-[USERNAME]** - pivot definitions only visible for USERNAME

### CalculatedFields

The `CalculatedFields` object in meta collection responsible for holding formulas for generated (calculated) fields that can be added to queries. In a way these are macros to be reused. Below is the JSON format for a calculated field:

Formula field have MongoDB aggregation pipeline expression format. 
It must use `@` instead of `$` to avoid conflicts with MongoDB operators, field names are serialized with `#` instead of `.`.

#### JSON Format

```json
{
  "_id" : "CalculatedFields",
  "FieldName1" : {                     // field name corresponding to calculated field name
  {
    "Formula": { JSON },               // The formula defining the calculated field using expression JSON syntax.
    "DrillDown": "string",             // The drill-down logic for the field (e.g., a MongoDB query or filter).
    "LookupDef": ["string", ...],      // An optional array of lookup definitions associated with the field.
    "AggregationOperator": "string"    // The aggregation operator (e.g., "$sum", "$avg") to be used in aggregation pipelines.
  },
  ...
}
```

#### Field Descriptions

1. **`Formula`**:
   - A string representing the formula for the calculated field.
   - Example: `"$sum: $Amount"`.

2. **`DrillDown`**:
   - A string defining the drill-down behavior for the field.
   - Example: A MongoDB query or filter to retrieve detailed data.

3. **`LookupDef`**:
   - An optional array of strings representing lookup definitions.
   - These lookups may be used to enrich or map data for the calculated field.

4. **`AggregationOperator`**:
   - A string specifying the aggregation operator to be applied within aggregation pipeline.
   - Common values: `"$sum"`, `"$avg"`, `"$max"`, etc.

#### Example JSON
```json
{
  "_id" : "CalculatedFields",
  "AcctCcy" : {
    "Formula" : {
      "@cond" : {
        "else" : "@Currency",
        "if" : {
          "@gte" : [{
              "@strLenCP" : "@Currency"
            }, 6]
        },
        "then" : {
          "@substrCP" : ["@Currency", 3, 3]
        }
      }
    }
  },
  "Count" : {
    "Formula" : {
      "@literal" : 1
    }
  }
}
```
### FieldMap

`FieldMap` object is designed to represent metadata about a field in a pivot table. Below is the JSON format:

```json
{
  "_id" : "FieldsMap",
  "FieldName1" : {
  {
    "Purpose": "string",          // (string) The purpose of the field, represented by the `PivotFieldPurpose` enum.
    "Type": "string"              // (string) The fully qualified name of the field's data type (e.g., "System.String").
  },
  ...
}
```

#### Field Descriptions

2. **`Purpose`**:
   - Represents the purpose or role of the field in the pivot table.
   - Defined by the `PivotFieldPurpose` enum, which likely includes values such as `Key`, `Data`, or `Lookup`.

   - Possible `Purpose` values:

    1. **`Data`**:
       - Represents a field that contains the primary data to be aggregated or analyzed in the pivot table.
       - Example: Sales amount, quantity, or revenue.

    2. **`primary key 1`**:
       - Represents the first primary key field used to uniquely identify a record or group in the data.
       - Example: A unique identifier such as `OrderID` or `CustomerID`.

    3. **`primary key 2`**:
       - Represents the second primary key field, often used in conjunction with `PrimaryKey1` to form a composite key.
       - Example: A secondary identifier such as `ProductID` in a dataset where `OrderID` and `ProductID` together form a unique key.

    4. **`Key`**:
       - Represents a general key field used for grouping or categorizing data in the pivot table.
       - Example: Fields like `Region`, `Department`, or `Category`.

    5. **`Info`**:
       - Represents an informational field that provides additional context or metadata but is not directly used for aggregation or grouping.
       - Example: Descriptive fields like `Description` or `Comments`.

    6. **`Hidden`**:
       - Represents a field that is not displayed in the pivot table but may still be used internally for calculations or filtering.
       - Example: Fields used for intermediate calculations or backend logic.

3. **`Type`**:
   - A string representing the fully qualified name of the field's data type (e.g., `System.String`, `System.Int32`).
   - This is used to dynamically determine the type of the field at runtime.


#### Example JSON
Here is an example of a `SingleFieldMapping` object serialized to JSON:

```json
{
  "_id" : "FieldsMap",
  "AcctCcy" : {
    "Purpose" : "key",
    "Type" : "string"
  },
  "AtRisk" : {
    "Purpose" : "data",
    "Type" : "double"
  },
  "BlendImpact" : {
    "Purpose" : "data",
    "Type" : "double"
  },
  "Book" : {
    "Purpose" : "primary key 2",
    "Type" : "string"
  }
}
```
### FieldDescriptors

`FieldDescriptors` object used to define metadata for pivot table columns. Its purpose:
- Matching column names using regular expressions.
- Customizing the appearance of columns (background and alternate background colors).
- Specifying value formatting for display.

Below is the JSON format based on its properties:

#### JSON Format
```json
{
  "_id" : "FieldDescriptors",
  "Fields" : [
  {
      "NameRegex": "string",             // (string) A regular expression pattern for matching column names.
      "Background": "string",            // (string, nullable) The background color of the column (e.g., "White", "Red").
      "AlternateBackground": "string",   // (string, nullable) The alternate background color for the column.
      "Format": "string"                 // (string, nullable) The format string for displaying column values.
  },
  ...
  ]
}
```

#### Field Descriptions
1. **`NameRegex`**:
   - A string representing a regular expression pattern used to match column names.
   - Example: `"^Column.*"` matches all column names starting with "Column".

2. **`Background`**:
   - A string representing the background color of the column.
   - If `null`, the default color is `"White"`.
   - Example: `"Blue"`, `"Red"`.

3. **`AlternateBackground`**:
   - A string representing the alternate background color for the column.
   - If `null`, the default color is `"White"`.
   - Example: `"LightGray"`.

4. **`Format`**:
   - A string representing the format for displaying column values.
   - Example: `"C2"` for currency with two decimal places, `"P0"` for percentages with no decimals.

---

#### Example JSON
```json
{
  "_id" : "FieldDescriptors",
  "Fields" : [{
      "Background" : "#54440f57",
      "Format" : "",
      "NameRegex" : "Book$|^Ccy$|.*\\WCcy$|^Currency|.*\\WCurrency|Location|Layer|Desk|Portfolio"
    }, {
      "Background" : "#113f4857",
      "Format" : "N0",
      "NameRegex" : "Unexplained|RiskBasedUnexplained"
    }
  ]
}
```

### PredefinedPivots

`PredefinedPivots` used to store pivot information. There is no need to edit it manually - there is a UI for it. 
If you want more details, you can refer to [PivotDefinition](PivotDefinition.md).

### PredefinedPivots-[USERNAME]

These objects uses the same format as `PredefinedPivots`. Pivots defined here will only be visible to USERNAME.


