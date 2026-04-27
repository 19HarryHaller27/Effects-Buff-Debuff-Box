# Buffbox

| | |
|-|-|
| **Mod id** | `buffbox` |
| **Version** | 0.1.0 (see `modinfo.json`) |
| **Game** | Vintage Story 1.22.0+ |
| **.NET** | 10+ (`net10.0`) |

Movable HUD **buff/debuff** squares (timed). **Commands:** e.g. `/slow` (walk speed ×0.25 for 10s, boot icon), `/attackspeed` (mining and ranged speed +25% for 20s, broken sword icon). Uses Newtonsoft.Json. See `modinfo.json` for the short description.

## Build

1. **.NET SDK** for `net10.0`.
2. Set your game folder (contains `VintagestoryAPI.dll`) in **`Directory.Build.props`**, or build with  
   `dotnet build Buffbox.csproj -c Release -p:VintageStoryPath="C:\Path\To\Vintagestory"`  
   (or set `VINTAGE_STORY_PATH` in the environment).

After build, the project **copies the DLL and PDB to this folder** (`PublishToModFolder` target). You can also copy the folder into `%APPDATA%\Roaming\VintagestoryData\Mods\Buffbox\` to run in-game.

## Layout

- **Source:** `src/` and project root.
- `MOD_PAGE.md` and `tools/` for notes / helper scripts.
- `assets/`, `modinfo.json` for the mod package.

## License

[MIT](LICENSE)

**Author:** adams (`modinfo.json`).
