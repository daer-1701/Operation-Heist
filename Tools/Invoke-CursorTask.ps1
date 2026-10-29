[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-zA-Z0-9_-]+$')]
    [string]$TaskId
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$agentCommand = Join-Path $env:LOCALAPPDATA 'cursor-agent/agent.ps1'
$taskPath = Join-Path $projectRoot "Docs/Tasks/$TaskId.json"
$runsDirectory = Join-Path $projectRoot 'Docs/AgentRuns'
if (-not (Test-Path -LiteralPath $agentCommand)) { throw 'Cursor Agent CLI no está instalado.' }
if (-not (Test-Path -LiteralPath $taskPath)) { throw "Tarea inexistente: $TaskId" }
$task = Get-Content -LiteralPath $taskPath -Raw | ConvertFrom-Json
if ($task.status -ne 'pending') { throw "La tarea no está pendiente: $($task.status)" }
if ($task.owner -ne 'cursor') { throw 'La tarea no está asignada a Cursor.' }
if (-not $task.prompt -or -not $task.allowedPaths -or $task.allowedPaths.Count -eq 0) {
    throw 'Faltan instrucciones o rutas permitidas.'
}
New-Item -ItemType Directory -Path $runsDirectory -Force | Out-Null
$lockPath = Join-Path $runsDirectory 'cursor.lock'
$lockStream = $null
$previousLocation = Get-Location

function Save-TaskState {
    $temporaryTaskPath = "$taskPath.tmp"
    $task | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $temporaryTaskPath -Encoding utf8
    Move-Item -LiteralPath $temporaryTaskPath -Destination $taskPath -Force
}

try {
    $lockStream = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    Set-Location -LiteralPath $projectRoot
    $task.status = 'running'
    $task | Add-Member -NotePropertyName startedAt -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
    Save-TaskState
    $runName = "$TaskId-$([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))"
    $outputPath = Join-Path $runsDirectory "$runName.json"
    $errorPath = Join-Path $runsDirectory "$runName.stderr.txt"
    $scope = $task.allowedPaths -join ', '
    $prompt = @"
Read AGENTS.md and Docs/Coordination.md first. You are Cursor, working under Codex coordination.
Task ID: $TaskId
Allowed paths for modifications: $scope
Do not modify any other paths. Do not update the task record or coordinator files.
Do not invoke other agents. Use the installed dependencies and preserve unrelated work.
Task: $($task.prompt)
Report changed files, checks actually run, unresolved issues, and acceptance results. Respond in Spanish.
"@
    $agentArguments = @('--workspace', $projectRoot, '--trust', '--print', '--output-format', 'json')
    if ($task.mode -eq 'read-only') {
        $agentArguments += @('--mode', 'ask')
    } elseif ($task.mode -eq 'edit') {
        $agentArguments += '--force'
    } else {
        throw "Modo de tarea desconocido: $($task.mode)"
    }
    $agentArguments += $prompt
    & $agentCommand @agentArguments 1> $outputPath 2> $errorPath
    $runExitCode = $LASTEXITCODE
    $task | Add-Member -NotePropertyName lastOutput -NotePropertyValue "Docs/AgentRuns/$runName.json" -Force
    $task | Add-Member -NotePropertyName lastError -NotePropertyValue "Docs/AgentRuns/$runName.stderr.txt" -Force
    $task | Add-Member -NotePropertyName finishedAt -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
    if ($runExitCode -ne 0) { throw "Cursor terminó con código $runExitCode. Revisar $errorPath y $outputPath" }
    $result = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json
    if ($result.is_error -eq $true) { throw 'Cursor devolvió un resultado de error.' }
    if ($task.PSObject.Properties.Name -contains 'expectedMarker') {
        if (-not ([string]$result.result).Contains([string]$task.expectedMarker)) {
            throw 'La respuesta no incluye el marcador de aceptación esperado.'
        }
    }
    $task.status = 'awaiting_review'
    Save-TaskState
    Write-Output "Cursor terminó. Pendiente de revisión de Codex: $outputPath"
} catch {
    if ($lockStream) {
        $task.status = 'failed'
        $task | Add-Member -NotePropertyName failure -NotePropertyValue $_.Exception.Message -Force
        Save-TaskState
    }
    throw
} finally {
    Set-Location -LiteralPath $previousLocation.Path
    if ($lockStream) { $lockStream.Dispose() }
}
