# Transmission Tray Agent

A Windows system tray application for controlling a remote Transmission BitTorrent client.

## Features

- System tray icon showing download state (active/paused/disconnected)
- Left-click icon to toggle pause/resume all torrents
- Right-click for settings and exit menu
- Automatic Windows startup option
- Settings dialog for easy configuration
- No manual configuration files needed

## Building

### Prerequisites

- .NET 8 SDK

### Build Commands

**Development Build:**
```bash
dotnet build TransmissionTrayAgent.csproj
```

**Publish Single-File Executable:**
```bash
dotnet publish TransmissionTrayAgent.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin/Release/net8.0-windows/win-x64/publish/TransmissionTrayAgent.exe`

## ⚠️ Icon Files Required

**IMPORTANT:** The following icon files (.ico format) must be replaced in the `Resources/` directory before deploying to users:

1. **active.ico** - Green or blue icon indicating torrents are downloading
2. **paused.ico** - Yellow or orange icon indicating connected but paused
3. **disconnected.ico** - Red or gray icon indicating cannot reach server

Currently, placeholder empty files exist. Replace them with actual .ico files containing multiple resolutions (16x16, 32x32, 48x48 pixels).

**Creating icons:**
- Online: [favicon.io](https://favicon.io), [icon-converter.com](https://icon-converter.com), [icoconvert.com](https://icoconvert.com)
- Desktop: GIMP, Paint.NET, IcoFX, or Photoshop
- Start with PNG images and convert to multi-resolution .ico format

## Deployment

1. Copy the program folder (containing `TransmissionTrayAgent.exe`) to desired location
2. Run `TransmissionTrayAgent.exe`
3. Configure settings on first launch
4. Check "Start with Windows" if desired
5. Click "Save"

## Configuration

Settings are stored in: `%APPDATA%\TransmissionTrayAgent\settings.json`

The settings dialog allows configuration of:
- Transmission server host and port
- Username and password (if required)
- Polling interval (how often to check state)
- Auto-start with Windows

## Usage

- **Left-click icon**: Toggle pause/resume all torrents
- **Right-click icon**: Open context menu
  - **Settings**: Open settings dialog
  - **Exit**: Close application

## Technical Details

- Built with .NET 8 and Windows Forms
- Single-file self-contained executable (~10-15MB)
- No runtime dependencies required
- Settings stored in `%APPDATA%\TransmissionTrayAgent\`
- Windows startup managed via registry
