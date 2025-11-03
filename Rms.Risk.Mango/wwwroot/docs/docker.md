# Docker image creation

The provided Dockerfile outlines a multi-stage build process for creating a Docker container for the `Rms.Risk.Mango` application. Below is a step-by-step explanation of the container creation process.

Refer to https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

## Example Dockerfile

> Note: This is just an example file. You may need additional steps, or some steps can be removed
(updating CA certificates for instance). You need to adjust it for your organization. Replace any 
placeholder values (like hostnames, URLs) with your actual values before using this Dockerfile.

```dockerfile
# ---------------- MAKE BASE CONTAINER ---------------------

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app
EXPOSE 8080

# Copy private CA certificates if needed to access binary repos. Otherwise delete next 2 lines.
COPY ./Resources/Docker/Certs/*.crt /usr/local/share/ca-certificates/
RUN update-ca-certificates

ENV DEBIAN_FRONTEND noninteractive
RUN rm -rf /etc/localtime && \
    ln -s /usr/share/zoneinfo/Europe/London /etc/localtime && \
    apt-get update && \
    apt-get -y install unzip

ENV LANG en_GB.UTF-8
ENV LANGUAGE en_GB:en
ENV LC_ALL en_GB.UTF-8
ENV NLS_LANG ENGLISH_UNITED KINGDOM.WE8MSWIN1252

# ---------------- MAKE BUILD CONTAINER ---------------------

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

RUN echo Build image is `uname -a` && \
    cat /etc/*-release

# Copy private CA certificates if needed to access binary repos. Otherwise delete next 2 lines.
COPY ./Resources/Docker/Certs/* /usr/local/share/ca-certificates/
RUN update-ca-certificates

# Installing dependencies for openJDK (ANTLR4 needs it)
ENV DEBIAN_FRONTEND noninteractive
RUN rm -rf /etc/localtime && \
    ln -s /usr/share/zoneinfo/Europe/London /etc/localtime && \
	echo "------------------ apt-get update" && \
    apt-get update && \
	apt install -y openjdk-17-jdk

# ---------------- COPY PROJECTS TO BE USED IN DOTNET RESTORE ---------------------
# This saves a lot of time on subsequent rebuilds
#

COPY ["nuget.config", "dbMango.sln", "Directory.Build.props", "Directory.Packages.props", "./"]

COPY  Rms.Risk.Mango/Rms.Risk.Mango.csproj                                Rms.Risk.Mango/
COPY  Rms.Risk.Mango.Pivot.Core/Rms.Risk.Mango.Pivot.Core.csproj          Rms.Risk.Mango.Pivot.Core/
COPY  Rms.Service.Bootstrap/Rms.Service.Bootstrap.csproj                  Rms.Service.Bootstrap/
COPY  Rms.Risk.Mango.Language/Rms.Risk.Mango.Language.csproj              Rms.Risk.Mango.Language/
COPY  Rms.Risk.Mango.Pivot.UI/Rms.Risk.Mango.Pivot.UI.csproj              Rms.Risk.Mango.Pivot.UI/
COPY  Rms.Risk.Mango.Db.Plugin/Rms.Risk.Mango.Db.Plugin.csproj            Rms.Risk.Mango.Db.Plugin/
COPY  Rms.Risk.Mango.Interfaces/Rms.Risk.Mango.Interfaces.csproj          Rms.Risk.Mango.Interfaces/
COPY  Tests.Rms.Risk.Mango/Tests.Rms.Risk.Mango.csproj                    Tests.Rms.Risk.Mango/
COPY  Tests.Rms.Risk.Mango.Language/Tests.Rms.Risk.Mango.Language.csproj  Tests.Rms.Risk.Mango.Language/

# ---------------- RUN RESTORE ---------------------

RUN dotnet restore dbMango.sln

# ---------------- COPY SOURCES AND RUN BUILD ---------------------

COPY . .
ARG CONFIGURATION=Release
ARG BUILD_NUMBER=1.0.0

# Prior to this need to set versions in GlobalAssemblyInfo.cs
RUN dotnet build dbMango.sln -c $CONFIGURATION --no-restore -p:Version=$BUILD_NUMBER -p:AssemblyVersion=$BUILD_NUMBER

# ---------------- RUN UNIT TESTS ---------------------

RUN dotnet test dbMango.sln --logger "trx;logfilename=test-results.trx"  -c $CONFIGURATION --no-restore --no-build --results-directory /src/testresults

# ---------------- PUBLISH ---------------------

FROM build AS publish
WORKDIR /src

ARG CONFIGURATION=Release
ARG BUILD_NUMBER=1.0.0
RUN dotnet publish dbMango.sln -c $CONFIGURATION -o /app/publish --no-restore --no-build --no-self-contained -p:Version=$BUILD_NUMBER -p:AssemblyVersion=$BUILD_NUMBER

# ---------------- MAKE THE FINAL CONTAINER ---------------------
FROM base AS final
WORKDIR /app

ARG APPLICATION_ENV=Development
ARG BUILD_NUMBER=1.0.0

# Set metadata labels as you see fit
LABEL application="Rms.Risk.Mango"

COPY --from=publish /app/publish .
COPY --from=publish /src/testresults ./test-results
COPY --from=publish /src/Resources/Docker/oracle ./oracle
COPY --from=publish /src/Resources/Docker/data ./data

# strictly speaking we don't need these two in the output, but we need to publish them to private repo
# they will be copied to the host system later in the build process
COPY --from=publish /src/bin/Release/*.nupkg .
COPY --from=publish /src/bin/Release/*.snupkg .

ENV ASPNETCORE_ENVIRONMENT=$APPLICATION_ENV
ENV TNS_ADMIN=/app/oracle/$APPLICATION_ENV

ENV ASPNETCORE_URLS=http://*:8080/
# Configure Kestrel to bind http to port 8080. If you are using service mesh communications are in fact encrypted.
ENV Kestrel__EndpointDefaults__Protocols=Http

HEALTHCHECK CMD curl --fail http://localhost:8080/healthz/live || exit
ENTRYPOINT dotnet /app/Rms.Risk.Mango.dll
```

## Docker build parameters

The following arguments (`ARG`) can be passed to the `docker build` command for this Dockerfile:

1. **`CONFIGURATION`**  
   - Default: `Release`  
   - Description: Specifies the build configuration (e.g., `Debug` or `Release`).
   - Usage: Determines the build output configuration.

2. **`BUILD_NUMBER`**  
   - Default: `1.0.0`  
   - Description: The version number of the build.
   - Usage: Used for setting the application version in the build.

3. **`APPLICATION_ENV`**  
   - Default: `Development`  
   - Description: Specifies the application environment (e.g., `Development`, `Production`).
   - Usage: Sets the `ASPNETCORE_ENVIRONMENT` variable in the final container.

---

**Example `docker build` Command**

```sh
docker build \
  --build-arg CONFIGURATION=Release \
  --build-arg BUILD_NUMBER=1.2.3 \
  --build-arg APPLICATION_ENV=Production \
  -t my-image:1.2.3 .
```

These arguments allow you to customize the build process and configure the resulting Docker image.

## Dockerfile Explanation
---

**1. Base Container Creation**
The `base` stage sets up the runtime environment for the application:
- **Base Image**: `aspnet:9.0` is used as the base image, which includes the .NET runtime.
- **Working Directory**: The working directory is set to `/app`.
- **Certificates**: Database CA certificates are copied to the container and registered using `update-ca-certificates`.
- **Artifactory Proxy**: Adds the Artifactory proxy to the `sources.list` for accessing custom package repositories.
- **Environment Variables**: Sets locale and timezone configurations for the container.
- **Dependencies**: Installs required tools like `unzip` 

**2. Build Container Creation**
The `build` stage sets up the build environment:
- **Base Image**: `sdk:9.0` is used, which includes the .NET SDK for building the application.
- **Certificates**: Database CA certificates are copied and registered.
- **Dependencies**: Installs `openjdk-17-jdk` for Java-related dependencies. It needed for ANTLR4 compiler.

**3. Dependency Restoration**
- **Project Files**: Copies the solution file (`dbMango.sln`), project files, and configuration files to the container.
- **Restore Dependencies**: Runs `dotnet restore` to restore NuGet packages for all projects in the solution. This step is optimized for caching to speed up subsequent builds.

**4. Build the Application**
- **Source Code**: Copies the entire source code into the container.
- **Build**: Builds the solution using `dotnet build` with the specified configuration (default: `Release`).

**5. Run Unit Tests**
- **Testing**: Executes `dotnet test` to run unit tests. Test results are saved in the `/src/testresults` directory.

**6. Publish the Application**
The `publish` stage prepares the application for deployment:
- **Publish**: Runs `dotnet publish` to generate the application binaries in the `/app/publish` directory. The application is published as a framework-dependent deployment (not self-contained).

**7. Final Container Creation**
The `final` stage creates the production-ready container:
- **Base Image**: Uses the `base` stage as the foundation.
- **Application Files**: Copies the published application files, test results, and additional resources (e.g., Oracle configuration, data files) from the `publish` stage.
- **NuGet Packages**: Copies `.nupkg` and `.snupkg` files for publishing to Artifactory.
- **Environment Variables**: Configures runtime environment variables, including:
  - `ASPNETCORE_ENVIRONMENT`: Specifies the application environment (e.g., Development, Production).
  - `ASPNETCORE_URLS`: Configures Kestrel to listen on port `8080`.
  - `TNS_ADMIN`: Sets the Oracle configuration directory. (DB specific. You can avoid)
- **Health Check**: Defines a health check endpoint at `/healthz/live` to monitor the container's health.
- **Entry Point**: Specifies the command to run the application (`dotnet /app/Rms.Risk.Mango.dll`).

## **Summary**
This Dockerfile uses a multi-stage build process to create a lightweight, production-ready container for the `Rms.Risk.Mango` application. It ensures:
- Efficient dependency management and caching.
- Secure handling of certificates and environment variables.
- Comprehensive testing and versioning during the build process.
- A minimal runtime image optimized for performance and security.