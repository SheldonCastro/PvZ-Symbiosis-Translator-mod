# Language packs

Each locale is an independent folder under `PvZ_Symbiosis_Translator/Localization` in source, or `Mods/PvZ_Symbiosis_Translator/Localization` when installed. PT-BR is the most developed pack; other languages use the same format.

## Add a language

Copy `_template` to a locale folder such as `es-ES`. Edit `manifest.json`:

```json
{
  "locale": "es-ES",
  "displayName": "Español",
  "sourceLocale": "zh-CN",
  "version": "1.0.0",
  "gameVersion": "1.2.0"
}
```

`locale` must match the folder name. Use `sourceLocale: "zh-CN"`; source keys always match the original Chinese game text. A `supportedGameVersions` array can replace `gameVersion` when the pack has been tested on several versions.

Select the pack in Settings, or set `language` in the installed `Config/translation_config.json` and press `PageUp`. No DLL rebuild is needed.

## Files

| File | Purpose |
| --- | --- |
| `Strings/exact.json` | General source-to-target mappings |
| `Almanac/exact.json` | Almanac mappings, loaded into the same exact store |
| `Strings/context_overrides.json` | Exact mappings for a specific scene/hierarchy context |
| `Strings/dynamic_rules.json` | Whole-string rules for variable text |
| `ModStrings.json` | Translator UI labels, keyed by label ID |
| `glossary.json` | Terminology guidance; no runtime word replacement |
| `Fonts/manifest.json` | Font files, priority, and replacement/fallback mode |
| `Textures/manifest.json` | Optional explicit texture mappings |
| `Audio/manifest.json` | Experimental audio mappings |

Keep the template's files even when their objects or arrays are empty. Preserve ModStrings keys while translating their values. PT-BR contributors should follow the [style guide](PTBR_STYLE_GUIDE.md) and the maintained `pt-BR/glossary.json`.

## Exact strings and context

Exact files are JSON objects mapping original source strings to translations. Matching is ordinal and case-sensitive: spaces, punctuation, tags, and line breaks in the key are significant. Do not trim or normalize them.

```json
{
  "图鉴": "Almanaque",
  "数值 {0}": "Valor {0}"
}
```

An empty target (`""`) deliberately hides text and produces a warning; whitespace-only targets are rejected. Conflicting duplicates are reported, and the first valid mapping wins. Empty organizational section markers in the maintained exact file are not translations.

Context overrides are objects keyed by the exact context captured in runtime exports. Each contains its own source-to-target object. Copy the observed context rather than guessing a hierarchy path. Lookup checks the current context before the global exact store, then tries dynamic rules. Unmatched text stays in the original language; the runtime does not silently substitute another language pack.

## Dynamic text

Use dynamic rules only for genuinely variable text. Each entry has `id`, `pattern`, and `target`:

```json
[
  {
    "id": "sun-counter",
    "pattern": "^阳光：(?<amount>[0-9]+)$",
    "target": "Sol: ${amount}"
  }
]
```

This is a format example, not a request to add a rule to PT-BR. Patterns must be anchored with `^` and `$`, use named captures, and match the whole input. Targets reference those captures as `${name}`. Rules have a 20 ms regex timeout and run only after exact lookup fails. Keep the rule narrow and verify actual game examples.

## Placeholders and formatting

Preserve functional placeholders exactly, including their occurrence counts: `{0:N2}` is different from `{0}`, and `%d` is different from `%s`. Keep gameplay values unchanged. Target punctuation, line breaks, and supported TMP formatting can differ from the source. See [Rich text](RICH_TEXT_TRANSLATION.md) for examples and renderer limits.

## Fonts

The public PT-BR pack supplies Noto Sans as a fallback, with its OFL license. A font entry uses `id`, relative `file`, `license`, `priority`, and `applyMode` (`fallback` or `replace`). Higher priorities are considered first. Keep files and license notices inside `Fonts/`.

Test accents, missing glyphs, clipping, and scene changes in-game. A font that loads successfully may still render poorly at the game's sizes. See [Third-party notices](../THIRD_PARTY.md) before adding a font to a contribution.

## Validate and preview

For PT-BR, run from the repository root:

```powershell
dotnet run --project Tools/PackValidator -c Release -- ./PvZ_Symbiosis_Translator/Localization/pt-BR pt-BR 1.2.0
```

Substitute your folder, locale, and game version for another pack. Inspect `rejected`, `warnings`, and `issues`: entry errors can produce `PARTIAL` with exit code zero. Structural failures, such as malformed JSON or an incompatible manifest, stop loading.

Use Settings > QA for additional checks, then press `PageUp` to reload and inspect the affected screens. `Insert` compares the translation with the original. Automatic reload is optional; enable it in Settings while editing. Automated checks cannot judge wording or prove that every screen fits.

For texture work, follow [Textures](TEXTURES.md). Audio remains experimental; its runtime outline is in [Architecture](ARCHITECTURE.md#fonts-and-optional-assets).
