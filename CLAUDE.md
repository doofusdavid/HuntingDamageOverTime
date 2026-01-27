# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Vintage Story mod** written in C# targeting .NET 8.0. The mod adds bleeding damage over time when animals are hit by projectiles (arrows/spears). It runs server-side only.

## Build Commands

**Environment Setup**: Set the `VINTAGE_STORY` environment variable to your Vintage Story installation directory.

**Build and Package (recommended):**
```bash
cd HuntingDamageOverTime/ZZCakeBuild
dotnet run                              # Release build + package
dotnet run --configuration Debug        # Debug build + package
dotnet run -- --skip-json-validation    # Skip JSON validation
```

**Direct build:**
```bash
cd HuntingDamageOverTime/HuntingDamageOverTime
dotnet build -c Release
dotnet publish -c Release
```

**Output locations:**
- Build: `HuntingDamageOverTime/bin/{Configuration}/Mods/mod/`
- Package: `Releases/{modid}_{version}.zip`

## Running/Debugging

Launch profiles are configured in `Properties/launchSettings.json`:
- **Client**: Runs game client with mod loaded via `--addModPath`
- **Server**: Runs dedicated server with mod loaded

Both require the `VINTAGE_STORY` environment variable.

## Architecture

### Core Classes (HuntingDamageOverTimeModSystem.cs)

**HuntingDamageOverTimeModSystem** (ModSystem)
- Registers `EntityBehaviorBleedingDamage` behavior class
- Hooks `OnEntitySpawn` to add bleeding behavior to wildlife
- Also applies to already-spawned entities in `StartServerSide`
- Filters entities: includes `EntityAgent`, excludes players and hostile mobs (drifter, locust, zombie, specter, nightmare, temp)

**EntityBehaviorBleedingDamage** (EntityBehavior)
- Detects projectile hits in `OnEntityReceiveDamage` (checks for "arrow" or "spear" in source entity code)
- Calculates bleed damage as percentage of initial hit damage spread over duration
- Applies damage every second in `OnGameTick`
- Clears state on entity death

**Configurable constants:**
```csharp
BLEEDING_DAMAGE_PERCENT = 50f   // % of weapon damage dealt as total bleed
BLEEDING_DURATION_SECONDS = 15f // Duration of bleed effect
```

### Vintage Story API Patterns

- **Side awareness**: Check `entity.World.Side == EnumAppSide.Server` before server-only logic
- **ModSystem lifecycle**: `Start()` for both sides, `StartServerSide()`/`StartClientSide()` for side-specific
- **Entity behaviors**: Register with `api.RegisterEntityBehaviorClass()`, add dynamically with `entity.AddBehavior()`

## Mod Metadata

`modinfo.json` contains:
- `modid`: Used for asset paths and namespaces
- `version`: Used in package filename and release tags
- `dependencies.game`: Minimum Vintage Story version

## Release Process

GitHub Actions workflow (`.github/workflows/release.yml`) triggers on version tags (`v*.*.*`):
1. Downloads Vintage Story
2. Builds with Cake
3. Creates GitHub release with packaged ZIP

## API Reference

https://apidocs.vintagestory.at/
