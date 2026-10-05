$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
function Assert($condition, $message) { if (!$condition) { throw $message } }
foreach ($stem in 'Strings','GameStrings') {
    [xml]$base = Get-Content -LiteralPath (Join-Path $root "RemnantSaveGuardian/locales/$stem.resx")
    [xml]$ru = Get-Content -LiteralPath (Join-Path $root "RemnantSaveGuardian/locales/$stem.ru.resx")
    $map = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach ($node in $ru.root.data) {
        Assert (!$map.ContainsKey($node.name)) "Повторяющийся ключ: $stem/$($node.name)"
        $map.Add($node.name, [string]$node.value)
        Assert (![string]::IsNullOrWhiteSpace([string]$node.value)) "Пустой перевод: $stem/$($node.name)"
    }
    foreach ($node in $base.root.data) {
        Assert ($map.ContainsKey($node.name)) "Нет русского перевода: $stem/$($node.name)"
        $tokens = @([regex]::Matches([string]$node.value, '\{[\w:]+\}') | ForEach-Object Value | Sort-Object -Unique)
        $translatedTokens = @([regex]::Matches($map[$node.name], '\{[\w:]+\}') | ForEach-Object Value | Sort-Object -Unique)
        Assert (($tokens -join '|') -ceq ($translatedTokens -join '|')) "Изменены подстановки: $stem/$($node.name)"
    }
    Write-Output "$stem : $($base.root.data.Count) базовых ключей, $($map.Count) русских; подстановки сохранены."
}
$gamePath = Join-Path $root 'RemnantSaveGuardian/game.json'
$game = Get-Content -LiteralPath $gamePath -Raw | ConvertFrom-Json
$items = $game.events.psobject.Properties | ForEach-Object { $_.Value.psobject.Properties } | ForEach-Object { $_.Value }
$notes = @($items | Where-Object { $_.notes })
foreach ($item in $notes) { Assert ($item.notes -match '[А-Яа-яЁё]') "Непереведённая подсказка: $($item.name)" }
Write-Output "Игровые подсказки: $($notes.Count) записей на русском."
# The recorded upstream commit remains in this fork's Git history.
Push-Location $root
try { $originalText = git show '501b6ff:RemnantSaveGuardian/game.json' } finally { Pop-Location }
Assert ($LASTEXITCODE -eq 0) 'Не удалось прочитать исходную базу из истории Git.'
$original = ($originalText -join "`n") | ConvertFrom-Json
function Normalize-Game($value) {
    $value.version = 0
    $value.events.psobject.Properties | ForEach-Object { $_.Value.psobject.Properties } | ForEach-Object { $_.Value } | ForEach-Object { if ($_.notes) { $_.notes = '' } }
    return ConvertTo-Json -InputObject $value -Depth 100 -Compress
}
Assert ((Normalize-Game $original) -ceq (Normalize-Game $game)) 'Изменены технические идентификаторы или структура базы.'
[xml]$settings = Get-Content -LiteralPath (Join-Path $root 'RemnantSaveGuardian/Properties/Settings.settings')
Assert (($settings.SettingsFile.Settings.Setting | Where-Object Name -eq 'Language').Value.InnerText -eq 'ru') 'Русский язык не задан по умолчанию.'
Write-Output 'Структура базы и идентификаторы сохранений совпадают с оригиналом. Язык по умолчанию: русский.'
