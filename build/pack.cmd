@echo off
setlocal

dotnet build -c Release
\bin\NuGet.exe pack %~d0%~p0\TickSpec.Build.nuspec -outputdirectory package

endlocal
