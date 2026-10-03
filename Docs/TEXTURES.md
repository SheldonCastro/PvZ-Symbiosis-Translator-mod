# Texture localization

The mod replaces textures at runtime; it does not edit game archives. Texture replacement is off by default, and the shipped PT-BR texture manifest is empty. Enable replacement in Settings when testing a pack.

## Add a replacement

1. Find the exact runtime `Texture2D` name and dimensions through Diagnostics or a runtime catalog. [AssetExtractor](../Tools/AssetExtractor/README.md) can help inspect your local game files, but offline PathIDs are not runtime metadata IDs.
2. Edit the complete atlas, preserving width, height, alpha, and the position of every sprite region.
3. Save an 8-bit RGB/RGBA, non-interlaced PNG under the locale's `Textures` folder. Use the exact texture name, for example `Textures/UI/Home.png` for a texture named `Home`.
4. Run Settings > QA, reload with `PageUp`, and inspect the affected screen.

## Automatic matching

PNG filenames without the extension are matched exactly and case-sensitively, together with decoded dimensions. Subfolders only organize files: two `Home.png` files of the same dimensions conflict even in different directories.

A texture-only rule can replace several sprite regions backed by the same atlas. A rule matching distinct runtime textures is ambiguous and is skipped. If multiple rules target the same component, it keeps the original image.

## Explicit mappings

Use `Textures/manifest.json` to disambiguate by sprite, scene, hierarchy context, or a runtime metadata ID. For example:

```json
{
  "entries": [
    {
      "id": "home-atlas",
      "match": {
        "textureName": "Home",
        "width": 4096,
        "height": 8192,
        "scene": "Home"
      },
      "replacement": "UI/Home.png"
    }
  ]
}
```

Use values observed in your game. Optional `spriteName` and `context` narrow a match; `match.id` accepts a runtime metadata ID instead of a texture-name/dimensions match. Paths are relative to `Textures` and cannot leave that directory. Explicit mappings suppress automatic discovery for the same texture name. Mapping IDs and source identities must be unique.

## Atlases and reload

Replacement dimensions must match the original. The mod preserves sprite rects, pivots, pixels-per-unit, borders, supported custom geometry, and source sampling settings. Moving atlas regions breaks the game’s existing sprite coordinates. A shared replacement with conflicting sampling requirements is skipped.

Manual and automatic reload decode changed files before replacing active assignments. If a changed PNG is temporarily invalid, the previous image is kept when its ID, path, and matching identity still agree. A new invalid file is skipped. A malformed manifest or identity conflict rejects the reload and preserves the previous mapping set. Unchanged file hashes reuse decoded textures.

Replacement applies at scene initialization and reload points, not by scanning every frame. A graphic created later may need another reload. Disabling replacement restores the game’s original assignments; only mod-created textures and sprites are destroyed.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| No replacement | Enable textures; verify filename case, dimensions, active locale, and runtime match |
| Ambiguous mapping | Remove duplicate name/size files or add an explicit qualifier |
| Previous image remains | Check the reload error and save a complete, supported PNG |
| Broken atlas regions | Restore original dimensions, region positions, and alpha |
| QA passes but screen is unchanged | Inspect runtime Diagnostics; offline checks cannot prove a source exists |
| Late-created graphic stays original | Reload after opening the affected screen |

## Contributions and performance

Submit artwork you have permission to distribute, with its provenance and a screenshot of the localized result. Keep original game dumps and unchanged atlases local. Test navigation, repeated open/close, valid reload, and a temporarily invalid save. Include the relevant game/mod versions in a report.

Large atlases are expensive: a 4096×8192 RGBA32 image needs about 128 MiB for one uncompressed copy, before CPU/GPU duplication. Replacement textures currently remain CPU-readable, and bytes are copied into IL2CPP arrays individually. Avoid unnecessary reloads of large images; changes to decoding or ownership need in-game memory and lifecycle testing.
