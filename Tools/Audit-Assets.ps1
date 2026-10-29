[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$assetsRoot = Join-Path $projectRoot 'Assets'
$manifest = @(Import-Csv -LiteralPath (Join-Path $projectRoot 'Docs/AssetManifest.csv'))
$problems = [Collections.Generic.List[string]]::new()
$registered = @{}
foreach ($entry in $manifest) {
    if ($registered.ContainsKey($entry.path)) { $problems.Add("Duplicado: $($entry.path)") }
    $registered[$entry.path] = $entry
    if ($entry.origin -notin @('original-project', 'engine-configuration')) {
        $problems.Add("Origen no permitido: $($entry.path)")
    }
    if (-not $entry.method -or -not $entry.provenance) { $problems.Add("Falta procedencia: $($entry.path)") }
}
$files = @(Get-ChildItem -LiteralPath $assetsRoot -File -Recurse | Where-Object { $_.Extension -ne '.meta' })
foreach ($file in $files) {
    $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
    if (-not $registered.ContainsKey($relative)) { $problems.Add("Recurso sin registrar: $relative") }
    else { $registered.Remove($relative) }
}
foreach ($missing in $registered.Keys) { $problems.Add("Registro sin archivo: $missing") }
if ($problems.Count -gt 0) { throw ($problems -join [Environment]::NewLine) }
Write-Output "Auditoría de inventario OK: $($files.Count) archivos importables registrados, sin .meta."
Write-Output 'Comprueba inventario y origen declarado; no demuestra autoría ni aprobación institucional.'
