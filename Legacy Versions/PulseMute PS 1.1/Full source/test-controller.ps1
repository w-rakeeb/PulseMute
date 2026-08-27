$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Join-Path $root "Program.cs"
$tests = Join-Path $root "ControllerProtocolTests.cs"
$hidSharp = Join-Path $root "Dependencies\package\lib\net35\HidSharp.dll"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$testRoot = Join-Path $env:TEMP "PulseMute-PS-Controller-Tests"
$target = Join-Path $testRoot "ControllerProtocolTests.exe"

New-Item -ItemType Directory -Force -Path $testRoot | Out-Null
Copy-Item -LiteralPath $hidSharp -Destination (Join-Path $testRoot "HidSharp.dll") -Force

& $compiler `
    /nologo `
    /target:exe `
    /optimize+ `
    /platform:anycpu `
    /main:PulseMute.ControllerProtocolTests `
    /out:$target `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:$hidSharp `
    $source `
    $tests

if ($LASTEXITCODE -ne 0) {
    throw "Controller protocol test build failed with exit code $LASTEXITCODE"
}

& $target
if ($LASTEXITCODE -ne 0) {
    throw "Controller protocol tests failed with exit code $LASTEXITCODE"
}
