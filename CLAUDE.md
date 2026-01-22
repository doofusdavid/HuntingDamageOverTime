# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Vintage Story mod** written in C# targeting .NET 8.0. Vintage Story is a voxel survival game, and this mod extends its functionality through the VintagestoryAPI.

## Architecture

### Project Structure
- **HuntingDamageOverTime/HuntingDamageOverTime/** - Main mod project containing:
  - `HuntingDamageOverTimeModSystem.cs` - Main mod entry point inheriting from `ModSystem`
  - `modinfo.json` - Mod metadata (version, dependencies, mod ID)
  - `assets/` - Game assets including localization files
- **HuntingDamageOverTime/ZZCakeBuild/** - Cake build automation project using Cake.Frosting

### Key Architecture Points
- **ModSystem Pattern**: The main class inherits from `Vintagestory.API.Common.ModSystem` which provides lifecycle hooks
- **Side Awareness**: Vintage Story has server-side and client-side execution contexts:
  - `Start(ICoreAPI)` - Called on both sides
  - `StartServerSide(ICoreServerAPI)` - Server-only initialization
  - `StartClientSide(ICoreClientAPI)` - Client-only initialization
- **DLL References**: The mod references external Vintage Story DLLs via the `VINTAGE_STORY` environment variable pointing to the game installation

## Build System

### Environment Setup
Set the `VINTAGE_STORY` environment variable to your Vintage Story installation directory (containing VintagestoryAPI.dll).

### Build Commands

**Using Cake build automation (recommended):**
```bash
# From ZZCakeBuild directory
dotnet run --configuration Release  # Full build, validate, package
dotnet run --configuration Debug    # Debug build
dotnet run -- --skip-json-validation  # Skip asset JSON validation
```

**Using dotnet directly:**
```bash
# From HuntingDamageOverTime/HuntingDamageOverTime directory
dotnet build HuntingDamageOverTime.csproj -c Release
dotnet build HuntingDamageOverTime.csproj -c Debug
dotnet clean HuntingDamageOverTime.csproj
dotnet publish HuntingDamageOverTime.csproj -c Release
```

### Build Process
The Cake build system performs these tasks in order:
1. **ValidateJson** - Validates all JSON files in `assets/` directory
2. **Build** - Cleans and publishes the mod
3. **Package** - Creates distributable mod package:
   - Copies published DLL from `bin/{Configuration}/Mods/mod/publish/`
   - Copies `assets/`, `modinfo.json`, and `modicon.png` (if exists)
   - Creates ZIP file in `Releases/` directory named `{modid}_{version}.zip`

### Output Locations
- Build output: `HuntingDamageOverTime/bin/{Configuration}/Mods/mod/`
- Packaged release: `Releases/{modid}_{version}.zip`

## Mod Metadata

Mod configuration is in `modinfo.json`:
- `modid` - Unique identifier (used for asset paths and namespaces)
- `version` - Semantic version (used in package filename)
- `dependencies.game` - Minimum required Vintage Story version

## Asset Structure

Assets follow Vintage Story's domain-based path structure:
- Path format: `assets/{modid}/{type}/...`
- Localization: `assets/{modid}/lang/{languagecode}.json`

## API Documentation

**Primary Reference**: https://apidocs.vintagestory.at/

The Vintage Story API is organized into three main namespaces:
- **Vintagestory.API.Client** (`ICoreClientAPI`) - Client-side functionality (rendering, UI, input)
- **Vintagestory.API.Common** (`ICoreAPI`) - Shared utilities and systems (blocks, items, entities, world access)
- **Vintagestory.API.Server** (`ICoreServerAPI`) - Server-side logic (world generation, game rules, persistence)

Note: The API docs serve as a reference only, not a tutorial. For learning, consult the Official Vintage Story Wiki.
