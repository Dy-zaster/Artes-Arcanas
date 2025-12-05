# Graphics & Animations (`grf/*` + `.cr9`)

Sprites and UI assets live under `Original Pascal/Laa/grf`. The Delphi client loads raw bitmaps/JPEGs and pairs them with metadata stored in sibling `.cr9` files. Understanding these relationships is key before we can repack textures for MonoGame.

## Directory layout

- `grf/*.bmp` – 256-color or 16-bit bitmaps for characters (`m0.bmp`, `c0.bmp`), objects, GUIs, etc.
- `grf/*.jpg` – compressed UI backgrounds (menus, tables, icons). Loaded through `CrearDeJDD` helpers.
- `grf/*.cr9` – binary descriptors that define frame rectangles, pivot offsets, and multi-direction setups for sprites.
- Palette files such as `grf/mapa.pal` feed the minimap rendering via `MundoEspejo.pas:205`.

`Graficador.pas` centralizes the loading logic; `CrptGDD` is set to `Grf\`, so every asset path is relative to that folder.

## Animation descriptors (`.cr9`)

`Graficos.pas` defines three descriptor shapes:

| Descriptor | Used by | Fields |
| --- | --- | --- |
| `TdaMonstruo` | monsters/effects | `anchoMax`, `modix`, `modiy`, `acumy[0..MaxCuadrosM]`, `ancho[frame]`, `cenx/ceny[frame]` |
| `TdaJugador` | player armors | Same layout but sized to `MaxCuadrosJ` |
| `TDatoAnimacion` | single-direction effects | `inicioY`, `alto`, `ancho`, `cenx`, `ceny` |

Files are simply consecutive arrays of these descriptors:

- Object/effect animations: one `TdaMonstruo` (single direction) optionally followed by `FlagsEstilosAnimacion`.
- Monster animations: 5× `TdaMonstruo` (directions) + optional flags.
- Player animations: 5× `TdaJugador` + optional flags.

The associated bitmap usually contains every direction concatenated horizontally; the code copies `posicionA[i].anchoMax` strips into dedicated DirectDraw surfaces.

### Conversion approach

1. Parse the `.cr9` layout according to the descriptor counts above. Max sizes are defined in `Demonios.pas` (`MaxDirAni=4`, `MaxCuadrosJ=19`, `MaxCuadrosM=15`).
2. Slice the companion `.bmp` file into per-direction strips using `anchoMax` and `acumy`. Store per-frame rectangles and pivot offsets (`cenx/ceny` minus `modi*`).
3. Emit an intermediate JSON format like:
   ```json
   {
     "texture": "Characters/m0.png",
     "directions": [ ... ],
     "frames": [{"rect": [x,y,w,h], "pivot": [px,py]}]
   }
   ```
   This allows MonoGame to load a single PNG atlas and animate via SpriteBatch.
4. Plan to convert key bitmaps (with palette) into PNG while preserving transparency. The Delphi client sometimes relies on magenta (0xFF00FF) chroma key or grayscale overlays; document those per-asset as we convert.

## Static world graphics (`oc.b` + `grf/*.bmp`)

`Tablero.InicializarConstantesTablero` reads `bin/oc.b` into memory:

1. `TNombresGraficos` – array of 512 names (string[23]) that match filenames in `grf/` (e.g., `c32` for city tiles, `x12` for extended packs).
2. `TDescriptoresGraficos` – bounding boxes, occupancy masks, types (normal, roof, floor, bridge), render flags (allows antialiasing, transparency), positional offsets, and `RecursoEfecto` bits.
3. `CheckSum` – validation integer not currently used on the client.

At runtime `TcoleccionGraficosTablero` loads each bitmap whose descriptor has `dg_recuperarArchivo`, producing an `TElementoGrafico` that knows how to render with the right flags. To port this:

- Deserialize `oc.b` and store descriptors in a modern format (e.g., `graphics.json`).
- Batch-export referenced bitmaps into texture atlases grouped by usage (floors, walls, roofs) and keep the original occupancy masks to rebuild collision layers.

## Terrain sheet (`grf/terreno.jpg`)

The Delphi renderer does not load ground tiles from `oc.b`. Instead it uses a dedicated sheet named `terreno.jpg` (`576×480`, progressive JPEG) that lives next to the rest of the `.bmp` assets. Its layout mirrors the legacy `TMapaCompreso` terrain codes:

- Codes `0..31` are arranged in eight rows (four codes per row). Each code occupies a `144×48` block made out of 18 mini tiles (`6` columns × `3` rows).
- Every mini tile measures `24×16` pixels. During runtime the client picks one of the 18 variants based on tile coordinates to add subtle variation to large areas.
- Codes `28..31` represent liquids/fire and were rendered with special routines (`BltLiquido`). The MonoGame port now samples those frames from the same sheet (or atlas entry) and cycles through the 18 mini tiles to mimic movement.

The new `TerrainRenderer` first looks for the `terreno` entry inside any generated atlas manifest and only falls back to loading the standalone JPEG/PNG from `MonoGameClient/content/graphics` (or `Original Pascal/Laa/grf`). If both sources are missing it reuses the deterministic color palette introduced earlier.

## UI atlases

UI boards (`fondo.bmp`, `menu.jpg`, `barra.bmp`, `obj.jpg`, `ros.jpg`, `cjr.jpg`) are loaded through `Graficador` helpers. They already use conventional bitmap/JPEG formats, so data extraction mainly involves lossless conversion to PNG and mapping sprite rectangles by reading existing constants in `Juego.pas`/`UCliente.pas`.

## Atlas builder

The repository now includes a small CLI (`Tools/AtlasBuilder`) that repacks the legacy `grf/*` textures (BMP, PNG, JPG/JPEG) into Texture2D-friendly atlases alongside a JSON manifest. The MonoGame client automatically loads the manifest/atlases (if present) and falls back to the raw files when they are missing.

```bash
cd Tools/AtlasBuilder
DOTNET_CLI_HOME="$PWD" dotnet run -- \
    --source "../../Original Pascal/Laa/grf" \
    --output "../../MonoGameClient/content/graphics/atlases" \
    --atlas-size 2048
```

- `--source` defaults to the legacy `Original Pascal/Laa/grf` folder.
- `--output` defaults to `MonoGameClient/content/graphics/atlases`.
- `--atlas-size` controls the square dimensions of each atlas (pixels). Increase it if you want fewer atlas files, decrease it to avoid GPU limits.
- All `.bmp`, `.png`, `.jpg`, and `.jpeg` files inside the source folder are packed. This keeps resources like `terreno.jpg`, `ros.jpg`, and UI boards consistent with the rest of the pipeline.

The command produces:

- `atlas_manifest.json` – describes every sprite’s atlas/rectangle coordinates.
- `atlas_00.png`, `atlas_01.png`, … – the packed textures.

Drop the generated folder under `MonoGameClient/content/graphics/atlases` (already part of the fallback search roots). When absent, the client continues to stream the original BMPs directly from `Original Pascal/Laa/grf`.

## Action items

- [ ] Write a converter that walks `grf/`, finds each `.cr9`, and emits JSON descriptors plus PNG atlases.
- [x] Recreate the `oc.b` metadata in a cross-platform format and generate a lookup table to drive MonoGame's tile renderer.
- [ ] Document per-asset transparency expectations (magenta key vs. alpha) so SpriteBatch settings match the Delphi behavior.
