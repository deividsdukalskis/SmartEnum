[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    function Invoke-Dotnet {
        & dotnet @args
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
    }

    Invoke-Dotnet restore SmartEnum.slnx
    Invoke-Dotnet format SmartEnum.slnx --no-restore --severity info --verify-no-changes
    Invoke-Dotnet build SmartEnum.slnx -c Release --no-restore -warnaserror
    Invoke-Dotnet run --project GeneratorTests -c Release --no-build
    Invoke-Dotnet run --project TestProject -c Release --no-build

    $verificationRoot = Join-Path $PSScriptRoot 'bin/verification'
    $packageDirectory = Join-Path $verificationRoot 'packages'
    $consumerDirectory = Join-Path $verificationRoot 'consumer'
    $cacheDirectory = Join-Path $verificationRoot 'cache'
    New-Item -ItemType Directory -Force $packageDirectory, $consumerDirectory, $cacheDirectory | Out-Null
    # A unique local version prevents NuGet from reusing an older smoke-test build.
    $packageVersion = '0.0.0-verify.' + [Guid]::NewGuid().ToString('N')
    Invoke-Dotnet pack SmartEnum/SmartEnum.csproj -c Release --no-build --no-restore -o $packageDirectory "-p:PackageVersion=$packageVersion" -warnaserror

    $packageFile = Join-Path $packageDirectory "SmartEnum.$packageVersion.nupkg"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($packageFile)
    try {
        if ('analyzers/dotnet/cs/SmartEnum.dll' -notin $archive.Entries.FullName) { throw 'Analyzer DLL missing from package.' }
        if ($archive.Entries.FullName -match '^lib/') { throw 'Analyzer package unexpectedly exposes a runtime library.' }
    }
    finally { $archive.Dispose() }

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <RestorePackagesPath>$cacheDirectory</RestorePackagesPath>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="SmartEnum" Version="$packageVersion" /></ItemGroup>
</Project>
"@ | Set-Content (Join-Path $consumerDirectory 'Consumer.csproj')
    @'
using SmartEnum;

if (Root.MapDataToTypeUnvalidated(1, "package") is not Leaf { Name: "package" })
    throw new System.Exception("Packaged generator did not initialize the consumer correctly.");
System.Console.WriteLine("PASS isolated NuGet consumer");

[SmartEnum<int>("Id")]
public abstract partial class Root { public string Name { get; } }
public partial class Leaf : Root { public const int Id = 1; }
'@ | Set-Content (Join-Path $consumerDirectory 'Program.cs')

    Invoke-Dotnet restore (Join-Path $consumerDirectory 'Consumer.csproj') --source $packageDirectory
    Invoke-Dotnet build (Join-Path $consumerDirectory 'Consumer.csproj') -c Release --no-restore -warnaserror
    Invoke-Dotnet run --project (Join-Path $consumerDirectory 'Consumer.csproj') -c Release --no-build
    Write-Output 'Verification completed successfully.'
}
finally { Pop-Location }
