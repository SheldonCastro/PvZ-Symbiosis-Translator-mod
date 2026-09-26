using System;
using System.Collections.Generic;
using System.Linq;

namespace PvZSymbiosisTranslator.Diagnostics;

public sealed class DiagnosticsService
{
    public DiagnosticsSnapshot Capture(DiagnosticsInput input)
    {
        input??=new DiagnosticsInput();var checks=new List<RuntimeHealthCheck>();
        Add(checks,"translationStore",input.TranslationStoreReady,RuntimeHealthStatus.Fail,input.ExactEntries+" exact");
        Add(checks,"exactStore",input.ExactEntries>0,RuntimeHealthStatus.Fail,input.ExactEntries.ToString());
        checks.Add(new RuntimeHealthCheck{Id="contextOverrides",Status=RuntimeHealthStatus.Pass,Detail=input.ContextOverrides.ToString()});
        checks.Add(new RuntimeHealthCheck{Id="dynamicRules",Status=RuntimeHealthStatus.Pass,Detail=input.DynamicRules.ToString()});
        Add(checks,"collector",input.CollectorReady,RuntimeHealthStatus.Warning,input.StringsObserved+" observed");
        checks.Add(new RuntimeHealthCheck{Id="font",Status=!input.CustomFontEnabled?RuntimeHealthStatus.NotApplicable:input.FontReady?RuntimeHealthStatus.Pass:RuntimeHealthStatus.Warning,Detail=!input.CustomFontEnabled?"disabled":input.FontReady?"ready":"not ready"});
        checks.Add(new RuntimeHealthCheck{Id="textures",Status=!input.TextureEnabled?RuntimeHealthStatus.NotApplicable:!string.IsNullOrWhiteSpace(input.LastTextureError)?RuntimeHealthStatus.Warning:RuntimeHealthStatus.Pass,Detail=!input.TextureEnabled?"disabled":$"{input.TextureMappings} mappings ({input.TextureExplicitMappings} explicit, {input.TextureAutomaticMappings} automatic); {input.TextureAppliedComponents} applied; {input.TextureCachedTextures} cached; {input.TextureRetainedLastGood} retained; {input.LastTextureReloadDurationMilliseconds} ms"+(string.IsNullOrWhiteSpace(input.LastTextureError)?"":"; "+input.LastTextureError)});
        checks.Add(Optional("audio",input.AudioEnabled,input.AudioMappings));
        checks.Add(new RuntimeHealthCheck{Id="watcher",Status=!input.WatcherConfigured?RuntimeHealthStatus.NotApplicable:input.WatcherActive?RuntimeHealthStatus.Pass:RuntimeHealthStatus.Fail,Detail=!string.IsNullOrWhiteSpace(input.WatcherError)?input.WatcherError:input.WatcherActive?$"{input.WatchedFiles} files":"disabled"});
        Add(checks,"settingsUi",input.SettingsRootCount<=1,RuntimeHealthStatus.Fail,input.SettingsActive?"active":"inactive");
        Add(checks,"harmony",input.HarmonyReady,RuntimeHealthStatus.Fail,input.HarmonyReady?"patches detected":"patches unavailable");
        Add(checks,"exports",input.ExportDirectoryReady,RuntimeHealthStatus.Warning,input.ExportDirectoryReady?"ready":"unavailable");
        Add(checks,"languageDirectory",input.LanguageDirectoryReady,RuntimeHealthStatus.Fail,input.ActiveLanguage);
        checks.Add(new RuntimeHealthCheck{Id="translatorCanvas",Status=input.TranslatorCanvasCount==0?RuntimeHealthStatus.Pass:RuntimeHealthStatus.Fail,Detail=input.TranslatorCanvasCount.ToString()});
        checks.Add(new RuntimeHealthCheck{Id="launcher",Status=input.LauncherCount<=1?RuntimeHealthStatus.Pass:RuntimeHealthStatus.Fail,Detail=input.LauncherCount.ToString()});
        checks.Add(new RuntimeHealthCheck{Id="settingsRoots",Status=input.SettingsRootCount<=1?RuntimeHealthStatus.Pass:RuntimeHealthStatus.Fail,Detail=input.SettingsRootCount.ToString()});
        return new DiagnosticsSnapshot{Timestamp=DateTimeOffset.UtcNow,State=input,Health=checks,Failures=checks.Count(x=>x.Status==RuntimeHealthStatus.Fail),Warnings=checks.Count(x=>x.Status==RuntimeHealthStatus.Warning)};
    }
    private static RuntimeHealthCheck Optional(string id,bool enabled,int mappings)=>new(){Id=id,Status=!enabled?RuntimeHealthStatus.NotApplicable:RuntimeHealthStatus.Pass,Detail=!enabled?"disabled":mappings==0?"0 mappings / not configured":mappings+" mappings"};
    private static void Add(List<RuntimeHealthCheck> checks,string id,bool pass,RuntimeHealthStatus failed,string detail)=>checks.Add(new RuntimeHealthCheck{Id=id,Status=pass?RuntimeHealthStatus.Pass:failed,Detail=detail});
}
