# MeterHUD

**MeterHUD 1.0.0** is a free, open-source Windows application for monitoring measurements from the **UNI-T UT161E** USB HID multimeter. It includes an OBS-friendly measurement overlay, measurement recording to CSV, configurable colors, and Polish/English application translations.

## Requirements

- Windows x64
- UNI-T UT161E connected over USB HID for live measurements
- OBS Studio is optional; use its chroma-key filter with the overlay when recording or streaming

The Windows installer is self-contained (no separate .NET runtime installation required). The installer chooses Polish or English based on Windows language settings, with English as fallback.

## Install

Download `MeterHUD-1.0.0-Setup.exe` from [GitHub Releases](https://github.com/Dedko-Serwis/MeterHUD/releases) when the release is published. Run the installer, then launch MeterHUD. Uninstall using Windows **Installed apps**. To install a newer version, run the newer installer.

## Build from source

Requires the .NET 10 SDK and Windows for the WPF application.

```powershell
dotnet restore MeterHUD.slnx
dotnet build MeterHUD.slnx -c Release
dotnet test MeterHUD.slnx -c Release
```

To build a self-contained x64 installer, install Inno Setup 6 or 7 and run:

```powershell
.\scripts\build-release.ps1 -Version 1.0.0
```

The installer is generated in `dist/`, which is not committed to Git.

## Settings and compatibility

For compatibility with earlier MultimeterOverlay builds, existing user settings and the default CSV directory remain under `%LOCALAPPDATA%\MultimeterOverlay\`. The application itself is named MeterHUD.

## Updates

MeterHUD can check for newer public GitHub Releases. Downloaded installers are checked against a SHA-256 digest supplied by GitHub before launch. A release without a suitable digest cannot be installed through the built-in updater.

## License

Copyright © 2026 DEDKO Serwis.

MeterHUD is licensed under the **GNU General Public License v3.0 only (GPL-3.0-only)**, including commercial use. When redistributing the program or modified versions, comply with the GPL, including applicable copyright notices and corresponding-source requirements. Private modifications need not be published. See [LICENSE](LICENSE).

Third-party component notices: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

This project is independent of UNI-T and OBS Studio and is not endorsed by their manufacturers.
