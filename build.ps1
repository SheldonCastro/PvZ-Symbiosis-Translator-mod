param([ValidateSet('Debug','Release')][string]$Configuration = 'Release', [string]$GameDir = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\Tools\check-zero-canvas.ps1"
dotnet build "$PSScriptRoot\PvZ_Symbiosis_Translator\PvZ_Symbiosis_Translator.csproj" -c $Configuration "-p:GameDir=$GameDir" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
dotnet run --project "$PSScriptRoot\Tests" -c Release -- "$PSScriptRoot\PvZ_Symbiosis_Translator\Localization\pt-BR"
if ($LASTEXITCODE -ne 0) { throw 'Tests/pack validation failed' }
