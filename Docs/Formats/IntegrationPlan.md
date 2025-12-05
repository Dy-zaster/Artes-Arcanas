# MonoGame Integration Plan for Legacy Data

The extraction suite (`Tools/LegacyDataExtractor`) now emits JSON snapshots for every legacy asset: maps, items, spells, commerce tables, monsters, attack/animation mappings, and static graphic descriptors. The next milestone is to consume these files inside the new MonoGame solution while keeping the data pipeline modular and testable.

## Data layer layout

1. **Content assemblies**
   - `Laa.Content.Core`: shared DTOs that mirror the JSON structure (entities, loot tables, map layers).
   - `Laa.Content.Json`: JSON loaders + validation (checksums, counts) + caching. This library references `System.Text.Json` only and exposes repositories via interfaces (`IMapRepository`, `IItemRepository`, `IMonsterRepository`, etc.).
   - `Laa.Content.Tools`: optional command-line harness for re-exporting or validating assets locally/CI.

2. **Runtime usage**
   - The MonoGame client references `Laa.Content.Core` + `Laa.Content.Json` and consumes repositories via dependency injection (or a simple service locator during bootstrap).
   - JSON files live under `MonoGameClient/content/data/` (or similar). For initial prototyping, point the repositories directly to `Docs/Formats/Samples/*.json` to avoid duplication.

## Loading order prototype

1. **Boot services**
   ```csharp
   var contentRoot = Path.Combine(AppContext.BaseDirectory, "content", "data");
   var validators = new ExtractionValidators(); // optional checks
   var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

   var mapRepo = new JsonMapRepository(Path.Combine(contentRoot, "maps"), jsonOptions, validators);
   var itemRepo = new JsonItemRepository(Path.Combine(contentRoot, "items.json"), jsonOptions, validators);
   var spellRepo = new JsonSpellRepository(Path.Combine(contentRoot, "spells.json"), jsonOptions, validators);
   var monsterRepo = new JsonMonsterRepository(Path.Combine(contentRoot, "monsters.json"), jsonOptions, validators);
   var attackMapRepo = new JsonAttackMappingRepository(Path.Combine(contentRoot, "attack_map.json"));
   var animMapRepo = new JsonAnimationMappingRepository(Path.Combine(contentRoot, "anim_map.json"));
   var graphicsRepo = new JsonGraphicRepository(Path.Combine(contentRoot, "graphics.json"));
   ```

2. **Tile rendering**
   - `JsonMapRepository` reads `map_0.json` and exposes layers (terrain, graphics). Feed the graphic descriptors into a texture atlas builder that slices the BMP assets (later replaced by atlases exported from the `grf/` folder).

3. **Inventory/shop UI**
   - Combine item descriptors + commerce inventories: the shop UI pulls `CommerceFile` entries and resolves each `ArtefactSlot` through `IItemRepository` to render names, icons, modifiers.

4. **Spellbook UI**
   - `ISpellRepository` exposes spell descriptors; the GUI groups them by school, required level, etc., referencing the exported icon IDs.

5. **Monster viewer / combat logic**
   - `IMonsterRepository` provides attack stats, loot modifiers, animation styles. Attack/animation map repositories translate player armor/weapon combos to sprite indices.

## Validation & regression tests

- Before loading JSON, the repositories should run basic assertions:
  - `Checksum` fields (items, spells, graphics) must match the values exported from the Delphi files.
  - Record counts should match expectations (e.g., `monsters.Count == 184`).
  - Optionally, store hash-of-file metadata in `Docs/Formats/Samples/*.json.meta` to detect stale data.
- Add a `dotnet test` project that exercises `JsonXRepository` classes against the sample JSON to ensure the schema stays aligned.

## Next engineering tasks

1. **Create `Laa.Content` solution structure** in `MonoGameClient` with the repositories outlined above. *(Done: `Laa.Content.Core` + `Laa.Content.Json` expose DTOs/repositories and `ContentContext` wires them up inside the client.)*
2. **Wire the repositories into the MonoGame bootstrap** (replace the placeholder `Game1` logic with a scene that can display map tiles using the data layer).
3. **Add automated validation**: a test project that loads every JSON sample and asserts counts/checksums; include it in CI before we rely on the data.
4. **Plan asset packaging**: decide where to store the JSON exports in the final build (likely under `Content/Data` and processed via the MonoGame content pipeline or raw file copy).
