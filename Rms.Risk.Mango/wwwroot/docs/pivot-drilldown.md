# Drilldown Functionality in Pivot Tables

Drilldown functionality allows users to explore data in greater detail by navigating from a 
summarized view to more granular levels of data. This feature is essential for analyzing specific 
data points, understanding their contributions, and identifying patterns or anomalies. Below is a 
comprehensive guide to understanding and setting up drilldown functionality in pivot tables.

## What is Drilldown?

Drilldown enables users to:
- Navigate from aggregated data (e.g., totals, averages) to the underlying detailed data.
- View specific rows or documents that contribute to a particular data point.
- Explore related data fields or key fields dynamically.

For example, clicking on a total sales figure for a region can reveal the individual sales transactions 
that contributed to that total.

## How Drilldown Works

Drilldown functionality is implemented using **pivot definitions** and **drilldown configurations**. 
When a user clicks on a column or data point, the system determines the appropriate collection and 
pivot definition to execute. The drilldown process includes:

1. **Identifying the Target Field**: The column or data point clicked by the user is mapped to its real field name.
2. **Applying Filters**: Filters are dynamically generated to show only the rows or documents relevant to the selected data point.
3. **Executing the Drilldown**: The system either displays a detailed report or opens a document view, depending on the level of detail available.

## Setting Up Drilldown in Pivot Definitions

To enable drilldown functionality, you need to configure the pivot definition with the appropriate settings. 
Below are the key components:

### 1. Drilldown Definition
Each pivot definition can include a list of drilldown configurations. These configurations specify:

- **ColumnName**: The column to drill down from. Use `"<Default>"` for a default condition.
- **DrilldownPivot**: The name of the pivot definition to use for the drilldown report.
- **DrilldownCondition**: A formula or condition to apply during the drilldown. 
  This formula uses **aggregation pipeline expression** syntax. This can include variables like:
- 
  - `<column_name>`: Replaced with the column value. `column_name` here is the actual column name.
    Example: `<Region>` will be replaced with column `Region` value. I.e., for instance, "EMEA".
  - `<COLNAME>`: Replaced with the column name. I.e. for `Region` column it will be "Region".

#### Example:
```json
{
  "Drilldown": [
    {
      "ColumnName": "Region",
      "DrilldownPivot": "RegionDetails",
      "DrilldownCondition": "{ \"Region\": \"<Region>\" }"
    },
    {
      "ColumnName": "<Default>",
      "DrilldownPivot": "DefaultDetails",
      "DrilldownCondition": "{ \"<COLNAME>\": { \"$ne\": 0.0 } }"
    }
  ]
}
```

### 2. Key Fields
Key fields are used to group and filter data during the drilldown. Ensure that the pivot definition 
includes the necessary key fields:

- **PrimaryKey1**: The first level of key fields.
- **PrimaryKey2**: The second level of key fields.

When drilldown condition applied pivot check if all **PrimaryKey1** applied to the output first. 
If they are not all present it'll add them to the list of pivot keys and then pivot gets executed. 
If all **PrimaryKey1** fields are present the same check goes for **PrimaryKey2**, and they added if missing.
If all **PrimaryKey1** and **PrimaryKey2** fields are already present the whole document gets displayed.

### 3. Data Fields
Data fields represent the values to be aggregated or analyzed. These fields are included in the 
drilldown report in addition to already present data fields.

## Customizing Drilldown Behavior

Drilldown functionality can be customized to suit specific requirements. Below are some options:

### Custom Drilldowns

You can define custom drilldowns for specific columns or conditions. For example:
- Use a different pivot definition for a specific column.
- Apply additional filters or conditions dynamically.

### Document View

If all key fields are already displayed, the drilldown can open a detailed document view 
instead of a report. This is useful for inspecting individual records.

### Dynamic Filters

Filters can be dynamically generated based on the selected data point. For example:

- Filter by the value of the clicked column.
- Combine multiple filters for key fields and data fields.

## Using Drilldown in Pivot Tables

1. **Click on a Data Point**:
   - In the pivot table, click on a column or data point to initiate the drilldown.

2. **View the Drilldown Report**:
   - The system will display a detailed report or document view based on the drilldown configuration.

3. **Navigate Back**:
   - Use the navigation options to return to the original pivot table or explore other data points.

## Best Practices for Drilldown Configuration

1. **Define Default Drilldowns**:
   - Always include a default drilldown configuration to handle cases where no specific column is matched.

2. **Use Meaningful Key Fields**:
   - Ensure that key fields are relevant and provide meaningful groupings for the data.

3. **Optimize Filters**:
   - Keep filters concise and relevant to improve performance and clarity.

4. **Test Custom Drilldowns**:
   - Test custom drilldowns thoroughly to ensure they work as expected for all scenarios.

## Example Use Case

### Scenario:
You have a pivot table showing total sales by region. You want to enable drilldown to view individual 
sales transactions for each region.

### Configuration:
1. Add a drilldown definition for the "Region" column:
```json
{
  "ColumnName": "Region",
  "DrilldownPivot": "SalesTransactions",
  "DrilldownCondition": "{ \"Region\": \"<Region>\" }"
}
```
2. Include "TransactionID" and "Date" as key fields in the drilldown pivot definition.
3. Add "Amount" and "Quantity" as data fields.

### Result:
When a user clicks on a region, the system will display a detailed report of sales transactions 
for that region.

---

Drilldown functionality is a powerful tool for exploring data interactively. By configuring pivot 
definitions and drilldown settings, you can provide users with a seamless and intuitive way to analyze 
data at multiple levels of detail.