# UI System

The translator uses native game UI assets and existing game-owned canvases.

- Home owns one `PvZTranslator.Native.Languages` launcher.
- Settings repurposes a native `HelpWindow` and contains General, Content, Translator, QA, and Diagnostics pages.
- Approved controls are cloned below an existing game Canvas after game scripts/listeners are stripped and translator callbacks are bound.
- The factory rejects any clone subtree containing Canvas, CanvasScaler, or GraphicRaycaster.
- Translator ownership markers avoid claiming game hierarchy objects with similar names.
- Almanac/Home lifecycle handling removes or restores the launcher at bounded lifecycle points.

`Tools/check-zero-canvas.ps1` enforces the static invariant and runtime diagnostics report `TranslatorCanvasCount`. UI scaling, labels, notifications, and page state are driven by configuration and `ModLabels`.

Historical native UI catalogs and contact sheets are internal evidence, not current architecture authority or public assets.
