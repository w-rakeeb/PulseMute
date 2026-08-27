$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root "dist"
$source = Join-Path $root "Program.cs"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$target = Join-Path $out "PulseMute.exe"

if (!(Test-Path $compiler)) {
    throw "C# compiler not found at $compiler"
}

New-Item -ItemType Directory -Force -Path $out | Out-Null

& $compiler `
    /nologo `
    /target:winexe `
    /optimize+ `
    /platform:anycpu `
    /out:$target `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    $source

Write-Host "Built $target"
