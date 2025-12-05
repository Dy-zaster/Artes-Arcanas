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
| 2. Core systems | Rendering & content | 🛠️ In progress | Content layer scaffolding plus a camera-driven tile renderer that now consumes the legacy `terreno.jpg` sheet (from atlases or raw files), animates water/lava tiles, and adds interim gradient edge blending; the oc.b-driven static graphic loader and debug overlays/panels are in place (`Laa.Content.Core` + `Laa.Content.Json` + `ContentContext` feed the MonoGame client); next up is tackling the true pseudo-mosaic edge logic and expanding the scene graph/input abstractions. |
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
2. Replace the temporary gradient blending with a faithful port of the Delphi pseudo-mosaic system (`MZ_h`, `MZ_v`, `MZ_si`, etc.) so tile transitions match the original renderer.
3. Leverage the new repositories/`ContentContext` in `Laa.Monogame.Client` to bake the oc.b-driven textures into atlases (so on-demand BMP loads are no longer required) and hook up inventory/shop/spellbook overlays for schema validation.
4. Expand `Docs/Networking/Protocol.md` as more opcodes are decoded and define corresponding C# packet types/interfaces.
5. Decide on serialization format (JSON vs. binary) for translated data tables and create adapters accordingly (plan how exporters integrate with build/patch pipeline).
6. Add regression tests for the extraction suite (checksum validation, record counts) and wire them into CI so data drift is caught automatically.
