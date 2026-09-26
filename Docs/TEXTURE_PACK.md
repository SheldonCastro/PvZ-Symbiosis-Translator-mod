# External Texture Packs

The canonical workflow is documented in [Textures](TEXTURES.md). In short:

1. Identify the exact runtime Texture2D name.
2. Preserve the complete atlas dimensions and alpha.
3. Save an 8-bit RGB/RGBA non-interlaced PNG under `Localization/<locale>/Textures` with that exact name.
4. Run QA, reload with `PageUp` or the Settings action, and inspect the real screen.
5. Use `manifest.json` only to disambiguate with metadata, sprite, scene, or context.

Folders do not namespace names. Duplicate name-and-size identities fail. Explicit mappings take precedence by texture name. Offline container/PathIDs are authoring evidence and are not runtime metadata IDs.

The runtime probe command `--pvz-texture-probe` exercises replacement, valid/invalid reload, property preservation, restoration, and readback, then exits. Scratch files live under the ignored deployed Cache directory.

Do not distribute original game dumps or the untranslated private `Home.png` test. Follow [Texture Contributions](TEXTURE_CONTRIBUTING.md).
