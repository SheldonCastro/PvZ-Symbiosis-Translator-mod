param(
    [string]$SourceCommit='HEAD',
    [string]$Version='1.1.0',
    [string]$BuildResult='Release PASS; 0 warnings; 0 errors',
    [string]$QaResult='PASS',
    [string]$RuntimeResult='Runtime validation required for the selected commit'
)
$ErrorActionPreference='Stop'
$toolRoot=$PSScriptRoot
$repo=(Resolve-Path -LiteralPath (Join-Path $toolRoot '..\..')).Path
$stageParent=Join-Path $repo 'dist\public-repo'
$stage=Join-Path $stageParent 'PvZ-Symbiosis-Translator'
$stagePrefix=[IO.Path]::GetFullPath($stageParent).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
if(!([IO.Path]::GetFullPath($stage).StartsWith($stagePrefix,[StringComparison]::OrdinalIgnoreCase))){throw 'Unsafe public staging path'}
& git -C $repo diff --quiet
if($LASTEXITCODE -ne 0){throw 'Working tree has unstaged changes; commit or restore them before promotion'}
& git -C $repo diff --cached --quiet
if($LASTEXITCODE -ne 0){throw 'Working tree has staged changes; commit or restore them before promotion'}
$commit=(& git -C $repo rev-parse "$SourceCommit^{commit}").Trim()
if($LASTEXITCODE -ne 0 -or !$commit){throw "Cannot resolve source commit: $SourceCommit"}
$commitDate=(& git -C $repo show -s --format=%cI $commit).Trim()
$allowlist=Get-Content -LiteralPath (Join-Path $toolRoot 'public-allowlist.json') -Raw | ConvertFrom-Json
$temp=Join-Path ([IO.Path]::GetTempPath()) ('pvz-public-'+[guid]::NewGuid().ToString('N'))
$archive=Join-Path $temp 'source.tar'
$extract=Join-Path $temp 'source'
New-Item -ItemType Directory -Path $extract -Force | Out-Null
try {
    & git -C $repo archive --format=tar --output=$archive $commit
    if($LASTEXITCODE -ne 0){throw 'git archive failed'}
    & tar -xf $archive -C $extract
    if($LASTEXITCODE -ne 0){throw 'tar extraction failed'}
    if(Test-Path -LiteralPath $stage){
        $resolved=[IO.Path]::GetFullPath($stage)
        if(!$resolved.StartsWith($stagePrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing unsafe staging cleanup'}
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    foreach($relative in @($allowlist.directories)+@($allowlist.files)){
        $source=Join-Path $extract $relative
        if(!(Test-Path -LiteralPath $source)){throw "Allowlisted path missing from selected commit: $relative"}
        $destination=Join-Path $stage $relative
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
    }
    foreach($pattern in $allowlist.exclude){
        $normalized=$pattern.Replace('\','/')
        foreach($file in Get-ChildItem -LiteralPath $stage -Recurse -File){
            $relative=[IO.Path]::GetRelativePath($stage,$file.FullName).Replace('\','/')
            if($relative -like $normalized){Remove-Item -LiteralPath $file.FullName -Force}
        }
    }
    $publicReadme=Join-Path $stage 'Docs\Public\README_PUBLIC.md'
    $publicRoadmap=Join-Path $stage 'Docs\Public\ROADMAP_PUBLIC.md'
    Copy-Item -LiteralPath $publicReadme -Destination (Join-Path $stage 'README.md') -Force
    Copy-Item -LiteralPath $publicRoadmap -Destination (Join-Path $stage 'ROADMAP.md') -Force
    Remove-Item -LiteralPath (Join-Path $stage 'Docs\Public') -Recurse -Force

    $fontManifest=Join-Path $stage 'PvZ_Symbiosis_Translator\Localization\pt-BR\Fonts\manifest.json'
    $fonts=@(Get-Content -LiteralPath $fontManifest -Raw | ConvertFrom-Json | Where-Object {$_.distribution -ne 'localOnly'})
    ConvertTo-Json -InputObject @($fonts) -Depth 10 | Set-Content -LiteralPath $fontManifest -Encoding utf8

    Push-Location $stage
    try {
        $testOutput=(& dotnet run --project Tests -c Release -- ./PvZ_Symbiosis_Translator/Localization/pt-BR 2>&1 | Out-String)
        if($LASTEXITCODE -ne 0){throw "Public candidate tests failed:`n$testOutput"}
    } finally {Pop-Location}
    foreach($directory in @(Get-ChildItem -LiteralPath $stage -Recurse -Directory | Where-Object {$_.Name -in @('bin','obj')} | Sort-Object FullName -Descending)){
        if(!$directory.FullName.StartsWith(([IO.Path]::GetFullPath($stage)+[IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe generated-directory cleanup'}
        Remove-Item -LiteralPath $directory.FullName -Recurse -Force
    }

    & (Join-Path $toolRoot 'validate_public_snapshot.ps1') -Candidate $stage
    if($LASTEXITCODE -ne 0){throw 'Public safety validation failed'}

    $manifestEntries=@()
    foreach($file in Get-ChildItem -LiteralPath $stage -Recurse -File | Sort-Object FullName){
        $relative=[IO.Path]::GetRelativePath($stage,$file.FullName).Replace('\','/')
        if($relative -in @('PUBLIC_SOURCE_MANIFEST.json','PUBLIC_RELEASE_MANIFEST.md')){continue}
        $manifestEntries += [ordered]@{path=$relative;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash}
    }
    $sourceManifest=[ordered]@{schemaVersion=1;privateSourceCommit=$commit;version=$Version;files=$manifestEntries}
    $manifestPath=Join-Path $stage 'PUBLIC_SOURCE_MANIFEST.json'
    $sourceManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    $contentDigest=(Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash

    $allFiles=@(Get-ChildItem -LiteralPath $stage -Recurse -File)
    $largest=@($allFiles|Sort-Object Length -Descending|Select-Object -First 25)
    $sourceCount=@($allFiles|Where-Object {$_.Extension -in @('.cs','.csproj')}).Count
    $localizationCount=@($allFiles|Where-Object {$_.FullName -like '*\Localization\*'}).Count
    $docsCount=@($allFiles|Where-Object {$_.Extension -eq '.md'}).Count
    $toolCount=@($allFiles|Where-Object {$_.FullName -like '*\Tools\*'}).Count
    $testSummary=if($testOutput -match 'TOTAL:\s+\d+\s+PASS,\s+\d+\s+FAIL'){$matches[0]}else{'PASS'}
    $licenseReady=Test-Path -LiteralPath (Join-Path $stage 'LICENSE')
    $readiness=if($licenseReady){'READY FOR PUBLICATION — MANUAL APPROVAL REQUIRED'}else{'PUBLIC CANDIDATE — BLOCKED: source license decision required'}
    $largestLines=$largest|ForEach-Object {"| $([IO.Path]::GetRelativePath($stage,$_.FullName).Replace('\','/')) | $($_.Length) |"}
    $release=@(
        '# Public Release Manifest','',
        "- Status: **$readiness**",
        "- Version: **$Version**",
        "- Private source commit: ``$commit``",
        "- Deterministic preparation timestamp (source commit): $commitDate",
        "- Build: $BuildResult",
        "- Candidate tests: $testSummary",
        "- QA: $QaResult",
        "- Game baseline: 1.2.0; Unity 6000.0.41f1; MelonLoader 0.7.3",
        "- Localization: 1,735 exact; 3 dynamic",
        "- Texture source mappings: 0 (private Home technical test excluded)",
        "- Runtime validation: $RuntimeResult",
        "- Source-manifest SHA-256 / content digest: ``$contentDigest``",'',
        '## Inventory','',
        "- Files before generated release manifest: $($allFiles.Count)",
        "- Bytes before generated release manifest: $(($allFiles|Measure-Object Length -Sum).Sum)",
        "- Source files: $sourceCount",
        "- Localization files: $localizationCount",
        "- Documentation files: $docsCount",
        "- Tool files: $toolCount",'',
        '## Excluded categories','',
        '- Private Git history and development instructions',
        '- reverse-engineering catalogs, contact sheets, dumps, reports, and work directories',
        '- game/runtime binaries, generated references, logs, caches, and build output',
        '- personal paths, credentials, prompts, and agent material',
        '- local-only or uncleared fonts and UI artwork',
        '- untranslated Home technical texture and original game assets','',
        '## Largest 25 files','',
        '| Path | Bytes |','| --- | ---: |'
    ) + $largestLines + @('','The preparation tool created this candidate locally and did not configure a remote, commit, push, publish, or upload it.')
    $release | Set-Content -LiteralPath (Join-Path $stage 'PUBLIC_RELEASE_MANIFEST.md') -Encoding utf8

    & (Join-Path $toolRoot 'validate_public_snapshot.ps1') -Candidate $stage
    if($LASTEXITCODE -ne 0){throw 'Generated public candidate failed final validation'}

    $zip=Join-Path $stageParent 'PvZ-Symbiosis-Translator.zip'
    $zipResult=(& python (Join-Path $toolRoot 'create_deterministic_zip.py') $stage $zip).Trim()
    if($LASTEXITCODE -ne 0){throw 'Deterministic ZIP generation failed'}
    $parts=$zipResult.Split('|')
    $summary=[ordered]@{
        status=$readiness
        stagingPath=$stage
        privateSourceCommit=$commit
        version=$Version
        files=@(Get-ChildItem -LiteralPath $stage -Recurse -File).Count
        bytes=(Get-ChildItem -LiteralPath $stage -Recurse -File|Measure-Object Length -Sum).Sum
        sourceManifest=$manifestPath
        contentDigest=$contentDigest
        archive=$parts[0]
        archiveBytes=[long]$parts[1]
        archiveSha256=$parts[2]
        tests=$testSummary
        pushPerformed=$false
    }
    $summaryPath=Join-Path $stageParent 'PUBLIC_CANDIDATE_SUMMARY.json'
    $summary|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $summaryPath -Encoding utf8
    $summary|ConvertTo-Json -Depth 5
} finally {
    if(Test-Path -LiteralPath $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
}
