# Removes ONLY .cs files sitting next to Buffbox.dll (wrong for VS 1.22+).
# Does not touch src\*.cs under the mod folder.
$modRoots = @(
    (Join-Path $env:APPDATA 'VintagestoryData\Mods\Buffbox'),
    (Join-Path $env:LOCALAPPDATA 'VintagestoryData\Mods\Buffbox')
)
foreach ($modRoot in $modRoots) {
    if (-not (Test-Path -LiteralPath $modRoot)) { continue }
    Get-ChildItem -LiteralPath $modRoot -File -Filter '*.cs' -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Removing $($_.FullName)"
        Remove-Item -LiteralPath $_.FullName -Force
    }
}
Write-Host 'Done. If nothing printed, no root .cs files were found in those paths.'
