$ErrorActionPreference = "Stop"

$projectRoot = $env:CLAUDE_PROJECT_DIR
if (-not $projectRoot) {
    $projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$inputJson = [Console]::In.ReadToEnd()
if (-not $inputJson) {
    exit 0
}

$hookInput = $inputJson | ConvertFrom-Json
$filePath = $hookInput.tool_input.file_path
if (-not $filePath) {
    exit 0
}

if (-not [System.IO.Path]::IsPathRooted($filePath)) {
    $filePath = Join-Path $projectRoot $filePath
}

$backendPath = [System.IO.Path]::GetFullPath((Join-Path $projectRoot "backend")) + [System.IO.Path]::DirectorySeparatorChar
$fullFilePath = [System.IO.Path]::GetFullPath($filePath)
if (-not $fullFilePath.StartsWith($backendPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    exit 0
}

Push-Location $projectRoot
try {
    docker compose build backend
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
