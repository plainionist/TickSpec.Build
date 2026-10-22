@echo off
setlocal

dotnet pack "%~dp0..\src\TickSpec.Build\TickSpec.Build.fsproj" -c Release ^
  "-p:NuspecFile=%~dp0TickSpec.Build.nuspec" ^
  "-p:NuspecBasePath=%~dp0." ^
  -o "%~dp0..\package"

endlocal & exit /b %ERRORLEVEL%
