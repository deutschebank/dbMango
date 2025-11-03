![Template](Rms.Risk.Mango/wwwroot/images/mango.svg){height=170}

# dbMango - fully audited MongoDB shell.

`dbMango` is a secure, audited MongoDB management tool designed for database administrators, developers, and support teams. 
It provides a user-friendly interface for managing multiple MongoDB databases with features like data browsing, 
querying, aggregation, structure synchronization, and data migration. Access is strictly controlled through
LDAP-based roles (Admin, ReadOnly, ReadWrite), ensuring compliance and security. dbMango simplifies complex 
MongoDB operations with tools like Aggregation for Humans (AFH), which offers a human-readable syntax for 
creating aggregation pipelines. All privileged actions are logged for auditing, and the platform supports 
tasks like backups, restores, and shell access, making it an essential tool for efficient and compliant 
database management.

`dbMango` is implemented as a web application using Blazor and ASP.NET Core and uses the latest MongoDB .NET Driver. Some
very old versions of MongoDB servers are not supported by this driver.

See [Documentation](Rms.Risk.Mango/wwwroot/docs/index.md) for more details.

# Compilation

The project is built using .NET 9.0 SDK and requires Visual Studio 2022 (version 17.14.14 or later).

Project `Rms.Risk.Mango.Language` requires `Antlr4` package. 
See [notes here](Rms.Risk.Mango.Language/README.md) regarding `Antlr4` compilation issue.

Project `Rms.Risk.Mango.Db.Plugin` contains Deutsche Bank proprietary data and is not part of the open source version of dbMango. 
However, it is **NOT** required to build the solution. If you do not have access to this project, please exclude it from the solution
(or just ignore "not found" warning). dbMango will build and run without it.

# Copyright

```
/* dbMango
 * Copyright 2025 Deutsche Bank AG
 * SPDX-License-Identifier: Apache-2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
```