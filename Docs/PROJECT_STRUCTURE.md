# Project Structure

| Path | Purpose |
| --- | --- |
| `PvZ_Symbiosis_Translator/` | Maintained mod source and localization sources |
| `Tests/` | Dependency-light executable test harness |
| `Tools/PackValidator/` | Offline language-pack validator |
| `Tools/TranslationExport/` | Translation export/audit tool |
| `Tools/AssetExtractor/` | User-run offline extractor; generated output stays local |
| `Tools/NativeUiCatalog/` | Private native UI analysis tool and reference output |
| `Tools/PublicRelease/` | Public snapshot preparation and validation |
| `Docs/` | Current documentation plus clearly identified historical reference |
| `work/`, `bin/`, `obj/`, `dist/` | Generated/untracked state |

The source tree is authoritative. The sibling deployed `Mods/PvZ_Symbiosis_Translator` directory is a runtime installation and may contain user edits; it is never copied back or published wholesale.
