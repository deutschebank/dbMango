# MongoDB human-readable aggregation pipeline

> Code name: Aggregation for Humans (AFH)

This should be 1:1 back and forth translation between human readable language and aggregation pipeline JSON.

The Aggregation for Humans (AFH) syntax is designed to simplify the process of creating and managing MongoDB aggregation pipelines. It addresses the challenges of working with MongoDB's native JSON-based pipeline by introducing a more human-readable and developer-friendly syntax. Below are the key benefits of AFH over the traditional JSON pipeline:

#### 1. Improved Readability
 

  - Human-Friendly Syntax: 

AFH uses intuitive keywords like `WHERE`, `GROUP BY`, `SORT BY`, and `JOIN`, making it easier to understand the purpose of each stage in the pipeline.

Example:

```sql
    FROM "CollectionName"
    PIPELINE {
        WHERE field1 == "value" AND field2 > 10
        GROUP BY field3 LET sum(field4) AS Total
        SORT BY field3 ASC
    }
```

This is much clearer compared to deeply nested JSON structures.

- Reduced Verbosity:
AFH eliminates the need for repetitive JSON keys and braces, making the pipeline more concise and easier to scan.

 

#### 2. Simplified Syntax
- Declarative Approach:
  - AFH adopts a declarative style, similar to SQL, which is more familiar to many developers.
  - It avoids the operator-centric approach of JSON (e.g., `$match`, `$group`, `$sort`), reducing the learning curve.

- Less Nesting:
  - JSON pipelines often involve deeply nested structures, which can be hard to follow. AFH flattens these structures, improving clarity.

 

#### 3. Error Reduction
- Fewer Syntax Errors:
  - JSON pipelines are prone to errors like missing commas, incorrect brackets, or misplaced quotes. AFH reduces these issues with its simplified syntax.

- Validation and Parsing:
  - AFH scripts are parsed into an Abstract Syntax Tree (AST), which can validate the structure and catch errors early.

 

#### 4. Enhanced Maintainability
- Easier to Modify:
  - AFH scripts are easier to update and extend due to their clear structure and reduced verbosity.
  - Developers can quickly identify and modify specific stages without navigating through complex JSON.

- Reusable Patterns:
  - AFH allows for better reuse of pipeline components, making it easier to maintain and share common patterns.

 

#### 5. Better Debugging and Visualization
- Logical Flow:
  - The AFH syntax presents the pipeline as a logical sequence of operations, making it easier to understand the data flow.

- Explainability:
  - AFH scripts can be converted into JSON for execution, while retaining the original human-readable format for documentation and debugging.

 

#### 6. Developer Productivity
- Faster Development:
  - Writing AFH scripts is faster than constructing JSON pipelines, especially for complex queries.
  - The syntax is designed to minimize boilerplate and focus on the logic of the query.

- Integration with Tools:
  - AFH integrates with tools like the `Browse` and `Aggregation Script` pages in dbMango, allowing users to write, test, and execute pipelines seamlessly.

 

#### 7. Educational Value
- Easier Learning Curve:
  - AFH is more approachable for developers new to MongoDB aggregation, as it abstracts away the complexities of JSON syntax.

- Documentation and Examples:
  - AFH scripts are easier to document and share, making them ideal for onboarding and collaboration.

 

### Comparison Example

 

JSON Pipeline:

```json
    [
       { "$match": { "field1": "value", "field2": { "$gt": 10 } } },
       { "$group": { "_id": "$field3", "Total": { "$sum": "$field4" } } },
       { "$sort": { "field3": 1 } }
    ]
```

AFH Equivalent:

```sql
    FROM "CollectionName"
    PIPELINE {
       WHERE field1 == "value" AND field2 > 10
       GROUP BY field3 LET sum(field4) AS Total
       SORT BY field3 ASC
    }
```

#### Conclusion


AFH significantly improves the experience of working with MongoDB aggregation pipelines by making them more readable, maintainable, and developer-friendly. It reduces the complexity of JSON, enabling faster development and easier debugging, while maintaining the full power of MongoDB's aggregation framework.


# FAQ

Q: Why not using SQL? 

A: It only available for Atlas (paid, hosted version of MongoDB). Also it can't support all the features of aggregation pipleline without propietary extensions anyway.

Q: Why not simply implementing SQL translator?

A: It looks very hard and I doubt it'll support all features of aggregation pipeline.

Q: What are the key advantages of AFH syntax over JSON?

A: AFH syntax offers several advantages:
   - **Readability**: It uses intuitive keywords like `WHERE`, `GROUP BY`, and `SORT BY`, making pipelines easier to understand.
   - **Reduced Verbosity**: Eliminates repetitive JSON keys and braces, making scripts concise.
   - **Error Reduction**: Simplified syntax reduces common JSON errors like missing commas or brackets.
   - **Maintainability**: Easier to modify and extend due to its clear structure.

Q: How does AFH handle complex pipeline stages?

A: AFH allows embedding plain JSON for unknown or complex stages using the `DO` block. This ensures full compatibility with MongoDB's aggregation framework while retaining the benefits of AFH syntax.

Q: Can AFH scripts be converted back to JSON?

A: Yes, AFH scripts are designed to be 1:1 convertible to JSON. This ensures that developers can use AFH for readability and debugging while executing the equivalent JSON pipeline in MongoDB.

Q: Is AFH suitable for beginners?

A: Absolutely. AFH's declarative style and SQL-like syntax make it more approachable for developers new to MongoDB aggregation. It abstracts away the complexities of JSON, providing a smoother learning curve.

Q: Does AFH support all MongoDB aggregation features?

A: AFH is designed to support all MongoDB aggregation features. For stages that don't have a direct AFH equivalent, developers can use the `DO` block to include raw JSON. For unsupported parameters `OPTIONS` part containe plain JSON applied to the result of AFH stage translation.

Q: How does AFH improve debugging?

A: AFH scripts present the pipeline as a logical sequence of operations, making it easier to trace data flow. Additionally, the human-readable format simplifies identifying and fixing issues.

Q: Can AFH scripts be reused across projects?

A: Yes, AFH's modular and declarative structure makes it easy to reuse and share pipeline components, improving maintainability and collaboration.
