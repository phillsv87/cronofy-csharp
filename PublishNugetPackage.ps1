#!/usr/local/bin/pwsh
param(
    [string]$projectDir="$PSScriptRoot/src/Cronofy",
    [string]$nugetKey=$null,
    [switch]$noPush,
    [switch]$noPack,
    [switch]$noCheckNblCommon
)

$ErrorActionPreference = "Stop"

if(!$noCheckNblCommon -and (Test-Path "~/.nblcommon.csproj")){
    [xml]$nc=Get-Content "~/.nblcommon.csproj"
    $dr=$nc.Project.PropertyGroup.DirectNblRef
    if( $dr -and ($dr.ToUpper() -eq "TRUE"))
    {
        throw "DirectNblRef is enabled in ~/.nblcommon.csproj. This is not allowed durring package publishing"
    }
}

if(Test-Path -Path "~/.nugetkey"){
    Write-Host "Use key ~/.nugetkey"
    $nugetKey=(Get-Content "~/.nugetkey" -Raw).Trim()
}

if(!$nugetKey){
    Write-Host "Use key $env:NblKeyDir/nuget.key"
    $nugetKey=(Get-Content "$env:NblKeyDir/nuget.key" -Raw).Trim()
}

if(!$nugetKey){
    throw "nugetkey not specified. Either use the -nugetKey argument or set the NblKeyDir enviornment variable"
}

Push-Location $projectDir
try{
    [xml]$proj = Get-Content *.csproj
    [string]$Version=$proj.Project.PropertyGroup.Version
    [string]$PackageId=$proj.Project.PropertyGroup.PackageId

    $Version=$Version.Trim()
    $PackageId=$PackageId.Trim()

    if(!$Version){
        throw "Project.PropertyGroup.Version not set"
    }

    if(!$PackageId){
        throw "Project.PropertyGroup.PackageId not set"
    }

    Write-Host "Pack and Publish $PackageId-$Version"

    if(!$noPack){
        dotnet pack -c Release
        if( -not $?){
            throw "Pack $PackageId Failed"
        }
    }

    if(!$noPush){
        dotnet nuget push "bin/Release/$($PackageId).$($Version).nupkg" -k $nugetKey -s https://api.nuget.org/v3/index.json
        if( -not $?){
            throw "Publish $PackageId Failed"
        }
    }

    Write-Host "Publish $PackageId-$Version Success" -ForegroundColor DarkGreen

}finally{
    Pop-Location
}
