# Hunting Damage Over Time

A Vintage Story mod that adds realistic bleeding mechanics to hunting. When you hit an animal with an arrow or spear, it will take damage over time, making hunting more immersive and realistic.

## Features

- **Bleeding Effect**: Animals hit by arrows or spears bleed for 30 seconds
- **Damage Over Time**: 0.5 damage per second while bleeding
- **Configurable**: Easy to adjust damage and duration values
- **Server-Compatible**: Works on multiplayer servers (server-side only installation)

## Installation

### For Players

1. Download the latest release `.zip` file from the [Releases](../../releases) page
2. Place the zip file in your Vintage Story `Mods` folder:
   - **Windows**: `%appdata%/VintagestoryData/Mods/`
   - **Linux**: `~/.config/VintagestoryData/Mods/`
   - **Mac**: `~/Library/Application Support/VintagestoryData/Mods/`
3. Restart Vintage Story

### For Server Owners

1. Download the latest release `.zip` file
2. Place it in your server's `Mods` folder
3. Restart the server
4. Clients do **not** need to install the mod

## Configuration

To adjust the bleeding mechanics, modify the constants in `HuntingDamageOverTimeModSystem.cs`:

```csharp
public const float BLEEDING_DAMAGE_PER_SECOND = 0.5f;  // Damage per second
public const float BLEEDING_DURATION_SECONDS = 30f;    // Bleed duration in seconds
```

## Building from Source

### Prerequisites

- .NET 8.0 SDK
- Vintage Story installation
- `VINTAGE_STORY` environment variable set to your Vintage Story installation path

### Build Commands

```bash
# Using Cake (recommended)
cd HuntingDamageOverTime/ZZCakeBuild
dotnet run --configuration Release

# Using dotnet directly
cd HuntingDamageOverTime/HuntingDamageOverTime
dotnet build -c Release
```

The packaged mod will be in `Releases/huntingdamageovertime_<version>.zip`

## Development

This mod uses:
- **EntityBehavior** system to track bleeding state
- **OnEntityReceiveDamage** to detect projectile hits
- **OnGameTick** to apply damage over time

See [CLAUDE.md](CLAUDE.md) for detailed architecture information.

## Compatibility

- **Game Version**: Vintage Story 1.21.0+
- **Side**: Server-side (clients don't need it)
- **Multiplayer**: ✅ Fully compatible

## License

[Add your license here]

## Credits

Created for Vintage Story modding community