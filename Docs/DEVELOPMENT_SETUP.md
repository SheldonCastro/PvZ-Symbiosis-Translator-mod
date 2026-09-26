# Development Setup

1. Obtain and install the supported game separately.
2. Install MelonLoader 0.7.3 and launch once to generate `MelonLoader/Il2CppAssemblies`.
3. Clone the private repository into `<game>/MOD_PROJECT`, or retain another checkout and pass `-GameDir`.
4. Run `git pull --ff-only`, then `./build.ps1 -Configuration Release`.
5. Use `./deploy.ps1` for reviewed runtime changes. Never copy generated dependency DLLs into Git.
6. Keep extraction output, logs, diagnostics, backups, and working assets outside tracked source.

The private `origin/main` branch is authoritative. Preserve the `pre-hermes-stable` tag. Use focused commits after relevant validation.

Useful locations:

- maintained source: `MOD_PROJECT/PvZ_Symbiosis_Translator`
- deployed instance: `<game>/Mods/PvZ_Symbiosis_Translator`
- runtime log: `<game>/MelonLoader/Latest.log`
- ignored public staging: `MOD_PROJECT/dist/public-repo`
- local extracted assets: a separate ignored workspace

See [Project Structure](PROJECT_STRUCTURE.md), [Building](BUILDING.md), and [Dependencies](DEPENDENCIES.md).
