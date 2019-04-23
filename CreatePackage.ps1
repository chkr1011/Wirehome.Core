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
Remove-Item ..\Wirehome.Core.Hosts.Console\bin\Debug\netcoreapp2.2 -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Building project..."
&$msbuild ..\Wirehome.Core.Hosts.Console\Wirehome.Core.Hosts.Console.csproj /t:Build /p:Configuration="Debug" /p:TargetFramework="netcoreapp2.2" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m

Write-Host "Creating package..."
$source = "..\Wirehome.Core.Hosts.Console\bin\Debug\netcoreapp2.2"
$destination = ".\Wirehome.Core-v$packageVersion.zip"
If(Test-path $destination) {Remove-item $destination}
 Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($source, $destination)