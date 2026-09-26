# Texture Contribution Guide

## Asset policy

Do not upload extracted game dumps or unchanged original game atlases. Submit only a localized derivative you are allowed to distribute. Keep extraction output local and provide screenshots rather than source dumps when reporting an identity.

## Automatic workflow

1. Identify the runtime `Texture2D` name through Diagnostics/catalog evidence.
2. Edit the complete atlas without moving or resizing regions.
3. Preserve original pixel dimensions and alpha.
4. Save an 8-bit RGB/RGBA, non-interlaced PNG as `Textures/<optional folders>/<TextureName>.png`.
5. Run QA and test the actual screen, scene transitions, close/reopen, and reload.

Folders do not namespace texture names. Two files named `Home.png` with the same dimensions conflict even in different folders.

## Explicit manifest

Use a manifest entry when filename plus dimensions is ambiguous:

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

Optional `spriteName` and `context` narrow the rule further. Metadata IDs come from runtime catalogs and must not be replaced with offline PathIDs. An explicit rule wins over automatic discovery for that Texture2D name.

## Acceptance evidence

- QA has no texture errors.
- Replacement dimensions match.
- Text/art is visibly localized.
- Alpha edges and sprite regions are intact.
- Buttons, sliced images, RawImage/SpriteRenderer paths, and sampling look correct where relevant.
- `PageUp` reload works.
- A temporary invalid save retains the previous image.
- Navigation and repeated open/close do not regress.
- The PR identifies the asset's provenance and redistribution basis.

`Home.png` currently used in private runtime testing is untranslated and must not be submitted as final artwork.
