param([string]$DotNetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
[xml]$project = Get-Content -LiteralPath (Join-Path $root 'RemnantSaveGuardian/RemnantSaveGuardian.csproj')
$version = [string]$project.Project.PropertyGroup.Version
$out = Join-Path $root "artifacts/publish-$version"
New-Item -ItemType Directory -Path $out -Force | Out-Null
& $DotNetPath publish (Join-Path $root 'RemnantSaveGuardian/RemnantSaveGuardian.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:ContinuousIntegrationBuild=true -o $out -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'Сборка релиза завершилась с ошибкой.' }
foreach ($name in 'LICENSE','README.md','CHANGELOG.md') { Copy-Item -LiteralPath (Join-Path $root $name) -Destination $out -Force }
$archive = Join-Path $root "artifacts/RemnantSaveGuardianRU-$version-win-x64.zip"
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath (Join-Path $root 'artifacts/SHA256SUMS.txt') -Encoding utf8
Write-Output "Готово: $archive"
