# dbMango - fully audited MongoDB shell

dbMango is an audited MongoDB shell designed to manage multiple MongoDB databases. It provides a secure and controlled environment for database administrators, support teams, and developers. The system enforces restricted access to production databases based on LDAP group memberships and ensures audit records are created for privileged operations. This helps maintain compliance and accountability in database management.

## Overview

### Key Features
 

- LDAP-Based Access Control: Ensures only authorized users can access specific databases or perform privileged actions.  
- Audit Logging: Tracks all privileged operations, including who performed them and when, for compliance and accountability.  
- User-Friendly UI: Blazor-based components provide a modern and intuitive interface for managing MongoDB databases.  
- Support for Multiple Databases: Handles multiple MongoDB instances, making it suitable for large-scale environments.  

This structure ensures dbMango is a secure, efficient, and user-friendly tool for managing MongoDB databases in production and development environments.



### Admin access
Admin access in dbMango provides a secure and feature-rich environment for database administrators to manage MongoDB databases effectively. The key features—structure synchronization, data backup and restore, and shell access—offer significant advantages in terms of control, efficiency, and compliance.

1. Data migration
  - Facilitates data migration between MongoDB databases.  
  - Allows users to start, monitor, and cancel migration jobs.  
  - Displays migration progress, including metrics like ReadDPS, WriteDPS, and overall DPS.  
  - Provides options for transformations and detailed error reporting.

2. Sync Structure
  - Compares and synchronizes the database structure (collections and indexes) between environments.  
  - Highlights differences (e.g., collections to add/remove, indexes to create/remove).  
  - Supports manual and automated synchronization with options like dry runs and selective updates.  
  - Warns about collections requiring manual sharding or unsharding.

3. Download data
  - Provides a way to export data or database structures to files.  
  - Supports downloading JSON representations of collections or other database artifacts.  
  - Useful for backups or transferring data between environments.

4. Shell
  - Acts as an interactive MongoDB shell for executing commands.  
  - Provides restricted access to production databases based on LDAP group policies.  
  - Ensures all privileged commands are logged for auditing purposes.

### User access

The user pages in dbMango provide functionality for developers and support teams to interact with MongoDB databases in a controlled and secure manner. These pages are designed to simplify common tasks like inserting, deleting, updating documents, and executing queries, while ensuring compliance with access restrictions and audit requirements.


1. Find / Browse

  - Users can input queries using a custom syntax (e.g., AFH syntax) or JSON.
  - Provides validation and error handling for query syntax.
  - Displays query results in a structured format, such as JSON or a tabular view.
  - Allows users to explore the data returned by the query.
  - Users can interact with individual documents in the result set, such as viewing, updating, or deleting them (based on permissions).
  - Enforces access control policies to ensure only authorized users can perform write operation (base on LDAP group membership).

2. Aggregation / Aggregation for humans (AFH)

  - Provide a user-friendly interface for creating, running, and managing MongoDB aggregation pipelines. 

  - Simplify the process of working with MongoDB's powerful but complex aggregation framework by introducing a more human-readable syntax and intuitive tools.

3. Insert / Delete
  - Allows users to insert/delete documents in MongoDB collections. 
  - Provides a user-friendly interface for adding data while enforcing validation rules.  
  - Useful for support teams needing to make controlled updates to production data.


### Aggregation for Humans (AFH)

dbMango supports its own language for Aggregation queries. It can be seamlessly translated to aggregation pipeline JSON back and forth. It has a lot of advantages over plain aggregation pipeline JSON however you still can use plain JSON if you prefer to do so. Moreover dbMango providing REST API for such translation.

See [AFH overview](afh-overview.md) for more details.
 

