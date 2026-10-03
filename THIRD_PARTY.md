# Third-party notices

The root [MIT License](LICENSE) covers project-owned source and material. It does not grant rights to Plants vs. Zombies, PvZ Symbiosis, their assets, trademarks, or other third-party material. Exact translation dictionaries include original source strings needed for matching.

## Included font

Noto Sans Regular is distributed under the **SIL Open Font License 1.1**, copyright 2018 The Noto Project Authors. The font, [OFL text](PvZ_Symbiosis_Translator/Localization/pt-BR/Fonts/OFL.txt), and [provenance](PvZ_Symbiosis_Translator/Localization/pt-BR/Fonts/README.md) are kept together in the PT-BR pack. Preserve those notices when redistributing it.

## External dependencies

| Dependency | Author/project | License | Use |
| --- | --- | --- | --- |
| MelonLoader | LavaGang and contributors | Apache-2.0 | Installed separately |
| Harmony | Andreas Pardeike and contributors | MIT | Referenced from MelonLoader |
| Il2CppInterop | BepInEx / Il2CppInterop contributors | LGPL-3.0; see [upstream license](https://github.com/BepInEx/Il2CppInterop/blob/master/LICENSE) | Referenced from MelonLoader |
| Newtonsoft.Json | James Newton-King and contributors | MIT | Referenced from MelonLoader |
| UnityPy | K0lb3 and contributors | MIT | Optional offline asset tooling |
| Pillow | Python Pillow contributors | MIT-CMU | Optional offline asset tooling |
| Unity and generated game assemblies | Unity and the respective game rights holders | Their respective terms | Local build references only |

Runtime and game DLLs are not shipped in this source tree. Obtain them through the game and MelonLoader installations. Python tools declare their dependencies separately; those packages retain their own notices. Project links and acknowledgements are in [CREDITS.md](CREDITS.md).

## Assets excluded from source

ContinuumBold, `Background2.png`, and `buttonsmall.png` are not included because redistribution permission has not been established. Noto's OFL does not apply to them. Original game dumps and atlases are also excluded. Any contributed replacement font, texture, or audio needs its own provenance and distribution terms.
