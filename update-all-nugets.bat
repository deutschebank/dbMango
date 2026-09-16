@echo off

cd tools\update-nugets
call build.bat
cd ..\..

@if exist Directory.Packages.props.new (
    del /F /Q Directory.Packages.props.new
)

echo Running tools\update-nugets\bin\update-nugets.exe
cd

tools\update-nugets\bin\update-nugets.exe

if exist Directory.Packages.props.new (
    move /Y Directory.Packages.props.new Directory.Packages.props
)

