# Legacy Data Formats Overview

The original Delphi 6 client loads almost every gameplay asset from binary blobs stored under `Original Pascal/Laa/bin` and pixel assets inside `Original Pascal/Laa/grf`. This folder aggregates the reverse-engineering notes required to extract data for the MonoGame port.

## High-level asset map

| Domain | Files / folders | Loader reference |
| --- | --- | --- |
| Maps & world topology | `bin/*.mpv` | `Tablero.pas` (`TDatosMapa`, `TDatosMapaExtendido`) |
| Static world graphics | `bin/oc.b` + `grf/{0..x}.bmp` | `Tablero.InicializarConstantesTablero`, `Graficador.pas` |
| Monsters & behavior | `bin/std.mon`, `bin/mp_ataq.b`, `bin/mp_anim.b` | `Demonios.pas` |
| Items & spells | `bin/obj.b`, `bin/cjr.b`, `bin/comercio.b` | `Objetos.pas`, `EditorDeComerciantes` |
| UI / sprite sheets | `grf/*.bmp`, `grf/*.jpg`, `grf/*.cr9` | `Graficos.pas`, `Graficador.pas` |
| Audio | `snd/*.wav`, `snd/*.mid` | Directly loaded via `DirectSound` wrappers |

## Next actions for extraction

1. Convert every `.mpv` into an intermediate JSON/Tilemap representation while preserving headers, resource placements, sensors, merchants, and extended flags. See `Map.md`.
2. Build a small C# tool that reads `obj.b` / `cjr.b` records (little-endian structures) and emits strongly typed DTOs for MonoGame (`DataTables.md`).
3. Decode `.cr9` animation descriptor tables and bake them into `.json` files next to the PNG spritesheets produced from `grf/*.bmp` (`Graphics.md`).
4. Automate validation by comparing MonoGame-produced screenshots against Delphi renders for a handful of sample maps.

Each specialized document in this folder dives deeper into the corresponding binary format and outlines the conversion approach.

## Extraction tooling

- `Tools/LegacyDataExtractor` is a .NET 8 console application that reads the legacy blobs and emits JSON. It currently understands `.mpv` map files and the `obj.b` item catalogue (see [`Map.md`](Map.md) and [`DataTables.md`](DataTables.md)).
- Run it from the repository root (NuGet/network access is not required thanks to the local `NuGet.Config` inside the tool directory):

  ```bash
  cd Tools/LegacyDataExtractor
  DOTNET_CLI_HOME="$PWD" dotnet run -- map "../../Original Pascal/Laa/bin/0.mpv" --out ../../Docs/Formats/Samples/map_0.json
  DOTNET_CLI_HOME="$PWD" dotnet run -- items "../../Original Pascal/Laa/bin/obj.b" --out ../../Docs/Formats/Samples/items.json
  DOTNET_CLI_HOME="$PWD" dotnet run -- spells "../../Original Pascal/Laa/bin/cjr.b" --out ../../Docs/Formats/Samples/spells.json
  DOTNET_CLI_HOME="$PWD" dotnet run -- commerce "../../Original Pascal/Laa/bin/comercio.b" --out ../../Docs/Formats/Samples/commerce.json
  DOTNET_CLI_HOME="$PWD" dotnet run -- monsters "../../Original Pascal/Laa/bin/std.mon" --out ../../Docs/Formats/Samples/monsters.json
  DOTNET_CLI_HOME="$PWD" dotnet run -- attackmap "../../Original Pascal/Laa/bin/mp_ataq.b" --out ../../Docs/Formats/Samples/attack_map.json
  DOTNET_CLI_HOME="$PWD" dotnet run -- animmap "../../Original Pascal/Laa/bin/mp_anim.b" --out ../../Docs/Formats/Samples/anim_map.json
  ```

- Sample outputs live in `Docs/Formats/Samples/map_0.json` (maps), `Docs/Formats/Samples/items.json` (items), `Docs/Formats/Samples/spells.json` (spells), `Docs/Formats/Samples/commerce.json` (shop inventories), `Docs/Formats/Samples/monsters.json` (monster stats), `Docs/Formats/Samples/attack_map.json` (attack mapping), and `Docs/Formats/Samples/anim_map.json` (armor animation mapping). They mirror the structures returned by the parsers and can be used as fixtures while wiring the MonoGame runtime.
