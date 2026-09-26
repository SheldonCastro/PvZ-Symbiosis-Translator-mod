# Contributing

Thank you for helping improve PvZ Symbiosis Translator. Open one focused change at a time.

## Contribution types

Translation corrections, new translations, texture localization, code fixes/features, documentation, and compatibility testing are welcome in a future public repository.

## Required rules

- Match source strings exactly and ordinally; do not add fuzzy or global substring translation.
- Preserve placeholder identity and count.
- Keep target TMP markup valid.
- Follow the canonical glossary and PT-BR style guide.
- Use dynamic rules only for genuinely variable whole strings.
- Do not invent plant, zombie, item, or mechanic names without context.
- Do not submit game binaries, generated game assemblies, extracted asset dumps, original game assets, credentials, private logs, caches, or personal paths.
- Add meaningful tests for behavior changes and update current documentation.
- Keep runtime work bounded and lightweight. Never create a translator Canvas.

## Translation changes

State the Chinese source, current target, proposed target, scene/context, and reason. Include a screenshot when layout matters. Run PackValidator and full QA.

## Texture changes

Follow [Docs/TEXTURE_CONTRIBUTING.md](Docs/TEXTURE_CONTRIBUTING.md). Submit only artwork you are allowed to distribute. Preserve the full atlas dimensions/alpha and provide runtime screenshot evidence.

## Code changes

Build and test:

```powershell
./build.ps1 -Configuration Release -GameDir '<game-directory>'
```

Describe runtime testing for hooks, assets, or UI. Public CI checks dependency-independent tests; the runtime DLL still needs a local game installation.

## Pull requests

Complete the PR checklist, explain the concrete behavior change, and disclose remaining limits. A contribution is not a promise that the project owner can publish it until source and asset licensing are resolved.
