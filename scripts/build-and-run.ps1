param(
    [string]$UnityPath,
    [string]$ProjectPath = "$PSScriptRoot\..\unity_project",
    [switch]$NoRun
)

function Resolve-UnityEditorPath {
    param([string]$Override)

    function Resolve-FromPathOrFolder {
        param([string]$PathOrFolder)
        if (-not $PathOrFolder) { return $null }
        if (-not (Test-Path $PathOrFolder)) { return $null }
        $resolved = (Resolve-Path $PathOrFolder).Path
        # If a file, assume it's the Unity.exe
        if (Test-Path $resolved -PathType Leaf) { return $resolved }
        # If a directory, try typical layout: <...>\Editor\Unity.exe
        $direct = Join-Path $resolved 'Editor\Unity.exe'
        if (Test-Path $direct) { return $direct }
        # If this looks like a Hub Editor root (contains version folders), pick newest
        try {
            $versions = Get-ChildItem $resolved -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending
            foreach ($v in $versions) {
                $candidate = Join-Path $v.FullName 'Editor\Unity.exe'
                if (Test-Path $candidate) { return $candidate }
            }
        } catch { }
        # Fallback: recursive search (shallow)
        try {
            $cand = Get-ChildItem -Path $resolved -Recurse -Filter 'Unity.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($cand) { return $cand.FullName }
        } catch { }
        return $null
    }

    # 1) Respect explicit override (file or folder)
    $found = Resolve-FromPathOrFolder -PathOrFolder $Override
    if ($found) { return $found }

    # 2) Env var UNITY_EDITOR_PATH (file or folder)
    $found = Resolve-FromPathOrFolder -PathOrFolder $env:UNITY_EDITOR_PATH
    if ($found) { return $found }

    # 3) Known Unity Hub locations (C: and D:)
    $hubRoots = @(
        'C:\\Program Files\\Unity\\Hub\\Editor',
        'D:\\Unity\\Unity Hub\\Editor'
    )
    foreach ($hubRoot in $hubRoots) {
        $found = Resolve-FromPathOrFolder -PathOrFolder $hubRoot
        if ($found) { return $found }
    }

    # 4) Legacy standalone install
    $standalone = 'C:\\Program Files\\Unity\\Editor\\Unity.exe'
    if (Test-Path $standalone) { return $standalone }

    throw "Unity editor not found. Provide -UnityPath to Unity.exe or its parent folder (e.g., ...\\<version>), or set UNITY_EDITOR_PATH."
}

function Invoke-UnityBuild {
    param([string]$UnityExe, [string]$ProjPath, [string]$LogPath)
    Write-Host "[Build] Using Unity: $UnityExe" -ForegroundColor Cyan
    Write-Host "[Build] Project: $ProjPath" -ForegroundColor Cyan
    & $UnityExe `
        -batchmode -nographics -quit `
        -projectPath $ProjPath `
        -executeMethod FrontierAges.EditorTools.Build.StandaloneBuilder.BuildWindows64 `
        -logFile $LogPath
    return $LASTEXITCODE
}

try {
    $unityExe = Resolve-UnityEditorPath -Override $UnityPath
    $projFull = (Resolve-Path $ProjectPath).Path
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    $buildDir = Join-Path $repoRoot "build"
    if (-not (Test-Path $buildDir)) { New-Item -ItemType Directory -Path $buildDir | Out-Null }
    $logPath = Join-Path $buildDir "cli-build.log"

    $code = Invoke-UnityBuild -UnityExe $unityExe -ProjPath $projFull -LogPath $logPath
    Write-Host "[Build] Unity exited with code $code" -ForegroundColor Yellow

    $latestDir = Join-Path $repoRoot "bin\windows-x64-latest"
    $exePath = Join-Path $latestDir "FrontierAges.exe"
    if (-not (Test-Path $exePath)) {
        Write-Host "[Build] Could not find built exe at $exePath" -ForegroundColor Red
        Write-Host "[Build] Inspect build log: $logPath" -ForegroundColor Red
        exit 1
    }

    Write-Host "[Build] Success → $exePath" -ForegroundColor Green
    if (-not $NoRun) {
        Write-Host "[Run] Launching game..." -ForegroundColor Green
        Start-Process -FilePath $exePath -WorkingDirectory $latestDir | Out-Null
    }
}
catch {
    Write-Error $_
    exit 1
}
