# Data Files

Place the exported JSON files (maps, items, spells, commerce, monsters, mappings, graphics) inside this directory when running the MonoGame client outside of the repository root. During development the client automatically falls back to `Docs/Formats/Samples`, but for packaged builds copy the files here using the following layout:

- `map_*.json` files go directly under `content/data` or in a `content/data/maps/` folder. The runtime only looks for files named `map_<id>.json`.
- All other JSON files (`items.json`, `spells.json`, `commerce.json`, `monsters.json`, `attack_map.json`, `anim_map.json`, `graphics.json`) go directly under `content/data`.

EOF
