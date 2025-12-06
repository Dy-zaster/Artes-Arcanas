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

1. Parse the `.cr9` layout according to the descriptor counts above. Max sizes are defined in `Demonios.pas` (`MaxDirAni=4`, `MaxCuadrosJ=19`, `MaxCuadrosM=15`). The `LegacyDataExtractor` command `animations` now walks `grf/*.cr9` and emits `content/data/animations.json` with every direction/frame entry (kind, offsets, `acumy/ancho/cenx/ceny`, and optional style flags).
2. Slice the companion `.bmp` file into per-direction strips using `anchoMax` and `acumy`. Store per-frame rectangles and pivot offsets (`cenx/ceny` minus `modi*`). This step will feed future atlas builds that pack animated sprites.
3. Convert key bitmaps (with palette) into PNG while preserving transparency. The Delphi client sometimes relies on magenta (0xFF00FF) chroma key or grayscale overlays; document those per-asset as we convert so SpriteBatch can reproduce the same blending.

## Static world graphics (`oc.b` + `grf/*.bmp`)

`Tablero.InicializarConstantesTablero` reads `bin/oc.b` into memory:

1. `TNombresGraficos` – array of 512 names (string[23]) that match filenames in `grf/` (e.g., `c32` for city tiles, `x12` for extended packs).
2. `TDescriptoresGraficos` – bounding boxes, occupancy masks, types (normal, roof, floor, bridge), render flags (allows antialiasing, transparency), positional offsets, and `RecursoEfecto` bits.
3. `CheckSum` – validation integer not currently used on the client.

At runtime `TcoleccionGraficosTablero` loads each bitmap whose descriptor has `dg_recuperarArchivo`, producing an `TElementoGrafico` that knows how to render with the right flags. To port this:

- Deserialize `oc.b` and store descriptors in a modern format (e.g., `graphics.json`).
- Batch-export referenced bitmaps into texture atlases grouped by usage (floors, walls, roofs) and keep the original occupancy masks to rebuild collision layers. When the runtime loads legacy BMPs (either directly or as part of the generated atlases) it applies the same color-key treatment as Delphi (`etNegro`), turning pure black pixels into transparent ones so old assets without alpha behave as expected until new atlases are supplied.

## Terrain sheet (`grf/terreno.jpg`)

The Delphi renderer does not load ground tiles from `oc.b`. Instead it uses a dedicated sheet named `terreno.jpg` (`576×480`, progressive JPEG) that lives next to the rest of the `.bmp` assets. Its layout mirrors the legacy `TMapaCompreso` terrain codes:

- Codes `0..31` are arranged in eight rows (four codes per row). Each code occupies a `144×48` block made out of 18 mini tiles (`6` columns × `3` rows).
- Every mini tile measures `24×16` pixels. During runtime the client picks one of the 18 variants based on tile coordinates to add subtle variation to large areas.
- Codes `28..31` represent liquids/fire and were rendered with special routines (`BltLiquido`). The MonoGame port now samples those frames from the same sheet (or atlas entry) and cycles through the 18 mini tiles to mimic movement.

The new `TerrainRenderer` first looks for the `terreno` entry inside any generated atlas manifest and only falls back to loading the standalone JPEG/PNG from `MonoGameClient/content/graphics` (or `Original Pascal/Laa/grf`). If both sources are missing it reuses the deterministic color palette introduced earlier. While the original pseudo-mosaic logic gets ported, the MonoGame renderer overlays simple gradient blends along tile borders whenever two terrain types meet so transitions are less abrupt.

## UI atlases

UI boards (`fondo.bmp`, `menu.jpg`, `barra.bmp`, `obj.jpg`, `ros.jpg`, `cjr.jpg`) are loaded through `Graficador` helpers. They already use conventional bitmap/JPEG formats, so data extraction mainly involves lossless conversion to PNG and mapping sprite rectangles by reading existing constants in `Juego.pas`/`UCliente.pas`.

### Legacy HUD layout (bottom bar)

The Delphi client does not treat UI windows as floating, independent panels. Instead it builds a single HUD strip anchored to the bottom edge of the screen that is always visible while the world scrolls underneath. That strip is assembled from several bitmaps in `grf/` and divided into fixed regions:

- **Minimap block (far left)** – uses a dedicated background and renders the minimap plus the current map name (e.g., `Isla de los Ogros`). This sits in the lower‑left corner and never moves.
- **Character stats panel** – immediately to the right of the minimap. This block shows class/level and primary stats (`Fuerza`, `Constitución`, `Inteligencia`, `Sabiduría`, `Destreza`) plus percentages for hit chance, evasion, and resistances. It uses a stone/dark background and text is drawn directly over that bitmap.
- **Center status/message area** – a wide dark panel occupying the bottom center. It has two responsibilities:
  - acts as a scrolling message log (“Tu avatar se aburrió de esperar y se dejó matar”, “Has sido resucitado”, etc.);
  - hosts the “Menú” subsection with the avatar portrait and numeric values for `Salud`, `Mana`, `Comida` plus a couple of quick‑action icons (fist, weapon, rune).
- **Equipment + inventory/spellbook (right side)** – the bottom‑right region combines several concepts into one composite block:
  - the **paper doll** (outline of the avatar) with equipment slots around it (helm, armor, backpack, etc.) is rendered over a dedicated background;
  - to the right of the doll there is a fixed grid of square cells. The same grid is reused in two modes:
    - **Inventario** – cells contain item icons (bags, potions, books) pulled from `obj.jpg`;
    - **Hechizos** – cells show rune icons (blue glyphs) representing spells;
  - the labels “Inventario” and “Hechizos” above the grid work as tabs, but visually they are just text drawn on the same HUD bar rather than separate windows.

There is no concept of draggable windows in the original client for these elements: the minimap, stats, message log, equipment, inventory, and spell grid all live inside this monolithic bottom HUD and are positioned with absolute coordinates inside that strip. When porting to MonoGame, the goal is to reproduce this *continuous bar* (using `cjr`/`ros`/`bmenu`/`obj` assets) rather than independent floating windows. The new UI framework in `Laa.Monogame.Client.UI` exists purely to help place and skin that HUD with atlas sprites while keeping the overall structure identical to Delphi’s layout.

### Legacy UI bitmap catalogue

Every UI surface under `Original Pascal/Laa/grf` is referenced by name inside the Delphi client. The table below documents what each bitmap holds, which part of the HUD it skins, and the code that loads or blits it.

#### `menu.jpg`

- Loaded as `fondoMenu` during `PrepararInterfaz` (`Original Pascal/Laa/Juego.pas:562`).
- `TJForm.paint` blits the full 640×480 image whenever the game is in menu/login/creation states, before overlaying dynamic text (`Original Pascal/Laa/Juego.pas:3637-3679`).
- The picture already contains the parchment background, logo placement, and decorative frame, so none of those widgets are assembled dynamically; MonoGame should treat it as a single-screen backdrop.

#### `bmenu.jpg`

- Loaded into `botonesMenu` (`Original Pascal/Laa/Juego.pas:563`) and sliced via `TGBoton.DefinirGraficos`.
- Rectangles at `Original Pascal/Laa/Juego.pas:575-592` show exactly which sprite is used for *Aceptar*, *Cancelar*, *Ingresar*, *Crear*, *Salir*, and the dice button. Each pair of `Point` arguments marks the idle and pressed state offsets inside the sheet.
- Keep both states when porting so button hover/press feedback stays identical to Delphi.

#### `fondo.bmp`

- Main HUD board (`Fondo`) loaded at `Original Pascal/Laa/Juego.pas:566`.
- Supplies virtually every static piece of the bottom strip: the minimap parchment (`CopiarCanvasASuperficie` into `PnMapa`, line 637), the stats panel/background (`PnInfo`, `Original Pascal/Laa/Juego.pas:645-669`), the grid panel (`PnGrids`, `Original Pascal/Laa/Juego.pas:635-640`), text scroll buttons (`B_txArriba/B_txAbajo` reference `fondo.canvas`, lines 583-586), the equipment slots, and the message log.
- Extra helper sprites (minimap pointer, spell highlight frame, “can’t use” icon) are copied from specific coordinates in this bitmap (`Original Pascal/Laa/Juego.pas:618-625`). Repacking the HUD requires preserving each of those sub-rectangles.

#### `tccb.jpg`

- Packed into the DirectDraw surface `TablaCC_Botones` (`Original Pascal/Laa/Juego.pas:567`).
- Provides the entire 160 px-wide side panel for the construction/information menus. `PintarMenuConstruccion` (`Original Pascal/Laa/UCliente.pas:877-904`) blits fixed areas such as the Fabricar button, the three-material strip, the scrollable list, and the four pagination buttons using the named `Area*` rectangles defined at the top of `UCliente.pas`.
- When migrating the UI keep each area as a separate sprite so the hover logic (highlighting the proper button based on cursor position) continues to work.

#### `tcca.jpg`

- Loaded as `TablaCC_Arca` (`Original Pascal/Laa/Juego.pas:568`) and used whenever the right-hand inventory panel slides in.
- `PintarMenuComercio` and `PintarMenuObjetos` (`Original Pascal/Laa/UCliente.pas:1106-1179`) draw the background grid, chest/bag/corpse icons, and the “Guardar/Sacar del baúl” buttons by sampling named regions (`AreaTablaIconos`, `AreaDeIconoDeBaul`, `AreaOrBotonGuardarEnBaul`, etc.). The asset also includes the decorative frame around the 3×N item list.
- The MonoGame UI should keep the same 46 px spacing and button slices so that cursor hit-tests stay aligned with the art.

#### `barra.bmp`

- Converted into the surface `BarraVidaMana` (`Original Pascal/Laa/Juego.pas:569`).
- `DibujarBarrasVidaMana` renders the left health bar and right mana bar by blitting the 116×32 header and then masking the fill with `BltFxMascara` (`Original Pascal/Laa/Juego.pas:2683-2699`). The PK flag swaps to the red-tinted variant in the same bitmap.
- The file contains both frames (top half) and the fill masks (bottom half). Export both when generating atlases; the MonoGame version reuses the same rectangles to animate the fill width.

#### `obj.jpg`

- Item icon atlas loaded as `Iconos_Objetos` (`Original Pascal/Laa/Juego.pas:627`).
- Every icon is 40×40 and laid out in an 8-column strip, which is why `PintarObjeto` samples `(id and $7)*40` for the X coordinate and `(id shr 3)*40` for the Y coordinate (`Original Pascal/Laa/Juego.pas:1298-1359`). The same offsets are used inside store/bag menus (`Original Pascal/Laa/UCliente.pas:854-865`).
- Besides regular items, ids `<4` correspond to paper-doll hands; those frames are reused when drawing equipped weapons, so keep the first row intact.

#### `ros.jpg`

- Portrait sheet (`Rostros`) loaded at `Original Pascal/Laa/Juego.pas:628`.
- `Tjugador.PrepararImagenJugador` paints the appropriate face and applies debuff highlights before the panel copies it into the HUD (`Original Pascal/Laa/Sprites.pas:800-825` and `Original Pascal/Laa/Juego.pas:3356-3360`).
- Each 40×40 slot is a different “rostro” (class/race/gender). This sheet has nothing to do with spellbooks; the name literally stands for *rostros* and the MonoGame client should bind it to the portrait widget only.

#### `cjr.jpg`

- Rune/spell atlas, loaded as `IconosCjr` (`Original Pascal/Laa/Juego.pas:629`).
- `PintarConjuro` picks 40×40 tiles using the same `(col,row)` math as the item sheet to render the spell grid (`Original Pascal/Laa/Juego.pas:1369-1384`), and `PintarMenuComercio` reuses those frames when merchants sell scrolls (`Original Pascal/Laa/UCliente.pas:1127-1132`).
- The code applies brightness effects to show mana requirements, so retain full RGB data (and ensure the atlas uses straight alpha).

#### `logo.bmp`

- Splash image drawn while DirectX initializes in `TFEsperar` (`Original Pascal/Laa/SScreen.pas:96-130`).
- Stored as `logo.bmp` (`CrearDeGDD` call at line 113). The loading form displays it at `(0,0)` together with the progress bar, so the MonoGame splash can simply blit the entire bitmap once.

## Atlas builder

The repository now includes a small CLI (`Tools/AtlasBuilder`) that repacks the legacy `grf/*` textures (BMP, PNG, JPG/JPEG) into Texture2D-friendly atlases alongside a JSON manifest. The MonoGame client automatically loads the manifest/atlases (if present) and falls back to the raw files when they are missing.

```bash
cd Tools/AtlasBuilder
DOTNET_CLI_HOME="$PWD" dotnet run -- \
    --source "../../Original Pascal/Laa/grf" \
    --output "../../MonoGameClient/content/graphics/atlases" \
    --atlas-size 2048 \
    --chroma-key #000000
```

- `--source` defaults to the legacy `Original Pascal/Laa/grf` folder.
- `--output` defaults to `MonoGameClient/content/graphics/atlases`.
- `--atlas-size` controls the square dimensions of each atlas (pixels). Increase it if you want fewer atlas files, decrease it to avoid GPU limits.
- `--chroma-key` defaults to black (`#000000`). Pass `none` to disable replacement or specify another hex color if you want to experiment with different legacy keys.
- All `.bmp`, `.png`, `.jpg`, and `.jpeg` files inside the source folder are packed. This keeps resources like `terreno.jpg`, `ros.jpg`, and UI boards consistent with the rest of the pipeline.

The command produces:

- `atlas_manifest.json` – describes every sprite’s atlas/rectangle coordinates.
- `atlas_00.png`, `atlas_01.png`, … – the packed textures.

Drop the generated folder under `MonoGameClient/content/graphics/atlases` (already part of the fallback search roots). When absent, the client continues to stream the original BMPs directly from `Original Pascal/Laa/grf`.

## Animation preview inside the MonoGame client

- `animations.json` is loaded next to the rest of the JSON catalogues and `AnimationTextureProvider` now resolves every key straight from the atlas manifest (no BMP fallback paths). The provider rebuilds per-direction strips by applying the legacy `modix/modiy/cenx/ceny` offsets so pivots match Delphi’s `TanimacionMonstruo.draw` routines.
- `Game1` exposes an opt-in debug viewer so we can validate each animation without networking. Press `F5` to toggle it, `F6/F7` to cycle the loaded `.cr9` entries, `F8` to iterate directions, and `F9` to flip the sprite. The preview is anchored at the map center and renders through the same camera used by the terrain renderer, which keeps scale/alignment consistent with the pseudo-mosaic world.
- The HUD now displays the active animation key/direction/mirror state so we can cross-reference it with the legacy table IDs when something looks off.
- `F10` switches the viewer into *avatar mode*. In this mode the client resolves player sprites exactly like Delphi’s `InfMapeoAnimaciones[(armadura + (clase << 5) + (raza << 8) + genero) & $FFF]` lookup and automatically switches to the mapped animation. Use `J/U` to move through the 32 armor slots, `K/I` for the eight classes, `L/O` for the seven races (the eighth entry is reserved/unused), and `P` to flip gender. The HUD line shows the resulting combination and the animation id so we can verify `anim_map.json` without digging through the Pascal UI.
- Every monster nest now instantiates a lightweight `MonsterEntity` (descriptor + animation key) and the runtime `MonsterRenderer` feeds those entities through the same atlas pipeline as the rest of the scene. This gives immediate visual feedback about which creatures populate each spawn point and lays the groundwork for wiring real AI/networked monsters without rewriting render code. `F11` cycles the monsters through Idle → Move → Attack → Dead while `F12` toggles attack mode for quick testing; these hotkeys exist purely for debugging and will later be driven by networking updates.
- The new `WorldState` keeps track of every `MonsterEntity` (and will later host players/NPCs) so renderers/simulations simply consume/read shared state; when the networking layer arrives it only needs to mutate `WorldState`.
- The `Networking/` namespace introduces a simple `INetworkClient` and a `MockNetworkClient` implementation that replays scripted `NetworkMessage` instances (spawn/move/attack/death). `Game1` subscribes to those events, queues them, and applies the payloads through `ProcessNetworkEvents()`, which keeps the `WorldState` authoritative and lets the renderer reflect network changes automatically.
- A tiny `MonsterSimulation` service still exists as a fallback so entities wander around when no network feed is available; it automatically disables itself as soon as the mock client connects so the scripted packets remain the source of truth. Use it for offline animation stress tests or when iterating on renderer changes without networking.

## Action items

- [x] Write a converter that walks `grf/`, finds each `.cr9`, and emits JSON descriptors plus PNG-ready metadata (see `LegacyDataExtractor animations` output in `content/data/animations.json`). Atlas packing remains pending.
- [x] Recreate the `oc.b` metadata in a cross-platform format and generate a lookup table to drive MonoGame's tile renderer.
- [x] Hook the MonoGame runtime to the exported animations (`F5` viewer) using the atlas manifest instead of direct BMP reads.
- [ ] Document per-asset transparency expectations (magenta key vs. alpha) so SpriteBatch settings match the Delphi behavior.

## Pseudo-mosaic masks (`ti.bmp`)

The Delphi client never relied on texture filtering to smooth terrain boundaries. Instead it upscales the 64×64 `TMapaCompreso` grid into a 256×256 board and then blends corner/edge tiles with a set of 4×4 alpha stencils stored in `grf/ti.bmp`:

- `ti.bmp` is an 8-bit, 24×128 bitmap where each 16-pixel row block encodes one of the `MZ_*` patterns (`MZ_h`, `MZ_v`, `MZ_h2`, `MZ_v2`, `MZ_si`, `MZ_sd`, `MZ_ii`, `MZ_id`) plus a fully transparent strip.
- Every pixel value 0–8 maps to a deterministic mix (`0` = use overlay color, `4` = 50/50, `8` = keep destination). `BltAlphaTile` and `BltAlphaLiquido` loop over the mask and apply the weight by averaging source/destination colors multiple times in 16-bit space.
- Liquids use the same masks but sample palette-indexed data from `liq.bmp`/`Paleta_Liquidos` instead of the terrain sheet.

The MonoGame renderer loads `ti.bmp`, converts each stencil into a normalized float array, and builds a cached `Texture2D` per `(tile code, atlas variant, pattern)` so the GPU reproduces the exact blending. Matching the original look requires both this mask and the 4×4 terrain upscaling rules described in [`Map.md`](Map.md).
