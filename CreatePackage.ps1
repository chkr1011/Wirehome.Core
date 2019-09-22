param([string]$assemblyVersion, [string]$packageVersion)

if ([string]::IsNullOrEmpty($assemblyVersion)) {$assemblyVersion = "0.0.1"}
if ([string]::IsNullOrEmpty($packageVersion)) {$packageVersion = "0.0.1"}

$vswhere = ${Env:\ProgramFiles(x86)} + '\Microsoft Visual Studio\Installer\vswhere'
$msbuild = &$vswhere -products * -requires Microsoft.Component.MSBuild -latest -find MSBuild\**\Bin\MSBuild.exe

Write-Host
Write-Host "Assembly version = $assemblyVersion"
Write-Host "Package version  = $packageVersion"
Write-Host "MSBuild path     = $msbuild"
Write-Host

Write-Host "Cleaning output directory..."
Remove-Item Wirehome.Core.Hosts.Console\bin\Release\netcoreapp2.2\publish -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Building project..."
&dotnet publish .\Wirehome.Core.Hosts.Console\Wirehome.Core.Hosts.Console.csproj --configuration Release /p:FileVersion=$assemblyVersion /p:Version=$packageVersion

Write-Host "Creating package..."
$source = ".\Wirehome.Core.Hosts.Console\bin\Release\netcoreapp2.2\publish"
$destination = ".\Wirehome.Core.Hosts.Console\bin\Wirehome.Core-Portable-v$packageVersion.zip"
If(Test-path $destination) {Remove-item $destination}
 Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($source, $destination) 