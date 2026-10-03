# Architecture

PvZ Symbiosis Translator is a MelonLoader mod for Unity IL2CPP. It reads external language packs and applies translations at runtime without rewriting game archives.

## Startup and text

`PvZSymbiosisTranslatorMod` is the entry point. It resolves the installed mod directory through `ModPaths`, loads configuration and the selected pack, and connects Harmony hooks and scene events.

`LocaleManager` loads exact strings, context overrides, and dynamic rules. `TranslationService` tries a context override first, then a global exact entry, then an enabled full-string dynamic rule. If nothing matches, it returns the original. The glossary guides translators; it never performs substring replacement.

Text hooks handle TextMeshPro and Unity UI Text. The mod retains original text per component so toggling translation is reversible and rendered translations do not become new source keys. Rich-text settings are restored with the original text. Legacy `TextMesh` enumeration is unavailable in this game's generated bindings.

## Fonts and optional assets

`FontStore` loads locale fonts and applies replacements or fallback fonts. It retains the original component font for restoration. Translator UI font selection is independent of the game-text replacement toggle.

`TextureStore` loads localized PNGs and applies them to matching `Image`, `RawImage`, and `SpriteRenderer` components. It owns replacement textures and sprites; the originals belong to the game. It restores component assignments before destroying replacements. Sprite regions, pivots, borders, geometry, and texture sampling settings are preserved. See [Textures](TEXTURES.md) for matching and reload rules.

`AudioStore` maps exact clip names to replacement files and preloads them through Unity coroutines. WAV, MP3, and OGG paths are recognized. Audio is experimental, disabled by default, and the shipped pack has no mappings. Playback, looping, and scene changes still need testing with any new audio pack.

## Native Settings UI

The Home language button opens a native `HelpWindow` with General, Content, Translator, QA, and Diagnostics pages. Controls are cloned below a game-owned Canvas, with game scripts/listeners removed before translator callbacks are attached.

`NativeUiFactory` rejects clone subtrees containing `Canvas`, `CanvasScaler`, or `GraphicRaycaster`. Ownership markers distinguish translator controls from the game's own hierarchy. Home and Almanac events manage the launcher as those screens open and close.

The [static guard](../Tools/check-zero-canvas.ps1) checks prohibited creation/traversal patterns; runtime Diagnostics also reports `TranslatorCanvasCount`. The static check alone cannot prove the runtime count.

## Reload and diagnostics

Text/config reload builds the new state before activation. `ReloadCoordinator` isolates optional asset failures so they do not discard working text translation. Texture reload keeps a previous replacement when an editor briefly leaves its PNG invalid.

`AutoReloadService` watches the active locale, batches save events for 750 ms, and queues reload work for Unity's main thread. It watches string/Almanac JSON, ModStrings, optional asset manifests, and texture PNGs. Font and audio file contents are not watched directly; reload manually after changing them. Neither the watcher nor text export opens hidden game windows.

QA examines pack data; Diagnostics describes observed runtime state. Text capture retains original source/context pairs for translator exports. These reports are generated under the installed mod directory, not source files. See [Development](DEVELOPMENT.md#qa-diagnostics-and-exports) for their use.
