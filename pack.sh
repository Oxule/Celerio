#!/usr/bin/env bash
dotnet build .\Celerio.Analyzers\Celerio.Analyzers.csproj -c Release -f netstandard2.0
dotnet build .\Celerio.Shared\Celerio.Shared.csproj -c Release -f netstandard2.0
cd .\Celerio.Analyzers
nuget pack .\Celerio.nuspec -Properties Configuration=Release -OutputDirectory ..\nugets