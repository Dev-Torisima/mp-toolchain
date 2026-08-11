@echo off
cd /d %~dp0
cd ..\compiler
dotnet publish /p:PublishProfile=win-x64
dotnet publish /p:PublishProfile=win-x86
dotnet publish /p:PublishProfile=win-arm64
dotnet publish /p:PublishProfile=linux-x64
dotnet publish /p:PublishProfile=linux-arm
dotnet publish /p:PublishProfile=linux-arm64
dotnet publish /p:PublishProfile=osx-x64
dotnet publish /p:PublishProfile=osx-arm64
