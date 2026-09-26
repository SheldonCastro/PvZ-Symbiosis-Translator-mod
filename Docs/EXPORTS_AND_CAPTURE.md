# Exports and Capture

`PageDown` and Translator Mode produce text-oriented authoring output below the deployed mod directory. Outputs include unresolved/current/runtime sources, dynamic candidates/families, context views, QA, and Diagnostics.

Capture preserves stable source/context identity and filters empty strings, translator-owned labels, rendered targets, and repeat noise. Dynamic candidates contain whole genuinely variable strings; suggestions are never installed automatically. Existing manual targets in pending work remain intact while a source is still pending.

Texture extraction is separate. The runtime supports an explicit developer dump-request mechanism and an opt-in readback probe, while the offline AssetExtractor reads the user's local game files. Normal text diagnostics do not toggle translation, activate hidden windows, or dump textures.

Exports, logs, and extraction output are generated/private data and are excluded from public promotion unless a specific sanitized document is reviewed.
