# Configuration

Configuration file have a specific name: `Rms.Risk.Mango.Settings.[ENVITONMENT].json`. Where `[ENVITONMENT]` (no square brackets) is the name
of the environment server running in. Environment name provided by `ASPNETCORE_ENVIRONMENT` environment variable.
Here’s a breakdown of the purpose of each parameter in the provided JSON configuration:

## **`SecuritySettings`**
This section contains security-related settings, including encryption, authentication, and certificate management.

1. **`MasterKeyFileName`**:
   - Path to the RSA private key file (in `.p12` format) used for decrypting encrypted values (e.g., passwords).
   - This file is critical for decrypting values that start with `*`.

2. **`MasterKeyPassword`**:
   - Path to the file containing the password for the `MasterKeyFileName`.
   - This ensures that the master key is securely protected and not hardcoded in the configuration.

3. **`CertificateFileName`**:
   - Path to the client certificate file (in `.p12` format) used for mutual TLS authentication.
   - This certificate is used to authenticate the application to external services.

4. **`CertificatePassword`**:
   - Encrypted password for the client certificate (`CertificateFileName`).
   - The password is required to unlock the certificate for use.

5. **`LogoffIdlePeriod`**:
   - Specifies the duration of inactivity after which a user is automatically logged off.
   - Example: `"05:00:00"` means 5 hours of inactivity.

---

### **`Oidc`** (OpenID Connect)
This section configures the OpenID Connect (OIDC) authentication settings.

1. **`ClientId`**:
   - The unique identifier for the application registered with the OIDC provider.
   - Example: `"dbMango_DEV"`.

2. **`Secret`**:
   - Encrypted client secret associated with the `ClientId`.
   - Used to authenticate the application with the OIDC provider.

3. **`ValidClientIds`**:
   - A list of additional client IDs that are considered valid for authentication.
   - Typically used for multi-environment setups.

4. **`ConfigUrl`**:
   - URL of the OIDC provider's configuration endpoint.
   - Example: `"https://uat.organization.com/am/oauth2/global"`.

5. **`ConfigCacheFile`**:
   - Path to a temporary file where the OIDC configuration is cached.
   - Useful in development or unstable environments to avoid repeated requests to the OIDC provider.

6. **`ForceRedirectUrlProtocol`**:
   - Forces the protocol (e.g., `https`) for redirect URLs during authentication.
   - Ensures secure communication. When running on the cloud with, for example, service mesh enabled
     internally application using `http` while externally it's visible as `https` behind the proxy. 
     So external OAuth2 provider should use `https` while application itself running `http` server.

7. **`ForceRedirectUrlPort`**:
   - Forces the port for redirect URLs during authentication.
     This is also related to containers - internal exposed port is not always
     the same as the prort visible externally via proxy.
   - `-1` means no specific port is enforced (i.e. do not apply eny overrides).

---

### **`Ldap`** (Lightweight Directory Access Protocol)
This section configures LDAP settings for user authentication and role mapping.

1. **`Url`**:
   - The LDAP server URL.
   - Example: `"ldaps://mycompany.com"` (uses `ldaps` for secure communication).

2. **`UserName`**:
   - The service account username used to connect to the LDAP server.
   - Example: `"MYDOMAIN\\service-for-dbMango"`.

3. **`Password`**:
   - Encrypted password for the LDAP service account.

4. **`EntryPoint`**:
   - The base distinguished name (DN) for LDAP queries.
   - Example: `"DC=mydomain,DC=mycompany,DC=com"`.

5. **`RoleGroupMapping`**:
   - Maps LDAP groups to application roles.
   - Example: Admin, ReadOnly, ReadWrite groups.

---

### **`CASubjectToAccept`**
- A list of acceptable Certificate Authority (CA) subjects for validating client certificates.
- Example:
  > `"CN=My Organisation Root CA, OU=PKI, O=My Organisation, C=UK"`

---

### **`ClientCertWhiteList`**
- A list of specific client certificates that are explicitly allowed to access the application.
- Typically used for additional security in environments requiring mutual TLS.

---

### Encrypted Values
- Values starting with `*` (e.g., `CertificatePassword`, `Oidc.Secret`, `Ldap.Password`) are encrypted using the RSA key specified in `MasterKeyFileName`.
- These values are decrypted at runtime using the master key and its password.

### Summary
This configuration is designed to ensure secure communication, authentication, and access control for the application. It uses:
- **RSA encryption** for sensitive values.
- **OIDC** for user authentication.
- **LDAP** for role-based access control.
- **TLS certificates** for secure communication and mutual authentication.

Here’s a breakdown of the purpose of each parameter in the `DbMangoSettings` configuration:

---

### **`DbMangoSettings`**

This section contains settings specific to the `dbMango` application, including 
instance details, database connection settings, and audit configurations.

#### **Top-Level Parameters**
1. **`InstanceName`**:
   - The name of the `dbMango` instance.
   - Example: `"dbMango"`.
   - Purpose: Used to identify the instance in logs, UI, and other contexts. Usually set to something like PROD, UAT, DEV, but can be more complex in mylti-instance environments.

2. **`EnableDatabaseOverrides`**:
   
> DB specific. Do not use

   - A boolean flag indicating whether database-specific overrides are enabled.
   - Example: `true`.
   - Purpose: Allows the application to use configuration setting overrides loaded from the external database. This setting is DB specific.

3. **`Initial`**:
   - The initial database environment or instance name.
   - Example: `"UAT database"`.
   - Purpose: Specifies the default database environment to connect to upon startup.
     If used in environment with multiple databases using different LDAP access groups,
     Admin privileges for this database grants access to Onboarding functionality.

4. **`AuditExpireDays`**:
   - The number of days after which audit logs are considered expired and can be purged.
   - Example: `365`.
   - Purpose: Ensures audit logs are retained only for the required duration to comply with data retention policies.

5. **`DefaultTimeout`**:
   - The default timeout for database operations.
   - Example: `"00:00:15"` (15 seconds).
   - Purpose: Specifies the maximum time to wait for a database operation before timing out.

6. **`MongoDbDocUrl`**:
   - The URL to the MongoDB documentation or command reference file.
   - Example: `"/app/data/mongodb-command.zip"` or `https://raw.githubusercontent.com/mongodb/docs/5084326383812cc65297bc8151760b9250a723b6/content/manual/v7.0/source/reference/command/`.
   - Purpose: Provides a local or remote path to MongoDB documentation for reference.

7. **`MongoDbDocProxyUrl`**:
   - A proxy URL for accessing MongoDB documentation.
   - Example: `""` (empty string).
   - Purpose: Used when a proxy is required to access the documentation.

---

#### **`Settings`**
This section contains MongoDB-specific connection and operation settings.

1. **`MongoDbSocketTimeout`**:
   - The maximum time to wait for a socket operation.
   - Example: `"00:02:00"` (2 minutes).
   - Purpose: Prevents long waits for unresponsive socket operations.

2. **`MongoDbServerSelectionTimeout`**:
   - The maximum time to wait for server selection.
   - Example: `"00:00:10"` (10 seconds).
   - Purpose: Limits the time spent trying to select a MongoDB server from the cluster.

3. **`MongoDbConnectTimeout`**:
   - The maximum time to wait for a connection to be established.
   - Example: `"00:00:10"` (10 seconds).
   - Purpose: Ensures quick failure if a connection cannot be established.

4. **`MongoDbMinConnectionPoolSize`**:
   - The minimum number of connections in the connection pool.
   - Example: `"0"`.
   - Purpose: Specifies the minimum number of idle connections to maintain.

5. **`MongoDbMaxConnectionPoolSize`**:
   - The maximum number of connections in the connection pool.
   - Example: `"100"`.
   - Purpose: Limits the number of concurrent connections to the database.

6. **`MaxConnectionIdleTime`**:
   - The maximum time a connection can remain idle before being closed.
   - Example: `"00:10:00"` (10 minutes).
   - Purpose: Frees up unused connections to conserve resources.

7. **`MaxConnectionLifeTime`**:
   - The maximum lifetime of a connection.
   - Example: `"00:30:00"` (30 minutes).
   - Purpose: Ensures connections are periodically refreshed to avoid stale connections.

8. **`MongoDbQueryTimeout`**:
   - The maximum time to wait for a query to complete.
   - Example: `"60"` (seconds).
   - Purpose: Prevents long-running queries from blocking resources.

9. **`MongoDbPingTimeoutSec`**:
   - The timeout for pinging the MongoDB server.
   - Example: `"5"` (seconds).
   - Purpose: Ensures quick detection of unresponsive servers.

10. **`MongoDbQueryBatchSize`**:
    - The number of documents to fetch in a single batch during a query.
    - Example: `"5000"`.
    - Purpose: Optimizes query performance by controlling batch size.

11. **`MongoDbConnectionRetries`**:
    - The number of times to retry a failed connection attempt.
    - Example: `"1"`.
    - Purpose: Provides resilience against transient connection issues.

12. **`MongoDbRetryTimeoutSec`**:
    - The timeout between connection retry attempts.
    - Example: `"5"` (seconds).
    - Purpose: Prevents rapid retries that could overwhelm the server.

---

#### **`AuditLogsInOracle`**
> DB specific parameter. Do not use.
- A boolean flag indicating whether audit logs should be stored in Oracle.
- Example: `false`.
- Purpose: Determines whether audit logs are stored in Oracle or another system.

---

#### **`OracleConnectionSettings`**
> DB specific section. Do not use.

This section contains settings for connecting to an Oracle database.

1. **`ConnectionString`**:
   - The connection string for the Oracle database.
   - Example: `"user id=xxx;data source=yyyy;Connection Timeout=60;Pooling=true;Min Pool Size=1;Max Pool Size=5;Connection Lifetime=120;Incr Pool Size=1;Decr Pool Size=1;"`.
   - Purpose: Specifies the details for connecting to the Oracle database, including user credentials, connection pooling, and timeouts.

2. **`Password`**:
   - The encrypted password for the Oracle database user.
   - Example: `*S9nFFwo4TJF/...`.
   - Purpose: Ensures secure authentication to the Oracle database.

3. **`Wallet`**:
   - The path to the Oracle wallet file for TCPS connections.
   - Example: `"/app/oracle/UAT"`.
   - Purpose: Provides secure storage for Oracle credentials and connection details.

4. **`TnsAdmin`**:
   - The path to the Oracle TNS (Transparent Network Substrate) admin directory.
   - Example: `"/app/oracle/UAT"`.
   - Purpose: Specifies the location of the TNS configuration files for Oracle connectivity.

---

### Summary
The `DbMangoSettings` configuration defines the operational parameters for the `dbMango` application, including MongoDB connection settings, audit log retention, and Oracle database connectivity. It ensures secure, efficient, and reliable database operations while providing flexibility for overrides and customizations.

Here’s a breakdown of the purpose of each parameter in the `DatabasesConfig` configuration:

---

### **`DatabasesConfig`**
This section defines the configuration for multiple MongoDB database instances. 
Each database instance has its own settings for access control, connection details, and authentication.

---

### **`Databases`**
This is a collection of database instances, each identified by a unique name (e.g., `"Main UAT"`, `"Backup UAT"`).

#### **`Instance-name-here`**
This is the configuration for the `"Instance-name-here"` database environment.

1. **`Groups`**:
   - Defines the LDAP groups that control access to the database.
   - Example:
     - `"Admin"`: `"AA-A-WRITE"`
     - `"ReadOnly"`: `"AA-A-READ"`
     - `"ReadWrite"`: `"AA-A-ADMIN"`
   - Purpose: Specifies the LDAP groups for different roles:
     - **Admin**: Full access to the database.
     - **ReadOnly**: View-only access.
     - **ReadWrite**: Read and write access.

2. **`Config`**:
   - Contains the connection and authentication settings for the database.

   - **`MongoDbUrl`**:
     - The connection string for the MongoDB instance.
     - Example: `"mongodb://host-name-here:27017"`.
     - Purpose: Specifies the host and port of the MongoDB server.

   - **`MongoDbDatabase`**:
     - The name of the *only* database dbMango is allowed to connect to. If not specified dbMango can connect to any database on the server (UI selector shown).
     - Example: `"Data"`.
     - Purpose: Identifies the specific database within the MongoDB instance.

   - **`DirectConnection`**:
     - A boolean flag indicating whether to use a direct connection to the MongoDB server.
     - Example: `true`.
     - Purpose: Ensures the application connects directly to the specified MongoDB server without using a replica set.

   - **`UseTls`**:
     - A boolean flag indicating whether to use TLS (Transport Layer Security) for the connection.
     - Example: `true`.
     - Purpose: Enables secure communication between the application and the MongoDB server.

   - **`AllowShardAccess`**:
     - A boolean flag indicating whether direct access to shards within sharded MongoDB instance is allowed.
     - Example: `false`.
     - Purpose: Restricts direct access to shards for environments where shards are not directly visible to dbMango.

   - **`Auth`**:
     - Contains authentication details for connecting to the MongoDB server.

     - **`User`**:
       - The username for authentication.
       - Example: `"admin"`.
       - Purpose: Specifies the MongoDB user with the required permissions.

     - **`Password`**:
       - The encrypted password for the MongoDB user.
       - Example: `*SDeFFX3...`.
       - Purpose: Ensures secure authentication to the MongoDB server.

     - **`AuthDatabase`**:
       - The database used for authentication.
       - Example: `"admin"`.
       - Purpose: Specifies the MongoDB database where the user credentials are stored.

     - **`Method`**:
       - The authentication method used.
       - Example: `"SCRAM-SHA-256"`.
       - Purpose: Indicates the authentication mechanism (e.g., SCRAM-SHA-256 for secure password-based authentication).

### Summary
The `DatabasesConfig` section defines configurations for multiple MongoDB environments, including access control, connection details, and authentication. Each environment is tailored to specific requirements, such as direct connections, replica sets, TLS usage, and sharded collection access. This ensures flexibility and security for managing MongoDB databases in different environments.

## Authentication

Authentication implemented via OAuth2 Authorization code flow. See the main [README.md](../readme.md#Setup%20CIDP) file
for the more detailed description.

First you need to register your application as Oidc client to obtain ClientID and ClientSecret.
The client type you need is "Classic server side rendered Web Application".

At the end of app creation you need to note your `clientId` and `secret`. These parameters needs 
to be put into a configuration file.

    "SecuritySettings": {
      "Oidc": {
        "ClientId": "xxxxx_DEV",
        "ValidClientIds": [],
        // dotnet user-secrets set "SecuritySettings:Oidc:Secret" "actual value"
        "Secret": "",
        "ConfigUrl": "https://host-name-here/oauth2/global",
        "ConfigCacheFile":  "%TEMP%/.oidc.json"
      },
      // ...
    }

Note that you should not put your secret directly into this file instead use the following command:

    dotnet user-secrets set "SecuritySettings:Oidc:Secret" "actual value"

This way your client secret will not be save into `git` and you avoid an audit point.

It is recommended to use different `clientId` for different environments. Note `_DEV` in 
`xxxxx_DEV`.

`Rms.Service.Bootstrap` have an ability to cache OIDC configuration into a temporary file.
To do so set `ConfigCacheFile` parameters as shown above. Do not use this feature for `PROD`.
But it's useful in `DEV` where OIDC may be restarted/restarting unpredictably.

There are a couple of things you need to know while implementing/using OAuth2 authentication for Blazor
applications:

1. Blazor uses web sockets, so all the suggested mechanics for ASP.NET are not working
2. Saying that there *are* some places where you can use them: when user enters the address in the blowser the
   first call in a plain HTTP(s) request. Here you have an access to `HttpContext`.
3. User activity needs to be tracked as best practice requires user to be logged out after 15 minutes of inactivity.

Standard template provides the following:

- Login page: [Login.cshtml](Pages/Login.cshtml.cs). This is plain ASP.NET page used to initiate CIDP authentification.
- Lgout page: [Logout.cshtml](Pages/Logout.cshtml.cs). This is plain ASP.NET page used to initiate user logout.
- Authentication script [auth-helper.js](../Rms.Service.Bootstrap/scripts/auth-helper.js) used to track and report user inactivity. 
  It gets injected via [_Host.cshtml](Pages/_Host.cshtml)
- The cornerstone of login/logout functionality [LogoutHandler.cs](../Rms.Service.Bootstrap/Security/LogoutHandler.cs) provided 
  by `Rms.Service.Bootstrap`.

## Authorization

dbMango uses LDAP authentication. I.e. you need to be a member of one of the configured groups
to have an access to the application.

Such configuration is done via `Rms.Risk.Mango.Settings.XXX.json` where `XXX` is your environment name.
The configuration example is as follows:

```json
  "DatabasesConfig": {
    "Databases": {
      "Test database": {
        "Groups": {
          "Admin":     "LDAP-GROUP-ADMIN",
          "ReadOnly":  "LDAP-GROUP-READONLY",
          "ReadWrite": "LDAP-GROUP-READWRITE"
        },
        "Config": {
          "MongoDbUrl": "mongodb://<IP address or host name>:27017",
          "MongoDbDatabase": "<database name>",
          "DirectConnection": false,
          "UseTls": true,
          "AllowShardAccess": true,
          "Auth": {
            "User": "admin",
            "Password": "*<encrypted password here>",
            "AuthDatabase": "admin",
            "Method": "SCRAM-SHA-256"
          }
        }
      }
    }
  }
```

For password encryption you should use built-in encryption page accessible by `/admin/encrypt` URL.
Note that password once encrypted can't be decrypted by user (there is no UI for it for reason!).
Encrypted values can be used anywhere in the config. They should start with the star `*` followed by BASE64-encoded value.
Luckily the encryption page provided generating this exact format.
