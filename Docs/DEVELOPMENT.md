# Development

## Setup and build

Install a .NET SDK that can target .NET 6 and the .NET 6 runtime. Use PowerShell for the scripts below. You also need your own game installation matching the [supported baseline](../README.md#installation). Launch it with MelonLoader once to generate `MelonLoader/Il2CppAssemblies`.

Clone your fork anywhere, then run from its root:

```powershell
./build.ps1 -Configuration Release -GameDir 'C:\Games\PvZ_Symbiosis'
```

This checks the zero-Canvas rule, builds the mod, runs the test harness, and checks the PT-BR pack. The DLL is written to `PvZ_Symbiosis_Translator/bin/Release/net6.0/PvZ_Symbiosis_Translator.dll`.

The project references MelonLoader, Harmony, Il2CppInterop, Newtonsoft.Json, Unity, and generated game assemblies from `GameDir`. References use `Private=false` so these dependencies are not copied into the mod output. If a reference is missing, check that MelonLoader's first launch completed and that `GameDir` points to the folder containing `PVZGS.exe`.

## Repository layout

| Path | Contents |
| --- | --- |
| `PvZ_Symbiosis_Translator/` | Mod source, configuration, and language packs |
| `Tests/` | Executable test harness; no game installation needed |
| `Tools/PackValidator/` | Offline language-pack checks |
| `Tools/TranslationExport/` | Export recorded text and inspect dynamic rules |
| `Tools/AssetExtractor/` | Offline texture/sprite authoring utility |
| `Docs/` | Contributor guides |

## Tests and pack checks

Run the checks that do not need the game:

```powershell
./Tools/check-zero-canvas.ps1
dotnet run --project Tests -c Release -- ./PvZ_Symbiosis_Translator/Localization/pt-BR
dotnet run --project Tools/PackValidator -c Release -- ./PvZ_Symbiosis_Translator/Localization/pt-BR pt-BR 1.2.0
```

The test harness returns a failure exit code when a test fails. PackValidator can return success with `PARTIAL` status when it skips invalid entries: read its `rejected`, `warnings`, and `issues` fields as well as its exit code. It checks pack loading and referenced assets; full texture decoding and additional editorial checks are available in the in-game QA page.

CI runs the static guard and the C# harness. The runtime DLL requires local game references, so a passing CI run does not replace a local build or gameplay testing.

## Local deployment

Close the game, then run:

```powershell
./deploy.ps1 -GameDir 'C:\Games\PvZ_Symbiosis'
```

The script builds first and backs up the installed mod under `<game>/BACKUPS`. It stops if the installed PT-BR exact dictionaries or ModStrings differ from source, so review those differences before deploying. Other differing user files are preserved and listed in `preserved-conflicts.txt` in the backup. The DLL hash and installed packs are checked afterward.

For changes to Harmony hooks, fonts, textures, or native UI, test the affected screen in the game. Check scene changes, reopening Settings, translation toggling, and reload as relevant. Record which checks you actually ran in the PR.

## QA, diagnostics, and exports

**QA** checks localization data: JSON, placeholders, markup, terminology risks, and optional assets. **Diagnostics** checks runtime integration and state, including hooks, fonts, reload, and the translator's UI objects. Neither proves complete translation coverage or correct visual layout.

Use the QA and Diagnostics pages in Settings to inspect or export reports. `PageDown` exports tracked original text and diagnostics under the installed mod folder. Translator Mode provides context and pending translations; suggestions are not installed automatically. Remove personal paths and unrelated data before sharing logs or reports.

To regenerate exports from previously captured data:

```powershell
dotnet run --project Tools/TranslationExport -c Release -- '<game>/Mods/PvZ_Symbiosis_Translator' pt-BR
dotnet run --project Tools/TranslationExport -c Release -- --audit-dynamic ./PvZ_Symbiosis_Translator/Localization/pt-BR
```

The first command reads recorded dumps, not the running game. The second lists exact entries that overlap dynamic rules so a translator can review them. See [AssetExtractor](../Tools/AssetExtractor/README.md) for offline image extraction.

## Working on a change

Keep PRs focused and add tests for changed behavior. Preserve exact source matching and the existing game Canvas: the mod clones native controls and never creates a Canvas of its own. Keep generated files, local references, and game dumps outside tracked source.

Update [CHANGELOG.md](../CHANGELOG.md) for user-visible changes. Version changes belong with a release; documentation and cleanup work do not need a version bump. See [Architecture](ARCHITECTURE.md) before changing object ownership or reload behavior.
