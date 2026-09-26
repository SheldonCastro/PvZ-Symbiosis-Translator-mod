# Building and Testing

## Prerequisites

Install a .NET SDK that can target .NET 6. Launch the game with MelonLoader once so its generated IL2CPP assemblies exist. The default repository layout is `<game>/MOD_PROJECT`; otherwise pass `-GameDir`.

## Canonical build

```powershell
./build.ps1 -Configuration Release -GameDir 'C:\Games\PvZ_Symbiosis'
```

The script runs the zero-Canvas static guard, builds the runtime DLL, runs the complete test harness, and validates the maintained pt-BR pack. Expected output is `PvZ_Symbiosis_Translator/bin/Release/net6.0/PvZ_Symbiosis_Translator.dll`.

Run tests alone:

```powershell
dotnet run --project Tests -c Release -- ./PvZ_Symbiosis_Translator/Localization/pt-BR
```

Deploy only with the game closed:

```powershell
./deploy.ps1 -GameDir 'C:\Games\PvZ_Symbiosis'
```

Deployment backs up the active mod, preserves differing user files, verifies the DLL hash, and validates installed locales. Review the backup's `preserved-conflicts.txt`.

The runtime project cannot build without the user's local MelonLoader, Unity, generated IL2CPP, and game assemblies. They are references with `Private=false` and must not be committed or redistributed.
