# Third-party assets and dependencies

This private development repository preserves the recovered project's history. Its author branding, **Xyll**, does not assert ownership of third-party assets. Private access is not itself a redistribution license. Review this inventory before changing repository visibility or publishing a package.

| Item | Location / provenance | License status | Required action |
| --- | --- | --- | --- |
| Noto Sans Regular | `PvZ_Symbiosis_Translator/Localization/pt-BR/Fonts/NotoSans-Regular.ttf`; provenance recorded in that directory's README | SIL Open Font License 1.1; copyright 2018 The Noto Project Authors; included `OFL.txt` | Preserve license and attribution; follow OFL requirements for any modified version. |
| ContinuumBold | `Localization/pt-BR/Fonts/ContinuumBold.ttf`; supplied from the user's PvZ Fusion translator setup | **Permission not established.** Font manifest explicitly says `distribution: localOnly`. No license file for this font was found in the recovered source. | Confirm original author/source and redistribution terms or replace/omit from distributed packages. Do not apply Noto's OFL to this file. |
| Background2.png | `PvZ_Symbiosis_Translator/UI/Background2.png`; supplied from another translation/modding project | **Redistribution status unconfirmed.** No specific license/provenance declaration in the recovered source establishes permission. | Identify creator and obtain/record permission or replace before public distribution. |
| buttonsmall.png | `PvZ_Symbiosis_Translator/UI/buttonsmall.png`; same supplied UI asset set | **Redistribution status unconfirmed.** | Same review as Background2; no ownership claim by this project. |
| MelonLoader / Harmony / Il2CppInterop and utility managers | Referenced from the user's installed MelonLoader runtime | External dependency licenses apply; binaries are not included in this repository or copied by deploy | Obtain through their legitimate distributions; do not package runtime DLLs with this mod. |
| Unity and generated game assemblies | Compile-time references to the user's Unity IL2CPP installation | Owned/licensed by their respective rights holders; not shipped here | Obtain the game separately; do not commit or redistribute these assemblies. |

The Noto provenance file records this source: https://github.com/notofonts/noto-fonts/blob/main/hinted/ttf/NotoSans/NotoSans-Regular.ttf. This restoration preserves that existing attribution; it does not independently grant rights.

Exact translation dictionaries contain source-language strings for matching. Legacy `Resources/Strings` data and historical reports are retained as project evidence, not as a substitute for the game or a claim to rights in the underlying text.

The recovered reachable history was checked for original game executables, generated/runtime DLLs, Unity asset containers and metadata binaries. None were found. Bundled binary source assets found in the active tree are the two fonts and two UI PNGs listed above. Audio and texture replacement manifests ship empty.

The original project source code and original project-owned material are licensed under the root MIT `LICENSE`. That license does not cover third-party game content or other assets beyond their own licenses. The public-candidate allowlist excludes ContinuumBold, the two uncleared UI PNGs, native UI contact sheets/catalog output, the untranslated runtime `Home.png` test, and original/extracted game assets. Noto Sans is included with its OFL and provenance files.
