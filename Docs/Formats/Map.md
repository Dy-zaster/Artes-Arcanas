# Map Files (`bin/*.mpv`)

Each LAa map lives in a standalone `.mpv` file located inside `Original Pascal/Laa/bin`. The same structure is consumed by the server (`Tablero.pas`) and the Delphi client (`TTablero.RecuperarMapa`). Files are little-endian dumps of Delphi records, so fields appear on disk in the exact order declared. The loader performs the following steps:

1. Read `TDatosMapa` (fixed 24 bytes) for metadata such as the label, neighbor ids, and counts for the blocks that follow (`Tablero.pas:121`).
2. Read the compressed terrain array `TMapaCompreso = array[0..63,0..63] of byte` (4096 bytes). Those 64×64 codes expand to a 256×256 logical board when `ActualizarTableroTiles` runs.
3. Iterate through variable length sections using the counts provided by the header:
   - `N_Graficos` entries of `TGrafico` (static props, walls, roofs).
   - `N_Sensores` entries of `TSensor` plus optional `TTextoSensor` (length 127) strings.
   - `N_Nidos` entries of `TNidoCriaturas` (monster spawners).
   - `N_Comerciantes` entries of `TComerciante_mapa` plus optional `TTextoComerciante` strings.
4. If `BytesDatosExtendidos` > 0, read `TDatosMapaExtendido` (up to 252 bytes) for respawn points, dungeon flags, and scripted flag behaviors.

## Header reference

### `TDatosMapa`

| Field | Size | Description |
| --- | --- | --- |
| `nombre` | 24 B | CP‑1252 string shown in the UI. |
| `N_Graficos` | 2 B | Number of static graphics records. |
| `BanderasMapa` | 2 B | Map flags (safe, combat, indoor, weather bits). |
| `MapaNorte/Sur/Este/Oeste` | 1 B each | Neighbor map ids. |
| `VersionMapa` | 1 B | Should match `VERSION_ARCHIVO_MAPA` (=1). |
| `N_nidos` | 1 B | Monster nest count. |
| `N_NPC` | 1 B | Reserved (not used by client). |
| `N_Comerciantes` | 1 B | Merchant count. |
| `N_Sensores` | 1 B | Sensor trigger count. |
| `nousado1..nousado7` | 7 B | Legacy padding. |
| `BytesDatosExtendidos` | 1 B | Bytes of `TDatosMapaExtendido` that follow the dynamic lists. |

### `TDatosMapaExtendido`

This structure stores map-level behaviors and is capped at 252 bytes. Relevant fields:

- Respawn information (`posx_Resucitar`, `posy_Resucitar`, `mapa_Resucitar`).
- `FlagsCalabozo`, `FlagsAutolimpiables` – initial and self-resetting flag states.
- Three `TArreglo32bytes` arrays that describe per-flag behaviors for server and client (`ComportamientoFlag`, `Dato1Flag`, `Dato2Flag`).
- Detector configuration arrays: `ComportamientoDetector`, `Dato1Detector`, `Dato2Detector`, `FlagsADetectar`, `EstadosADetectar` (8 entries each) used for in-map automations.

## Static graphics block (`TGrafico`)

Static props (trees, roofs, walls) use descriptor metadata stored inside the map file and pixel data stored in `grf/*.bmp`. Each `TGrafico` (Tablero.pas:207) includes:

- `codigoFlags` (word): indexes into `InfGra` (loaded from `bin/oc.b`) plus flag bits (e.g., horizontal flip).
- `posx`, `posy` (bytes): top-left tile coordinates for placement.
- `flagsGrafico`, `sub_z` (bytes): rendering hints (antialiasing, sub‑layer order).

When converting to MonoGame, load sprites from the `Grf/` directory and reuse the metadata to reproduce draw order, mirroring, and collision masks.

`flagsGrafico` stores the `fgfx_*` bits defined in `Tablero.pas:160`. They now map 1:1 to the `StaticGraphicFlags` enum so runtime code can query them directly:

| Bit | Flag | Meaning |
| --- | --- | --- |
| `0x01` | `fgfx_Espejo` | Draw mirrored (the original editor labeled this as “rotación”). |
| `0x02` | `fgfx_TransparenteNatural` | Natural chroma transparency (obeys atlas alpha). |
| `0x04` | `fgfx_TransparenteForzado` | Forced transparency (used for special overlays). |
| `0x08` | `fgfx_Ilusion` | Illusion/invisibility helper (affects draw order). |
| `0x20` | `fgfx_SensibleAFlags` | Depends on dungeon flag state. |
| `0x40` | `fgfx_Antialisado` | Eligible for the old antialiasing routine. |
| `0x80` | `fgfx_Levitacion` | Hovering objects bob vertically. |

The MonoGame `StaticGraphicRenderer` already respects the mirroring flag (reproducing the Delphi rotation trick), and the remaining bits are available for future behavior parity work.

**Placement origin:** `posx` in the map file points to the center column of the 8×8 occupancy mask. The Delphi client subtracts four tiles before applying the mask (and before sampling pseudo-collision data), so the MonoGame renderer now mirrors that behavior by subtracting `4 * tileWidth` from the horizontal base position before applying the descriptor offsets. Without this shift, wide assets such as statues, arches, and bridges appear a few cells to the right of their intended terrain.

**Draw order:** the editor sorts objects by `((posy << 9) | sub_z)` (`OrdenadoRapido`), so `posy` dominates and `sub_z` only breaks ties. The MonoGame client now mimics that sort so interior props (yunques, mesas) stay beneath the corresponding roofs even if their `SubLayer` values differ.

## Sensors, nests, and merchants

- `TSensor` defines interactive triggers (resurrection shrines, portals, clan banners). Fields: `Tipo`, `posx/posy`, two key requirements, `dato1..dato4`, and `flagsSensor` bits (clan-only, apprentice-only, etc.). If `ListaTextoSensor` exists the same index stores the localized prompt.
- `TNidoCriaturas` stores `tipo`, `posx`, `posy`, and `cantidad` for spawn pools.
- `TComerciante_mapa` stores NPC merchants embedded in the map: type, position, backing monster code, inventory (`TInventarioArtefactos`), and per-slot inflation multipliers.

## Conversion strategy

1. Write a C# reader that sequentially deserializes each block according to the layouts above (remember double-byte alignment rules). The Pascal code uses packed records, so no extra padding is present.
2. Expand the 64×64 `TMapaCompreso` grid into the 256×256 logical map by applying the same logic as `ActualizarTableroTiles` (copy `Mapapos[i,j].terBol` into `MapaTiles`). Documented behavior: terrain bits (`mskTerreno`) control collision, and the low 10 bits store bag/corpse ids.
3. Emit a friendly format (e.g., Tiled JSON) with layers for terrain, props, sensors, nests, and merchants. Keep the original binary blob around for validation by storing the SHA-1 and source offset in the JSON metadata.
4. Name the exported files `map_<id>.json` (or place them inside `content/data/maps/`) so the MonoGame client can auto-discover them.
5. Validate by loading a handful of `.mpv` files in Delphi and taking screenshots; ensure the MonoGame renderer produces matching layouts before fully migrating.

### Terrain expansion details

`ActualizarTableroTiles` is more than a 4×4 repetition: for each 64×64 cell it looks at the four cardinal neighbors (`n`, `e`, `s`) and picks alternate terrain codes for the sub-quadrants so beaches, floors, and rivers gain rounded edges before the pseudo-mosaic masks are applied. The MonoGame `MapTransformer` now mirrors this routine when it expands the JSON terrain, guaranteeing that the 256×256 grid feeding the renderer matches the precise patterns the Delphi client produced.
