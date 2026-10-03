# AssetExtractor

A read-only utility for inspecting local Unity Texture2D and Sprite assets while preparing texture translations. It exports PNGs and records names, dimensions, source files, and PathIDs. SpriteAtlas metadata is included where UnityPy supports it. It does not repack game files or use the running game.

## Setup

Use Python 3.11–3.14. From this directory:

```powershell
python -m venv .venv
./.venv/Scripts/python.exe -m pip install -r requirements.txt
./.venv/Scripts/python.exe -m pip install --no-deps -e .
```

The pinned dependencies are in `requirements.txt`; UnityPy and Pillow handle decoding. The examples below use the virtual environment directly.

## Scan and extract

```powershell
./.venv/Scripts/python.exe -m pvz_asset_extractor scan --input 'C:\Games\PvZ_Symbiosis\PVZGS_Data'
./.venv/Scripts/python.exe -m pvz_asset_extractor extract --input 'C:\Games\PvZ_Symbiosis\PVZGS_Data' --output 'C:\TranslationWork\ExtractedAssets'
```

Keep output outside the input tree and repository. Scan inventories containers and object types without decoding images. Extract reads supported images and checks input hashes afterward. Add `--game-version 1.2.0` if automatic version detection fails; this labels the output and does not change decoding. Use `--verbose` for per-object progress.

Exit codes are `0` for success, `2` for partial scan/extraction failures, and `1` for a fatal error or interruption. Read `Reports/failures.json` when output is incomplete.

## Output and resume

Under `<output>/<version>/`, `Texture2D/` contains complete textures and `Sprites/` contains individual sprite images. `Manifests/` holds JSON metadata and `assets.csv`; `Reports/` holds source hashes, progress, summaries, and failures. Source-file identity and PathID distinguish assets that share a name.

Resume is the default. Existing images are validated before reuse; failed or missing outputs are retried. The workspace must match the same input hashes and decoder versions. Use a new output directory after updating the game. `--overwrite` regenerates images for the same dataset; it does not bypass that identity check. A workspace lock prevents simultaneous writers.

A Texture2D may be an entire atlas. A Sprite refers to a region with its own pivot, border, rotation, and mesh. Sprite output can include UnityPy's packing/mesh interpretation, so it is not always a simple rectangle crop. Use a complete texture when following the runtime [texture replacement guide](../../Docs/TEXTURES.md). Offline PathIDs are not runtime metadata IDs, and the mod does not directly load these export manifests.

## Tests and limits

```powershell
./.venv/Scripts/python.exe -m unittest discover -s tests -v
```

Tests use synthetic data. Successful decoding checks image integrity, not every shader or sprite's appearance. Some Unity schemas, custom containers, missing streamed payloads, and SpriteAtlas relationships may be unsupported; failures retain the asset identity and exception. Only Texture2D/Sprite images and SpriteAtlas metadata are supported, not audio, OCR, or scene editing.

Large atlases and container dependencies can use substantial memory. Input files are hashed again on resume, which also adds I/O. Avoid updating the game while extraction is running.

Keep extracted assets local. The tool grants no redistribution rights over game content. See [Third-party notices](../../THIRD_PARTY.md) for dependencies and asset terms.
