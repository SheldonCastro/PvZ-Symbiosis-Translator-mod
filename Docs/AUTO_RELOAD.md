# Auto Reload

Auto reload watches only relevant files under the active locale and debounces editor save/rename bursts for 750 ms. Work is queued to Unity's main thread.

Relevant files include exact/context/dynamic JSON, ModStrings, optional manifests, and texture/audio/font assets. Temporary files and unrelated locales are ignored.

Text/config activation is transactional. Texture activation retains last-known-good content for a temporarily invalid PNG with the same identity. Optional subsystem failures remain isolated so text translation is not discarded.

Manual `PageUp` reload uses the same maintained backends. Auto reload does not activate hidden game UI or scan every frame.
