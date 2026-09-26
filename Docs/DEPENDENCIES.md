# Dependencies

## Runtime/build

- .NET 6
- MelonLoader 0.7.3
- Harmony and Il2CppInterop distributed with the user's MelonLoader installation
- Unity 6000.0.41f1 assemblies generated/provided by the user's game installation
- game-generated `Assembly-CSharp` references

Runtime/game references use `Private=false`. They are neither copied into build output nor eligible for public source/release packaging.

## Tools

- PowerShell for build, deploy, and public-candidate tooling
- Python 3 for project scripts
- UnityPy and Pillow for AssetExtractor; see its `pyproject.toml` and `requirements.txt`

## Assets

Noto Sans includes OFL 1.1. ContinuumBold and project UI artwork need separate redistribution review. Original game assets remain owned by their respective rights holders and are excluded.

See [THIRD_PARTY.md](../THIRD_PARTY.md).
