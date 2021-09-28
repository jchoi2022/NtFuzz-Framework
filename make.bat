@echo off
set BUILDDIR=%cd%\build
mkdir %BUILDDIR%
dotnet build -c Debug -o %BUILDDIR% src/FuzzingFramework.fsproj
@echo on
