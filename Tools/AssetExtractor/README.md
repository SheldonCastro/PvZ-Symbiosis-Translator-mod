# AssetExtractor

A read-only offline extractor for Unity **Texture2D** and **Sprite** assets. It inventories serialized files, exports PNGs, preserves source/PathID identity, and writes JSON manifests and an Excel-friendly CSV index. SpriteAtlas metadata is collected when UnityPy can deserialize it.

This tool does not use GPU readback, OCR, the running game, MelonLoader or the runtime mod. It never calls UnityPy's save/repack APIs. Output must be outside the input tree; source hashes are compared before and after a complete extraction.

## Requirements and setup

Tested on Windows x64 with **Python 3.11.16**, **UnityPy 1.25.2** and **Pillow 12.3.0**. The declared Python range is 3.11–3.14, but other interpreter/platform combinations have not been integration-tested. Prefer Python 3.11 for reproducing these results.

From this directory:

```powershell
py -3.11 -m venv .venv
./.venv/Scripts/python.exe -m pip install -r requirements.txt
./.venv/Scripts/python.exe -m pip install --no-deps -e .
```

If your interpreter is not registered as `py -3.11`, use its full executable path for the first command. Activation is optional; all examples below assume the venv interpreter is on PATH. On Unix, use `.venv/bin/python`.

`requirements.txt` pins the tested dependency closure. `pyproject.toml` declares the UnityPy/Pillow requirements and package entry point. No globally installed UnityPy is required.

The API follows [UnityPy's project documentation](https://github.com/K0lb3/UnityPy): read objects and use their decoded `image` property. UnityPy/Pillow and their dependencies retain their own licenses.

## Scan

```powershell
python -m pvz_asset_extractor scan --input 'C:\Games\PvZ_Symbiosis\PVZGS_Data'
```

Scan discovers `.assets`, bundles, Unity header signatures, extensionless level/global containers and other recognizable serialized headers recursively. `.resS` and `.resource` companions are not opened as standalone serialized files; UnityPy resolves them from their parent asset data.

It prints container counts, object-type counts, Unity versions and errors. Scan does **not** decode PNGs or prove that all objects can be deserialized. Unsupported/non-target types appear in the inventory, not as successfully extracted assets. The first milestone decodes only Texture2D, Sprite and SpriteAtlas metadata.

## Extract

```powershell
python -m pvz_asset_extractor extract `
  --input 'C:\Games\PvZ_Symbiosis\PVZGS_Data' `
  --output 'C:\Games\PvZ_Symbiosis\TranslatorWorkspace\ExtractedAssets'
```

The tool attempts to read `PlayerSettings.bundleVersion` from `globalgamemanagers`. If that fails, the output version is `unknown`, and the reason is recorded. You can supply a version explicitly:

```powershell
python -m pvz_asset_extractor extract --input '<data-directory>' --output '<workspace>' --game-version '<observed-version>'
```

No game path or game version is hardcoded in program logic. `--game-version` affects labeling, not deserialization. Normal progress is per container and every 250 supported objects; `--verbose` reports each processed object. Exit codes: **0** complete success, **2** partial extraction/scan failures, **1** fatal error or interruption.

## Output

```text
<output>/<version>/
├── Texture2D/<safe-source>_<identity-hash>/<PathID>_<safe-name>.png
├── Sprites/<safe-source>_<identity-hash>/<PathID>_<safe-name>.png
├── Manifests/
│   ├── textures.json
│   ├── sprites.json
│   ├── atlases.json
│   └── assets.csv
└── Reports/
    ├── source_identity.json
    ├── progress.jsonl
    ├── summary.json
    └── failures.json
```

Names are sanitized for Windows, including reserved device names, traversal, empty values and trailing dots/spaces. A source-identity suffix prevents containers that sanitize alike from colliding. PathID separates objects with the same Unity name. The manifests retain original names, full relative container identities and numeric PathIDs.

### Texture2D versus Sprite

A Texture2D is the full texture, which can contain an entire atlas. A Sprite is one logical image using a region and possibly packing rotation, mesh shape or alpha data from that texture. Both are exported independently through UnityPy. Sprite output may reflect packing and mesh processing rather than a plain rectangle crop.

Texture entries include original dimensions/format, streamed-resource metadata when present, exported path, PNG SHA-256 and status. Sprite entries retain available rect, pivot, border, direct texture references and atlas overrides. Sprite PNG `width`/`height` describe the exported image; `rect` preserves Unity metadata independently. References include file ID/serialized-file information when available, because PathID alone is not globally unique.

Atlas entries contain packed sprite pointers, texture references and available render metadata. No missing pivots, borders, source files or IDs are invented. Atlas relationship warnings are retained even if image export succeeds.

Identical PNG hashes are marked as duplicate content, but every logical asset still has its own entry and image. Hashes identify encoded PNG bytes, not a cross-decoder visual-equivalence guarantee.

CSV uses UTF-8 with BOM, proper quoting and formula-safe name cells. JSON preserves the exact original names. Entries and manifests have deterministic source/PathID ordering; elapsed times and progress logs naturally vary between runs.

## Resume and overwrite

Resume is the default. The source identity records SHA-256 for **all files under the input**, including resource companions, plus decoder versions. An existing workspace is accepted only for the exact same dataset and decoder identity.

- Completed images are reopened, checked for nonzero/expected dimensions and hashed before reuse. They are not decoded again from Unity.
- Each completed object is appended to a flushed progress journal. Only an incomplete final journal line is discarded after an interruption.
- Failed or missing outputs are retried. An orphan PNG from an interrupted write is accepted only if a fresh decode produces identical bytes.
- A nonempty directory without matching state, changed source data, or corrupt existing PNG stops safe reuse. Use a new output directory for changed game data.
- `--overwrite` explicitly regenerates PNGs for the **same** source identity. It does not bypass the different-dataset check or delete unrelated files.
- A process-held workspace lock prevents simultaneous writers and releases automatically when the process exits.

The full input is hashed again on resume. This costs some I/O but catches changed `.resS` payloads even when names and timestamps are unchanged. Do not update the game while extracting.

## Validation and tests

```powershell
python -m unittest discover -s tests -v
```

Unit tests use synthetic images/objects, not game fixtures. They cover names, IDs, paths, serialization, CSV, hashes, output validation, resume, interrupted journals, duplicate content, source changes, metadata omissions and isolated decode errors.

Extraction validates each PNG immediately, then performs another pass across **all successful image outputs** and checks source hashes. This validates file integrity/dimensions, not visual correctness of every Unity shader or sprite interpretation.

See [the real integration report](../../Docs/ASSET_EXTRACTOR_VALIDATION.md) for counts, failures and measured limits on the installed Unity 6 game. A successful inventory alone is not called extraction PASS.

## Limitations and troubleshooting

- Unity 6 object schemas are not universally supported. Deserialization exceptions are recorded; other objects continue. Version detection can fail independently from image extraction.
- Only Texture2D/Sprite images and SpriteAtlas metadata are decoded. No AudioClip, TextAsset, scene graph, OCR or PSD export is implemented.
- Files that have no recognizable Unity header or expected filename may not be discovered. Encrypted/custom bundles and split-file layouts require separate validation.
- The tool processes containers incrementally. Decoded sprite image caches are bounded to roughly 64 MiB between objects, but a single large atlas and the current container/dependencies can exceed that amount. It is not a hard process-memory limit.
- Empty/missing streamed texture payloads can fail with an upstream file/decoder error. `failures.json` retains the original exception and asset identity; do not substitute a guessed image.
- SpriteAtlas relationships are best effort. An installation with no SpriteAtlas objects cannot validate real atlas-object decoding.
- File permissions, insufficient disk space, malformed state or concurrent writers require resolving the reported problem before retrying. Keep outputs outside the game data and repository.
- The runtime replacement system does not directly consume this offline database yet. Replacement matching and version migration are [planned tools](../README.md).

## Copyright

This tool contains no game assets. You must provide a legally obtained local game installation. Extracted assets remain subject to the rights of their respective copyright holders; this tool grants no redistribution permission. Generated assets and manifests should **not be committed to this repository**. Ignore rules cover generated workspaces without broadly ignoring intentional mod source PNGs.
