> This is an example instruction. Adjust it for your organization.

# Onboarding process

The onboarding process for dbMango involves configuring a MongoDB instance, setting up access controls, and submitting the required information to the relevant teams. Below is a step-by-step description of the process:

 

1. Fill in the JSON Configuration
- Use the provided JSON template to define the configuration for your MongoDB instance. You can use Copy button on Onboarding page to create a template if you have access. Otherwise use the Json below.
- Key Fields:
 - Name: Specify the name of the MongoDB instance.
 - Groups: Define LDAP groups for access control:
   - `Admin`: For users with administrative privileges.
   - `ReadOnly`: For users with read-only access.
   - `ReadWrite`: For users with read and write access.
 - Config:
   - `MongoDbUrl`: Provide the connection string for the MongoDB instance.
   - `Auth`: Include the MongoDB admin username and an encrypted password (see step 2 for encryption).
   - `DirectConnection`: Set to `true` for direct connections.
   - `UseTls`: Set to `true` to enable TLS for secure communication.
   - `AllowShardAccess`: Set to `false` to restrict shard access.
 - Contacts: Provide a list of email addresses or a contact person for the instance.

Json template:

```json
{
  "Name": "Instance name here",
  "Value": {
    "Groups": {
      "Admin": "XXX-ADMIN",
      "ReadOnly": "XXX-RO",
      "ReadWrite": "XXX-RW"
    },
    "Config": {
      "MongoDbUrl": "mongodb://hostname1.com:27017,hostname2.com:27017",
      "MongoDbDatabase": "",
      "Auth": {
        "User": "admin",
        "Password": "<encrypted password here>",
        "AuthDatabase": "admin",
        "Method": "SCRAM-SHA-256"
      },
      "AdminAuth": null,
      "DirectConnection": false,
      "UseTls": true,
      "AllowShardAccess": false
    },
    "Contacts": "app-list@company.com"
  }
}
```

2. Encrypt the MongoDB Password
- Visit dbMango Encryption Tool on /admin/encrypt URL (it's not present in the menu).
- Enter the MongoDB admin password and encrypt it.
- Copy the value from the “As password” field (it should start with `*`).
- Replace the `<encrypted password here>` placeholder in the JSON with the encrypted password.

 

3. Submit the JSON Configuration
- Send an email to mailto:mail@company.com with the following details:
 - The completed JSON configuration.
 - Instance name.
 - Group email for SL3 support.
 - Group email for SL2/SL1 production support.

 

4. Note on Access Control
- dbMango uses the provided LDAP groups to enforce access control:
 - Admin: Full access to administrative features.
 - ReadOnly: Limited to viewing data.
 - ReadWrite: Allows data modifications.
- Even though dbMango requires MongoDB admin credentials for full functionality, access is restricted by the LDAP groups, ensuring that not all users have admin privileges.

 

5. Create a Change Request
- Submit a change request with a task assigned to the FX Infra team.
- Include the following details:
 - ITSK Number: The task number for tracking.
 - Change Window: The scheduled time for onboarding.
- For assistance, contact mailto:mail@company.com.

 

Important Notes
- dbMango is designed to work with MongoDB admin credentials. Using a limited user may restrict functionality.
- Ensure that all required details are accurate to avoid delays in the onboarding process.
