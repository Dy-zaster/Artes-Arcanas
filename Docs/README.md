# Laa MonoGame Client Modernization

The legacy LAa client lives under `Original Pascal/Laa` and was written with Delphi 6. This repository now also contains an incremental rewrite that targets MonoGame + .NET 8 to give the project a maintainable and cross-platform future.

## Repository layout

- `Original Pascal/` – historical Pascal/Delphi sources for the client, server, and tooling.
- `MonoGameClient/` – new .NET 8 solution that will host the MonoGame-driven client.
- `Docs/` – planning notes, architecture decisions, and migration status.

## Technology stack

| Concern | Choice | Notes |
| --- | --- | --- |
| Runtime | .NET 8 | LTS, modern tooling, good AOT story | 
| Game framework | MonoGame DesktopGL | Cross-platform, closest match to original fixed-function style |
| Content pipeline | `MonoGame.Content.Builder.Task` | Enables mgcb-driven asset builds within `dotnet build` |
| Rendering backend | OpenGL (via DesktopGL) | Works across Windows/macOS/Linux without platform-specific forks |

All runtime names, classes, and variables inside the new client are written in English to keep consistency with modern practices and engine documentation.

## Getting started

1. Install the .NET 8 SDK (`dotnet --version` should report 8.x).
2. Install the MonoGame templates locally if you want to scaffold additional projects:
   ```bash
   DOTNET_CLI_HOME="$PWD" dotnet new install MonoGame.Templates.CSharp
   ```
3. Restore and build the new solution (network access to NuGet is required to download the MonoGame packages listed below):
   ```bash
   cd MonoGameClient
   DOTNET_CLI_HOME="$PWD" dotnet restore
   DOTNET_CLI_HOME="$PWD" dotnet build
   ```
4. Run the client:
   ```bash
   cd src/Laa.Monogame.Client
   DOTNET_CLI_HOME="$PWD" dotnet run
   ```
   The prototype opens a 1280×720 window, loads the exported JSON data, and renders the selected legacy map with camera/overlay controls.

## NuGet packages currently referenced

- `MonoGame.Framework.DesktopGL` – main framework runtime used by `Game1` and the platform bootstrapper.
- `MonoGame.Content.Builder.Task` – allows `dotnet build` to compile `Content/Content.mgcb` assets without running the Pipeline GUI.

Additional packages (serialization, networking, UI widgets, etc.) will be introduced as the port progresses and are enumerated in the migration plan.

## Development guidelines

- Keep MonoGame-specific glue isolated under `Laa.Monogame.Client` so future shared libraries can be hosted in sibling projects.
- Favor C#-style naming (PascalCase for types/methods, camelCase for locals/fields with `_` prefix for private fields).
- Mirror the original client's feature set incrementally: first render loop and scene graph, then asset loading, UI, networking, and gameplay systems.
- Document new architectural decisions inside `Docs/` (see `MigrationPlan.md`).

## Current MonoGame client snapshot

The new client already boots with the extracted JSON data and renders the sample maps via a camera-driven tile renderer.

- Use `dotnet run` under `MonoGameClient/src/Laa.Monogame.Client` to launch the prototype scene.
- Pan with `WASD`/arrow keys, zoom with the mouse wheel or `+`/`-`.
- Toggle metadata overlays: `Tab` cycles through sensors → nests → merchants → all → off, number keys `0-4` jump directly to None/Sensors/Nests/Merchants/All.
- Console logs report which map and content payloads were loaded; this helps validate repository wiring before gameplay systems exist.

Refer to `Docs/MigrationPlan.md` for the step-by-step porting strategy.
