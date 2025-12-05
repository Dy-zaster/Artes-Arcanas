# Binary Data Tables (`bin/*.b`, `bin/std.mon`)

The client boot sequence (`Juego.pas:381`) loads a series of binary tables before rendering anything. These blobs are plain dumps of Delphi records and can be deserialized directly with little-endian readers.

## Monsters (`std.mon`)

Loaded via `Demonios.InicializarMonstruos`. The file is a sequence of `TDescripcionMonstruoYTipo` records (`Demonios.pas:536`):

```pascal
TDescripcionMonstruoYTipo = packed record
  tipoMonstruo: byte;
  descripcion: TDescripcionMonstruo;
end;
```

`TDescripcionMonstruo` spans ~80 bytes and includes stats, AI behavior (`Comportamiento`), loot tables, resistances, attack definitions (`TDanno[0..1]`), animation style, and visual metadata. The converter should build a JSON entry per monster id with those fields so the MonoGame client can instantiate NPCs deterministically.

## Attack & animation mappings (`mp_ataq.b`, `mp_anim.b`)

- `mp_ataq.b` → `TInformacionDeMapeoDeAtaques` (`Demonios.pas:581`): 4096 entries covering 32 armor pieces × 8 classes × 8 races × 2 genders. Each entry stores `ConArmas` plus padding used to determine which attack animation to play for a given avatar equipment setup.
- `mp_anim.b` → `TInformacionDeMapeoDeAnimaciones` (`Demonios.pas:582`): 4096 bytes that map the same index space to animation ids. These values feed `InfMapeoAnimaciones` so the renderer knows which spritesheet to select based on the player's armor combination.

For the port we can remap those arrays into higher-level descriptors such as `{ armorId, classId, raceId, gender, animationId, attackStyle }`.

## Items & spells (`obj.b`, `cjr.b`)

`Objetos.pas` defines:

```pascal
TArchivoObjetos = record
  Nombre: TNombresObjetos;    // 256 × string[31]
  Datos:  TDescriptoresObjetos; // 256 × TDescriptorObjeto
  CheckSum: integer;
end;

TArchivoConjuros = record
  Nombre: TNombresConjuros;      // 32 × string[23]
  Datos:  TDescriptoresConjuros; // 32 × TDescriptorConjuro
  CheckSum: integer;
end;
```

Each descriptor contains costs, damage types, crafting requirements, race/class restrictions, spell schools, and UI icon ids. Export both blobs to human-readable manifests (`items.json`, `spells.json`). That data will later hydrate MonoGame UI panels (inventory, crafting, hotbars) without referencing Delphi code.

## Commerce tables (`comercio.b`)

Editors under `Original Pascal/EditorDeComerciantes` read `bin/comercio.b` as `TArchivoComercios` (`Objetos.pas:200`). It contains 32 `TInventarioArtefactos` arrays (each 30 artifact slots). While the client relies on per-map merchants embedded in `.mpv`, `comercio.b` still provides the canonical item pools for generic shops. To preserve compatibility we should deserialize this file as well and expose it to the networking layer when negotiating merchant inventories.

## Graphic descriptors (`oc.b`)

Even though `oc.b` is referenced from `Tablero`, it acts like a data table: names + `TDescriptorGrafico` entries. Section [Graphics & Animations](Graphics.md) covers the rendering implications. From a tooling perspective we treat it like the other `.b` files—read entire arrays and re-emit them as JSON.

## Conversion checklist

- [x] Implement a reusable `BinaryReader` helper in C# that mirrors Delphi's packed record layout (little-endian, 1-byte string length prefixes).
- [x] Create DTOs for `ItemDescriptor`, `SpellDescriptor`, commerce inventories, monster descriptors, mapping tables, and static graphic descriptors, plus CLI commands (`dotnet run -- items …`, `-- spells …`, `-- commerce …`, `-- monsters …`, `-- attackmap …`, `-- animmap …`, `-- graphics …`) that emit JSON snapshots (see `Docs/Formats/Samples/*.json`).
- [ ] Hook up regression tests to validate the exported data (monster counts, checksum verification, etc.).
- [ ] Write regression tests that deserialize the existing binaries and verify sample values against the Delphi structures (e.g., monster names, item costs) to catch byte-order regressions.
- [ ] Store SHA-1 hashes of each source file next to the exported JSON so future contributors can tell when the legacy data changed.
