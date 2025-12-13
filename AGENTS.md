# Repository Guidelines

## Project Structure & Module Organization

- `Main.cs`: primary Harmony patches, gameplay hooks, and high-level multiplayer flow.
- `Packets/`: network message types and handlers (client/server). Subfolders group by domain (e.g., `Lobby`, `Game`, `State`, `Handlers`).
- `LoadSaveOverrides/`: multiplayer-aware save/load containers and BinaryFormatter binder.
- `StateManagement/`: observer-based state syncing (e.g., building state updates).
- `ServerBrowser/`, `ServerLobby/`, `UI/`: menu screens, lobby UI, and related scripts/prefabs glue.
- `Riptide/`, `RiptideSteamTransport/`: networking and Steam transport integration.
- `Enums/`, `Constants.cs`, `ErrorCodeMessages.cs`, `ReflectionHelper/`: shared types/utilities.

## Build, Test, and Development Commands

This mod is typically compiled/loaded by the game’s mod loader (there is no `.csproj` here).

- Validate changes quickly: `rg -n "TODO|FIXME|throw|NotImplementedException" -S .`
- Inspect recent log output: `Get-Content .\\output.txt -Tail 200`
- Check history/context: `git log -n 20 --oneline`

To run locally, copy/enable the mod in *Kingdoms and Castles* and **fully restart the game** after changes. Keep host/client mod versions identical.

## Coding Style & Naming Conventions

- Language: C# (Unity/Mono). Prefer conservative language features to avoid in-game compiler issues.
- Indentation: 4 spaces; braces on new lines (match existing files).
- Names: `PascalCase` for types/methods, `camelCase` for locals/fields. Packet properties are public and serialized—treat renames as breaking changes.
- Logging: use `Main.helper.Log(...)` with short, searchable messages.

## Testing Guidelines

No automated test suite. Verify in-game with a minimal repro:

- Host ↔ join, place buildings, save/load, leave/rejoin, and confirm sync.
- When reporting bugs, include `output.txt` excerpts around the first exception and “Save Transfer” markers.

## Commit & Pull Request Guidelines

Git history uses short, informal summaries. For contributions:

- Commits: one-line, descriptive, avoid profanity; include a scope when helpful (e.g., `save: fix load fallback`).
- PRs: describe the issue, repro steps, expected vs actual, and attach relevant `output.txt` snippets. Note game version, mod version, and whether it’s Workshop or local mod folder.

## Agent-Specific Notes

- Avoid edits that depend on newer C# syntax not supported by the runtime compiler.
- Prefer small, isolated fixes; multiplayer regressions are easy to introduce—add logs around save/load and connect/disconnect paths.
