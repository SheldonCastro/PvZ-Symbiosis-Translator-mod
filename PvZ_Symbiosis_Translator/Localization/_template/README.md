# External language packs

Copy `Localization/_template` to `Localization/es-ES` (or another locale). Set the matching locale, display name, version, sourceLocale `zh-CN` and gameVersion in manifest.json. Set `language` in the single active file `Mods/PvZ_Symbiosis_Translator/Config/translation_config.json`. Press **PageUp** to reload; **PageDown** exports the current translation diagnostics. No DLL recompilation is needed.

`Strings/exact.json` and `Almanac/exact.json` are JSON objects mapping original source strings to targets. Preserve source whitespace, punctuation, line endings and tags exactly. Targets may use different text, line breaks and TMP markup. An empty target intentionally hides text and produces a warning; a whitespace-only target is rejected. Missing strings remain original. Duplicate conflicting mappings are reported and skipped; the first valid mapping wins. Preserve functional placeholders exactly. Changes to literal numbers produce nonblocking warnings. A glossary is editorial guidance, never substring replacement.

For example, a target may add formatting even when the source has none:

```json
{
  "全部禁用": "<size=75%>Desativar tudo</size>",
  "数值 {0}": "<color=#FFD700>Valor {0}</color>"
}
```

These examples are documentation only; they are not installed as translations. Keep functional placeholders such as `{0}` exactly. See [the rich text guide](../../../Docs/RICH_TEXT_TRANSLATION.md) for warnings, translation toggling with **Insert**, and TMP versus legacy UI limits.

`context_overrides.json`: object keyed by `Scene/Root/Child` hierarchy, each value an exact source-to-target object. These precede ordinary exact lookup. Obtain paths from runtime dumps.

`dynamic_rules.json`: array of objects with `id`, `pattern` and `target`. Only genuine variable texts belong here. Pattern must start with `^`, end with `$`, use named groups and match the entire input. Target uses each group exactly once, such as `${amount}`. Regex has a 20ms timeout and runs after exact lookup. Empty array is valid. No legacy regex is enabled by default.

Fonts/Textures/Audio manifests are arrays. Reserved asset entries use `id` and a relative `file`, validated within their directory. Empty arrays are supported. Asset replacement remains a separate milestone; setting a flag alone does not implement it.

Run `./build.ps1` to build and validate the shipped pack. `./deploy.ps1` refuses deployment while the game is running, backs up the previous mod and preserves existing locale files/configuration. For an existing locale update, review differences before copying changed JSON files. Dependencies and game assets are never deployed.

Translation migration is not linguistic approval. `Docs/migration_review.json` records omitted legacy entries; source keys are never normalized. Old Resources directories are retained as evidence and are no longer loaded.
