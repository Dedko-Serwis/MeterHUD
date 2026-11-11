param([string]$Version = "0.2.0")
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $root "MeterHUD\MeterHUD.csproj"
$publish = Join-Path $root "dist\publish"
dotnet test (Join-Path $root "MeterHUD.slnx") -c Release
if ($LASTEXITCODE -ne 0) { throw "Testy nie powiodły się" }
dotnet publish $project -c Release -r win-x64 --self-contained true -p:Version=$Version -o $publish
if ($LASTEXITCODE -ne 0) { throw "Publikacja nie powiodła się" }
$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { throw "Brak Inno Setup 6: $iscc" }
& $iscc "/DMyAppVersion=$Version" "/DSourceDir=$publish" (Join-Path $root "installer\MeterHUD.iss")
if ($LASTEXITCODE -ne 0) { throw "Budowa instalatora nie powiodła się" }
Get-FileHash (Join-Path $root "dist\MeterHUD-$Version-Setup.exe") -Algorithm SHA256
