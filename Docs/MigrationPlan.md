# Migration Plan – LAa Client to MonoGame

## Current snapshot

- Legacy client (`Original Pascal/Laa`) built with Delphi 6, heavily tied to Windows-specific APIs and Spanish identifiers.
- New solution `MonoGameClient/Laa.Monogame.Client` created with .NET 8 and MonoGame DesktopGL.
- Game bootstrap (`Program.cs`) instantiates `Game1`, which sets up a 1280×720 swap chain, a placeholder sprite batch, and a Content Pipeline definition (`Content/Content.mgcb`).
- NuGet dependencies declared but not yet restored locally (MonoGame feeds blocked inside this environment). Restoration succeeds when NuGet is reachable.
- Networking research started: see `Docs/Networking/Protocol.md` for opcode-level documentation extracted from the Delphi sources.
- Map loading now expands the legacy 64×64 compressed terrain to 256×256 and applies a transpose so screen quadrants match the Delphi client; static graphics/sensors/nests/merchants are transformed accordingly and camera zoom allows viewing the full map. Temporary edge gradients are suppressed when a real terrain sheet is present to avoid neon artifacts.

## Goals

1. Feature-parity client that preserves core gameplay, UI, and networking behavior from the Delphi version.
2. Clean separations for rendering, assets, simulation, and networking to simplify future tooling and testing.
3. English-only identifiers and comments for maintainability and alignment with .NET ecosystem conventions.
4. Documentation-first mindset: every architectural decision or format conversion lives under `Docs/`.

## Port strategy and status

| Phase | Focus | Status | Key Tasks |
| --- | --- | --- | --- |
| 0. Foundations | Build system & render loop | ✅ Completed | Solution bootstrap, MonoGame packages, placeholder rendering, repo documentation. |
| 1. Data extraction | Understand Delphi assets | ✅ Completed | `Tools/LegacyDataExtractor` exports maps, items, spells, commerce tables, monsters, attack/animation mappings, and static graphics as JSON; sample snapshots live under `Docs/Formats/Samples`. |
| 2. Core systems | Rendering & content | 🛠️ In progress | Content layer scaffolding plus a camera-driven tile renderer that consumes the legacy `terreno.jpg` sheet (from atlases or raw files), animates water/lava tiles, **and now ports the true pseudo-mosaic system using `ti.bmp` masks + Delphi’s 4× upscaling rules**; the oc.b-driven static graphic loader and debug overlays/panels are in place (`Laa.Content.Core` + `Laa.Content.Json` + `ContentContext` feed the MonoGame client); next up is expanding the scene graph/input abstractions. |
| 3. Gameplay & networking | Logic parity | ⏳ Pending | Port combat loop, inventory/trade, quests, and networking protocol (client-first, server later). |
| 4. Polishing | UX + toolchain | ⏳ Pending | Recreate audio, localization, accessibility, add modern updater/launcher, QA automation. |

Status legend: ✅ done, ⏳ planned/not started, 🛠️ in progress.

**Recommended next step:** finish the tile edge blending for terrain, continue migrating the oc.b descriptors + extracted bitmaps into shared texture atlases (instead of on-demand BMP loads), and keep layering entities/UI (inventory, shops, spellbooks) on top of the renderer while preserving the debug overlays/panels for validation.

## Workstreams & milestones

### 1. Asset + data pipeline
- Reverse engineer Delphi resource loaders (graphics, animations, map definitions) and describe each format in `Docs/Formats/*.md`. **Status:** ✅ Complete extraction suite implemented under `Tools/LegacyDataExtractor`; integration plan documented in `Docs/Formats/IntegrationPlan.md`.
- Build CLI converters (could be dotnet tools or scripts) to emit MonoGame-friendly formats (PNG, JSON, TMX, etc.). **Status:** ⏳ runtime loads BMPs directly from `Original Pascal/Laa/grf` (or `content/graphics`); migration to packaged atlases still pending.
- Extend `Content/Content.mgcb` with folders per asset type and integrate into CI so `dotnet build` fails on missing content. **Status:** ⏳.

### 2. Engine foundation
- Break the new solution into projects (`Client`, `Core`, `Infrastructure`) to separate UI/gameplay logic from platform glue. **Status:** 🛠️ `Laa.Content.Core` + `Laa.Content.Json` created, referenced by `Laa.Monogame.Client`.
- Create subsystems for input, timing, and viewport scaling to replicate the deterministic feel of the 2D MMORPG. **Status:** 🛠️ basic camera + overlay/data controls exist (WASD/arrow movement, mouse wheel/`+`/`-` zoom, overlay toggles via `Tab`/number keys, map cycling with `[`/`]`, HUD toggle via `F1`, repository panels via `I`/`S`/`M`/`C`, world panel via `F2`); formal input abstractions and deterministic ticking still pending.
- Provide service interfaces (e.g., `IGameStateService`, `INetworkClient`) so future unit tests can mock them. **Status:** ⏳.

### 3. Gameplay systems
- Port the following modules iteratively, verifying behavior against the original client:
  1. Character creation/login UI.
  2. Map streaming + entity interpolation.
  3. Combat/skills casting loop.
  4. Inventory, crafting, and merchant dialogs.
  5. Chat, party, and guild UX.
- Each module gets a design note (problem statement, Pascal references, and chosen C# structure).

### 4. Networking
- Document packet formats from the Pascal client (`Docs/Networking/Protocol.md`). **Status:** ✅ initial opcodes captured; keep expanding as we port features.
- Implement a .NET socket client with pluggable encryption/compression so it can talk to the existing server before any server rewrite.  
  **Progress:** `ServerCommandDecoder` now understands the sprite chat opcodes (`h`/`H`), the global info packets (`i`), los reportes de combate (`d`/`D`/`C`) y los efectos (`S`/`=`/`s`). `Game1` enruta todo al HUD a través del nuevo `ServerMessageCatalog` (respetando los mismos textos en español) y dispara destellos en el mapa con `WorldEffectSystem` para visualizar los FX que antes sólo existían en el cliente Delphi.  
  **Networking progress:**  
  1. **Handshake + login envelope – ✅** `TcpNetworkClient` ahora espera el desafío de 4 bytes del servidor y envía exactamente los mismos bloques que `TratarIniciarSesion` (handshake, `VersionLA`, login/creación con `EmpaquetarPassword`), así que el servidor recibe un paquete válido apenas se abre el socket. Las credenciales se leen de `LegacyLoginConfig.json`, que se genera automáticamente junto al ejecutable si no existe.  
  2. **Outbound command writers – ⏳** Serialize keyboard/mouse actions (`m/M`, `A/B`, `Y/y`, `j`, `KG/KS/Ki`, etc.) with little-endian helpers so every gameplay input can be forwarded.  
  3. **Equipment/inventory sync – ⏳** Decode opcodes `#208..#245`, `#0..#7`, `b`, `#192..#207` to keep the HUD slots aligned with the authoritative server state.  
  4. **Local avatar controller – ⏳** Translate MonoGame input into outgoing packets and update the predicted player position (currently we only log `LocalPlayerPositionCommand`).  
  5. **Session lifecycle – ⏳** Handle disconnects/timeouts, `/agr` mode toggles, error opcodes (`i/#8`, `I/#0..#24`) and multi-session guardrails so the client recovers gracefully.
- Abstract serialization/deserialization to isolate endianness and versioning concerns.

### 5. Tooling + QA
- Recreate essential editors (maps, sprites) either as MonoGame tools or web apps once the core client stabilizes.
- Add automated regression tests around data conversions and deterministic gameplay pieces (e.g., combat formulas).
- Establish GitHub Actions workflow that restores NuGet, builds, and runs tests on push.

## Technical considerations

- **Coordinate system**: adopt top-left origin with integer pixel units to match the original assets; document conversions anywhere floating-point math is introduced.
- **Localization**: even though code switches to English identifiers, keep Rune/Spanish text assets externalized and UTF-8 encoded.
- **Input**: unify keyboard + mouse events via MonoGame's `KeyboardState`/`MouseState`, but expose them through engine interfaces for future controller/mobile support.
- **Performance**: batch draw calls aggressively, leverage `SpriteSortMode.Deferred`, and consider render targets for lighting/post effects once parity is achieved.
- **Testing**: create headless tests for content parsers and combat rules; use MonoGame's ability to run off-screen when collecting reference screenshots.

## Risks & mitigations

- **NuGet/network access** – Document commands and keep `NuGet.config` customizable so contributors behind firewalls can restore packages via an internal feed.
- **Legacy unknowns** – Some Delphi behaviors may rely on undefined order or Windows messages; capture findings early in Docs to avoid rewrites.
- **Server protocol drift** – Until the server gets modernized, the client must mimic the exact packet structure; prioritize protocol documentation before touching networking.

## Immediate next steps

1. Flesh out MonoGame content structure (fonts, textures, atlases) and wire a texture manager so JSON data can drive real sprites.
2. Finish integrating the pseudo-mosaic renderer with future atlas builds (pre-baked overlays, mip-aware blending) now that `ti.bmp` masks and the 4× upscaler mirror Delphi behavior.
3. Leverage the new repositories/`ContentContext` in `Laa.Monogame.Client` to bake the oc.b-driven textures into atlases (so on-demand BMP loads are no longer required) and hook up inventory/shop/spellbook overlays for schema validation.
4. Expand `Docs/Networking/Protocol.md` as more opcodes are decoded and define corresponding C# packet types/interfaces.
5. Decide on serialization format (JSON vs. binary) for translated data tables and create adapters accordingly (plan how exporters integrate with build/patch pipeline).
6. Add regression tests for the extraction suite (checksum validation, record counts) and wire them into CI so data drift is caught automatically.

### Upcoming UI & gameplay milestones

1. **Overlay framework** – ✅ base window/labelling system now lives inside the MonoGame client (press `F3` to toggle the roadmap panel) so every future screen reuses the same plumbing.
2. **Inventory window** – press `F4` to toggle a window that now mimics the legacy layout: equipment slots on the left (helmet/armor/weapon/etc.) and a backpack grid on the right, both populated with names from `ItemDocument`. Icons/dragging/tooltips will hook in once the runtime exposes real inventory state.
3. **Spellbook window** – the new `B` toggle opens a grouped spell list that pulls mana/level requirements from `SpellDocument`. Future iterations will attach icons and allow filtering/hotkey binding per school.
4. **Merchant/shop window** – the `V` toggle previews the vendor inventories extracted via `CommerceDocument` (name + price from `ItemDocument`), and the `,`/`.` keys cycle through each merchant definition. Upcoming work: distinguish vendor types, add buy/sell workflow, and show gold + availability.
5. **HUD inferior clásico** – la barra completa (`fondo.bmp`) ya se ancla al borde inferior y ahora incluye el minimapa dibujado con la paleta original (`Mapa.pal`) y el resaltado verde, además de las barras de vida/maná en sus marcos superiores (usando el sprite `barra`). El panel central sigue mostrando metadatos del mapa/overlay y el log desplazable de mensajes, y cada subsección (minimap, stats, mensajes, moneda) mantiene los offsets del cliente Delphi para ir reemplazando placeholders con datos reales en cuanto llegue el networking. El bloque derecho dejó atrás los textos provisionales: `HudPaperDollWidget` dibuja los slots fijos (`CrdndDstnObjts`) más la grilla de inventario (6×3) usando los iconos de `obj.jpg` y admite scroll con la rueda (filas completas) al igual que la pestaña de runas (`cjr.jpg`). Los tabs “Inventario / Hechizos” reaccionan al cursor, la selección de runas actualiza el texto “Hechizo: …” y la rueda no vuelve a afectar al zoom mientras estamos sobre la UI. Finalmente, el bloque «Menú» ya coloca un rostro real (`ros.jpg`), reintroduce los textos de clase/atributos/habilidades, muestra los botones rápidos (puño, arma, runa) y ahora despliega el detalle real de cada selección: costo/nivel/peso, daño/resistencias, restricciones por raza/clase y la descripción de maná/atributos para los hechizos. Los quick slots también quedaron funcionales: al hacer clic en puño/arma/runa se prepara la acción correspondiente, el estado se refleja en el HUD y el retrato permite rotar entre los rostros disponibles. Además, todos los labels/barras del HUD ya leen de un `PlayerState` centralizado (HP/MP/Comida/Oro, stats básicos y líneas de combate), así que en cuanto el networking envíe datos podremos poblarlos sin tocar la UI. Para que la interfaz muestre los textos en español sin llenar la pantalla de “???”, el `DebugTextRenderer` ahora incluye minúsculas, la ñ/Ñ, vocales acentuadas (mayúsculas/minúsculas), diéresis, signos de apertura (`¡`, `¿`), letras con cedilla y símbolos habituales del HUD (coma, paréntesis, porcentaje, #, etc.). Finalmente, el `ServerCommandDecoder` validado ahora descarta opcodes desconocidos, impone límites a los lotes y reporta errores a la consola/HUD para que los paquetes corruptos no bloqueen la cola de red.

Press `F3` to show/hide the roadmap panel and `F4` for the mock inventory grid—handy while iterating on layout before wiring real inventory state.
