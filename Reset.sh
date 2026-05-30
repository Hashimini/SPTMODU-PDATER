#!/bin/bash

rm -rf bin obj
dotnet clean
dotnet restore
clear
echo "Cleansed"
echo "My default publish command:"
echo "dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true"
