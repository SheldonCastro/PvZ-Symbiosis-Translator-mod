$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '..\PvZ_Symbiosis_Translator'
$patterns = @('AddComponent\s*<\s*(Canvas|CanvasScaler|GraphicRaycaster)\s*>', 'new\s+GameObject\("PvZTranslation(UI|Toast)"\)', 'GetComponentsInChildren\s*<')
$violations = @()
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File -Filter '*.cs') {
    if ($file.FullName -match '\\(obj|bin)\\') { continue }
    foreach ($pattern in $patterns) {
        $violations += @(Select-String -LiteralPath $file.FullName -Pattern $pattern)
    }
}
if ($violations.Count) { $violations | ForEach-Object { Write-Output $_ }; throw 'Zero-Canvas/static traversal regression' }
Write-Output 'PASS zero translator Canvas creation and no generic descendant traversal'
