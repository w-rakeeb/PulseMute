$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Split-Path -Parent $root
$source = Join-Path $root "Program.cs"
$icon = Join-Path $root "PulseMute.ico"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$target = Join-Path $out "PulseMute 1.6 Stable.exe"

if (!(Test-Path $compiler)) {
    throw "C# compiler not found at $compiler"
}

& $compiler `
    /nologo `
    /target:winexe `
    /optimize+ `
    /platform:anycpu `
    /win32icon:$icon `
    /out:$target `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    $source

Write-Host "Built $target"
