[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $root
try {
    $gitRoot = & git rev-parse --show-toplevel
    if ($LASTEXITCODE -ne 0 -or [IO.Path]::GetFullPath($gitRoot) -ne [IO.Path]::GetFullPath($root)) {
        throw 'Git no apunta a la raíz de este proyecto.'
    }
    foreach ($path in @('Library/probe', 'Temp/probe', 'Logs/probe', 'UserSettings/probe', 'Docs/AgentRuns/probe')) {
        & git check-ignore -q -- $path
        if ($LASTEXITCODE -ne 0) { throw "Git no excluye $path" }
    }
    foreach ($path in @('Assets/01_Scripts/Player/Movement/FpsController.cs', 'Assets/01_Scripts/Player/Movement/FpsController.cs.meta', 'Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/ProjectVersion.txt', '.agents/skills/operation-heist-ui/SKILL.md', '.vscode/settings.json')) {
        if (-not (Test-Path -LiteralPath $path)) { throw "Falta $path" }
        & git check-ignore -q -- $path
        if ($LASTEXITCODE -eq 0) { throw "Recurso fuente excluido por error: $path" }
    }
    foreach ($path in @('Docs/Progress.md','Docs/Architecture.md','Docs/CodingGuidelines.md','.cursorignore','.ignore','.agents/skills/operation-heist-unity/SKILL.md','.agents/skills/operation-heist-ui/SKILL.md')) {
        if (-not (Test-Path -LiteralPath $path)) { throw "Falta contexto/configuración: $path" }
    }
    foreach ($path in @('.vscode/settings.json','.vscode/extensions.json','.vscode/launch.json')) {
        Get-Content -LiteralPath $path -Raw | ConvertFrom-Json | Out-Null
    }
    $extensions = @(& cursor --list-extensions)
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar Cursor.' }
    foreach ($extension in @('nromanov.dotrush','ms-dotnettools.vscode-dotnet-runtime')) {
        if ($extension -notin $extensions) { throw "Falta extensión $extension" }
    }
    $sdkList = @(& dotnet --list-sdks)
    if ($LASTEXITCODE -ne 0 -or -not ($sdkList -match '^10\.')) { throw 'DotRush instalado requiere SDK .NET 10 disponible.' }
    & ./Tools/Audit-Assets.ps1
    if ($LASTEXITCODE -ne 0) { throw 'Falló la auditoría de assets.' }
    Write-Output 'Entorno OK: Git, exclusiones, fuentes, contexto, skills, configuración JSON, extensiones C# y SDK.'
    Write-Output 'La validación de skills y el handshake LSP se ejecutan por separado; esto no ejecuta Unity ni comprueba el depurador adjunto.'
} finally { Pop-Location }
