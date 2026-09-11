param()
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetsPath = Join-Path $projectRoot 'Assets'
# Fail closed if private tools are accidentally copied back into the student project.
$privateNames = @('AuthManager', 'Evaluator', 'RubricConfig', 'OllamaClient', 'TeacherAssessmentWindow')
foreach ($file in Get-ChildItem -LiteralPath $assetsPath -Recurse -File) {
    if ($privateNames -contains $file.BaseName) { throw "Private file in student project: $($file.FullName)" }
    if ($file.Extension -in @('.cs', '.unity', '.prefab', '.asset', '.uxml')) {
        $contents = [IO.File]::ReadAllText($file.FullName)
        foreach ($privateName in $privateNames) {
            if ($contents -match ('\b' + $privateName + '\b')) { throw "Private reference in $($file.FullName): $privateName" }
        }
    }
}
$uniqueName = 'Visor3D-Alumnat-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$stageRoot = Join-Path $projectRoot ('Temp/' + $uniqueName)
$packageRoot = Join-Path $stageRoot 'Visor3D-Alumnat'
$deliveries = Join-Path $projectRoot 'Deliveries'
New-Item -ItemType Directory -Path $packageRoot,$deliveries -Force | Out-Null
try {
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings', 'Docs', 'Tools')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination $packageRoot -Recurse
    }
    foreach ($file in @('README.md', 'GUIA_D_US.md', '.gitignore')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $packageRoot
    }
    $zipPath = Join-Path $deliveries ($uniqueName + '.zip')
    Compress-Archive -LiteralPath $packageRoot -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Output $zipPath
}
finally {
    $resolvedStage = [IO.Path]::GetFullPath($stageRoot)
    $allowedTemp = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Temp')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedStage.StartsWith($allowedTemp, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe staging path' }
    if (Test-Path -LiteralPath $resolvedStage) { Remove-Item -LiteralPath $resolvedStage -Recurse -Force }
}
