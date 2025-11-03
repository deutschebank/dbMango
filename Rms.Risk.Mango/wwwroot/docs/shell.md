# Shell

## Overview
The `Shell` component is a administrative interface designed to facilitate database management and 
command execution. It provides a structured and interactive environment for administrators to connect 
to databases, execute commands, and view results. The page supports dynamic routing, allowing users 
to specify database and command contexts directly in the URL.

This component is particularly useful for managing MongoDB instances, as it integrates features 
like JSON validation, Markdown-based documentation, and encrypted data handling. The interface is 
designed to be user-friendly while maintaining robust security and audit capabilities.


## Navigation
The `Shell` page supports multiple routes to accommodate different contexts:
- `/admin/shell`: Opens the default shell interface.
- `/admin/shell/{DatabaseStr}`: Opens the shell for a specific database.
- `/admin/shell/{DatabaseStr}/{DatabaseInstanceStr}`: Opens the shell for a specific database and instance.
- `/admin/shell/{DatabaseStr}/{DatabaseInstanceStr}/{Command}`: Opens the shell for a specific database, instance, and preloaded command.

These routes allow administrators to quickly navigate to the desired context without manually configuring 
the interface. The dynamic routing ensures that the page adapts to the provided parameters, making it 
efficient for repetitive tasks.


## Features
The `Shell` page includes the following key features:

1. **Command Execution**:
   Users can execute MongoDB commands directly from the interface. Commands can be entered as JSON, 
   and the results are displayed in a structured format. The page supports both single commands and 
   scripts (JSON arrays of commands).

2. **JSON Formatting and Validation**:
   The interface includes tools to format and validate JSON input. This ensures that commands are 
   syntactically correct before execution, reducing the likelihood of errors.

3. **Documentation Integration**:
   The page integrates with a documentation service to provide context-sensitive help. 
   Markdown-based documentation is displayed for the current command, helping users understand 
   its purpose and usage.

4. **Encrypted Data Handling**:
   Sensitive data can be encrypted and used securely within commands. The encryption process 
   is seamless and integrates with the encryption component.

5. **Audit Records**:
   All executed commands include metadata for auditing purposes. This ensures that operations
   are traceable and linked to the initiating user.

6. **Clipboard Utilities**:
   Users can copy URLs and results to the clipboard for sharing or further analysis.


## Usage of Encrypted Data
The `Shell` page supports the use of encrypted data to enhance security during command execution. 

### Encrypting Data
To encrypt sensitive data:
1. Click the "Encrypt" button in the interface.
2. A dialog box will appear, allowing you to enter the clear text data.
3. The data is encrypted and returned as a placeholder in the format `[*encrypted_value]`.

### Using Encrypted Data
Encrypted placeholders can be directly used in JSON commands. For example:
```json
{
  "username": "admin",
  "password": "[*encrypted_password]"
}
```
 
Before execution, the `Shell` page automatically decrypts these placeholders. This ensures that 
sensitive data is never exposed in plain text during execution.


## Audit Records
Audit records are a critical feature of the `Shell` page, ensuring that all operations are 
traceable and compliant with organizational policies.


>WARNING! Audit records contain an exact copy of the executed command, including any sensitive data.
Always use encrypted placeholders `[*encrypted_value]` for sensitive information to avoid exposure in audit logs.

### Metadata Inclusion
Before executing a command, the metadata appended to the command JSON. This metadata includes:
- **Ticket Number**: A unique identifier for the operation, ensuring it is linked to a valid task.
- **User Email**: The email address of the user initiating the command.

For example, a command JSON might include the following metadata:
```json
{
  "comment": "ticket: 12345, email: user@example.com",
  "ping": 1
}
```
### Task Validation

For audit purposes `Shell` ensures that the user has a valid task number before executing commands. 
The task number is a concept coming from the enterprise world where each operation must be linked to a specific
task or ticket. 

Task validation is usually performed by checking the provided task number against active tasks. However this 
functionality highly depends on the organization's task management system and may require additional integration.
By default task validation is not performed, but it can be implemented as needed. See [Plugin Development](./plugins.md) for more information on how to extend the functionality.

If the task number is invalid or not provided, the user is prompted to provide a valid one. 
This prevents unauthorized operations and ensures that all actions are logged against an active task.

### Importance of Audit Records
Audit records are essential for:
- **Accountability**: Linking operations to specific users and tasks.
- **Compliance**: Ensuring that all actions adhere to organizational policies.
- **Troubleshooting**: Providing a clear history of executed commands for debugging purposes.

By maintaining detailed audit records, the `Shell` page ensures that administrative operations 
are secure, transparent, and well-documented.