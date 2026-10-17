<#
.SYNOPSIS
  Runs WICKFALL's Unity project headless and prints a short summary.

.EXAMPLES
  powershell -File Tools/unity.ps1 -Method Wickfall.EditorTools.WickfallSetup.Run
  powershell -File Tools/unity.ps1 -Tests EditMode
  powershell -File Tools/unity.ps1 -Tests PlayMode -Filter "Wickfall.Tests.PlayerTests"

.NOTES
  - The Unity editor must be closed (batch mode locks the project).
  - Only one Unity process at a time.
  - Exit code 1073807364 means the run was killed (not a compile error): run it again.
#>
param(
    [string]$Method = "",
    [ValidateSet("", "EditMode", "PlayMode")] [string]$Tests = "",
    [string]$Filter = "",
    [switch]$NoGraphics
)

$ErrorActionPreference = "Stop"
$unity = "C:\Program Files\Unity\Hub\Editor\6000.2.7f2\Editor\Unity.exe"
$project = Split-Path -Parent $PSScriptRoot
$logs = Join-Path $project "Logs"
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"

$args = @("-batchmode", "-projectPath", $project)
if ($NoGraphics) { $args += "-nographics" }

if ($Tests -ne "") {
    $results = Join-Path $logs "TestResults_$($Tests)_$stamp.xml"
    $log = Join-Path $logs "unity_tests_$($Tests)_$stamp.log"
    $args += @("-runTests", "-testPlatform", $Tests, "-testResults", $results, "-logFile", $log)
    if ($Filter -ne "") { $args += @("-testFilter", $Filter) }
} else {
    $log = Join-Path $logs "unity_$stamp.log"
    $args += @("-quit", "-logFile", $log)
    if ($Method -ne "") { $args += @("-executeMethod", $Method) }
}

$sw = [Diagnostics.Stopwatch]::StartNew()
# Wait for Unity itself only: -Wait would also wait for helper processes Unity leaves running
# (the licensing client), which never exit.
$p = Start-Process -FilePath $unity -ArgumentList $args -PassThru -WindowStyle Hidden
$p.WaitForExit()
$sw.Stop()
Write-Output ("Unity exit code: {0}  ({1:n0} s)  log: {2}" -f $p.ExitCode, $sw.Elapsed.TotalSeconds, $log)

if (Test-Path $log) {
    $errors = Select-String -Path $log -Pattern "error CS\d+|Scripts have compiler errors|Exception:|\[WICKFALL[^\]]*\]" |
        ForEach-Object { $_.Line.Trim() } | Select-Object -Unique -First 60
    if ($errors) { $errors | ForEach-Object { Write-Output $_ } }
}

if ($Tests -ne "" -and (Test-Path $results)) {
    [xml]$xml = Get-Content $results
    $run = $xml.'test-run'
    Write-Output ("Tests: {0} total, {1} passed, {2} failed, {3} skipped" -f $run.total, $run.passed, $run.failed, $run.skipped)
    $xml.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
        Write-Output ("FAILED {0}" -f $_.fullname)
        $msg = $_.failure.message.'#cdata-section'
        if ($msg) { Write-Output ("   " + ($msg -split "`n" | Select-Object -First 4) -join "`n   ") }
    }
} elseif ($Tests -ne "") {
    Write-Output "No test results file was written (compile error or killed run?)."
}
exit $p.ExitCode
