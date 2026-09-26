# Rich text for translators

Translation values can carry their own TextMeshPro (TMP) presentation. The original Chinese key stays **exactly** as observed; its tags, spaces, punctuation and line breaks are part of the lookup identity. The Portuguese target can use a different layout, color, size, emphasis or line count. Formatting belongs directly in `Strings/exact.json`, `Almanac/exact.json` and `Strings/context_overrides.json`; no separate formatting file is required.

```json
{
  "全部禁用": "<size=75%>Desativar tudo</size>",
  "图鉴": "<color=#FFD700><size=110%><b>Almanaque</b></size></color>",
  "确定": "<align=center><cspace=-0.05em>Confirmar</cspace></align>"
}
```

The source does not need matching tags. A source such as `<color=#F81000>伤害：</color>` may become `<b><color=#00FF00>Dano:</color></b>` or simply `Dano:`. Newlines can be added, removed or reorganized. `20*18点/2.5秒。` may become `20 × 18 de dano/2,5 s.`. Numbers are reviewed through nonblocking warnings when they look changed; decimal comma and multiplication symbol changes are supported.

## TMP formatting reference

TMP is the final renderer. The mod passes target text through rather than translating or normalizing markup. Common categories include:

| Purpose | Example |
| --- | --- |
| Size and relative size | `<size=75%>Texto</size>`, `<size=+4>Texto</size>`, `<size=-2>Texto</size>`, `<size=1.2em>Texto</size>` |
| Color and alpha | `<color=#FF0000>Texto</color>`, `<color=#FF000080>Texto</color>`, `<alpha=#80>Texto` |
| Emphasis | `<b>Negrito</b>`, `<i>Itálico</i>`, `<u>Sublinhado</u>`, `<s>Riscado</s>` |
| Case and weight | `<uppercase>ABC</uppercase>`, `<lowercase>abc</lowercase>`, `<smallcaps>Texto</smallcaps>`, `<font-weight=700>Texto</font-weight>` |
| Alignment and spacing | `<align=center>Texto</align>`, `<cspace=-0.05em>Texto</cspace>`, `<mspace=1em>Texto</mspace>`, `<space=1em>` |
| Position and offset | `<voffset=0.1em>Texto</voffset>`, `<pos=10%>Texto</pos>`, `<indent=1em>Texto</indent>`, `<line-indent=1em>Texto</line-indent>` |
| Flow and width | `<line-height=120%>Texto</line-height>`, `<margin=1em>Texto</margin>`, `<width=80%>Texto</width>`, `<nobr>Sem quebra</nobr>`, `<page>` |
| Highlight and baseline | `<mark=#FFFF0080>Texto</mark>`, `<sup>2</sup>`, `<sub>2</sub>` |
| Assets and interactions | `<font="name">Texto</font>`, `<gradient="name">Texto</gradient>`, `<sprite name="icon">`, `<link="id">Texto</link>`, `<style="name">Texto</style>` |
| Other presentation | `<rotate=10>Texto</rotate>`, `<br>`, `<noparse><size=999>literal</size></noparse>` |

Nest paired tags in the order you want TMP to apply them, for example `<color=#7CFF00><b>Plantar!</b></color>`. `<noparse>...</noparse>` makes enclosed angle brackets literal; the advisory parser does not treat its contents as active tags. State-style or newer tags may be accepted by TMP even if the advisory check flags unusual syntax. The available font, gradient, sprite and style names depend on the actual game component and assets. Preview these in the game before relying on them.

## Functional placeholders

Placeholders carry runtime values and remain strict. Preserve their exact identity and occurrence count:

```json
{
  "数值 {0}": "<size=80%>Valor {0}</size>",
  "生命：{0:N2}": "Vida: {0:N2}",
  "得分 %d": "Pontos %d"
}
```

Dropping `{0}`, changing `{0:N2}` to `{0}` or introducing an extra `%s` rejects **that entry only**. The rest of the locale still loads. Literal numbers such as `2.5` are not placeholders. A source `50%` followed by ordinary prose is treated as a percentage rather than `%d` formatting. For dynamic rules, preserve each named group exactly once, for example `<color=#FFD700>Sol: ${amount}</color>` with an anchored pattern containing `(?<amount>...)`.

An empty target string `""` is allowed to intentionally hide text and generates a warning. A whitespace-only target is rejected; use `""` when hiding is intentional. Unknown sources still display their original text. A rejected mapping also falls back to its original source unless another valid mapping provides that exact key.

## Edit and reload

1. Find the exact original source in the Translator page exports or the aggregated files under `TranslationExport`. Keep the JSON key unchanged.
2. Edit only the target value in the active locale's `Strings/exact.json` or its context/Almanac file.
3. Save valid JSON and press **PageUp**. The mod reloads the external pack and refreshes tracked components without rebuilding the DLL.
4. Use **Insert** to compare with the original. Disabling translation restores both original source text and the component's original rich-text setting. Re-enabling applies the target formatting again.
5. Inspect the result in the actual UI, including line breaks, clipping, fonts and any markup-dependent assets. Automated tests validate data flow and component state, not visual appearance.

**PageDown** remains a read-only diagnostics export. Text exports use the tracked original source, so translated markup is not written back as a new Chinese lookup key. Export actions do not extract textures or activate hidden menus.

## Validation and reports

Malformed JSON, required-file failures and invalid manifests are structural errors and can prevent a pack from loading. Individual missing placeholders, null targets, whitespace-only targets or conflicting mappings are recorded as **error** issues and skipped. The first valid mapping for a key wins. Ordinary formatting changes cause no error. Possible number changes and suspicious or unknown TMP markup are **warnings**; they do not block loading. Warnings are heuristics, not linguistic verdicts.

The runtime writes `Mods/PvZ_Symbiosis_Translator/Diagnostics/translation_validation.json` on each pack load. It contains locale, timestamp, loaded/rejected/warning/fatal counts and per-entry source file, source, target, severity and reason. The console prints a concise summary and points to the report when entries are rejected. Detailed per-entry console messages are available through `debugLogging`. `Tools/PackValidator` uses the same checks and reports `PARTIAL` when it isolates bad entries.

The standalone unit tests exercise exact, context and dynamic values, placeholder safety, report contents and reload from an edited scratch pack. An opt-in runtime smoke probe checks TMP and legacy text properties with synthetic translations, and can temporarily test PageUp against a backed-up external pack. See [validation evidence](RICH_TEXT_VALIDATION.md) for the dated test record. Legacy `UnityEngine.UI.Text` has a smaller rich-text vocabulary than TMP; a TMP-specific tag may display literally or be ignored there. The mod enables `supportRichText` when needed and restores its previous value when translation is disabled with Insert, but does not emulate TMP tags for legacy UI.

This guide does not change the user's current pt-BR wording or promise that every TMP feature is available in every game font/material.
