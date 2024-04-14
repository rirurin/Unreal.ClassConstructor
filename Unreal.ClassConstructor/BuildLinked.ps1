# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/Unreal.ClassConstructor/*" -Force -Recurse
dotnet publish "./Unreal.ClassConstructor.csproj" -c Release -o "$env:RELOADEDIIMODS/Unreal.ClassConstructor" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location