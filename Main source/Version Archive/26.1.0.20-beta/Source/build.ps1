$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Split-Path -Parent $root
$source = Join-Path $root "Program.cs"
$settingsSource = Join-Path $root "SidebarSettings.cs"
$assemblyInfo = Join-Path $root "AssemblyInfo.cs"
$icon = Join-Path $root "PulseMute.ico"
$professionalIcon = Join-Path $root "PulseMute-professional.ico"
$professionalLogo = Join-Path $root "PulseMute-professional.png"
$hidSharp = Join-Path $root "Dependencies\package\lib\net35\HidSharp.dll"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$target = Join-Path $out "PulseMute Beta.exe"

if (!(Test-Path $compiler)) {
    throw "C# compiler not found at $compiler"
}

if (!(Test-Path $hidSharp)) {
    throw "HidSharp dependency not found at $hidSharp"
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
    /reference:$hidSharp `
    "/resource:$hidSharp,HidSharp.dll" `
    "/resource:$professionalIcon,PulseMuteProfessional.ico" `
    "/resource:$professionalLogo,PulseMuteProfessional.png" `
    $source `
    $settingsSource `
    $assemblyInfo

if ($LASTEXITCODE -ne 0) {
    throw "PulseMute Beta build failed with exit code $LASTEXITCODE"
}

Write-Host "Built $target"
