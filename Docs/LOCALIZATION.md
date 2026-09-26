# Localization

The project is MultiLang: each locale is a self-contained directory below `Localization/<locale>`. PT-BR is currently the primary and most complete pack, not an architectural limit. Copy `_template` to start another language and update its manifest. `locale` must match the folder, `sourceLocale` is `zh-CN`, and the tested game version must be declared.

## Files

- `Strings/exact.json`: general exact mappings and empty organizational markers
- `Almanac/exact.json`: Almanac exact mappings
- `Strings/context_overrides.json`: context-specific exact mappings
- `Strings/dynamic_rules.json`: anchored full-string regex rules with named captures
- `ModStrings.json`: translator UI labels
- `glossary.json`: editorial guidance
- optional `Fonts`, `Textures`, and `Audio` manifests/assets

Matching is ordinal and case-sensitive. Do not trim or normalize sources. Preserve functional placeholder identity and occurrence counts. Target line breaks, punctuation, and supported TMP markup may differ. Empty targets intentionally hide text and are reported.

Validate with `Tools/PackValidator`, run full QA in the native Settings UI, then validate the actual screen. Automated checks establish technical integrity, not translation quality or complete coverage.
