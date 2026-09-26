param(
    [Parameter(Mandatory=$true)][string]$Candidate,
    [switch]$Json
)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $Candidate).Path
$issues=[System.Collections.Generic.List[object]]::new()
function Add-Issue([string]$Kind,[string]$Path,[string]$Detail){$issues.Add([pscustomobject]@{kind=$Kind;path=$Path;detail=$Detail})}
$files=@(Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName)
$textExtensions=@('.cs','.csproj','.json','.md','.ps1','.py','.toml','.txt','.yml','.yaml','.csv','.gitignore','.gitattributes')
$forbiddenSegments=@('.git','bin','obj','work','dist','Mods','Cache','Logs','.venv','__pycache__','TestResults','artifacts')
$forbiddenNames=@('Latest.log','GameAssembly.dll','global-metadata.dat','UnityPlayer.dll','PVZGS.exe','AGENTS.md')
$forbiddenExtensions=@('.dll','.exe','.pdb','.assets','.resS','.unity3d','.log')
foreach($file in $files){
    $relative=[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')
    $segments=$relative.Split('/')
    foreach($segment in $segments){if($forbiddenSegments -ccontains $segment){Add-Issue 'forbidden-path' $relative $segment;break}}
    if($forbiddenNames -ccontains $file.Name){Add-Issue 'forbidden-file' $relative $file.Name}
    if($forbiddenExtensions -ccontains $file.Extension){Add-Issue 'forbidden-extension' $relative $file.Extension}
    $isText=$textExtensions -contains $file.Extension -or $file.Name -in @('.gitignore','.gitattributes')
    if(!$isText){continue}
    # The validator necessarily contains scan patterns. Its source is still checked for paths/extensions above.
    if($relative -eq 'Tools/PublicRelease/validate_public_snapshot.ps1'){continue}
    $content=[IO.File]::ReadAllText($file.FullName)
    if($content -match '(?i)([A-Z]:\\Users\\|/Users/|/home/)'){Add-Issue 'personal-path' $relative $matches[0]}
    $attributionFiles=@('LICENSE','README.md','CREDITS.md','THIRD_PARTY.md')
    if($relative -notin $attributionFiles -and $content -match '(?i)Sheldon'){Add-Issue 'personal-name' $relative 'owner personal name'}
    if($content -match '(?i)(OPENAI_API_KEY|sk-[A-Za-z0-9_-]{16,}|(?:password|passwd|secret|api[_-]?key|access[_-]?token)\s*[:=]\s*["''][^"'']+["''])'){Add-Issue 'possible-secret' $relative $matches[0]}
}
$result=[pscustomobject]@{
    status=if($issues.Count -eq 0){'PASS'}else{'FAIL'}
    root=$root
    files=$files.Count
    bytes=($files|Measure-Object Length -Sum).Sum
    issues=@($issues)
}
if($Json){$result|ConvertTo-Json -Depth 6}else{
    $result|Format-List status,root,files,bytes
    if($issues.Count){$issues|Format-Table -AutoSize}
}
if($issues.Count){exit 1}
