# Runtime Diagnostics

Diagnostics capture implemented runtime state: mod/game/Unity/MelonLoader versions, locale, scene/context, translation store counts, observed/translated/preserved/unknown strings, font readiness, watcher state, UI root/launcher/Canvas counts, reload state, exports, texture state, and audio mappings.

Texture diagnostics track explicit and automatic mappings, cache count, created sprites, applied components, retained-last-good count, load duration, and the last sanitized texture error. Catalog exports include runtime names, dimensions, scene/hierarchy, and metadata IDs. Absolute personal paths are not part of public-facing summaries.

Health results are Pass, Warning, Fail, Info, or NotApplicable. Disabled optional systems and enabled systems with zero configured mappings are not automatically failures.

Use the native Diagnostics page to refresh, copy, or export. Review `Latest.log` locally before sharing because logs can contain machine paths or unrelated sensitive data.
