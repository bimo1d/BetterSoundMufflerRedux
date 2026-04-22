param(
    [string]$Ksp2Root = "C:\Games\Kerbal Space Program 2",
    [string]$CompilerPath = ""
)

$ErrorActionPreference = "Stop"

$modId = "BetterSoundMufflerRedux"
$repoRoot = $PSScriptRoot
$sourceRoot = Join-Path $repoRoot "PluginSource"
$buildRoot = Join-Path $repoRoot ".build"
$managedRoot = Join-Path $Ksp2Root "KSP2_x64_Data\Managed"
$deployRoot = Join-Path $Ksp2Root "mods\$modId"
$outputDll = Join-Path $buildRoot "$modId.dll"
$swinfo = Join-Path $repoRoot "swinfo.json"
$localizations = Join-Path $repoRoot "localizations"

function Require-Path([string]$Path, [string]$Name) {
    if (!(Test-Path -LiteralPath $Path)) {
        throw "$Name not found: $Path"
    }
}

function Add-Reference([System.Collections.Generic.List[string]]$List, [string]$Name, [bool]$Required = $true) {
    $path = Join-Path $managedRoot $Name
    if (Test-Path -LiteralPath $path) {
        $List.Add($path)
        return
    }

    if ($Required) {
        throw "Reference not found: $path"
    }
}

if ([string]::IsNullOrWhiteSpace($CompilerPath)) {
    $CompilerPath = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (!(Test-Path -LiteralPath $CompilerPath)) {
        $CompilerPath = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
    }
}

Require-Path $CompilerPath "C# compiler"
Require-Path $managedRoot "KSP2 managed folder"
Require-Path $swinfo "swinfo.json"
Require-Path $localizations "localizations folder"
Require-Path (Join-Path $localizations "better_sound_muffler.csv") "localization file"

$netStandardRoot = "C:\Program Files\dotnet\packs\NETStandard.Library.Ref\2.1.0\ref\netstandard2.1"
Require-Path $netStandardRoot "netstandard reference folder"

New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null
New-Item -ItemType Directory -Force -Path $deployRoot | Out-Null

$references = New-Object "System.Collections.Generic.List[string]"
Get-ChildItem -LiteralPath $netStandardRoot -Filter "*.dll" | Sort-Object Name | ForEach-Object {
    $references.Add($_.FullName)
}
Add-Reference $references "Assembly-CSharp.dll"
Add-Reference $references "Assembly-CSharp-firstpass.dll" $false
Add-Reference $references "ReduxLib.dll"
Add-Reference $references "Redux.UI.Component.dll" $false
Add-Reference $references "SpaceWarp2.dll"
Add-Reference $references "SpaceWarp2.UI.dll"
Add-Reference $references "UnityEngine.dll"
Add-Reference $references "UnityEngine.CoreModule.dll"
Add-Reference $references "UnityEngine.InputLegacyModule.dll"
Add-Reference $references "Unity.Mathematics.dll" $false
Add-Reference $references "UnityEngine.UI.dll"
Add-Reference $references "UnityEngine.UIElementsModule.dll"
Add-Reference $references "UnityEngine.UIModule.dll" $false
Add-Reference $references "UnityEngine.IMGUIModule.dll" $false
Add-Reference $references "Unity.TextMeshPro.dll"
Add-Reference $references "AK.Wwise.Unity.API.dll"
Add-Reference $references "AK.Wwise.Unity.API.WwiseTypes.dll" $false
Add-Reference $references "AK.Wwise.Unity.MonoBehaviour.dll" $false
Add-Reference $references "0Harmony.dll"

$sources = Get-ChildItem -LiteralPath $sourceRoot -Filter "*.cs" | Sort-Object Name | ForEach-Object { $_.FullName }
if ($sources.Count -eq 0) {
    throw "No source files found: $sourceRoot"
}

$args = New-Object "System.Collections.Generic.List[string]"
$args.Add("/nologo")
$args.Add("/noconfig")
$args.Add("/nostdlib+")
$args.Add("/target:library")
$args.Add("/optimize+")
$args.Add("/debug-")
$args.Add("/nowarn:1684,1685")
$args.Add("/out:$outputDll")

foreach ($reference in $references) {
    $args.Add("/reference:$reference")
}

foreach ($source in $sources) {
    $args.Add($source)
}

& $CompilerPath @args
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

try {
    Copy-Item -LiteralPath $outputDll -Destination (Join-Path $deployRoot "$modId.dll") -Force
    Copy-Item -LiteralPath $swinfo -Destination (Join-Path $deployRoot "swinfo.json") -Force
    Copy-Item -LiteralPath $localizations -Destination $deployRoot -Recurse -Force
}
catch {
    throw "Built successfully, but deploy failed. Close KSP2 if it is running and start build.ps1 again. $($_.Exception.Message)"
}

Write-Host "Built: $outputDll"
Write-Host "Deployed: $deployRoot"
