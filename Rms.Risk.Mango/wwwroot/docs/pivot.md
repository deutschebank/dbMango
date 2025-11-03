# Pivot

dbMango provides a dynamic and interactive pivot table functionality. It is designed to allow users to 
explore and analyze data collections in a structured and customizable way. 
These are the key features:

- **Data Exploration:** Users can explore data collections and pivots interactively.
- **Custom Filtering:** Users can apply additional filters to refine the data displayed in the pivot table.
- **State Persistence:** The component remembers the user's selections and preferences across sessions.
- **Deep Linking:** Users can share or bookmark specific pivot table configurations.

See also [Meta- collections](meta.md), [Drilldown functionality](pivot-drilldown.md)

## Keys
Depending on collection some fields treated as keys, some as information and the rest are data. Key fields 
can be used for aggregation. For example fields like Book, Desk or StructureId are keys. Info fields on other 
hand can't be used as aggregation keys, but neither should be aggregated a data. Example of such fields 
could be all the market data - Opening/Closing Spot, Volatility, interest rate etc. Data fields are bread
and butter for pivots - you can aggregate them, calculate averages, min/max and so other math operations
on them.

Once a day pivot service extracting a sample of 1000 documents for the latest COB and extending the 
list of available fields automatically. All new string fields added as keys, all doubles as data.

## Data

Data fields can be selected via Data tab. Note that not all pivots supports this feature. For some 
pivots data fields are fixed and hardcoded within the query. For others you can turn data to be aggregated
on or off and use drag-and-drop to reorder columns.

## Filtering

Pivot supports comprehensive filtering. You can create very sophisticated conditions including various 
fields and logical grouping. Pivot have two types of filters - pre-filters (discussed above) and 
post-filters applied to the results (discussed here). To understand how pre-filters work you need to know 
how documents are stored in the data storage. You may think of pre-filter as WHERE clause in SQL. 
Note that some fields are created when query runs. For such fields you can't use pre-filters as they 
are not available in the original document. Only fields that exists in the original document (for MongoDB 
at the first level of JSON, for SQL for columns in the source table).

## Sorting

There are multiple sorting modes supported (click on the column title one more time to change):

- No sort
- Abs value descending
- Abs value ascending
- Value descending
- Value ascending

Totals shown at the bottom for currently visible row set. I.e. with post-filter applied for all rows, 
not just a currently visible page.

## Highlighting

There is a special highlighting for top/bottom N%.

This feature works this way: let's say we have a column with numbers of 100, 90, 0, -50. In this case
100 and 90 will be counted as positive with maximum of 100, -5 as negative with minimum of -50. "Block
highlighting" will be applied for 100 and 90 (green) as it more than top 20% of the positive max of 100
(which is of course 80) and -5 as it within bottom 20% of negative minimum (which is -5). For this 
column absolute total is 100+90+0+5 = 195. So 20% of it is 156. Starting from the top pivot will calculate
running total and draw the line where this running total reach 156 (i.e. after 100 and 90).

## Graphs

Pivot can draw results graphs. The examples of it can be found in BFG collection. This feature is 
useful for historical analysis and general visualization. 

Post filtering is extremely useful here. Usually such queries selecting multiple books, desks, 
curves, instruments etc. But using post-filtering (see above) you can narrow the result set down 
to a single or reasonable number of, for example, curves. Graphs can be smooth lines, 
stepped lines or "kind of bar charts" when you select stepped line and fill the space below the graph.

# Discovering fields in documents within collection

When working with MongoDB collections, it is essential to ensure that all fields in the data are 
properly mapped for use in pivot tables. Missing field mappings can 
occur when new fields are introduced or when the metadata is incomplete. To address this, a sampling-based 
approach is used to efficiently discover and update missing field mappings.

### How Sampling Helps

Sampling allows the system to inspect a small, representative subset of documents from a collection 
instead of scanning the entire dataset. This approach is efficient and scalable, especially for large 
collections. By analyzing the sampled documents, the system can identify fields that are not yet 
mapped and update the metadata accordingly.

### Steps in the Process

1. **Retrieve the Collection**:
  The system connects to the MongoDB collection to prepare for sampling.

2. **Focus on Relevant Data**:
  The system identifies the most recent "COB" (Close of business) date, which represents the latest data 
  in the collection. If available, the sampling process is limited to documents with this date to ensure
  relevance.

3. **Random Sampling**:
  A random subset of 16 documents is selected from the collection using MongoDB's sampling capabilities. 
  This ensures that the process is efficient while still providing a representative view of the data.

4. **Analyze Sampled Documents**:
  Each sampled document is inspected to identify fields that are not yet mapped. The system examines the 
  structure of the document, including nested fields, to ensure comprehensive coverage.

5. **Update Metadata**:
  For each missing field discovered in the sampled documents:
     - The field's name, type, and purpose are inferred.
     - The metadata is updated to include the new field, ensuring it is available for future queries and processing.

6. **Handle Nested Fields**:
  If a field contains nested documents, the system recursively inspects the nested structure to discover 
  and map all subfields.

### Benefits of Sampling-Based Discovery

1. **Efficiency**:
  By analyzing only a small subset of documents, the process avoids the overhead of scanning the entire collection.

2. **Relevance**:
  Focusing on the most recent data ensures that the discovered fields are up-to-date and relevant to current operations.

3. **Scalability**:
  The approach works effectively for collections of any size, making it suitable for large datasets.

4. **Comprehensive Coverage**:
  The recursive inspection of nested fields ensures that all fields, including those in complex structures, are discovered and mapped.

The sampling-based approach to discovering missing field mappings is a powerful and efficient method 
for maintaining up-to-date metadata in MongoDB collections. By focusing on a small, representative subset 
of documents, the system ensures that all fields are properly mapped without compromising performance or 
scalability. This process is essential for ensuring the accuracy and completeness of data used in pivot 
tables and other analytical tools.

