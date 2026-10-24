$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$stage = Join-Path $root 'artifacts/package'
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$build = Join-Path $root 'HotbarStudio/bin/Release'
Copy-Item -LiteralPath (Join-Path $build 'HotbarStudio.dll'),(Join-Path $build 'HotbarStudio.json'),(Join-Path $root 'LICENSE'),(Join-Path $root 'NOTICE') -Destination $stage
New-Item -ItemType Directory -Path (Join-Path $stage 'images') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'HotbarStudio/images/icon.png') -Destination (Join-Path $stage 'images/icon.png')
$manifest = Get-Content -LiteralPath (Join-Path $stage 'HotbarStudio.json') -Raw | ConvertFrom-Json
if ($manifest.AssemblyVersion -ne '1.0.0.1' -or $manifest.DalamudApiLevel -ne 15 -or $manifest.Author -ne 'Roxyz0501' -or !$manifest.RepoUrl -or !$manifest.IconUrl) { throw 'Manifest verification failed' }
$zip = Join-Path $root 'artifacts/HotbarStudio-1.0.0.1.zip'
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
 $names = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\','/') } | Sort-Object)
 $allowed = @('HotbarStudio.dll','HotbarStudio.json','LICENSE','NOTICE','images/icon.png') | Sort-Object
 if (Compare-Object $names $allowed) { throw 'Unexpected package entries' }
} finally { $archive.Dispose() }
$metadata = [ordered]@{ version='1.0.0.1'; api=15; bytes=(Get-Item -LiteralPath $zip).Length; sha256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash; files=$names }
$metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root 'release-metadata.json') -Encoding utf8
$metadata | ConvertTo-Json -Depth 5
