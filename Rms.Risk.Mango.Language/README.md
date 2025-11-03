# AFH language

See [documentation](../Rms.Risk.Mango/wwwroot/docs/afh-overview.md).

# Compiling

Unfortunately there is something is wrong with `Antlr4` tooling. According to https://github.com/kaby76/Antlr4BuildTasks/blob/master/Readme.md the
project is setup correctly however we are experiencing the following error:

> ANT02: Went through the complete probe list looking for an Antlr4 tool jar, but could not find anything

To workaround it you can use one of the options:

1. To download it automatically during the build, uncomment this section within Rms.Risk.Mango.Language.csproj :
   ```
   <!--
     <PropertyGroup>
       <AntlrJarDir>$(SolutionDirectory)Resources/Antlr4</AntlrJarDir>
       <AntlrJarFile>antlr4-4.13.1-complete.jar</AntlrJarFile>
       <AntlrJarPath>$(AntlrJarDir)/$(AntlrJarFile)</AntlrJarPath>
       <AntlrDownloadUrl>https://www.antlr.org/download/antlr-4.13.2-complete.jar</AntlrDownloadUrl>
     </PropertyGroup>
   
     <Target Name="EnsureAntlr4Jar" BeforeTargets="Antlr4Compile">
       <Message Text="Ensuring ANTLR jar exists at $(AntlrJarPath)" Importance="High" />
       <MakeDir Directories="$(AntlrJarDir)" Condition="!Exists('$(AntlrJarDir)')" />
       <Exec Condition="!Exists('$(AntlrJarPath)') AND '$([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform($([System.Runtime.InteropServices.OSPlatform]::Windows)))' == 'true'"
             Command="powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -Command &quot;Invoke-WebRequest -Uri $(AntlrDownloadUrl) -OutFile '$(AntlrJarPath)'&quot;" />
       <Exec Condition="!Exists('$(AntlrJarPath)') AND '$([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform($([System.Runtime.InteropServices.OSPlatform]::Windows)))' != 'true'"
             Command="bash -c &quot;curl -L -o '$(AntlrJarPath)' $(AntlrDownloadUrl)&quot;" />
       <Message Condition="Exists('$(AntlrJarPath)')" Text="ANTLR jar present: $(AntlrJarPath)" Importance="High" />
     </Target>
   -->
   ```

2. Download `jar` https://www.antlr.org/download.html (direct link https://www.antlr.org/download/antlr-4.13.2-complete.jar)
   and put it into `$(SolutionDirectory)Resources/Antlr4` folder. Create one if needed.

Now it should work.