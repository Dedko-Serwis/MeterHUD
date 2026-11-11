param([string]$Version = "1.0.0")
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $root "MeterHUD\MeterHUD.csproj"
$publish = Join-Path $root "dist\publish"
dotnet test (Join-Path $root "MeterHUD.slnx") -c Release
if ($LASTEXITCODE -ne 0) { throw "Testy nie powiodły się" }
dotnet publish $project -c Release -r win-x64 --self-contained true "-p:Version=$Version" "-p:AssemblyVersion=$Version.0" "-p:FileVersion=$Version.0" "-p:InformationalVersion=$Version" -o $publish
if ($LASTEXITCODE -ne 0) { throw "Publikacja nie powiodła się" }
$isccCandidates = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe"
    "$env:ProgramFiles\Inno Setup 7\ISCC.exe"
    "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe"
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
)

foreach ($regPath in @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1',
    'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1',
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
    'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1'
)) {
    $install = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue

    if ($install -and $install.InstallLocation) {
        $isccCandidates += Join-Path $install.InstallLocation 'ISCC.exe'
    }
}

$iscc = $isccCandidates |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1

if (-not $iscc) {
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}

if (-not $iscc) {
    throw "Nie znaleziono Inno Setup 6 ani 7 (ISCC.exe)."
}

Write-Host "Inno Setup: $iscc"
& $iscc "/DMyAppVersion=$Version" "/DSourceDir=$publish" (Join-Path $root "installer\MeterHUD.iss")
if ($LASTEXITCODE -ne 0) { throw "Budowa instalatora nie powiodła się" }
Get-FileHash (Join-Path $root "dist\MeterHUD-$Version-Setup.exe") -Algorithm SHA256
