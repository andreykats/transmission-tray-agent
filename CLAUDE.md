# Claude Code Development Guide

This document provides instructions for building and releasing the Transmission Tray Agent project using Claude Code or manual commands.

## Project Overview

- **Type:** .NET 8 Windows Forms application
- **Target:** Windows x64 only
- **Output:** Self-contained single-file executable (~154MB)
- **Repository:** https://github.com/andreykats/transmission-agent-win

## Build Instructions

### Prerequisites

- .NET 8 SDK installed
- Windows or cross-platform build environment

### Development Build

For quick testing and development:

```bash
dotnet build TransmissionTrayAgent.csproj
```

Output: `bin/Debug/net8.0-windows/win-x64/TransmissionTrayAgent.exe` (requires .NET runtime)

### Release Build (Self-Contained)

For distribution to end users:

```bash
dotnet publish TransmissionTrayAgent.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin/Release/net8.0-windows/win-x64/publish/TransmissionTrayAgent.exe` (~154MB)

### VS Code Tasks

The project includes pre-configured VS Code tasks in `.vscode/tasks.json`:

- **Build** - Development build
- **Publish** - Release build with single-file output
- **Watch** - Auto-rebuild on file changes

Run via: `Cmd+Shift+B` (Mac) or `Ctrl+Shift+B` (Windows/Linux)

## Release Workflow

The project uses GitHub Actions for automated releases. Releases are triggered **manually** via workflow dispatch.

### Creating a Release

#### Via GitHub Web Interface:

1. Navigate to https://github.com/andreykats/transmission-agent-win/actions
2. Click "Release" workflow in the left sidebar
3. Click "Run workflow" button (top right)
4. Fill in the inputs:
   - **Version:** e.g., `v1.0.0` (must follow format `vX.Y.Z` or `vX.Y.Z-suffix`)
   - **Pre-release:** Check if this is a beta/RC release
5. Click "Run workflow"
6. Wait ~3-5 minutes for completion
7. Check the Releases page: https://github.com/andreykats/transmission-agent-win/releases

#### Via GitHub CLI:

```bash
# Standard release
gh workflow run release.yml -f version=v1.0.0

# Pre-release
gh workflow run release.yml -f version=v1.0.0-rc1 -f prerelease=true
```

#### Monitor Progress:

```bash
# List workflow runs
gh run list --workflow=release.yml

# Watch specific run (get ID from list command)
gh run watch <run-id>

# View logs if failed
gh run view <run-id> --log
```

### What Happens During Release

The workflow automatically:

1. ✅ **Validates** version format (must be `vX.Y.Z` or `vX.Y.Z-suffix`)
2. 🔧 **Sets up** .NET 8 SDK on Windows runner
3. 📦 **Restores** NuGet dependencies
4. 🏗️ **Builds** self-contained single-file executable
5. 📝 **Renames** executable to `TransmissionTrayAgent-vX.Y.Z-win-x64.exe`
6. 🔐 **Generates** SHA256 checksum file (`SHA256SUMS.txt`)
7. 🚀 **Creates** GitHub release with tag
8. 📎 **Uploads** executable and checksum as release assets

### Release Assets

Each release includes:

- **TransmissionTrayAgent-vX.Y.Z-win-x64.exe** - Self-contained executable (~154MB)
- **SHA256SUMS.txt** - Checksum for security verification
- **Source code** (zip/tar.gz) - Automatically included by GitHub

### Version Numbering

Follow semantic versioning with `v` prefix:

- **Patch releases** (bug fixes): `v1.0.1`, `v1.0.2`
- **Minor releases** (new features): `v1.1.0`, `v1.2.0`
- **Major releases** (breaking changes): `v2.0.0`
- **Pre-releases** (testing): `v1.0.0-rc1`, `v1.0.0-beta1`

## Development Workflow

### Making Changes

1. Create a feature branch:
   ```bash
   git checkout -b feature/my-feature
   ```

2. Make your changes and test locally:
   ```bash
   dotnet build
   dotnet run
   ```

3. Commit with descriptive message:
   ```bash
   git add .
   git commit -m "Add feature: description"
   ```

4. Push and create pull request:
   ```bash
   git push origin feature/my-feature
   gh pr create
   ```

### Updating Version Numbers

Version metadata is stored in [TransmissionTrayAgent.csproj](TransmissionTrayAgent.csproj:11-14):

```xml
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
<InformationalVersion>1.0.0</InformationalVersion>
```

**Note:** The release workflow overrides these with the input version, so they serve as defaults.

To update manually:
1. Edit `TransmissionTrayAgent.csproj`
2. Update all four version properties
3. Commit: `git commit -am "Bump version to X.Y.Z"`

## Troubleshooting

### Release Workflow Fails

**Version format error:**
- Ensure version follows `vX.Y.Z` format (e.g., `v1.0.0`)
- Pre-release suffixes allowed: `v1.0.0-rc1`, `v1.0.0-beta1`

**Release already exists:**
- Delete the release and tag first:
  ```bash
  gh release delete v1.0.0 --yes
  git tag -d v1.0.0
  git push origin :refs/tags/v1.0.0
  ```

**Build errors:**
- Check Actions logs: https://github.com/andreykats/transmission-agent-win/actions
- Ensure code compiles locally: `dotnet build`

### Local Build Issues

**Missing .NET SDK:**
```bash
# Check installed versions
dotnet --list-sdks

# Install .NET 8 SDK from:
# https://dotnet.microsoft.com/download/dotnet/8.0
```

**Missing dependencies:**
```bash
dotnet restore TransmissionTrayAgent.csproj
```

**Icon files missing:**
- Ensure `Resources/*.ico` files exist
- See [README.md](README.md:36-49) for icon requirements

### Git Issues

**Large files in history:**
- Build artifacts should be gitignored
- See [.gitignore](.gitignore)
- If needed, clean history:
  ```bash
  FILTER_BRANCH_SQUELCH_WARNING=1 git filter-branch --force \
    --index-filter 'git rm -r --cached --ignore-unmatch bin obj' \
    --prune-empty --tag-name-filter cat -- --all
  git push --force origin main
  ```

## File Structure

```
transmission-agent-win/
├── .github/
│   └── workflows/
│       └── release.yml          # GitHub Actions release workflow
├── .vscode/
│   └── tasks.json               # VS Code build tasks
├── Resources/
│   ├── active.ico               # Green tray icon
│   ├── paused.ico               # Yellow tray icon
│   └── disconnected.ico         # Red/gray tray icon
├── Models/                       # Data models
├── Services/                     # Business logic
├── Program.cs                    # Entry point
├── TrayApplicationContext.cs     # Main application logic
├── SettingsForm.cs              # Settings UI
├── TransmissionClient.cs        # API client
├── TransmissionTrayAgent.csproj # Project configuration
├── .gitignore                   # Git ignore rules
├── README.md                    # User documentation
└── CLAUDE.md                    # This file
```

## CI/CD Configuration

The release workflow is defined in [.github/workflows/release.yml](.github/workflows/release.yml).

### Workflow Triggers

- **Manual only** (`workflow_dispatch`)
- No automatic builds on push/PR (by design)

### Workflow Permissions

- **contents: write** - Required for creating releases and uploading assets

### Workflow Secrets

- **GITHUB_TOKEN** - Automatically provided by GitHub Actions

## References

- **Repository:** https://github.com/andreykats/transmission-agent-win
- **Actions:** https://github.com/andreykats/transmission-agent-win/actions
- **Releases:** https://github.com/andreykats/transmission-agent-win/releases
- **.NET 8 Docs:** https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8
- **GitHub CLI:** https://cli.github.com/manual/
