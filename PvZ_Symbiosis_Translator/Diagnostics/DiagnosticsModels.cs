using System;
using System.Collections.Generic;

namespace PvZSymbiosisTranslator.Diagnostics;

public enum RuntimeHealthStatus { Pass, Warning, Fail, Info, NotApplicable }

public sealed class RuntimeHealthCheck
{
    public string Id { get; init; } = "";
    public RuntimeHealthStatus Status { get; init; }
    public string Detail { get; init; } = "";
}

public sealed class DiagnosticsInput
{
    public string ModVersion="",GameVersion="",UnityVersion="",MelonVersion="",ActiveLanguage="",Scene="",Context="";
    public bool Il2Cpp,TranslatorEnabled,CustomFontEnabled,TextureEnabled,AudioEnabled;
    public bool TranslationStoreReady,CollectorReady,FontReady,WatcherConfigured,WatcherActive,SettingsActive,HarmonyReady,ExportDirectoryReady,LanguageDirectoryReady;
    public int ExactEntries,DynamicRules,ContextOverrides,TextureMappings,AudioMappings;
    public int TextureExplicitMappings,TextureAutomaticMappings,TextureCachedTextures,TextureCreatedSprites,TextureAppliedComponents,TextureRetainedLastGood;
    public int StringsObserved,RuntimeTranslations,Preserved,KnownUntranslated,Unknown,Captured,WatchedFiles,TranslatorCanvasCount,LauncherCount,SettingsRootCount;
    public string CurrentTab="",LastReloadResult="",LastReloadError="",LastImportantEvent="",WatcherError="";
    public DateTimeOffset? LastFileChange,LastAutoReload,LastManualReload;
    public long LastReloadDurationMilliseconds,LastTextureReloadDurationMilliseconds;
    public string LastTextureError="";
}

public sealed class DiagnosticsSnapshot
{
    public DateTimeOffset Timestamp { get; init; }
    public DiagnosticsInput State { get; init; } = new();
    public IReadOnlyList<RuntimeHealthCheck> Health { get; init; } = Array.Empty<RuntimeHealthCheck>();
    public int Failures { get; init; }
    public int Warnings { get; init; }
    public RuntimeHealthStatus OverallStatus => Failures>0?RuntimeHealthStatus.Fail:Warnings>0?RuntimeHealthStatus.Warning:RuntimeHealthStatus.Pass;
}
