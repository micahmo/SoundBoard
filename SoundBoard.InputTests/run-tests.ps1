# Build and exercise the app without launching the user's SoundBoard instance.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (!(Test-Path $vswhere)) { throw 'Install Visual Studio/Build Tools with .NET desktop build tools and the .NET Framework 4.8 targeting pack.' }
$msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'MSBuild was not found.' }
& $msbuild (Join-Path $repo 'SoundBoard.sln') /restore /p:RestorePackagesConfig=true /p:Configuration=Release /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
& $msbuild (Join-Path $PSScriptRoot 'SoundBoard.InputTests.csproj') /p:Configuration=Release /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Input test build failed.' }
& (Join-Path $PSScriptRoot 'bin\Release\SoundBoard.InputTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Input tests failed.' }
# The smoke test owns one WPF Application in its own STA process. Execution policy is process-local.
& powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'WindowsUiSmoke.ps1')
if ($LASTEXITCODE -ne 0) { throw 'WPF picker smoke test failed.' }
