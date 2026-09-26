param(
    [ValidateSet('Release')][string]$Configuration = 'Release',
    [string]$GameDir = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath $GameDir).Path
if (!(Test-Path -LiteralPath (Join-Path $gameRoot 'PVZGS.exe'))) { throw 'GameDir must point to the game installation' }
if (Get-Process PVZGS -ErrorAction SilentlyContinue) { throw 'Close PVZGS before deployment' }
& "$PSScriptRoot\build.ps1" -Configuration Release -GameDir $gameRoot
if ($LASTEXITCODE -ne 0) { throw 'Build/tests failed; active mod was not changed' }
$source = Join-Path $PSScriptRoot 'PvZ_Symbiosis_Translator'
$built = Join-Path $source 'bin\Release\net6.0\PvZ_Symbiosis_Translator.dll'
if (!(Test-Path -LiteralPath $built)) { throw 'Expected new DLL is missing' }
$mods = Join-Path $gameRoot 'Mods'
$destination = Join-Path $mods 'PvZ_Symbiosis_Translator'
$legacy = Join-Path $mods 'PvZSymbiosisTranslation'
$dll = Join-Path $mods 'PvZ_Symbiosis_Translator.dll'
$oldDll = Join-Path $mods 'PvZSymbiosisTranslation.dll'
$sourceLocales = Join-Path $source 'Localization'
$runtimeLocales = Join-Path $destination 'Localization'
$sourcePt = Join-Path $sourceLocales 'pt-BR'
$runtimePt = Join-Path $runtimeLocales 'pt-BR'
function Relative-File([string]$From, [string]$File) {
    $base = [IO.Path]::GetFullPath($From).TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)) + [IO.Path]::DirectorySeparatorChar
    $full = [IO.Path]::GetFullPath($File)
    if (!$full.StartsWith($base, [StringComparison]::OrdinalIgnoreCase)) { throw "File outside deployment source: $File" }
    return $full.Substring($base.Length)
}
function Json-Map([string]$File) {
    $parsed = Get-Content -LiteralPath $File -Raw | ConvertFrom-Json
    $map = [System.Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach ($property in $parsed.PSObject.Properties) { $map.Add($property.Name, [string]$property.Value) }
    return $map
}
$backup = Join-Path $gameRoot ('BACKUPS\deploy-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach ($path in @($dll, $oldDll, $destination, $legacy)) {
    if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination $backup -Recurse }
}
$runtimeExact = Join-Path $runtimePt 'Strings\exact.json'
if (Test-Path -LiteralPath $runtimeExact) {
    foreach ($relative in @('Strings\exact.json','Almanac\exact.json','ModStrings.json')) {
        $left = Json-Map (Join-Path $sourcePt $relative)
        $right = Json-Map (Join-Path $runtimePt $relative)
        if ($left.Count -ne $right.Count) { throw "pt-BR runtime/source differ in $relative; synchronize before deploy. Backup: $backup" }
        foreach ($key in $left.Keys) {
            if (!$right.ContainsKey($key) -or $left[$key] -cne $right[$key]) { throw "pt-BR runtime/source differ in $relative; synchronize before deploy. Backup: $backup" }
        }
    }
}
$conflicts = [System.Collections.Generic.List[string]]::new()
function Install-MissingFiles([string]$From, [string]$To, [string]$Label) {
    if (!(Test-Path -LiteralPath $From)) { return }
    foreach ($file in Get-ChildItem -LiteralPath $From -File -Recurse) {
        $relative = Relative-File $From $file.FullName
        $target = Join-Path $To $relative
        if (!(Test-Path -LiteralPath $target)) {
            New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $target
        } elseif ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $target).Hash) {
            # Existing destination wins; both versions remain recoverable in the backup/source.
            $conflicts.Add("$Label/$relative : existing destination preserved")
        }
    }
}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Install-MissingFiles $sourcePt $runtimePt 'pt-BR'
# An empty legacy dynamic_rules.json can be upgraded; a translator's nonempty rules stay untouched.
$sourceRules = Join-Path $sourcePt 'Strings\dynamic_rules.json'
$runtimeRules = Join-Path $runtimePt 'Strings\dynamic_rules.json'
if ((Test-Path -LiteralPath $runtimeRules) -and (Get-Content -LiteralPath $runtimeRules -Raw).Trim() -eq '[]') {
    Copy-Item -LiteralPath $sourceRules -Destination $runtimeRules -Force
}
$sourceTemplate = Join-Path $sourceLocales '_template'
$runtimeTemplate = Join-Path $runtimeLocales '_template'
New-Item -ItemType Directory -Path $runtimeTemplate -Force | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $sourceTemplate -File -Recurse) {
    $relative = Relative-File $sourceTemplate $file.FullName
    $target = Join-Path $runtimeTemplate $relative
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target -Force
}
# Earlier deploys copied C# sources to the runtime Localization root. The full mod was backed up above.
if (Test-Path -LiteralPath $runtimeLocales) {
    $safeRoot = (Resolve-Path -LiteralPath $runtimeLocales).Path + [IO.Path]::DirectorySeparatorChar
    foreach ($file in Get-ChildItem -LiteralPath $runtimeLocales -File -Filter '*.cs') {
        if (!$file.FullName.StartsWith($safeRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected source-file cleanup path' }
        Remove-Item -LiteralPath $file.FullName
    }
}
Install-MissingFiles (Join-Path $source 'Config') (Join-Path $destination 'Config') 'config'
$sourceUi = Join-Path $source 'bin\Release\net6.0\UI'
$runtimeUi = Join-Path $destination 'UI'
New-Item -ItemType Directory -Path $runtimeUi -Force | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $sourceUi -File) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $runtimeUi $file.Name) -Force }
foreach ($name in @('Background2.png','buttonsmall.png','theme.json')) {
    if (!(Test-Path -LiteralPath (Join-Path "$destination\UI" $name))) { throw "Missing deployed UI asset: $name" }
}
Copy-Item -LiteralPath $built -Destination $dll -Force
if ((Get-FileHash -LiteralPath $built).Hash -ne (Get-FileHash -LiteralPath $dll).Hash) { throw 'Deployed DLL hash mismatch' }
# Only after the new DLL is verified, retire the obsolete DLL from the active Mods folder.
if (Test-Path -LiteralPath $oldDll) {
    if ((Resolve-Path -LiteralPath $oldDll).Path -ne (Join-Path $mods 'PvZSymbiosisTranslation.dll')) { throw 'Unexpected legacy DLL path' }
    Move-Item -LiteralPath $oldDll -Destination (Join-Path $backup 'retired-PvZSymbiosisTranslation.dll')
}
$installedDlls = @(Get-ChildItem -LiteralPath $mods -File -Filter '*Translator*.dll')
if ($installedDlls.Count -ne 1 -or $installedDlls[0].FullName -ne $dll) { throw 'Expected exactly one active translator DLL' }
$validator = Join-Path $PSScriptRoot 'Tools\PackValidator'
$available = @(dotnet run --project $validator -c Release -- --list-locales $runtimeLocales | ConvertFrom-Json)
if ($LASTEXITCODE -ne 0 -or @($available | Where-Object { $_.locale -eq '_template' -or !(Test-Path -LiteralPath (Join-Path $runtimeLocales $_.locale)) }).Count -gt 0) { throw 'Invalid deployed locale discovery' }
$activeLanguage = (Get-Content -LiteralPath (Join-Path $destination 'Config\translation_config.json') -Raw | ConvertFrom-Json).language
if (@($available | Where-Object { $_.locale -eq $activeLanguage }).Count -ne 1 -or @($available | Where-Object { $_.locale -eq 'pt-BR' }).Count -ne 1) { throw 'Active locale or pt-BR missing from deployed locale discovery' }
foreach ($language in @('pt-BR',$activeLanguage) | Select-Object -Unique) {
    $folder = Join-Path $runtimeLocales $language
    if ((Get-Content -LiteralPath (Join-Path $folder 'manifest.json') -Raw | ConvertFrom-Json).locale -ne $language) { throw "Deployed manifest mismatch: $language" }
    $validation = dotnet run --project $validator -c Release -- $folder $language '1.2.0' | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $validation.status -ne 'PASS') { throw "Deployed pack validation failed: $language" }
}
$conflicts | Set-Content -LiteralPath (Join-Path $backup 'preserved-conflicts.txt') -Encoding utf8
Write-Output "Deployed PvZ_Symbiosis_Translator.dll. Post-deploy validation PASS. Preserved conflicts: $($conflicts.Count). Backup: $backup"
