using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Il2CppTMPro;
using PvZSymbiosisTranslator.Core;
using PvZSymbiosisTranslator.Configuration;
using PvZSymbiosisTranslator.Localization;
using PvZSymbiosisTranslator.Localization.Reload;
using PvZSymbiosisTranslator.Diagnostics;
using PvZSymbiosisTranslator.Assets;
using PvZSymbiosisTranslator.UI;
using PvZSymbiosisTranslator.UI.Pages;
using PvZSymbiosisTranslator.QA;
using HarmonyLib;

[assembly: MelonInfo(typeof(PvZSymbiosisTranslator.PvZSymbiosisTranslatorMod), "PvZ Symbiosis Translator", "1.1.0", "Xyll")]
[assembly: MelonGame("CherryGG", "PVZGS")]
namespace PvZSymbiosisTranslator;

public class PvZSymbiosisTranslatorMod : MelonMod
{
    public static ModConfig Config { get; private set; } = new();
    private static TranslationService service;
    private static ModPaths paths;
    private static readonly RuntimeStringCollector collector = new();
    private static readonly RuntimeTextScanner scanner = new();
    private static readonly TextureDiagnostics graphics = new();
    private static readonly TextureStore textures = new();
    private static readonly FontStore fonts = new();
    internal static readonly AudioStore Audio = new();
    private static HotkeyService hotkeys;
    private static readonly ToastService toast = new();
    private static readonly GameUiRootRegistry gameUiRoots = new();
    private static readonly TransactionalReloadService reloadTransactions = new();
    private static readonly AutoReloadService autoReload = new(TimeSpan.FromMilliseconds(750));
    private static readonly TranslatorSessionService translatorSession = new();
    private static TranslatorExportService translatorExports;
    private static readonly LocalizationQaService qaService = new();
    private static QaReportWriter qaReports;
    private static QaSnapshot qaSnapshot;
    private static bool qaDirty=true;
    private static readonly DiagnosticsService diagnosticsService = new();
    private static DiagnosticsReportWriter diagnosticsReports;
    private static DiagnosticsSnapshot diagnosticsSnapshot;
    private static bool diagnosticsDirty=true;
    private static DateTimeOffset? lastManualReload;
    private static readonly HashSet<string> knownRuntimeSources = new(StringComparer.Ordinal);
    private static DateTimeOffset? lastAutoReload;
    private static DateTimeOffset? lastAutoExport;
    private static bool translatorPageDirty;
    private static bool pendingTranslatorMenu;
    private static Il2Cpp.HomeManager currentHomeManager;


    private static string scene = "";
    internal static string CurrentScene => scene;
    private static bool assigning;
    private static int exactCount;
    private static int dynamicCount;
    private static int rejectedCount;
    private static int warningCount;
    private static NativeSettingsPage currentSettingsPage=NativeSettingsPage.General;
    private static int uiScaleSaveGeneration;
    private bool smokeDone;
    private int smokeFrames;
    private bool phase4ProbeStarted;
    private int phase4ProbeFrames;


    private sealed class PendingUiRefresh { public GameObject Root; public int Age=-1; }
    private static readonly Dictionary<int, PendingUiRefresh> pendingUiRefreshes = new();
    private sealed class PendingTextTrace
    {
        public Component Component;
        public string Root;
        public int RootId;
        public int ScheduledFrame;
    }
    private static readonly Dictionary<int, PendingTextTrace> pendingTextTraces = new();
    private static readonly Dictionary<int,int> nativeStatusGenerations = new();
    private sealed class TextState
    {
        public Component Component;
        public string Source, Rendered, Context, ActualScene, Hierarchy;
        public int SceneHandle, ParentId;
        public bool OriginalRichText;
        public bool Composite;
    }
    private static readonly Dictionary<int, TextState> texts = new();
    private static readonly Dictionary<string, int> textApiCounts = new(StringComparer.Ordinal);
    private sealed class LocalizationCandidate
    {
        public ModConfig Config;
        public LanguagePack Pack;
        public TranslationService Service;
        public Dictionary<string,string> Labels;
    }
    public static void ObserveTextApi(Component component, string source, string api, float? numericArgument = null)
    {
        if ((!Config.DebugLogging && !Config.TranslatorMode) || component == null || source == null || source.Length > 128 ||
            !(source.Contains('秒') || source.Contains('倍') || source.Contains('槽')) || IsModUi(component)) return;
        var key=api+":"+component.name;
        var count=textApiCounts.TryGetValue(key,out var previous) ? previous+1 : 1;
        textApiCounts[key]=count;
        if (count>4 && count%100!=0) return;
        var rendered=service?.Translate(source,null,Config.DynamicRules) ?? source;
        MelonLogger.Msg($"[TextAPI] {api}; calls={count}; object={component.name}; source={DiagnosticString.EscapeInvisible(source)}; target={DiagnosticString.EscapeInvisible(rendered)}" +
            (numericArgument.HasValue ? "; value="+numericArgument.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : ""));
    }
    public override void OnInitializeMelon()
    {
        paths = new ModPaths(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        paths.EnsureDirectories();
        translatorExports = new TranslatorExportService(paths.Root);
        qaReports = new QaReportWriter(paths.Root);
        diagnosticsReports = new DiagnosticsReportWriter(paths.Root);
        hotkeys = new HotkeyService(key => ToggleTranslation("hotkey:"+key), key => ReloadWithToast("hotkey:"+key), key => ExportTextDiagnostics("hotkey:"+key));
        try { collector.Load(paths.Dumps); }
        catch (Exception ex) { MelonLogger.Warning("Previous runtime dump could not be read: " + ex.Message); }
        Reload();
        LoadKnownRuntimeSources();
        UpdateAutoReloadWatcher();
    }
    public static bool Reload()
    {
        var ok=ReloadLocalization("all",true,true);
        if(ok)ReloadOptionalAssets();
        return ok;
    }

    private static bool ReloadLocalization(string scope,bool reloadConfig,bool validateOptionalAssets)
    {
        var files=validateOptionalAssets?10:7;
        var ok=reloadTransactions.Execute(scope,files,()=> {
            var config=reloadConfig?ConfigManager.Load(paths.ConfigFile,message=>MelonLogger.Msg("[CONFIG] "+message)):Config.Clone();
            var locale=paths.Locale(config.Language);
            var pack=LocaleManager.Load(locale,config.Language,Application.version,validateOptionalAssets,ReportPath(config.Language));
            var labels=ModLabels.LoadCandidate(locale,message=>pack.Validation.Add(ValidationSeverity.Warning,"ModStrings.json",null,null,message));
            if(pack.Validation.Rejected>0 || pack.Validation.Fatals>0)throw new FormatException("Candidate pack contains rejected or fatal entries");
            return new LocalizationCandidate {Config=config,Pack=pack,Service=new TranslationService(pack),Labels=labels};
        },candidate=> {
            ModLabels.Apply(candidate.Labels);
            service=candidate.Service;Config=candidate.Config;
            candidate.Pack.Dynamic.FailureDiagnostic=Config.DebugLogging?(id,source,failure)=>MelonLogger.Warning($"Dynamic rule {id}: {failure}; source={source}"):null;
            candidate.Pack.Validation.WriteIfRequested(Path.Combine(paths.Root,"Diagnostics","translation_validation.json"));
            exactCount=candidate.Pack.Exact.Count;dynamicCount=candidate.Pack.Dynamic.Count;rejectedCount=candidate.Pack.Validation.Rejected;warningCount=candidate.Pack.Validation.Warnings;
            qaDirty=true;
            diagnosticsDirty=true;
            LogValidation(candidate.Pack,Config.DebugLogging);
        });
        if(!ok)MelonLogger.Error("Locale/config ERROR; retaining previous valid state: "+reloadTransactions.Last.Error);
        return ok;
    }
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        pendingTranslatorMenu=false;
        ResetGameUiOwnership();
        scene = sceneName;
        UpdateTranslatorContext();
        textures.Restore();
        graphics.Reset();
        MelonLogger.Msg("Scene loaded: " + scene);
        VerifyZeroCanvas();
    }
    private static void VerifyZeroCanvas()
    {
        if(!Config.DebugLogging) return;
        var canvases=Resources.FindObjectsOfTypeAll<Canvas>();
        var owned=canvases.Where(c=>c!=null && IsTranslatorOwned(c.gameObject)).ToArray();
        MelonLogger.Msg($"[CORE] TranslatorCanvasCount={owned.Length}; gameCanvases={canvases.Length}");
        if(owned.Length>0) MelonLogger.Error("[CORE] Zero-Canvas invariant FAILED");
    }
    private static void ResetGameUiOwnership()
    {
        gameUiRoots.Clear();
        pendingUiRefreshes.Clear();
        pendingTextTraces.Clear();
        nativeStatusGenerations.Clear();
        if(Config.DebugLogging) MelonLogger.Msg("[GameUiRegistry] ownership boundary: cleared modal roots");
    }
    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (Config.Enabled && Config.TextureReplacement) textures.Apply(graphics);
        RefreshText(sceneName);
        VerifyZeroCanvas();

    }
    public override void OnUpdate()
    {
        hotkeys.Update(Config.ToggleTranslationKey, Config.ReloadTranslationKey, Config.DiagnosticKey);
        ProcessAutoReload();
        if(translatorPageDirty) { translatorPageDirty=false;RefreshNativeUiLabels(); }

        UpdateTextTraces();
        RefreshQueuedUiRoots();
        if(!smokeDone && scene=="Home" && Array.IndexOf(Environment.GetCommandLineArgs(),"--pvz-texture-probe")>=0)
        {
            smokeDone=true;
            try { TextureReplacementProbe.Run(paths.Root); TextureReadbackProbe.Run(Path.Combine(paths.Root,"Cache","ReadbackProbe")); }
            catch(Exception ex) {MelonLogger.Error("TEXTURE PROBE FAILED: "+ex);}
            finally {Application.Quit();}
        }
        if(!smokeDone && scene=="Home" && Array.IndexOf(Environment.GetCommandLineArgs(),"--pvz-readback-probe")>=0)
        {
            smokeDone=true;
            try { TextureReadbackProbe.Run(Path.Combine(paths.Root,"Cache","ReadbackProbe")); }
            catch(Exception ex) {MelonLogger.Error("READBACK PROBE FAILED: "+ex);}
            finally {Application.Quit();}
        }
        if (!smokeDone && scene == "Home" && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--pvz-translation-smoke") >= 0 && ++smokeFrames > 130)
        {
            smokeDone = true;
            RunSmokeTest();
        }
        var commandLine=Environment.GetCommandLineArgs();
        var phase4Probe=Array.IndexOf(commandLine,"--pvz-phase4-ui-probe")>=0||Array.IndexOf(commandLine,"--pvz-phase4-qa-probe")>=0||Array.IndexOf(commandLine,"--pvz-phase4-diagnostics-probe")>=0;
        if(!phase4ProbeStarted&&scene=="Home"&&phase4Probe&&++phase4ProbeFrames>180)
        {
            phase4ProbeStarted=true;MelonCoroutines.Start(RunPhase4UiProbe());
        }
    }
    private static bool ToggleTranslation(string trigger = "internal")
    {
        if(Config.DebugLogging) MelonLogger.Msg("[ACTION] "+trigger);
        VerifyZeroCanvas();
        Config.Enabled = !Config.Enabled;
        ConfigManager.Save(paths.ConfigFile, Config);
        fonts.SetEnabled(Config.Enabled && Config.FontReplacement);
        if(Config.Enabled) textures.Apply(graphics); else textures.Restore();
        RefreshText();
        RefreshNativeUiLabels();
        toast.Show(ModLabels.Get(Config.Language,Config.Enabled ? "translationEnabled" : "translationDisabled"));
        MelonLogger.Msg("Translation " + (Config.Enabled ? "ENABLED" : "DISABLED"));
        return true;
    }

    private static void RefreshNativeUiLabels()
    {
        foreach(var root in gameUiRoots.GetForegroundRoots()) {
            if(root==null) continue;
            if(IsNativeTranslatorMenuRoot(root)) UpdateNativeTranslatorMenu(root);
        }
    }

    private static GameObject FindDirectChild(Transform parent,string name)
    {
        if(parent==null) return null;
        for(int i=0;i<parent.childCount;i++) {
            var child=parent.GetChild(i);
            if(child!=null && string.Equals(child.gameObject.name,name,StringComparison.Ordinal)) return child.gameObject;
        }
        return null;
    }

    public static void InstallNativeHomeLauncher(Il2Cpp.HomeManager manager)
    {
        if(manager==null || manager.gameObject==null) return;
        currentHomeManager=manager;
        UpdateTranslatorContext();
        EnsureHomeLauncherHealthy("HomeManager.Start");
    }

    public static void EnsureHomeLauncherHealthy(string reason)
    {
        var manager=CurrentHomeManager();
        if(manager==null) { MelonLogger.Warning("[NativeUI] Home launcher health unavailable; reason="+reason); return; }
        GameObject canvas=null;
        foreach(var candidate in UnityEngine.Object.FindObjectsOfType<Canvas>(true)) {
            if(candidate!=null && candidate.gameObject!=null && candidate.gameObject.scene.handle==manager.gameObject.scene.handle && candidate.gameObject.name=="Canvas") { canvas=candidate.gameObject; break; }
        }
        if(canvas==null) {
            MelonLogger.Warning("[NativeUI] Home game Canvas not found"); return;
        }
        // Unity 6's generated IL2CPP Transform.Find(string) path overload can
        // bind to a ReadOnlySpan helper unavailable in this MelonLoader runtime.
        // Resolve the already cataloged direct children without that binding.
        var legacyParent=FindDirectChild(canvas.transform,"Button");
        var assets=NativeAssetResolver.Resolve(manager);
        var template=assets?.SmallButtonTemplate;
        var parent=canvas.transform;
        var existing=FindDirectChild(parent,NativeTranslatorMenuPolicy.LauncherName);
        var legacy=legacyParent==null ? null : FindDirectChild(legacyParent.transform,NativeTranslatorMenuPolicy.LauncherName);
        if(legacy!=null) UnityEngine.Object.Destroy(legacy);
        System.Action action=()=>OnLanguagesClicked(existing);
        UnityEngine.Events.UnityAction click=action;
        if(existing!=null) {
            var button=existing.GetComponent<Button>();
            var image=existing.GetComponent<Image>();
            if(button==null || image==null || image.sprite==null || !string.Equals(image.sprite.name,"ButtonSmall_0",StringComparison.Ordinal)) { UnityEngine.Object.Destroy(existing); existing=null; }
            else {
                button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(click);
                button.enabled=true;button.interactable=true;image.enabled=true;image.raycastTarget=true;existing.SetActive(true);
                var label=FindFirstText(existing);if(label!=null)label.text=ModLabels.Get(Config.Language,"menu.languages");
                if(Config.DebugLogging) MelonLogger.Msg($"[NativeUI] Home launcher healthy; reason={reason}; homeManager={manager.GetInstanceID()}; canvas={canvas.GetInstanceID()}; parent={canvas.GetInstanceID()}; launcher={existing.GetInstanceID()}; activeSelf={existing.activeSelf}; activeInHierarchy={existing.activeInHierarchy}; buttonEnabled={button.enabled}; interactable={button.interactable}; imageEnabled={image.enabled}; raycastTarget={image.raycastTarget}; listeners=1");
                VerifyZeroCanvas();return;
            }
        }
        if(template==null) { MelonLogger.Warning("[NativeUI] ButtonSmall launcher template missing"); return; }
        action=()=>OnLanguagesClicked(existing);
        click=action;
        var clone=NativeUiFactory.CloneNativeButton(template,parent,NativeTranslatorMenuPolicy.LauncherName,click);
        if(clone==null) return;
        existing=clone.Root;
        var rect=clone.Root.GetComponent<RectTransform>();
        rect.anchorMin=new Vector2(1f,1f); rect.anchorMax=new Vector2(1f,1f); rect.pivot=new Vector2(1f,1f);
        rect.anchoredPosition=new Vector2(-24f,-20f); rect.sizeDelta=new Vector2(280f,105f); rect.localScale=Vector3.one;
        ConfigureNativeButtonLabel(clone.Label,258f,84f,24f,36f);
        clone.Label.color=new Color(.97f,.90f,.72f,1f);
        clone.Label.text=ModLabels.Get(Config.Language,"menu.languages");
        clone.Root.SetActive(true);
        MelonLogger.Msg($"[NativeUI] Home Languages installed; hierarchy={clone.Hierarchy}; parent=Canvas; template=ButtonSmall_0; anchor=top-right; margin=24x20; size=280x105; originalListeners={clone.OriginalPersistentListeners}; listenersAfterRebind={clone.ListenersAfterRebind}; canvasCreated=False");
        VerifyZeroCanvas();
    }

    private static Il2Cpp.HomeManager CurrentHomeManager()
    {
        try { return currentHomeManager!=null && currentHomeManager.gameObject!=null ? currentHomeManager : null; }
        catch { currentHomeManager=null; return null; }
    }

    private static void OnLanguagesClicked(GameObject launcher)
    {
        var manager=CurrentHomeManager();
        var button=launcher==null ? null : launcher.GetComponent<Button>();
        var helpAlive=false;
        foreach(var help in UnityEngine.Object.FindObjectsOfType<Il2Cpp.HelpWindow>(true)) if(help!=null && help.gameObject!=null) {helpAlive=true;break;}
        if(Config.DebugLogging) MelonLogger.Msg($"[NativeUI] Languages click ENTER; scene={scene}; launcherAlive={launcher!=null}; launcherActive={(launcher!=null && launcher.activeInHierarchy)}; buttonInteractable={(button!=null && button.interactable)}; homeManagerAlive={manager!=null}; homeManagerActive={(manager!=null && manager.gameObject.activeInHierarchy)}; pendingMenu={pendingTranslatorMenu}; helpWindowAlive={helpAlive}");
        RequestNativeTranslatorMenu(manager);
    }

    private static void RequestNativeTranslatorMenu(Il2Cpp.HomeManager manager)
    {
        if(manager==null || manager.gameObject==null || !manager.gameObject.activeInHierarchy || pendingTranslatorMenu) return;
        pendingTranslatorMenu=true;
        if(Config.DebugLogging) MelonLogger.Msg("[NativeUI] translator menu requested through HomeManager.OnHelpClick");
        try { manager.OnHelpClick(); MelonCoroutines.Start(EnsureRequestedHelpWindow(manager)); }
        catch { pendingTranslatorMenu=false; throw; }
    }

    private static System.Collections.IEnumerator EnsureRequestedHelpWindow(Il2Cpp.HomeManager manager)
    {
        yield return null;yield return null;
        if(!pendingTranslatorMenu) yield break;
        if(manager==null || manager.gameObject==null || manager.helpWindowPrefab==null) {pendingTranslatorMenu=false;MelonLogger.Warning("[NativeUI] native HelpWindow request produced no window or prefab");yield break;}
        GameObject canvas=null;
        foreach(var item in UnityEngine.Object.FindObjectsOfType<Canvas>(true)) if(item!=null && item.gameObject.scene.handle==manager.gameObject.scene.handle && item.gameObject.name=="Canvas") {canvas=item.gameObject;break;}
        if(canvas==null) {pendingTranslatorMenu=false;MelonLogger.Warning("[NativeUI] native HelpWindow fallback has no game Canvas");yield break;}
        MelonLogger.Msg("[NativeUI] HomeManager.OnHelpClick produced no window; using native HelpWindow prefab fallback under game Canvas");
        var fallback=UnityEngine.Object.Instantiate<GameObject>(manager.helpWindowPrefab,canvas.transform,false);
        MelonCoroutines.Start(FinalizeFallbackHelpWindow(fallback));
    }

    private static System.Collections.IEnumerator FinalizeFallbackHelpWindow(GameObject fallback)
    {
        // Instantiate invokes Awake immediately, but Start may run on the next
        // frame. Let the game finish initializing its native HelpWindow first;
        // the normal Start patch may install the translator menu in that frame.
        yield return null;
        if(fallback==null) {pendingTranslatorMenu=false;MelonLogger.Warning("[NativeUI] native HelpWindow fallback was destroyed before initialization");yield break;}
        if(IsNativeTranslatorMenuRoot(fallback)) yield break;
        var window=fallback.GetComponent<Il2Cpp.HelpWindow>();
        if(window!=null && TryInstallNativeTranslatorMenu(window)) {
            MelonLogger.Msg("[NativeUI] native HelpWindow fallback converted directly after Start");
            yield break;
        }
        pendingTranslatorMenu=false;
        MelonLogger.Warning("[NativeUI] native HelpWindow fallback could not be converted; closing it");
        UnityEngine.Object.Destroy(fallback);
    }

    public static void TraceHomeLauncherSnapshot(string phase,Il2Cpp.HomeManager manager=null)
    {
        if(manager!=null) currentHomeManager=manager;
        if(!Config.DebugLogging) return;
        manager=CurrentHomeManager();
        GameObject canvas=null,parent=null,launcher=null;
        if(manager!=null) foreach(var item in UnityEngine.Object.FindObjectsOfType<Canvas>(true)) if(item!=null && item.gameObject.scene.handle==manager.gameObject.scene.handle && item.gameObject.name=="Canvas") {canvas=item.gameObject;break;}
        if(canvas!=null) {parent=canvas.gameObject;launcher=FindDirectChild(canvas.transform,NativeTranslatorMenuPolicy.LauncherName);}
        MelonLogger.Msg($"[NativeUI] Home snapshot; phase={phase}; scene={scene}; homeManager={(manager==null ? 0 : manager.GetInstanceID())}; canvas={(canvas==null ? 0 : canvas.GetInstanceID())}; parent={(parent==null ? 0 : parent.GetInstanceID())}; launcher={(launcher==null ? 0 : launcher.GetInstanceID())}; homeActive={(manager!=null && manager.gameObject.activeInHierarchy)}; launcherActive={(launcher!=null && launcher.activeInHierarchy)}");
    }

    public static void OnAlmanacSceneClosing(string sceneName)
    {
        if(!string.Equals(sceneName,"IllustratedIndex",StringComparison.Ordinal)) return;
        MelonCoroutines.Start(RepairAfterAlmanacReturn());
    }

    private static System.Collections.IEnumerator RepairAfterAlmanacReturn()
    {
        yield return null;yield return null;
        EnsureHomeLauncherHealthy("ButtonJump.CloseSceneButtonClick(IllustratedIndex)");
        TraceHomeLauncherSnapshot("after-almanac-return");
    }

    public static bool TryInstallNativeTranslatorMenu(Il2Cpp.HelpWindow window)
    {
        if(!pendingTranslatorMenu || window==null || window.gameObject==null) return false;
        pendingTranslatorMenu=false;
        var root=window.gameObject;
        var gameCanvas=root.transform.parent;
        while(gameCanvas!=null && gameCanvas.gameObject.GetComponent<Canvas>()==null) gameCanvas=gameCanvas.parent;
        var template=gameCanvas==null ? null : FindDirectChild(gameCanvas,NativeTranslatorMenuPolicy.LauncherName);
        if(gameCanvas==null || template==null) { MelonLogger.Warning("[NativeUI] translator menu template unavailable"); return false; }

        var title=FindDirectChild(root.transform,"Text1");
        var language=FindDirectChild(root.transform,"Text2");
        var status=FindDirectChild(root.transform,"Text3");
        var spare=FindDirectChild(root.transform,"Text4");
        var close=FindDirectChild(root.transform,"Image");
        if(title==null || language==null || status==null || spare==null || close==null) { MelonLogger.Warning("[NativeUI] HelpWindow structure changed"); return false; }
        var assets=NativeAssetResolver.Resolve(CurrentHomeManager());
        // Keep the game-created HelpWindow root name. Translator ownership is
        // marked below the native root so the game window never becomes a
        // translator-owned ancestor.
        status.name=NativeTranslatorMenuPolicy.StatusName;
        if(!NativeTranslatorShell.Build(root,assets,title,status,spare,close,Config.Language,page=>ShowNativeSettingsPage(root,page),CreateGeneralPageBindings(root),CreateContentPageBindings(root),CreateTranslatorPageBindings(root),CreateQaPageBindings(root),CreateDiagnosticsPageBindings(root),Config.UiScalePercent,fonts.ApplyTranslatorUi)) {
            MelonLogger.Warning("[NativeUI] final translator shell could not be built");
            UnityEngine.Object.Destroy(root);
            return false;
        }
        currentSettingsPage=NativeSettingsPage.General;
        ShowNativeSettingsPage(root,currentSettingsPage);
        UpdateNativeTranslatorMenu(root);
        RegisterUiRoot(root,GameUiRootKind.TranslatorMenu);
        MelonLogger.Msg("[NativeUI] translator menu installed; source=HelpWindow; pages=5; background=ChallengeBackground; tabs=ButtonSmall; close=OptionsBacktogameButton; gameCreated=True; repurposed=True; gameCanvas=True; translatorCanvas=False");
        VerifyZeroCanvas();
        return true;
    }

    public static void CloseNativeTranslatorMenu(GameObject root)
    {
        if(!IsNativeTranslatorMenuRoot(root)) return;
        nativeStatusGenerations.Remove(root.GetInstanceID());
        CloseUiRoot(root);
        MelonLogger.Msg("[NativeUI] translator menu closed through HelpWindow.OnClickClose");
    }

    private static bool IsNativeTranslatorMenuRoot(GameObject root)
        => root!=null && FindDirectChild(root.transform,NativeTranslatorMenuPolicy.StatusName)!=null;

    private static void ShowNativeSettingsPage(GameObject root,NativeSettingsPage page)
    {
        if(!IsNativeTranslatorMenuRoot(root)) return;
        currentSettingsPage=page;
        if(page==NativeSettingsPage.Qa)EnsureQaSnapshot();
        if(page==NativeSettingsPage.Diagnostics)EnsureDiagnosticsSnapshot();
        for(var i=0;i<root.transform.childCount;i++) {
            var child=root.transform.GetChild(i)?.gameObject;if(child==null)continue;
            foreach(var candidate in NativeSettingsMenuModel.Pages) if(child.name.StartsWith(NativeSettingsMenuModel.PagePrefix(candidate),StringComparison.Ordinal)) {child.SetActive(candidate==page);break;}
        }
        UpdateNativeTranslatorMenu(root);
    }

    private static System.Collections.IEnumerator RunPhase4UiProbe()
    {
        var commandLine=Environment.GetCommandLineArgs();
        var qaOnly=Array.IndexOf(commandLine,"--pvz-phase4-qa-probe")>=0;
        var diagnosticsOnly=Array.IndexOf(commandLine,"--pvz-phase4-diagnostics-probe")>=0;
        EnsureHomeLauncherHealthy("phase4-ui-probe");
        var root=FindOpenTranslatorMenu();
        if(root==null){RequestNativeTranslatorMenu(CurrentHomeManager());for(var i=0;i<6;i++)yield return null;root=FindOpenTranslatorMenu();}
        if(root==null){MelonLogger.Error("[Phase4Probe] first Settings window unavailable");yield break;}
        if(!diagnosticsOnly)
        {
            ShowNativeSettingsPage(root,NativeSettingsPage.Qa);yield return null;yield return null;
            MelonLogger.Msg($"[Phase4Probe] QA READY; status={qaSnapshot?.OverallStatus}; errors={qaSnapshot?.Errors}; warnings={qaSnapshot?.Warnings}; settingsRoots={CountOpenTranslatorMenus()}");
            if(qaOnly)yield break;
            for(var i=0;i<180;i++)yield return null;
            var window=root.GetComponent<Il2Cpp.HelpWindow>();if(window==null){MelonLogger.Error("[Phase4Probe] HelpWindow component unavailable for close lifecycle");yield break;}
            window.OnClickClose();
            for(var i=0;i<120&&CountOpenTranslatorMenus()!=0;i++)yield return null;
            if(CountOpenTranslatorMenus()!=0){MelonLogger.Error($"[Phase4Probe] close lifecycle retained {CountOpenTranslatorMenus()} Settings root(s)");yield break;}
            RequestNativeTranslatorMenu(CurrentHomeManager());for(var i=0;i<12;i++)yield return null;
            root=FindOpenTranslatorMenu();if(root==null){MelonLogger.Error("[Phase4Probe] reopened Settings window unavailable");yield break;}
        }
        ShowNativeSettingsPage(root,NativeSettingsPage.Diagnostics);yield return null;yield return null;
        diagnosticsDirty=true;EnsureDiagnosticsSnapshot();
        MelonLogger.Msg($"[Phase4Probe] Diagnostics READY; status={diagnosticsSnapshot?.OverallStatus}; canvas={diagnosticsSnapshot?.State.TranslatorCanvasCount}; launcher={diagnosticsSnapshot?.State.LauncherCount}; settingsRoots={diagnosticsSnapshot?.State.SettingsRootCount}");
        if(diagnosticsSnapshot?.OverallStatus==RuntimeHealthStatus.Fail||diagnosticsSnapshot?.State.SettingsRootCount!=1||diagnosticsSnapshot?.State.LauncherCount!=1||diagnosticsSnapshot?.State.TranslatorCanvasCount!=0)MelonLogger.Error("[Phase4Probe] FAIL: native QA/Diagnostics lifecycle health check failed");
        else MelonLogger.Msg("[Phase4Probe] PASS: native QA/Diagnostics lifecycle completed; game left open for inspection");
    }

    private static GameObject FindOpenTranslatorMenu()
    {
        foreach(var item in Resources.FindObjectsOfTypeAll<Il2Cpp.HelpWindow>())if(item!=null&&item.gameObject!=null&&IsNativeTranslatorMenuRoot(item.gameObject))return item.gameObject;
        return null;
    }

    private static int CountOpenTranslatorMenus()
        => Resources.FindObjectsOfTypeAll<Il2Cpp.HelpWindow>().Count(item=>item!=null&&item.gameObject!=null&&IsNativeTranslatorMenuRoot(item.gameObject));

    private static void ConfigureNativeButtonLabel(TMP_Text label,float width,float height,float min,float max)
    {
        if(label==null) return;
        var rect=label.rectTransform;
        if(rect!=null) { rect.anchorMin=new Vector2(.5f,.5f); rect.anchorMax=new Vector2(.5f,.5f); rect.anchoredPosition=Vector2.zero; rect.sizeDelta=new Vector2(width,height); rect.localScale=Vector3.one; }
        label.enableAutoSizing=true; label.fontSizeMin=min; label.fontSizeMax=max; label.fontSize=max; label.raycastTarget=false;
    }

    private static void SetNativeText(GameObject target,string value,Vector2 position,Vector2 size,float fontSize)
    {
        var label=FindFirstText(target); if(label==null) return;
        var rect=label.rectTransform; if(rect!=null) {rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;}
        label.enableAutoSizing=true;label.fontSizeMin=16f;label.fontSizeMax=fontSize;label.fontSize=fontSize;label.text=value;label.raycastTarget=false;
    }

    private static TMP_Text FindFirstText(GameObject root)
    {
        if(root==null) return null;
        var stack=new Stack<Transform>();stack.Push(root.transform);
        while(stack.Count>0) {var node=stack.Pop();if(node==null)continue;var text=node.gameObject.GetComponent(Il2CppInterop.Runtime.Il2CppType.Of<TMP_Text>())?.TryCast<TMP_Text>();if(text!=null)return text;for(var i=node.childCount-1;i>=0;i--)stack.Push(node.GetChild(i));}
        return null;
    }

    private static string CurrentLanguageDisplayName()
        => LocaleManager.DisplayName(Path.Combine(paths.Root,"Localization"),Config.Language);

    private static void CycleNativeLocale(GameObject root)
    {
        var infos=LocaleManager.AvailableLanguageInfos(Path.Combine(paths.Root,"Localization"),message=>MelonLogger.Warning("[NativeUI] "+message));
        var locales=infos.Select(x=>x.Locale).ToArray();
        if(locales.Length<2) {SetNativeMenuStatus(root,"status.onlyLocale");UpdateNativeTranslatorMenu(root);return;}
        var next=NativeTranslatorMenuPolicy.NextLocale(locales,Config.Language);
        if(SwitchLocale(next)) {SetNativeMenuStatus(root,"settings.language");UpdateNativeTranslatorMenu(root);}
    }

    private static void ToggleNativeMenuFeature(GameObject root,NativeMenuFeature feature)
    {
        if(feature==NativeMenuFeature.Translation) {ToggleTranslation("native-ui:settings.translation");UpdateNativeTranslatorMenu(root);return;}
        switch(feature) {
            case NativeMenuFeature.Font: Config.FontReplacement=!Config.FontReplacement; break;
            case NativeMenuFeature.Textures: Config.TextureReplacement=!Config.TextureReplacement; break;
            case NativeMenuFeature.Audio: Config.AudioReplacement=!Config.AudioReplacement; break;
            case NativeMenuFeature.TranslatorMode: Config.TranslatorMode=!Config.TranslatorMode; break;
        }
        ConfigManager.Save(paths.ConfigFile,Config);
        if(feature!=NativeMenuFeature.TranslatorMode) {
            ReloadOptionalAssets();
            if(Config.Enabled && Config.TextureReplacement) textures.Apply(graphics);
            RefreshText();
        }
        UpdateNativeTranslatorMenu(root);
        MelonLogger.Msg("[NativeUI] setting changed: "+feature);
    }

    private static GeneralPageBindings CreateGeneralPageBindings(GameObject root) => new() {
        CycleLanguage=()=>CycleNativeLocale(root),
        ToggleFeature=feature=>ToggleNativeMenuFeature(root,feature),
        ToggleNotifications=()=>ToggleUiNotifications(root),
        SetUiScale=value=>SetTranslatorUiScale(root,value),
        LanguageDisplayName=CurrentLanguageDisplayName,
        FeatureEnabled=FeatureEnabled,
        NotificationsEnabled=()=>Config.UiNotifications,
        UiScalePercent=()=>Config.UiScalePercent,
        Summary=GeneralPageSummary
    };

    private static ContentPageBindings CreateContentPageBindings(GameObject root) => new() {
        State=ContentState,
        ReloadAll=()=>ReloadMenuAction(root,"all"),
        ReloadTexts=()=>ReloadMenuAction(root,"texts"),
        ReloadFonts=()=>ReloadMenuAction(root,"fonts"),
        ReloadTextures=()=>ReloadMenuAction(root,"textures"),
        ReloadAudio=()=>ReloadMenuAction(root,"audio"),
        OpenLocale=()=>OpenMenuFolder(root,paths.Locale(Config.Language)),
        OpenMod=()=>OpenMenuFolder(root,paths.Root),
        OpenExports=()=>OpenMenuFolder(root,Path.Combine(paths.Root,"TranslationExport")),
        OpenTextures=()=>OpenMenuFolder(root,Path.Combine(paths.Locale(Config.Language),"Textures")),
        OpenAudio=()=>OpenMenuFolder(root,Path.Combine(paths.Locale(Config.Language),"Audio"))
    };

    private static TranslatorPageBindings CreateTranslatorPageBindings(GameObject root) => new() {
        State=TranslatorPageStateSnapshot,
        Toggle=setting=>ToggleTranslatorSetting(root,setting),
        ReloadNow=()=>ReloadMenuAction(root,"texts"),
        RefreshScreen=()=>RefreshCurrentVisibleScreen(root),
        ClearCapture=()=>ClearTranslatorCapture(root),
        CopySummary=()=>CopyTranslatorSummary(root),
        ExportUntranslated=()=>ExportTranslatorUntranslated(root),
        ExportCurrentScreen=()=>ExportTranslatorCurrentScreen(root),
        ExportSession=()=>ExportTranslatorSession(root),
        OpenExports=()=>OpenMenuFolder(root,Path.Combine(paths.Root,"TranslationExport"))
    };

    private static QaPageBindings CreateQaPageBindings(GameObject root) => new() {
        State=()=>qaSnapshot,
        RunFullQa=()=>RunQa(root),
        ExportReport=()=>ExportQaReport(root),
        ExportUnresolved=()=>ExportTranslatorUntranslated(root),
        OpenExports=()=>OpenExportCategory(root,"QA"),
        CopySummary=()=>CopyQaSummary(root)
    };

    private static DiagnosticsPageBindings CreateDiagnosticsPageBindings(GameObject root) => new() {
        State=()=>diagnosticsSnapshot,
        Refresh=()=>RefreshDiagnostics(root),Copy=()=>CopyDiagnostics(root),Export=()=>ExportDiagnosticsReport(root),
        OpenLogs=()=>OpenMenuFile(root,LatestLogPath()),OpenMod=()=>OpenMenuFolder(root,paths.Root),OpenExports=()=>OpenExportCategory(root,"Diagnostics")
    };

    private static void EnsureDiagnosticsSnapshot()
    {
        if(diagnosticsSnapshot!=null&&!diagnosticsDirty)return;
        try
        {
            var session=translatorSession.Snapshot();var roots=Resources.FindObjectsOfTypeAll<Il2Cpp.HelpWindow>().Count(x=>x!=null&&x.gameObject!=null&&IsNativeTranslatorMenuRoot(x.gameObject));
            var launchers=Resources.FindObjectsOfTypeAll<Button>().Count(x=>x!=null&&x.gameObject!=null&&x.gameObject.name==NativeTranslatorMenuPolicy.LauncherName);
            var canvases=Resources.FindObjectsOfTypeAll<Canvas>().Count(x=>x!=null&&IsTranslatorOwned(x.gameObject));
            var harmonyReady=false;try{var method=AccessTools.Method(typeof(Il2Cpp.HelpWindow),"Start");harmonyReady=HarmonyLib.Harmony.GetPatchInfo(method)?.Postfixes.Any(p=>p.PatchMethod?.DeclaringType?.Assembly==Assembly.GetExecutingAssembly())==true;}catch{}
            var last=reloadTransactions.Last;var exportDirectory=Path.Combine(paths.Root,"TranslationExport");Directory.CreateDirectory(exportDirectory);
            diagnosticsSnapshot=diagnosticsService.Capture(new DiagnosticsInput {
                ModVersion="1.1.0",GameVersion=Application.version,UnityVersion=Application.unityVersion,MelonVersion=typeof(MelonMod).Assembly.GetName().Version?.ToString()??"—",Il2Cpp=true,
                ActiveLanguage=Config.Language,Scene=string.IsNullOrWhiteSpace(scene)?"—":scene,Context=session.CurrentContext,TranslatorEnabled=Config.Enabled,CustomFontEnabled=Config.FontReplacement,TextureEnabled=Config.TextureReplacement,AudioEnabled=Config.AudioReplacement,
                TranslationStoreReady=service!=null,CollectorReady=true,FontReady=fonts.Ready,WatcherConfigured=Config.AutoReload,WatcherActive=autoReload.Active,SettingsActive=roots==1,HarmonyReady=harmonyReady,ExportDirectoryReady=Directory.Exists(exportDirectory),LanguageDirectoryReady=Directory.Exists(paths.Locale(Config.Language)),
                ExactEntries=exactCount,DynamicRules=dynamicCount,ContextOverrides=service?.Pack.Contexts.Sum(x=>x.Value.Count)??0,TextureMappings=textures.Count,AudioMappings=Audio.Count,
                TextureExplicitMappings=textures.ExplicitCount,TextureAutomaticMappings=textures.AutomaticCount,TextureCachedTextures=textures.CachedTextureCount,TextureCreatedSprites=textures.CreatedSpriteCount,TextureAppliedComponents=textures.AppliedCount,TextureRetainedLastGood=textures.RetainedLastGoodCount,LastTextureReloadDurationMilliseconds=textures.LastLoadDurationMilliseconds,LastTextureError=textures.LastError,
                StringsObserved=session.UniqueSeen,RuntimeTranslations=session.Translated,Preserved=session.Preserved,KnownUntranslated=session.KnownUntranslated,Unknown=session.Unknown,Captured=session.Unknown,WatchedFiles=autoReload.WatchedFileCount,TranslatorCanvasCount=canvases,LauncherCount=launchers,SettingsRootCount=roots,
                CurrentTab=currentSettingsPage.ToString(),LastFileChange=autoReload.LastDetectedAt,LastAutoReload=lastAutoReload,LastManualReload=lastManualReload,LastReloadDurationMilliseconds=last.DurationMilliseconds,LastReloadResult=!last.Attempted?"—":last.Success?"PASS":"FAIL",LastReloadError=last.Error??"",LastImportantEvent=last.Scope??"",WatcherError=autoReload.Error??""
            });diagnosticsDirty=false;MelonLogger.Msg($"[Diagnostics] refresh: status={diagnosticsSnapshot.OverallStatus}; failures={diagnosticsSnapshot.Failures}; warnings={diagnosticsSnapshot.Warnings}; translatorCanvas={canvases}; launcher={launchers}; settingsRoots={roots}");
        }
        catch(Exception ex){MelonLogger.Error("[Diagnostics] refresh failed safely: "+ex.Message);}
    }

    private static void RefreshDiagnostics(GameObject root){diagnosticsDirty=true;EnsureDiagnosticsSnapshot();UpdateNativeTranslatorMenu(root);SetNativeMenuStatus(root,diagnosticsSnapshot==null?"status.actionFailed":"status.diagnosticsRefreshed",diagnosticsSnapshot==null);}
    private static string DiagnosticsSummaryText(){EnsureDiagnosticsSnapshot();var s=diagnosticsSnapshot;if(s==null)return "Diagnostics unavailable";return $"PvZ Symbiosis Translator Diagnostics\nStatus: {s.OverallStatus}\nMod: {s.State.ModVersion}\nGame: {s.State.GameVersion}\nLocale: {s.State.ActiveLanguage}\nScene: {s.State.Scene}\nContext: {s.State.Context}\nFailures: {s.Failures}\nWarnings: {s.Warnings}\nTranslator Canvas: {s.State.TranslatorCanvasCount}\nLauncher: {s.State.LauncherCount}\nSettings roots: {s.State.SettingsRootCount}\nWatcher: {(s.State.WatcherActive?"PASS":"OFF")}";}
    private static void CopyDiagnostics(GameObject root){try{using var process=new System.Diagnostics.Process{StartInfo=new System.Diagnostics.ProcessStartInfo{FileName="clip.exe",UseShellExecute=false,RedirectStandardInput=true,CreateNoWindow=true}};process.Start();process.StandardInput.Write(DiagnosticsSummaryText());process.StandardInput.Close();process.WaitForExit(2000);if(process.ExitCode!=0)throw new IOException("clip.exe exit code "+process.ExitCode);SetNativeMenuStatus(root,"status.diagnosticsCopied");}catch(Exception ex){MelonLogger.Error("[Diagnostics] clipboard failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}}
    private static void ExportDiagnosticsReport(GameObject root){try{diagnosticsDirty=true;EnsureDiagnosticsSnapshot();if(diagnosticsSnapshot==null)throw new InvalidOperationException("Diagnostics unavailable");var result=diagnosticsReports.Write(diagnosticsSnapshot);MelonLogger.Msg("[Diagnostics] reports exported: "+Path.GetRelativePath(paths.Root,result.JsonPath)+"; "+Path.GetRelativePath(paths.Root,result.TextPath));SetNativeMenuStatus(root,"status.diagnosticsExported");}catch(Exception ex){MelonLogger.Error("[Diagnostics] export failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}}
    private static string LatestLogPath(){var game=Directory.GetParent(Directory.GetParent(paths.Root)?.FullName??paths.Root)?.FullName??paths.Root;return Path.Combine(game,"MelonLoader","Latest.log");}
    private static void OpenExportCategory(GameObject root,string category){var directory=Path.Combine(paths.Root,"TranslationExport",category);Directory.CreateDirectory(directory);OpenMenuFolder(root,directory);}
    private static void OpenMenuFile(GameObject root,string file){try{if(!File.Exists(file))throw new FileNotFoundException(file);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=file,UseShellExecute=true});SetNativeMenuStatus(root,"status.folderOpened");}catch(Exception ex){MelonLogger.Error("[NativeUI] file open failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}}

    private static QaRuntimeMetrics QaRuntimeState()
    {
        var snapshot=translatorSession.Snapshot();
        return new QaRuntimeMetrics {RuntimeObserved=snapshot.UniqueSeen,RuntimeTranslated=snapshot.Translated,Preserved=snapshot.Preserved,KnownUntranslated=snapshot.KnownUntranslated,Unknown=snapshot.Unknown};
    }

    private static void EnsureQaSnapshot()
    {
        if(qaSnapshot!=null&&!qaDirty)return;
        try {qaSnapshot=qaService.RunFullScan(paths.Locale(Config.Language),Config.Language,Application.version,QaRuntimeState());qaDirty=false;MelonLogger.Msg($"[QA] scan complete: status={qaSnapshot.OverallStatus}; errors={qaSnapshot.Errors}; warnings={qaSnapshot.Warnings}; info={qaSnapshot.Information}");}
        catch(Exception ex){MelonLogger.Error("[QA] scan failed safely: "+ex.Message);}
    }

    private static void RunQa(GameObject root)
    {
        qaDirty=true;EnsureQaSnapshot();UpdateNativeTranslatorMenu(root);SetNativeMenuStatus(root,qaSnapshot==null?"status.actionFailed":"status.qaComplete",qaSnapshot==null);
    }

    private static string QaSummaryText()
    {
        EnsureQaSnapshot();var s=qaSnapshot;if(s==null)return "QA unavailable";
        return $"PvZ Symbiosis Translator QA\nLocale: {s.Locale}\nStatus: {s.OverallStatus}\nErrors: {s.Errors}\nWarnings: {s.Warnings}\nInformation: {s.Information}\nExact: {s.ExactTranslations}\nDynamic: {s.DynamicRules}\nContext overrides: {s.ContextOverrides}\nRuntime unknown: {s.Runtime.Unknown}\nScan: {s.Timestamp:O}";
    }

    private static void CopyQaSummary(GameObject root)
    {
        try {using var process=new System.Diagnostics.Process {StartInfo=new System.Diagnostics.ProcessStartInfo {FileName="clip.exe",UseShellExecute=false,RedirectStandardInput=true,CreateNoWindow=true}};process.Start();process.StandardInput.Write(QaSummaryText());process.StandardInput.Close();process.WaitForExit(2000);if(process.ExitCode!=0)throw new IOException("clip.exe exit code "+process.ExitCode);SetNativeMenuStatus(root,"status.qaSummaryCopied");}
        catch(Exception ex){MelonLogger.Error("[QA] clipboard failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}
    }

    private static void ExportQaReport(GameObject root)
    {
        try {EnsureQaSnapshot();if(qaSnapshot==null)throw new InvalidOperationException("QA snapshot unavailable");var result=qaReports.Write(qaSnapshot,"1.1.0");MelonLogger.Msg("[QA] reports exported: "+Path.GetRelativePath(paths.Root,result.JsonPath)+"; "+Path.GetRelativePath(paths.Root,result.TextPath));SetNativeMenuStatus(root,"status.qaExported");}
        catch(Exception ex){MelonLogger.Error("[QA] export failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}
    }

    private static TranslatorPageState TranslatorPageStateSnapshot()
    {
        var snapshot=translatorSession.Snapshot();
        return new TranslatorPageState {
            Mode=Config.TranslatorMode,Debug=Config.DebugLogging,AutoReload=Config.AutoReload,CaptureUnknown=Config.CaptureUnknown,AutoExport=Config.AutoExportOnContextChange,DetailedContext=Config.DetailedContext,
            Context=snapshot.CurrentContext,Scene=string.IsNullOrWhiteSpace(scene)?"—":scene,UniqueSeen=snapshot.UniqueSeen,Translated=snapshot.Translated,Preserved=snapshot.Preserved,KnownUntranslated=snapshot.KnownUntranslated,RuntimeUnknown=RuntimeCoverageCounts().unknown,SessionUnknown=snapshot.Unknown,
            WatchedFiles=autoReload.WatchedFileCount,LastChange=autoReload.LastDetectedAt?.ToLocalTime().ToString("HH:mm:ss")??"—",LastReload=lastAutoReload?.ToLocalTime().ToString("HH:mm:ss")??"—",
            Watcher=ModLabels.Get(Config.Language,!string.IsNullOrWhiteSpace(autoReload.Error)?"translator.watcherError":autoReload.Active?"translator.watcherActive":"translator.watcherDisabled")
        };
    }

    private static void ToggleTranslatorSetting(GameObject root,TranslatorSetting setting)
    {
        switch(setting) {
            case TranslatorSetting.Mode: Config.TranslatorMode=!Config.TranslatorMode;break;
            case TranslatorSetting.Debug: Config.DebugLogging=!Config.DebugLogging;break;
            case TranslatorSetting.AutoReload: Config.AutoReload=!Config.AutoReload;break;
            case TranslatorSetting.CaptureUnknown: if(Config.TranslatorMode)Config.CaptureUnknown=!Config.CaptureUnknown;break;
            case TranslatorSetting.AutoExport: if(Config.TranslatorMode)Config.AutoExportOnContextChange=!Config.AutoExportOnContextChange;break;
            case TranslatorSetting.DetailedContext: if(Config.TranslatorMode)Config.DetailedContext=!Config.DetailedContext;break;
        }
        if(setting==TranslatorSetting.Debug&&service!=null)service.Pack.Dynamic.FailureDiagnostic=Config.DebugLogging?(id,source,failure)=>MelonLogger.Warning($"Dynamic rule {id}: {failure}; source={source}"):null;
        ConfigManager.Save(paths.ConfigFile,Config);if(setting==TranslatorSetting.AutoReload)UpdateAutoReloadWatcher();UpdateNativeTranslatorMenu(root);
        if(setting==TranslatorSetting.Mode&&Config.TranslatorMode)RefreshText(scene);
        SetNativeMenuStatus(root,setting==TranslatorSetting.AutoReload?(Config.AutoReload?"status.autoReloadEnabled":"status.autoReloadDisabled"):"status.settingSaved");
    }

    private static void RefreshCurrentVisibleScreen(GameObject root)
    {
        foreach(var item in gameUiRoots.GetForegroundRoots())if(item!=null&&!IsNativeTranslatorMenuRoot(item))RefreshUiRoot(item);
        RefreshText(scene);SetNativeMenuStatus(root,"status.screenRefreshed");translatorPageDirty=true;
    }

    private static void ClearTranslatorCapture(GameObject root){translatorSession.Clear();UpdateTranslatorContext();SetNativeMenuStatus(root,"status.captureCleared");UpdateNativeTranslatorMenu(root);}
    private static string TranslatorSummary()
    {
        var s=translatorSession.Snapshot();return $"PvZ Symbiosis Translator v1.1.0\nLocale: {Config.Language}\nContext: {s.CurrentContext}\nExact: {exactCount}\nDynamic: {dynamicCount}\nTranslated runtime: {s.Translated}\nUnknown runtime: {s.Unknown}\nCaptured this session: {s.Unknown}\nAuto reload: {(Config.AutoReload?"ON":"OFF")}\nWatcher: {(autoReload.Active?"OK":"OFF")}";
    }
    private static void CopyTranslatorSummary(GameObject root)
    {
        try { using var process=new System.Diagnostics.Process {StartInfo=new System.Diagnostics.ProcessStartInfo {FileName="clip.exe",UseShellExecute=false,RedirectStandardInput=true,CreateNoWindow=true}};process.Start();process.StandardInput.Write(TranslatorSummary());process.StandardInput.Close();process.WaitForExit(2000);if(process.ExitCode!=0)throw new IOException("clip.exe exit code "+process.ExitCode);SetNativeMenuStatus(root,"status.summaryCopied"); }
        catch(Exception ex){MelonLogger.Error("[Translator] clipboard failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}
    }
    private static object ReloadExportState()=>new {reloadTransactions.Last.Attempted,reloadTransactions.Last.Success,reloadTransactions.Last.Scope,reloadTransactions.Last.Timestamp,reloadTransactions.Last.DurationMilliseconds,reloadTransactions.Last.FilesProcessed,reloadTransactions.Last.Error};
    private static void ExportTranslatorUntranslated(GameObject root){try{translatorExports.ExportUntranslated(translatorSession.Snapshot(),Config.Language);SetNativeMenuStatus(root,"status.untranslatedExported");}catch(Exception ex){MelonLogger.Error("[Translator] export failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}}
    private static void ExportTranslatorCurrentScreen(GameObject root){try{var s=translatorSession.Snapshot();translatorExports.ExportContext(s,Config.Language,s.CurrentContext);SetNativeMenuStatus(root,"status.sceneExported");}catch(Exception ex){MelonLogger.Error("[Translator] screen export failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}}
    private static void ExportTranslatorSession(GameObject root){try{translatorExports.ExportSession(translatorSession.Snapshot(),Config.Language,ReloadExportState());SetNativeMenuStatus(root,"status.sessionExported");}catch(Exception ex){MelonLogger.Error("[Translator] session export failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}}

    private static void UpdateAutoReloadWatcher()
    {
        if(!Config.AutoReload){autoReload.Stop();translatorPageDirty=true;return;}autoReload.Start(paths.Locale(Config.Language));
        if(!autoReload.Active)MelonLogger.Error("[AutoReload] watcher failed: "+autoReload.Error);
        else MelonLogger.Msg($"[AutoReload] watcher active; files={autoReload.WatchedFileCount}; debounce=750ms");
        translatorPageDirty=true;
    }
    private static void ProcessAutoReload()
    {
        if(!Config.AutoReload||!autoReload.TryDequeue(DateTimeOffset.UtcNow,out var batch))return;
        MelonLogger.Msg($"[AutoReload] {batch.Files.Count} changed file(s); main-thread reload");
        var ok=ReloadLocalization("auto",false,true);if(ok){ReloadOptionalAssets();if(Config.Enabled&&Config.TextureReplacement)textures.Apply(graphics);RefreshText();lastAutoReload=DateTimeOffset.UtcNow;SetNativeMenuStatus("status.autoReloaded");}else SetNativeMenuStatus("status.reloadPreserved");
        translatorPageDirty=true;diagnosticsDirty=true;
    }

    private static void LoadKnownRuntimeSources()
    {
        knownRuntimeSources.Clear();var catalog=Path.Combine(paths.Dumps,"source_catalog.json");
        try{if(File.Exists(catalog))using(var doc=System.Text.Json.JsonDocument.Parse(File.ReadAllText(catalog)))foreach(var e in doc.RootElement.EnumerateArray())if(e.TryGetProperty("source",out var value)&&value.ValueKind==System.Text.Json.JsonValueKind.String)knownRuntimeSources.Add(value.GetString());}catch(Exception ex){MelonLogger.Warning("[Translator] source catalog unavailable: "+ex.Message);}
    }
    private static string RuntimeTranslationStatus(string source,string context,string target)
    {
        return service?.Classify(source,context,Config.DynamicRules,knownRuntimeSources.Contains(source)).StatusCode ?? "unknown";
    }
    private static void ObserveTranslatorSession(string source,string target,string context,string actualScene,string objectName,string component,string hierarchy)
    {
        if(!Config.TranslatorMode)return;var status=RuntimeTranslationStatus(source,context,target);if(status=="rendered-target")return;if((status=="unknown"||status=="known-untranslated")&&!Config.CaptureUnknown)return;
        if(translatorSession.Observe(source,target,VisibleTranslatorContext(),actualScene,objectName,component,hierarchy,status,Config.DetailedContext))translatorPageDirty=true;
    }
    private static string VisibleTranslatorContext()
    {
        foreach(var item in gameUiRoots.GetForegroundRoots().Reverse())if(item!=null&&!IsNativeTranslatorMenuRoot(item)){var n=item.name??"";if(n.StartsWith("CustomMode",StringComparison.Ordinal))return "CustomMode";if(n.StartsWith("Difficulty",StringComparison.Ordinal))return "Difficulty";if(n.StartsWith("Setup",StringComparison.Ordinal))return "Setup";}
        var home=CurrentHomeManager();if(home!=null&&home.gameObject.activeInHierarchy)return "Home";
        if(scene.StartsWith("Illustrated",StringComparison.Ordinal))return "Almanac";return string.IsNullOrWhiteSpace(scene)?"Unknown":scene;
    }
    private static void UpdateTranslatorContext()
    {
        var previous=translatorSession.CurrentContext;var next=VisibleTranslatorContext();if(!translatorSession.SetContext(next))return;
        if(Config.TranslatorMode&&Config.AutoExportOnContextChange&&previous!="Unknown")try{translatorExports.ExportContext(translatorSession.Snapshot(),Config.Language,previous,lastAutoExport);lastAutoExport=DateTimeOffset.UtcNow;}catch(Exception ex){MelonLogger.Warning("[Translator] auto-export failed: "+ex.Message);}
        translatorPageDirty=true;
    }

    private static ContentPageState ContentState()
    {
        var last=reloadTransactions.Last;
        return new ContentPageState {
            ActiveLanguage=CurrentLanguageDisplayName(),
            LocaleFolder=Path.Combine("Localization",Config.Language),
            PrimaryFont=ConfiguredFontNames(false),
            FallbackFonts=ConfiguredFontNames(true),
            Exact=exactCount,
            Dynamic=dynamicCount,
            TextureMappings=textures.Count,
            AudioMappings=Audio.Count,
            InstalledLocales=LocaleManager.AvailableLanguages(Path.Combine(paths.Root,"Localization")).Count,
            ReloadAttempted=last.Attempted,
            ReloadSucceeded=last.Success,
            ReloadTime=last.Attempted?last.Timestamp.ToLocalTime().ToString("HH:mm:ss"):"—",
            ReloadDurationMilliseconds=last.DurationMilliseconds,
            ReloadFiles=last.FilesProcessed,
            ReloadError=last.Error
        };
    }

    private static bool FeatureEnabled(NativeMenuFeature feature) => feature switch {
        NativeMenuFeature.Translation=>Config.Enabled,
        NativeMenuFeature.Font=>Config.FontReplacement,
        NativeMenuFeature.Textures=>Config.TextureReplacement,
        NativeMenuFeature.Audio=>Config.AudioReplacement,
        NativeMenuFeature.TranslatorMode=>Config.TranslatorMode,
        _=>false
    };

    private static void ToggleUiNotifications(GameObject root)
    {
        Config.UiNotifications=!Config.UiNotifications;
        ConfigManager.Save(paths.ConfigFile,Config);
        if(!Config.UiNotifications) {
            var status=FindDirectChild(root.transform,NativeTranslatorMenuPolicy.StatusName);
            var label=FindFirstText(status);if(label!=null)label.text="";
        }
        UpdateNativeTranslatorMenu(root);
        MelonLogger.Msg("[NativeUI] interface notifications "+(Config.UiNotifications?"enabled":"disabled"));
    }

    private static void SetTranslatorUiScale(GameObject root,float percent)
    {
        var clamped=TranslatorUiLayout.ClampScale(percent);
        if(Math.Abs(Config.UiScalePercent-clamped)<.001f)return;
        Config.UiScalePercent=clamped;
        NativeTranslatorShell.ApplyScale(root,clamped);
        UpdateNativeTranslatorMenu(root);
        var generation=++uiScaleSaveGeneration;
        MelonCoroutines.Start(PersistTranslatorUiScale(root,generation));
    }

    private static System.Collections.IEnumerator PersistTranslatorUiScale(GameObject root,int generation)
    {
        for(var i=0;i<12;i++)yield return null;
        if(generation!=uiScaleSaveGeneration)yield break;
        try {
            ConfigManager.Save(paths.ConfigFile,Config);
            MelonLogger.Msg($"[NativeUI] interface scale saved: {Config.UiScalePercent:0}%");
        } catch(Exception ex) {
            MelonLogger.Error("[NativeUI] interface scale save failed: "+ex.Message);
            SetNativeMenuStatus(root,"status.actionFailed",true);
        }
    }

    private static string GeneralPageSummary()
        => $"{ModLabels.Get(Config.Language,"general.language")}: {CurrentLanguageDisplayName()}\n"+
           $"{ModLabels.Get(Config.Language,"general.pack")}: {exactCount} {ModLabels.Get(Config.Language,"stats.exact").ToLowerInvariant()} • {dynamicCount} {ModLabels.Get(Config.Language,"stats.dynamic").ToLowerInvariant()}\n"+
           $"{ModLabels.Get(Config.Language,"general.version")}: 1.1.0";

    private static void ToggleDebugLogging(GameObject root)
    {
        Config.DebugLogging=!Config.DebugLogging;
        ConfigManager.Save(paths.ConfigFile,Config);
        Reload();
        LoadKnownRuntimeSources();
        UpdateAutoReloadWatcher();
        UpdateNativeTranslatorMenu(root);
        MelonLogger.Msg("[NativeUI] debug logging "+(Config.DebugLogging ? "enabled" : "disabled"));
    }

    private static void ReloadMenuAction(GameObject root,string subsystem)
    {
        var locale=paths.Locale(Config.Language);
        bool ok;
        switch(subsystem) {
            case "all": ok=ReloadWithToast("native-ui:reload-all");break;
            case "texts":
                ok=ReloadLocalization("texts",false,false);
                if(ok)RefreshText();
                break;
            case "fonts": ok=reloadTransactions.Execute("fonts",1,()=>locale,value=>{fonts.Load(value,Config.Enabled&&Config.FontReplacement);RefreshText();});break;
            case "textures": ok=reloadTransactions.Execute("textures",1,()=>locale,value=>{textures.Load(value,Config.Enabled&&Config.TextureReplacement,Config.DebugLogging);if(Config.Enabled&&Config.TextureReplacement)textures.Apply(graphics);});break;
            case "audio": ok=reloadTransactions.Execute("audio",1,()=>locale,value=>{Audio.ClearCache();Audio.Load(value,Config.Enabled&&Config.AudioReplacement);});break;
            default: throw new ArgumentOutOfRangeException(nameof(subsystem));
        }
        if(subsystem!="all")lastManualReload=DateTimeOffset.UtcNow;diagnosticsDirty=true;
        SetNativeMenuStatus(root,ok?"status.reloaded":"status.reloadPreserved",!ok);
        if(!ok)MelonLogger.Error("[NativeUI] reload action failed; previous translation retained: "+reloadTransactions.Last.Error);
        UpdateNativeTranslatorMenu(root);
    }

    private static void OpenMenuFolder(GameObject root,string folder)
    {
        try {
            if(!Directory.Exists(folder))throw new DirectoryNotFoundException(folder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {FileName=folder,UseShellExecute=true});
            SetNativeMenuStatus(root,"status.folderOpened");
        } catch(Exception ex) {MelonLogger.Error("[NativeUI] open folder failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}
    }

    private static string GameRoot()
        => Directory.GetParent(Directory.GetParent(paths.Root).FullName).FullName;

    private static void ExportUntranslatedMenu(GameObject root)
    {
        Dump(false);
        SetNativeMenuStatus(root,"status.exported");
        UpdateNativeTranslatorMenu(root);
    }

    private static void ExportCurrentSceneMenu(GameObject root)
    {
        try {
            var sources=new SortedSet<string>(StringComparer.Ordinal);
            var result=scanner.Scan(OriginalDiagnosticSource,(component,source)=>{
                if(component!=null && !IsModUi(component)) sources.Add(source);
            });
            var folder=Path.Combine(paths.Root,"TranslationExport","CurrentScene");Directory.CreateDirectory(folder);
            var context=string.IsNullOrWhiteSpace(scene) ? "Unknown" : string.Concat(scene.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));
            File.WriteAllText(Path.Combine(folder,context+".json"),System.Text.Json.JsonSerializer.Serialize(new {context,tmp=result.Tmp,legacy=result.Legacy,sources=sources.ToArray()},ConfigManager.HumanReadableJson));
            SetNativeMenuStatus(root,"status.sceneExported");
        } catch(Exception ex) {MelonLogger.Error("[NativeUI] current screen export failed: "+ex.Message);SetNativeMenuStatus(root,"status.actionFailed",true);}
    }

    private static void ReloadNativeMenu(GameObject root)
    {
        var ok=ReloadWithToast("native-ui:settings.reload");
        SetNativeMenuStatus(root,ok ? "status.reloaded" : "status.reloadFailed",!ok);
        UpdateNativeTranslatorMenu(root);
    }

    private static void ExportNativeMenu(GameObject root)
    {
        ExportTextDiagnostics("native-ui:settings.export");
        SetNativeMenuStatus(root,"status.exported");
    }

    private static void SetNativeMenuStatus(GameObject root,string key,bool important=false)
    {
        if(!IsNativeTranslatorMenuRoot(root)) return;
        var status=FindDirectChild(root.transform,NativeTranslatorMenuPolicy.StatusName);
        var label=FindFirstText(status);if(label==null)return;
        var id=root.GetInstanceID();
        var generation=nativeStatusGenerations.TryGetValue(id,out var previous)?previous+1:1;
        nativeStatusGenerations[id]=generation;
        var message=Config.UiNotifications || important ? ModLabels.Get(Config.Language,key) : "";
        label.text=message;
        if(message.Length>0)MelonCoroutines.Start(ClearNativeMenuStatus(root,id,generation,message,important?6f:3.5f));
    }

    private static System.Collections.IEnumerator ClearNativeMenuStatus(GameObject root,int id,int generation,string expected,float seconds)
    {
        var remaining=seconds;
        while(remaining>0f){yield return null;remaining-=Time.unscaledDeltaTime;}
        if(root==null || !nativeStatusGenerations.TryGetValue(id,out var current) || current!=generation)yield break;
        var label=FindFirstText(FindDirectChild(root.transform,NativeTranslatorMenuPolicy.StatusName));
        if(label!=null && string.Equals(label.text,expected,StringComparison.Ordinal))label.text="";
        nativeStatusGenerations.Remove(id);
    }

    private static void SetNativeMenuStatus(string key)
    {
        var important=key=="status.reloadFailed" || key=="status.reloadPreserved" || key=="status.actionFailed" || key=="status.invalidCandidate";
        foreach(var root in gameUiRoots.GetForegroundRoots()) if(IsNativeTranslatorMenuRoot(root)) SetNativeMenuStatus(root,key,important);
    }

    private static void UpdateNativeTranslatorMenu(GameObject root)
    {
        if(!IsNativeTranslatorMenuRoot(root)) return;
        var generalRoot=FindDirectChild(root.transform,NativeSettingsMenuModel.PagePrefix(NativeSettingsPage.General)+"Root");
        GeneralPage.Refresh(generalRoot,Config.Language,CreateGeneralPageBindings(root));
        var contentRoot=FindDirectChild(root.transform,NativeSettingsMenuModel.PagePrefix(NativeSettingsPage.Content)+"Root");
        ContentPage.Refresh(contentRoot,Config.Language,CreateContentPageBindings(root));
        var translatorRoot=FindDirectChild(root.transform,NativeSettingsMenuModel.PagePrefix(NativeSettingsPage.Translator)+"Root");
        TranslatorPage.Refresh(translatorRoot,Config.Language,NativeAssetResolver.Resolve(CurrentHomeManager()),CreateTranslatorPageBindings(root));
        var qaRoot=FindDirectChild(root.transform,NativeSettingsMenuModel.PagePrefix(NativeSettingsPage.Qa)+"Root");
        QaPage.Refresh(qaRoot,Config.Language,CreateQaPageBindings(root));
        if(currentSettingsPage==NativeSettingsPage.Diagnostics&&diagnosticsDirty)EnsureDiagnosticsSnapshot();
        var diagnosticsRoot=FindDirectChild(root.transform,NativeSettingsMenuModel.PagePrefix(NativeSettingsPage.Diagnostics)+"Root");
        DiagnosticsPage.RefreshPage(diagnosticsRoot,Config.Language,CreateDiagnosticsPageBindings(root));
        var translatorPrefix=NativeSettingsMenuModel.PagePrefix(NativeSettingsPage.Translator);
        SetNativeButtonState(root,translatorPrefix+"TranslatorMode",Config.TranslatorMode,"settings.enabledMasculine","settings.disabledMasculine");
        SetNativeButtonState(root,translatorPrefix+"DebugLogging",Config.DebugLogging,"settings.enabledMasculine","settings.disabledMasculine");

        foreach(var page in NativeSettingsMenuModel.Pages) {
            var tab=NativeTranslatorMenuPolicy.TabPrefix+page;
            SetNativeButtonText(root,tab,ModLabels.Get(Config.Language,NativeSettingsMenuModel.PageKey(page)));
            SetNativeButtonColor(root,tab,page==currentSettingsPage ? new Color(1f,.9f,.55f,1f) : Color.white);
        }
        UpdateActionLabel(root,NativeSettingsPage.Content,"ReloadAll","actions.reloadAll");
        UpdateActionLabel(root,NativeSettingsPage.Content,"ReloadTexts","actions.reloadTexts");
        UpdateActionLabel(root,NativeSettingsPage.Content,"ReloadFonts","actions.reloadFonts");
        UpdateActionLabel(root,NativeSettingsPage.Content,"ReloadTextures","actions.reloadTextures");
        UpdateActionLabel(root,NativeSettingsPage.Content,"ReloadAudio","actions.reloadAudio");
        UpdateActionLabel(root,NativeSettingsPage.Content,"OpenLocale","actions.openLocaleFolder");
        UpdateActionLabel(root,NativeSettingsPage.Content,"OpenExports","actions.openExportFolder");
        UpdateActionLabel(root,NativeSettingsPage.Content,"OpenMod","actions.openModFolder");
        UpdateActionLabel(root,NativeSettingsPage.Content,"OpenTextures","actions.openTexturesFolder");
        UpdateActionLabel(root,NativeSettingsPage.Content,"OpenAudio","actions.openAudioFolder");
        UpdateActionLabel(root,NativeSettingsPage.Translator,"ExportUntranslated","actions.exportUntranslated");
        UpdateActionLabel(root,NativeSettingsPage.Translator,"ExportDiagnostics","actions.exportDiagnostics");
        UpdateActionLabel(root,NativeSettingsPage.Translator,"ExportCurrentScene","actions.exportCurrentScene");
        UpdateActionLabel(root,NativeSettingsPage.Translator,"OpenLocale","actions.openLocaleFolder");
        UpdateActionLabel(root,NativeSettingsPage.Translator,"OpenExport","actions.openExportFolder");
        UpdateActionLabel(root,NativeSettingsPage.Diagnostics,"ExportDiagnostics","actions.exportDiagnostics");
        UpdateActionLabel(root,NativeSettingsPage.Diagnostics,"OpenLogs","actions.openLogs");
        UpdateActionLabel(root,NativeSettingsPage.Diagnostics,"OpenExport","actions.openExportFolder");
        UpdatePageLabels(root);
        UpdatePageSummaries(root);
        var title=FindDirectChild(root.transform,"Text1");var close=FindDirectChild(root.transform,"Image");
        var titleText=FindFirstText(title);if(titleText!=null)titleText.text=ModLabels.Get(Config.Language,"menu.title");
        var closeText=FindFirstText(close);if(closeText!=null)closeText.text=ModLabels.Get(Config.Language,"common.close");
        NativeTranslatorShell.ApplyMenuFont(root,fonts.ApplyTranslatorUi);
    }

    private static void UpdateActionLabel(GameObject root,NativeSettingsPage page,string name,string key)
        => SetNativeButtonText(root,NativeSettingsMenuModel.PagePrefix(page)+name,ModLabels.Get(Config.Language,key));

    private static void UpdatePageLabels(GameObject root)
    {
        for(var i=0;i<root.transform.childCount;i++) {
            var child=root.transform.GetChild(i)?.gameObject;if(child==null)continue;
            var marker="Label.";var index=child.name.IndexOf(marker,StringComparison.Ordinal);
            if(index<0)continue;
            var key=child.name.Substring(index+marker.Length);
            var label=FindFirstText(child);if(label!=null)label.text=ModLabels.Get(Config.Language,key);
        }
    }

    private static void SetNativeButtonState(GameObject root,string name,bool enabled,string enabledKey,string disabledKey)
    {
        SetNativeButtonText(root,name,ModLabels.Get(Config.Language,enabled ? enabledKey : disabledKey));
        SetNativeButtonColor(root,name,enabled ? new Color(.72f,1f,.72f,1f) : new Color(1f,.75f,.70f,1f));
    }

    private static void SetNativeButtonColor(GameObject root,string name,Color color)
    {
        var button=FindDirectChild(root.transform,name);var image=button?.GetComponent<Image>();if(image!=null)image.color=color;
    }

    private static void UpdatePageSummaries(GameObject root)
    {
        var counts=RuntimeCoverageCounts();
        SetPageSummary(root,NativeSettingsPage.General,
            $"{ModLabels.Get(Config.Language,"stats.version")}: 1.1.0    •    {ModLabels.Get(Config.Language,"stats.locale")}: {CurrentLanguageDisplayName()}\n"+
            $"{ModLabels.Get(Config.Language,"settings.packStatus")}: {exactCount} {ModLabels.Get(Config.Language,"stats.exact").ToLowerInvariant()} / {dynamicCount} {ModLabels.Get(Config.Language,"stats.dynamic").ToLowerInvariant()}");
        SetPageSummary(root,NativeSettingsPage.Content,
            $"{ModLabels.Get(Config.Language,"stats.font")}: {ConfiguredFontNames(false)}    •    {ModLabels.Get(Config.Language,"stats.fallbackFonts")}: {ConfiguredFontNames(true)}\n"+
            $"{ModLabels.Get(Config.Language,"stats.textureMappings")}: {textures.Count}    •    {ModLabels.Get(Config.Language,"stats.audioMappings")}: {Audio.Count}");
        SetPageSummary(root,NativeSettingsPage.Translator,
            $"{ModLabels.Get(Config.Language,"stats.exact")}: {exactCount}    •    {ModLabels.Get(Config.Language,"stats.dynamic")}: {dynamicCount}    •    {ModLabels.Get(Config.Language,"stats.rejected")}: {rejectedCount}    •    {ModLabels.Get(Config.Language,"stats.warnings")}: {warningCount}\n"+
            $"{ModLabels.Get(Config.Language,"stats.known")}: {counts.known}    •    {ModLabels.Get(Config.Language,"stats.unknown")}: {counts.unknown}\n\n"+
            $"{ModLabels.Get(Config.Language,"hotkeys.toggleTranslation")}: {Config.ToggleTranslationKey}    •    {ModLabels.Get(Config.Language,"hotkeys.reload")}: {Config.ReloadTranslationKey}    •    {ModLabels.Get(Config.Language,"hotkeys.export")}: {Config.DiagnosticKey}");
        var translatorCanvas=Resources.FindObjectsOfTypeAll<Canvas>().Count(c=>c!=null && IsTranslatorOwned(c.gameObject));
        SetPageSummary(root,NativeSettingsPage.Diagnostics,
            $"{ModLabels.Get(Config.Language,"stats.version")}: 1.1.0\n{ModLabels.Get(Config.Language,"stats.gameVersion")}: {Application.version}\n{ModLabels.Get(Config.Language,"stats.unityVersion")}: {Application.unityVersion}\n{ModLabels.Get(Config.Language,"stats.melonVersion")}: {typeof(MelonMod).Assembly.GetName().Version}\n"+
            $"{ModLabels.Get(Config.Language,"stats.locale")}: {Config.Language}    •    {ModLabels.Get(Config.Language,"stats.context")}: {scene}\n{ModLabels.Get(Config.Language,"stats.exact")}: {exactCount}    •    {ModLabels.Get(Config.Language,"stats.dynamic")}: {dynamicCount}    •    {ModLabels.Get(Config.Language,"stats.rejected")}: {rejectedCount}    •    {ModLabels.Get(Config.Language,"stats.warnings")}: {warningCount}\n"+
            $"{ModLabels.Get(Config.Language,"stats.font")}: {ConfiguredFontNames(false)}    •    {ModLabels.Get(Config.Language,"stats.fallbackFonts")}: {ConfiguredFontNames(true)}\n{ModLabels.Get(Config.Language,"stats.textureMappings")}: {textures.Count} ({(Config.TextureReplacement?"ON":"OFF")})    •    {ModLabels.Get(Config.Language,"stats.audioMappings")}: {Audio.Count} ({(Config.AudioReplacement?"ON":"OFF")})\n"+
            $"{ModLabels.Get(Config.Language,"stats.translatorCanvas")}: {translatorCanvas}    •    {ModLabels.Get(Config.Language,"settings.translation")}: {(Config.Enabled?"ON":"OFF")}    •    {ModLabels.Get(Config.Language,"settings.translatorMode")}: {(Config.TranslatorMode?"ON":"OFF")}    •    {ModLabels.Get(Config.Language,"settings.debugLogging")}: {(Config.DebugLogging?"ON":"OFF")}");
    }

    private static void SetPageSummary(GameObject root,NativeSettingsPage page,string value)
    {
        var item=FindDirectChild(root.transform,NativeSettingsMenuModel.PagePrefix(page)+NativeTranslatorMenuPolicy.SummaryName);
        var label=FindFirstText(item);if(label!=null)label.text=value;
    }

    private static (int known,int unknown) RuntimeCoverageCounts()
    {
        var knownSources=new HashSet<string>(StringComparer.Ordinal);var catalog=Path.Combine(paths.Dumps,"source_catalog.json");
        try {if(File.Exists(catalog))using(var doc=System.Text.Json.JsonDocument.Parse(File.ReadAllText(catalog)))foreach(var e in doc.RootElement.EnumerateArray())if(e.TryGetProperty("source",out var source))knownSources.Add(source.GetString());}catch{}
        var classified=collector.Snapshot().Select(e=>service?.Classify(e.Source,null,Config.DynamicRules,knownSources.Contains(e.Source)).Classification??TranslationClassification.Unknown).ToArray();
        return (classified.Count(x=>x==TranslationClassification.KnownUntranslated),classified.Count(x=>x==TranslationClassification.Unknown));
    }

    private static string ConfiguredFontNames(bool fallback)
    {
        try {
            using var doc=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(paths.Locale(Config.Language),"Fonts","manifest.json")));
            var names=doc.RootElement.EnumerateArray().Where(e=>(e.TryGetProperty("applyMode",out var mode)?mode.GetString():"fallback")==(fallback?"fallback":"replace")).Select(e=>Path.GetFileNameWithoutExtension(e.GetProperty("file").GetString())).ToArray();
            return names.Length==0 ? "—" : string.Join(", ",names);
        } catch {return "—";}
    }

    private static void SetNativeButtonText(GameObject root,string name,string value)
    {
        var button=FindDirectChild(root.transform,name);var label=FindFirstText(button);if(label!=null)label.text=value;
    }
    private static bool ReloadWithToast(string trigger = "internal")
    {
        if(Config.DebugLogging) MelonLogger.Msg("[ACTION] "+trigger);
        VerifyZeroCanvas();
        MelonLogger.Msg("[HOTKEY] reload requested: "+trigger);
        bool ok = Reload();
        lastManualReload=DateTimeOffset.UtcNow;diagnosticsDirty=true;
        if (ok) { UpdateAutoReloadWatcher();if(Config.Enabled && Config.TextureReplacement) textures.Apply(graphics); RefreshText(); toast.Show(ModLabels.Get(Config.Language,"reloaded")); SetNativeMenuStatus("status.reloaded"); RefreshNativeUiLabels(); }
        else { toast.Show(ModLabels.Get(Config.Language,"reloadFailed"), true); SetNativeMenuStatus("status.reloadFailed"); }
        return ok;
    }
    private static bool SwitchLocale(string locale)
    {
        if (string.Equals(locale, Config.Language, StringComparison.Ordinal)) return true;
        try
        {
            var candidate = LocaleManager.Load(paths.Locale(locale), locale, Application.version, false, ReportPath(locale));
            var candidateConfig = Config.Clone();
            candidateConfig.Language = locale;
            var candidateService = new TranslationService(candidate);
            ConfigManager.Save(paths.ConfigFile, candidateConfig);
            service = candidateService; Config = candidateConfig;
            candidate.Dynamic.FailureDiagnostic = candidateConfig.DebugLogging ? (id, source, failure) => MelonLogger.Warning($"Dynamic rule {id}: {failure}; source={source}") : null;
            candidate.Validation.WriteIfRequested(Path.Combine(paths.Root,"Diagnostics","translation_validation.json"));
            exactCount=candidate.Exact.Count;
            dynamicCount=candidate.Dynamic.Count;
            rejectedCount=candidate.Validation.Rejected;
            warningCount=candidate.Validation.Warnings;
            LogValidation(candidate, candidateConfig.DebugLogging);
        }
        catch (Exception ex) { MelonLogger.Error("Locale switch failed; retaining previous state: " + ex.Message); return false; }
        ReloadOptionalAssets();UpdateAutoReloadWatcher();if(Config.Enabled && Config.TextureReplacement) textures.Apply(graphics); RefreshText(); toast.Show(ModLabels.Get(Config.Language,"language") + ": " + locale);
        return true;
    }
    internal static bool ToggleForSmoke() => ToggleTranslation();
    private static string ReportPath(string locale) => Path.Combine(paths.Root,"Diagnostics","translation_validation."+locale+".json");
    private static void LogValidation(LanguagePack pack, bool detailed)
    {
        var report=pack.Validation;
        MelonLogger.Msg($"[Translation] Locale PASS: {pack.Locale}; {pack.Exact.Count} exact; {pack.Dynamic.Count} dynamic; {report.Rejected} rejected; {report.Warnings} warnings");
        if(report.Rejected>0) MelonLogger.Warning($"[Translation] {report.Rejected} invalid entries skipped; see Diagnostics/translation_validation.json");
        if(report.WriteError!=null) MelonLogger.Warning("[Translation] Validation report could not be written: "+report.WriteError);
        if(detailed) foreach(var issue in report.Issues)
            MelonLogger.Msg($"[Translation] {issue.Severity}: {issue.SourceFile} : {issue.Reason}");
    }
    private static void ReloadOptionalAssets()
    {
        var coordinator=new ReloadCoordinator(); var locale=paths.Locale(Config.Language);
        coordinator.Optional("Mod UI",()=>ModLabels.Load(locale),()=>{});
        coordinator.Optional("Fonts",()=>fonts.Load(locale,Config.FontReplacement && Config.Enabled),()=>fonts.SetEnabled(false));
        coordinator.Optional("Textures",()=>textures.Load(locale,Config.TextureReplacement && Config.Enabled,Config.DebugLogging),()=>textures.Restore());
        coordinator.Optional("Audio",()=>{Audio.ClearCache();Audio.Load(locale,Config.AudioReplacement && Config.Enabled);},()=>Audio.ClearCache());
        foreach(var result in coordinator.Results) MelonLogger.Msg($"Reload {result.Subsystem}: {result.Status} {result.Message}");
    }
    internal static bool ReloadForSmoke() => ReloadWithToast();
    public override void OnDeinitializeMelon() { autoReload.Dispose();textures.Dispose();Dump(false); }
    private static void Dump(bool notifyUser = true)
    {
        try
        {
            collector.Save(paths.Dumps);
            var known = new HashSet<string>(StringComparer.Ordinal);
            var catalog = Path.Combine(paths.Dumps, "source_catalog.json");
            if (File.Exists(catalog)) using (var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(catalog))) foreach (var e in doc.RootElement.EnumerateArray()) if (e.TryGetProperty("source", out var s)) known.Add(s.GetString());
            collector.SaveReports(paths.Dumps, s => service != null && service.Classify(s,null,Config.DynamicRules,known.Contains(s)).Classification is TranslationClassification.ExactTranslated or TranslationClassification.DynamicTranslated or TranslationClassification.ContextTranslated or TranslationClassification.Preserved, known);
            TranslationExportWriter.Write(paths.Root, paths.Locale(Config.Language), collector, s => service?.Translate(s, null, Config.DynamicRules) ?? s);
            MelonLogger.Msg($"Translated runtime: {collector.Snapshot().Count(e => service?.Classify(e.Source,null,Config.DynamicRules,known.Contains(e.Source)).IsTranslated==true)}");
            MelonLogger.Msg($"Preserved runtime: {collector.Snapshot().Count(e => service?.Classify(e.Source,null,Config.DynamicRules,known.Contains(e.Source)).Classification==TranslationClassification.Preserved)}");
            MelonLogger.Msg($"Known untranslated: {collector.Snapshot().Count(e => service?.Classify(e.Source,null,Config.DynamicRules,known.Contains(e.Source)).Classification==TranslationClassification.KnownUntranslated)}");
            MelonLogger.Msg($"Unknown runtime: {collector.Snapshot().Count(e => service?.Classify(e.Source,null,Config.DynamicRules,known.Contains(e.Source)).Classification==TranslationClassification.Unknown)}");
            if (notifyUser) toast.Show(ModLabels.Get(Config.Language,"exported"));
        }
        catch (Exception ex) { MelonLogger.Error("Runtime dump ERROR: " + ex.Message); }
    }
    private static void ExportTextDiagnostics(string trigger = "internal")
    {
        if(Config.DebugLogging) MelonLogger.Msg("[ACTION] "+trigger);
        VerifyZeroCanvas();
        var result = scanner.Scan(OriginalDiagnosticSource, (component, source) => {
            if(IsModUi(component)) return;
            var context=TextContextResolver.Resolve(component,scene);
            collector.Record(source,context.ActualScene,component.name,component.GetType().Name,context.Hierarchy);
        });
        Dump(false);
        MelonLogger.Msg($"Text diagnostics: TMP={result.Tmp}; Legacy={result.Legacy}; Chinese sources={result.Chinese}; textures=not scanned");
        toast.Show(ModLabels.Get(Config.Language,"exported"));
        SetNativeMenuStatus("status.exported");
    }
    private static string OriginalDiagnosticSource(Component component, string current)
    {
        if (IsModUi(component)) return null;
        return texts.TryGetValue(component.GetInstanceID(), out var state) && state.Component != null
            ? DiagnosticSource.Resolve(current, state.Source, state.Rendered) : current;
    }
    public static void ObserveGameText(string source)
    {
        if (!Config.Enabled || service == null || source == null) return;
        var result = service.Translate(source, null, Config.DynamicRules);
        if (!service.IsRenderedTargetOnly(source) && ((result == source && Config.TrackUntranslated) || Config.TranslatorMode)) collector.Record(source, scene, "", "game method", "");
        ObserveTranslatorSession(source,result,VisibleTranslatorContext(),scene,"","game method","");
    }
    internal static bool IsModUi(Component component)
    {
        return component!=null && IsTranslatorOwned(component.gameObject);
    }
    public static bool IsTranslatorOwned(GameObject gameObject)
    {
        if(gameObject==null) return false;
        for(var t=gameObject.transform;t!=null;t=t.parent)
            if(TextRefreshPolicy.IsTranslatorName(t.name)) return true;
        return false;
    }
    public static string TranslateComponent(Component component, string value)
    {
        if (assigning || component == null || value == null) return value;
        if(IsModUi(component)) return value;
        if (component is TMP_Text tmpText) fonts.Apply(tmpText);
        int id = component.GetInstanceID();
        if (!texts.TryGetValue(id, out var state) || state.Component == null)
        {
            state = new TextState { Component = component,
                OriginalRichText = component is TMP_Text initialTmp ? initialTmp.richText : component is UnityEngine.UI.Text initialLegacy && initialLegacy.supportRichText };
            texts[id] = state;
        }
        // OnEnable can observe an empty prefab label before the game configures its rich-text flag.
        if (string.IsNullOrEmpty(state.Source) && !string.IsNullOrEmpty(value))
            state.OriginalRichText = component is TMP_Text firstTmp ? firstTmp.richText : component is UnityEngine.UI.Text firstLegacy && firstLegacy.supportRichText;
        var sceneHandle=component.gameObject.scene.handle;
        var parentId=component.transform.parent == null ? 0 : component.transform.parent.GetInstanceID();
        if (state.Context == null || state.SceneHandle != sceneHandle || state.ParentId != parentId) {
            var context=TextContextResolver.Resolve(component,scene);
            state.Context=context.ContextKey; state.ActualScene=context.ActualScene; state.Hierarchy=context.Hierarchy;
            state.SceneHandle=sceneHandle; state.ParentId=parentId;
        }
        if (value != state.Rendered) state.Source = value;
        var source = state.Source ?? value;
        var result = Config.Enabled && service != null ? (state.Composite ? CompositeTranslation.Translate(source, value => service.Translate(value,state.Context,Config.DynamicRules)).Text : service.Translate(source, state.Context, Config.DynamicRules)) : source;
        var richText = RichTextPolicy.Desired(state.OriginalRichText, source, result, Config.Enabled);
        if (component is TMP_Text tmp && tmp.richText != richText) tmp.richText = richText;
        if (component is UnityEngine.UI.Text legacy && legacy.supportRichText != richText) legacy.supportRichText = richText;
        state.Rendered = result;
        if (Config.Enabled && !service.IsRenderedTargetOnly(source) && ((Config.TrackUntranslated && result == source) || Config.TranslatorMode) && component.name != "PvZTranslationSmokeProbe")
            collector.Record(source, state.ActualScene, component.name, component.GetType().Name, state.Hierarchy);
        if(component.name!="PvZTranslationSmokeProbe")ObserveTranslatorSession(source,result,state.Context,state.ActualScene,component.name,component.GetType().Name,state.Hierarchy);
        return result;
    }
    public static void Refresh(TMP_Text component)
    {
        if (component == null || IsModUi(component)) return;
        fonts.Apply(component);
        var result = TranslateComponent(component, component.text);
        assigning = true;
        try { if (component.text != result) component.text = result; }
        finally { assigning = false; }
        // The text setter marks the component dirty when a value actually changes.
    }
    public static void Refresh(UnityEngine.UI.Text component)
    {
        if (component == null) return;
        var result = TranslateComponent(component, component.text);
        assigning = true;
        try { if (component.text != result) component.text = result; }
        finally { assigning = false; }
    }
    private sealed class UiTraceEvaluation
    {
        public string Source;
        public string Target;
        public string Context;
        public string Lookup;
        public string State;
        public string ParsedBefore;
    }
    private static bool IsForensicRoot(GameObject root)
    {
        if(!Config.DebugLogging || root==null) return false;
        var name=root.name ?? "";
        return name.StartsWith("SetupWindow",StringComparison.Ordinal) || name.StartsWith("DifficultyWindow",StringComparison.Ordinal) || name.StartsWith("CustomModeWindow",StringComparison.Ordinal);
    }
    private static string ReadComponentText(Component component)
    {
        try { return component switch { TMP_Text tmp=>tmp.text, UnityEngine.UI.Text legacy=>legacy.text, TextMesh mesh=>mesh.text, _=>null }; }
        catch { return null; }
    }
    private static string ReadParsedText(Component component)
    {
        try { return component is TMP_Text tmp ? tmp.GetParsedText() : ReadComponentText(component); }
        catch { return "<unavailable>"; }
    }
    private static string Esc(string value) => value==null ? "<null>" : DiagnosticString.EscapeInvisible(value);
    private static string Utf16(string value) => value==null ? "" : string.Join(" ",value.Select(ch=>$"U+{(int)ch:X4}"));
    private static UiTraceEvaluation EvaluateUiTrace(Component component,string current)
    {
        var exists=texts.TryGetValue(component.GetInstanceID(),out var state) && state.Component!=null;
        var context=TextContextResolver.Resolve(component,scene).ContextKey;
        var source=exists && current==state.Rendered ? state.Source ?? current : current;
        string contextTarget=null,exactTarget=null,dynamicTarget=null,dynamicRule=null;
        var contextHit=source!=null && service?.Pack.Contexts.TryGetValue(context,out var contextStore)==true && contextStore.TryGet(source,out contextTarget);
        var exactHit=source!=null && service?.Pack.Exact.TryGet(source,out exactTarget)==true;
        var dynamicHit=source!=null && Config.DynamicRules && service?.Pack.Dynamic.TryTranslate(source,out dynamicTarget,out dynamicRule)==true;
        var target=Config.Enabled && service!=null ? service.Translate(source,context,Config.DynamicRules) : source;
        var stateText=exists ? $"yes/source={Esc(state.Source)}/lastTarget={Esc(state.Rendered)}" : "no";
        var lookup=$"context={(contextHit?"HIT:"+Esc(contextTarget):"MISS")}; exact={(exactHit?"HIT:"+Esc(exactTarget):"MISS")}; dynamic={(dynamicHit?"HIT:"+dynamicRule+":"+Esc(dynamicTarget):"MISS")}";
        return new UiTraceEvaluation { Source=source,Target=target,Context=context,Lookup=lookup,State=stateText,ParsedBefore=ReadParsedText(component) };
    }
    private static void TraceUiText(GameObject root,Component component,string before,string after,UiTraceEvaluation evaluation)
    {
        if(!IsForensicRoot(root) || component==null) return;
        var ctx=TextContextResolver.Resolve(component,scene);
        var sceneName=component.gameObject.scene.IsValid()?component.gameObject.scene.name:"<invalid>";
        var requested=!string.Equals(before,evaluation.Target,StringComparison.Ordinal);
        var performed=!string.Equals(before,after,StringComparison.Ordinal);
        MelonLogger.Msg($"[UITrace] root={root.name}#{root.GetInstanceID()}; component={component.GetInstanceID()}; path={ctx.Hierarchy}; type={component.GetType().FullName}; scene={sceneName}; activeSelf={component.gameObject.activeSelf}; activeInHierarchy={component.gameObject.activeInHierarchy}; current={Esc(before)}; parsedBefore={Esc(evaluation.ParsedBefore)}; textState={evaluation.State}; context={evaluation.Context}; {evaluation.Lookup}; final={Esc(evaluation.Target)}; wouldChange={requested}; assignmentPerformed={performed}; afterAssignment={Esc(after)}; parsedAfter={Esc(ReadParsedText(component))}; sourceLength={evaluation.Source?.Length ?? 0}; sourceUtf16={Utf16(evaluation.Source)}");
        if(!evaluation.Lookup.Contains("exact=HIT",StringComparison.Ordinal) && !evaluation.Lookup.Contains("context=HIT",StringComparison.Ordinal) && !evaluation.Lookup.Contains("dynamic=HIT",StringComparison.Ordinal))
            MelonLogger.Msg($"[UITraceSourceMiss] component={component.GetInstanceID()}; escaped={Esc(evaluation.Source)}; length={evaluation.Source?.Length ?? 0}; utf16={Utf16(evaluation.Source)}; leadingWhitespace={(evaluation.Source?.Length>0 && (char.IsWhiteSpace(evaluation.Source[0]) || evaluation.Source[0]=='\u200B' || evaluation.Source[0]=='\u00A0'))}; trailingWhitespace={(evaluation.Source?.Length>0 && (char.IsWhiteSpace(evaluation.Source[^1]) || evaluation.Source[^1]=='\u200B' || evaluation.Source[^1]=='\u00A0'))}");
        var id=component.GetInstanceID();
        if(!pendingTextTraces.ContainsKey(id)) pendingTextTraces[id]=new PendingTextTrace { Component=component,Root=root.name,RootId=root.GetInstanceID(),ScheduledFrame=Time.frameCount };
    }
    private static void UpdateTextTraces()
    {
        if(!Config.DebugLogging || pendingTextTraces.Count==0) return;
        foreach(var pair in pendingTextTraces.ToArray()) {
            var pending=pair.Value;
            var age=Time.frameCount-pending.ScheduledFrame;
            if(age<1) continue;
            try {
                if(pending.Component==null) { pendingTextTraces.Remove(pair.Key); continue; }
                if(age is 1 or 2) MelonLogger.Msg($"[UITraceFrame] root={pending.Root}#{pending.RootId}; component={pair.Key}; frame=+{age}; text={Esc(ReadComponentText(pending.Component))}; parsed={Esc(ReadParsedText(pending.Component))}; activeSelf={pending.Component.gameObject.activeSelf}; activeInHierarchy={pending.Component.gameObject.activeInHierarchy}");
            } catch(Exception ex) { MelonLogger.Msg($"[UITraceFrame] root={pending.Root}#{pending.RootId}; component={pair.Key}; frame=+{age}; invalid={ex.Message}"); pendingTextTraces.Remove(pair.Key); continue; }
            if(age>=2) pendingTextTraces.Remove(pair.Key);
        }
    }
    public static void RefreshUiRoot(GameObject root)
    {
        if (root == null) return;
        string rootName;
        try {
            if (!root.activeInHierarchy || IsModUi(root.transform)) return;
            var ownerScene=root.scene;
            if (!ownerScene.IsValid() || !ownerScene.isLoaded) return;
            rootName=root.name;
        } catch (Exception ex) { MelonLogger.Warning("[UIRefresh] invalid root: "+ex.Message); return; }
        var watch=Config.DebugLogging ? System.Diagnostics.Stopwatch.StartNew() : null;
        var visible=0; var changed=0;
        var visited=new HashSet<int>();
        void RefreshEligible(Component component)
        {
            try {
                if (component == null || component.gameObject == null || IsModUi(component) || !visited.Add(component.GetInstanceID())) return;
                var before=ReadComponentText(component);
                var evaluation=IsForensicRoot(root) ? EvaluateUiTrace(component,before) : null;
                if(component.gameObject.activeInHierarchy) {
                    if (component is TMP_Text tmp) { visible++; Refresh(tmp); }
                    else if (component is UnityEngine.UI.Text legacy) { visible++; Refresh(legacy); }
                }
                var after=ReadComponentText(component);
                if(after!=before) changed++;
                if(evaluation!=null) TraceUiText(root,component,before,after,evaluation);
            } catch (Exception ex) { MelonLogger.Warning("[UIRefresh] skipped invalid text: "+ex.Message); }
        }
        try {
            foreach (var tmp in ActiveHierarchy.Components<TMP_Text>(root)) RefreshEligible(tmp);
            foreach (var legacy in ActiveHierarchy.Components<UnityEngine.UI.Text>(root)) RefreshEligible(legacy);
            if(IsForensicRoot(root)) {
                // UnityEngine.TextMesh has no usable IL2CPP class pointer in this game build.
                // TMP and Unity UI Text remain the supported translation paths above.
                foreach(var image in ActiveHierarchy.Components<UnityEngine.UI.Image>(root)) {
                    if(image==null) continue;
                    var ctx=TextContextResolver.Resolve(image,scene);
                    var texture=image.sprite?.texture;
                    MelonLogger.Msg($"[UIAssetTrace] root={root.name}#{root.GetInstanceID()}; component={image.GetInstanceID()}; path={ctx.Hierarchy}; type={image.GetType().FullName}; activeSelf={image.gameObject.activeSelf}; activeInHierarchy={image.gameObject.activeInHierarchy}; sprite={image.sprite?.name ?? "<none>"}; texture={texture?.name ?? "<none>"}; dimensions={(texture==null?"n/a":texture.width+"x"+texture.height)}");
                }
                foreach(var renderer in ActiveHierarchy.Components<SpriteRenderer>(root)) {
                    if(renderer==null) continue;
                    var ctx=TextContextResolver.Resolve(renderer,scene); var texture=renderer.sprite?.texture;
                    MelonLogger.Msg($"[UIAssetTrace] root={root.name}#{root.GetInstanceID()}; component={renderer.GetInstanceID()}; path={ctx.Hierarchy}; type={renderer.GetType().FullName}; activeSelf={renderer.gameObject.activeSelf}; activeInHierarchy={renderer.gameObject.activeInHierarchy}; sprite={renderer.sprite?.name ?? "<none>"}; texture={texture?.name ?? "<none>"}; dimensions={(texture==null?"n/a":texture.width+"x"+texture.height)}");
                }
                foreach(var raw in ActiveHierarchy.Components<UnityEngine.UI.RawImage>(root)) {
                    if(raw==null) continue;
                    var ctx=TextContextResolver.Resolve(raw,scene); var texture=raw.texture;
                    MelonLogger.Msg($"[UIAssetTrace] root={root.name}#{root.GetInstanceID()}; component={raw.GetInstanceID()}; path={ctx.Hierarchy}; type={raw.GetType().FullName}; activeSelf={raw.gameObject.activeSelf}; activeInHierarchy={raw.gameObject.activeInHierarchy}; texture={texture?.name ?? "<none>"}; dimensions={(texture==null?"n/a":texture.width+"x"+texture.height)}");
                }
            }
        } catch (Exception ex) { MelonLogger.Warning("[UIRefresh] root changed during activation: "+ex.Message); }
        if (watch != null) MelonLogger.Msg($"[UIRefresh] root={rootName}; visible={visible}; changed={changed}; elapsedMs={watch.ElapsedMilliseconds}");
    }
    public static void RegisterUiRoot(GameObject root, GameUiRootKind kind)
    {
        if(root==null) return;
        if(IsTranslatorOwned(root)) {
            if(Config.DebugLogging) MelonLogger.Error($"[GameUiInvariant] REJECTED {kind}#{root.GetInstanceID()}: translatorAncestor=true; path={TextContextResolver.Resolve(root.transform,scene).Hierarchy}");
            return;
        }
        gameUiRoots.MarkOpen(root,kind);
        UpdateTranslatorContext();
        RefreshUiRoot(root);
        try { pendingUiRefreshes[root.GetInstanceID()]=new PendingUiRefresh { Root=root }; }
        catch(Exception ex) { if(Config.DebugLogging) MelonLogger.Warning("[UIRefresh] could not queue root: "+ex.Message); }
    }
    public static void CloseUiRoot(GameObject root)
    {
        if(ReferenceEquals(root,null)) return;
        if(IsTranslatorOwned(root)) return;
        gameUiRoots.MarkClosed(root);
        UpdateTranslatorContext();
        try { pendingUiRefreshes.Remove(root.GetInstanceID()); } catch { }
    }
    public static void DestroyUiRoot(GameObject root)
    {
        if(ReferenceEquals(root,null)) return;
        if(IsTranslatorOwned(root)) return;
        gameUiRoots.UnregisterDestroyed(root);
        UpdateTranslatorContext();
        try { pendingUiRefreshes.Remove(root.GetInstanceID()); } catch { }
    }
    public static void TraceUiMethod(Component window,string method,string phase)
    {
        if(!Config.DebugLogging || window==null) return;
        try {
            var root=window.gameObject;
            var translatorAncestor=IsTranslatorOwned(root);
            var rect=root.GetComponent<RectTransform>();
            var group=root.GetComponent<CanvasGroup>();
            var raycaster=root.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            var selectables=ActiveHierarchy.Components<UnityEngine.UI.Selectable>(root).ToArray();
            var parent=TextContextResolver.Resolve(window,scene).Hierarchy;
            var custom=window as Il2Cpp.CustomModeWindow;
            var description=custom?.eventDescriptionText;
            var selected=-1;
            try { if(custom?.customData?.eventList!=null) selected=custom.customData.eventList.Count; } catch { }
            MelonLogger.Msg($"[UiMethodTrace] method={method}; phase={phase}; type={window.GetType().FullName}; root={root.name}#{root.GetInstanceID()}; path={parent}; translatorAncestor={translatorAncestor}; activeSelf={root.activeSelf}; activeInHierarchy={root.activeInHierarchy}; anchoredPosition={(rect==null?"n/a":rect.anchoredPosition.ToString())}; localPosition={root.transform.localPosition}; localScale={root.transform.localScale}; canvasGroupAlpha={(group==null?"n/a":group.alpha.ToString(System.Globalization.CultureInfo.InvariantCulture))}; raycaster={(raycaster==null?"n/a":raycaster.enabled.ToString())}; selectables={selectables.Length}; interactable={selectables.Count(x=>x!=null && x.interactable)}; selectedEvents={selected}; descriptionId={(description==null?0:description.GetInstanceID())}; description={Esc(description?.text)}");
            if(translatorAncestor) MelonLogger.Error($"[GameUiInvariant] {method} {phase}: game window is inside PvZTranslationUI; registration will be rejected");
        } catch(Exception ex) { MelonLogger.Msg($"[UiMethodTrace] method={method}; phase={phase}; invalid={ex.Message}"); }
    }
    public static void TraceHomeRequest(Il2Cpp.HomeManager manager,string method,string phase)
    {
        if(!Config.DebugLogging || manager==null) return;
        TraceUiMethod(manager,method,phase);
        try {
            var setup=manager.setupWindowPrefab; var difficulty=manager.difficultyWindowPrefab;
            MelonLogger.Msg($"[HomeRequestTrace] method={method}; phase={phase}; setupPrefab={(setup==null?"dead":setup.name+"/self="+setup.activeSelf+"/hierarchy="+setup.activeInHierarchy)}; difficultyPrefab={(difficulty==null?"dead":difficulty.name+"/self="+difficulty.activeSelf+"/hierarchy="+difficulty.activeInHierarchy)}");
        } catch(Exception ex) { MelonLogger.Msg($"[HomeRequestTrace] method={method}; phase={phase}; invalid={ex.Message}"); }
    }
    public static void RefreshCustomEventDescription(Il2Cpp.CustomModeWindow window)
    {
        if(window==null || service==null) return;
        if(IsTranslatorOwned(window.gameObject)) {
            if(Config.DebugLogging) MelonLogger.Error($"[GameUiInvariant] REJECTED CustomMode composite#{window.GetInstanceID()}: translatorAncestor=true");
            return;
        }
        try {
            var component=window.eventDescriptionText;
            if(component==null) return;
            var id=component.GetInstanceID();
            var current=component.text;
            var source=texts.TryGetValue(id,out var previous) && previous.Component!=null
                ? DiagnosticSource.Resolve(current,previous.Source,previous.Rendered) : current;
            if(!Config.Enabled) { Refresh(component); return; }
            var context=TextContextResolver.Resolve(component,scene).ContextKey;
            var result=CompositeTranslation.Translate(source,value=>service.Translate(value,context,Config.DynamicRules));
            var selected=-1;
            try { if(window.customData?.eventList!=null) selected=window.customData.eventList.Count; } catch { }
            TranslateComponent(component,source);
            assigning=true;
            try { if(component.text!=result.Text) component.text=result.Text; }
            finally { assigning=false; }
            if(texts.TryGetValue(id,out var state)) { state.Source=source; state.Rendered=result.Text; state.Composite=true; }
            if(Config.DebugLogging) {
                var hierarchy=TextContextResolver.Resolve(component,scene).Hierarchy;
                MelonLogger.Msg($"[CompositeTranslation] window={window.GetInstanceID()}; selected={selected}; component={id}; path={hierarchy}; before={Esc(current)}; source={Esc(source)}; segments={result.Segments}; translated={result.Translated}; unknown={result.Unknown}; computed={Esc(result.Text)}; afterAssignment={Esc(component.text)}; parsedAfter={Esc(ReadParsedText(component))}");
                if(!pendingTextTraces.ContainsKey(id)) pendingTextTraces[id]=new PendingTextTrace { Component=component,Root=window.gameObject.name,RootId=window.gameObject.GetInstanceID(),ScheduledFrame=Time.frameCount };
            }
        } catch(Exception ex) { MelonLogger.Warning("[CompositeTranslation] refresh failed: "+ex.Message); }
    }
    private static void RefreshQueuedUiRoots()
    {
        if(pendingUiRefreshes.Count==0) return;
        foreach(var pair in pendingUiRefreshes.ToArray()) {
            var pending=pair.Value;
            try {
                if(pending.Root==null) { pendingUiRefreshes.Remove(pair.Key); continue; }
                pending.Age++;
                if(pending.Age==1) RefreshUiRoot(pending.Root);
                if(pending.Age>=1) pendingUiRefreshes.Remove(pair.Key);
            } catch { pendingUiRefreshes.Remove(pair.Key); continue; }
        }
    }
    private static void RefreshText(string sceneName = null)
    {
        var watch=System.Diagnostics.Stopwatch.StartNew();
        fonts.PruneDead();
        foreach (var key in new List<int>(texts.Keys)) if (texts[key].Component == null) texts.Remove(key);
        var requestedScene=sceneName ?? scene;
        var openRoots=gameUiRoots.GetForegroundRoots();
        int seen=0, changed=0;
        foreach(var state in texts.Values.ToArray()) {
            var component=state.Component;
            if(component==null || !component.gameObject.activeInHierarchy || IsModUi(component)) continue;
            var owner=component.gameObject.scene;
            var belongs=owner.IsValid() && owner.name==requestedScene;
            if(!belongs) belongs=openRoots.Any(root=>root!=null && (component.gameObject==root || component.transform.IsChildOf(root.transform)));
            if(!TextRefreshPolicy.ShouldRefresh(component!=null,component.gameObject.activeInHierarchy,IsModUi(component),belongs,false)) continue;
            var before=ReadComponentText(component);
            if(component is TMP_Text tmp) Refresh(tmp);
            else if(component is UnityEngine.UI.Text legacy) Refresh(legacy);
            else continue;
            seen++; if(ReadComponentText(component)!=before) changed++;
        }
        MelonLogger.Msg($"Text refresh: scene={requestedScene}; visible={seen}; changed={changed}; elapsedMs={watch.ElapsedMilliseconds}; fontStates={fonts.AppliedCount}");
    }
    private static void RunSmokeTest()
    {
        GameObject probe = null, legacyProbe = null, richProbe = null, richLegacyProbe = null;
        try
        {
            if (service == null) throw new Exception("No active locale");
            if (Config.FontReplacement && !fonts.Ready) throw new Exception("No locale font loaded");
            probe = new GameObject("PvZTranslationSmokeProbe");
            var tmp = probe.AddComponent<TextMeshProUGUI>();
            string source = "植物！";
            string expected = service.Translate(source);
            if (expected == source) throw new Exception("Smoke source has no translation");
            tmp.text = source;
            if (tmp.text != expected) throw new Exception("TMP setter failed");
            tmp.text = " 未知！ ";
            if (tmp.text != " 未知！ ") throw new Exception("Unknown fallback failed");
            tmp.text = source;
            Config.Enabled = false;
            Refresh(tmp);
            if (tmp.text != source) throw new Exception("Original source restoration failed");
            if (!Reload()) throw new Exception("Reload failed");
            Refresh(tmp);
            if (tmp.text != expected) throw new Exception("Reload refresh failed");
            legacyProbe = new GameObject("PvZTranslationSmokeProbe");
            var legacy = legacyProbe.AddComponent<UnityEngine.UI.Text>();
            legacy.text = source;
            if (legacy.text != expected) throw new Exception("Legacy UI setter failed");
            legacy.text = " 未知！ ";
            if (legacy.text != " 未知！ ") throw new Exception("Legacy UI fallback failed");
            var originalService=service;
            try {
                var richPack=new LanguagePack { Locale=Config.Language };
                richPack.Exact.Add("全部禁用", "<size=75%>Desativar tudo</size>");
                richPack.Exact.Add("确定", "<b>Confirmar</b>");
                service=new TranslationService(richPack);
                richProbe=new GameObject("PvZTranslationRichTextProbe");
                var richTmp=richProbe.AddComponent<TextMeshProUGUI>(); richTmp.richText=false;
                richTmp.text="全部禁用";
                richLegacyProbe=new GameObject("PvZTranslationRichTextLegacyProbe");
                var richLegacy=richLegacyProbe.AddComponent<UnityEngine.UI.Text>(); richLegacy.supportRichText=false;
                richLegacy.text="确定";
                if(richTmp.text!="<size=75%>Desativar tudo</size>" || !richTmp.richText || richLegacy.text!="<b>Confirmar</b>" || !richLegacy.supportRichText) throw new Exception("Rich target did not reach TMP/legacy renderer");
                Config.Enabled=false; Refresh(richTmp); Refresh(richLegacy);
                if(richTmp.text!="全部禁用" || richTmp.richText || richLegacy.text!="确定" || richLegacy.supportRichText) throw new Exception("F9 did not restore source and original rich-text state");
                Config.Enabled=true; Refresh(richTmp); Refresh(richLegacy);
                if(richTmp.text!="<size=75%>Desativar tudo</size>" || !richTmp.richText || !richLegacy.supportRichText) throw new Exception("F9 did not reapply rich text");
                var updatedPack=new LanguagePack { Locale=Config.Language };
                updatedPack.Exact.Add("全部禁用", "<size=70%><color=#FF0000>Desativar tudo</color></size>");
                service=new TranslationService(updatedPack); Refresh(richTmp);
                if(richTmp.text!="<size=70%><color=#FF0000>Desativar tudo</color></size>" || !richTmp.richText) throw new Exception("Changed target formatting was not refreshed");
                MelonLogger.Msg("RICH TEXT RUNTIME PASS: TMP/legacy target markup; F9 source and flags restored/reapplied; changed target refreshed; visual NOT TESTED");
            }
            finally { service=originalService; Config.Enabled=true; }
            TestTargetedUiRoot();
            if (Array.IndexOf(Environment.GetCommandLineArgs(),"--pvz-richtext-file-probe")>=0) TestExternalFormattingReload();
            if (!ToggleTranslation() || Config.Enabled) throw new Exception("F9 disable state failed");
            if (!ToggleTranslation() || !Config.Enabled) throw new Exception("F9 enable state failed");
            VerifyZeroCanvas();
            MelonLogger.Msg("SMOKE: text-only diagnostic scan");
            var graphicFolder=Path.Combine(paths.Dumps,"Textures");
            string GraphicFiles() => Directory.Exists(graphicFolder) ? string.Join("|",Directory.GetFiles(graphicFolder,"*",SearchOption.AllDirectories).OrderBy(p=>p).Select(p=>p+":"+File.GetLastWriteTimeUtc(p).Ticks)) : "";
            var graphicsBefore=GraphicFiles();
            var visibleBefore=tmp.text;
            legacyProbe.SetActive(false);
            var inactiveBefore=legacy.text;
            if (legacyProbe.activeSelf || legacyProbe.activeInHierarchy) throw new Exception("Inactive probe did not deactivate");
            ToggleTranslation(); ToggleTranslation();
            if (!ReloadWithToast() || legacyProbe.activeSelf || legacyProbe.activeInHierarchy || legacy.text!=inactiveBefore)
                throw new Exception("F9/F10 changed inactive text object");
            MelonLogger.Msg("INACTIVE UI REFRESH PASS: F9/F10 left activeSelf, activeInHierarchy and text unchanged");
            bool modeBefore=Config.TranslatorMode, debugBefore=Config.DebugLogging;
            foreach(bool mode in new[]{false,true}) {
                Config.TranslatorMode=mode;
                ExportTextDiagnostics();
                if(tmp.text!=visibleBefore || legacyProbe.activeSelf) throw new Exception("Text export changed visible/inactive UI state");
                if(GraphicFiles()!=graphicsBefore) throw new Exception("Text export wrote texture diagnostics");
                if(Config.DebugLogging!=debugBefore || !Config.Enabled) throw new Exception("Translator Mode affected independent settings");
            }
            Config.TranslatorMode=modeBefore;
            if(!collector.Snapshot().Any(e=>e.Source==source)) throw new Exception("Text export did not preserve tracked original source");
            MelonLogger.Msg("TEXT DIAGNOSTICS PASS: both translator modes; original source retained; closed UI/inactive object/text rendering unchanged; no texture dump writes");
            Dump();
            MelonLogger.Msg("SMOKE PASS: TMP/legacy UI, F9 toggle, reload, selector idempotence, fallback and dump; visual NOT TESTED");
        }
        catch (Exception ex) { MelonLogger.Error("SMOKE FAIL: " + ex); }
        finally
        {
            if (probe != null) UnityEngine.Object.Destroy(probe);
            if (legacyProbe != null) UnityEngine.Object.Destroy(legacyProbe);
            if (richProbe != null) UnityEngine.Object.Destroy(richProbe);
            if (richLegacyProbe != null) UnityEngine.Object.Destroy(richLegacyProbe);
            Application.Quit();
        }
    }
    private static void TestTargetedUiRoot()
    {
        GameObject root=null;
        var savedService=service; var savedEnabled=Config.Enabled;
        try {
            RefreshUiRoot(null);
            root=new GameObject("SetupWindowSmokeProbe");
            var active=new GameObject("ActiveLabel"); active.transform.SetParent(root.transform,false);
            var activeLegacy=new GameObject("ActiveLegacyLabel"); activeLegacy.transform.SetParent(root.transform,false);
            var inactive=new GameObject("InactiveLabel"); inactive.transform.SetParent(root.transform,false);
            var mod=new GameObject("PvZTranslationSettingsRoot"); mod.transform.SetParent(root.transform,false);
            var dynamic=new GameObject("DynamicLabel"); dynamic.transform.SetParent(root.transform,false);
            var contextual=new GameObject("ContextLabel"); contextual.transform.SetParent(root.transform,false);
            Config.Enabled=false;
            var activeText=active.AddComponent<TextMeshProUGUI>(); activeText.richText=false; activeText.text="全屏";
            var activeLegacyText=activeLegacy.AddComponent<UnityEngine.UI.Text>(); activeLegacyText.text="音效";
            var inactiveText=inactive.AddComponent<UnityEngine.UI.Text>(); inactiveText.text="音乐"; inactive.SetActive(false);
            var modText=mod.AddComponent<TextMeshProUGUI>(); modText.text="音乐";
            var dynamicText=dynamic.AddComponent<TextMeshProUGUI>(); dynamicText.text="17秒\u200B";
            var contextualText=contextual.AddComponent<TextMeshProUGUI>(); contextualText.text="确定";
            var testPack=new LanguagePack { Locale=Config.Language };
            testPack.Exact.Add("全屏","<size=85%><color=#FFD700>Tela cheia</color></size>");
            testPack.Exact.Add("音效","Efeitos");
            testPack.Dynamic.Add("seconds",@"^(?<value>\d+)秒(?<zwsp>\u200B?)$","${value} s${zwsp}");
            var contextStore=new ExactStringStore(); contextStore.Add("确定","Confirmar por contexto");
            testPack.Contexts.Add(TextContextResolver.Resolve(contextualText,scene).ContextKey,contextStore);
            service=new TranslationService(testPack); Config.Enabled=true;
            RefreshUiRoot(root);
            if(activeText.text!="<size=85%><color=#FFD700>Tela cheia</color></size>" || !activeText.richText || activeLegacyText.text!="Efeitos" ||
                inactiveText.text!="音乐" || modText.text!="音乐" || dynamicText.text!="17 s\u200B" || contextualText.text!="Confirmar por contexto")
                throw new Exception("Targeted root missed active text or mutated inactive/mod UI");
            var id=activeText.GetInstanceID(); var state=texts[id];
            if(state.Source!="全屏" || state.Rendered!=activeText.text) throw new Exception("Targeted root lost Chinese source");
            var tracked=texts.Count;
            RefreshUiRoot(root);
            if(texts.Count!=tracked || texts[id].Source!="全屏") throw new Exception("Targeted root was not idempotent");
            Config.Enabled=false; RefreshUiRoot(root);
            if(activeText.text!="全屏" || activeText.richText || activeLegacyText.text!="音效") throw new Exception("Targeted F9 off did not restore original");
            Config.Enabled=true; RefreshUiRoot(root);
            if(activeText.text!="<size=85%><color=#FFD700>Tela cheia</color></size>" || activeLegacyText.text!="Efeitos") throw new Exception("Targeted F9 on did not restore translation");
            inactive.SetActive(false);
            MelonLogger.Msg("TARGETED UI ROOT PASS: active TMP/legacy/context/dynamic, inactive legacy and mod UI excluded; source/F9/rich text/idempotence");
        } finally { service=savedService; Config.Enabled=savedEnabled; if(root!=null) UnityEngine.Object.Destroy(root); }
    }
    private static void TestExternalFormattingReload()
    {
        var file=Path.Combine(paths.Locale(Config.Language),"Strings","exact.json");
        var original=File.ReadAllBytes(file);
        var backup=Path.Combine(paths.Root,"Cache","RichTextProbe","exact-"+DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+".json");
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        File.WriteAllBytes(backup,original);
        GameObject probe=null;
        try {
            void SetTarget(string target) {
                var root=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file)).AsObject();
                if(root["全部禁用"]==null) throw new Exception("Runtime rich-text probe source key missing");
                root["全部禁用"]=target;
                var temp=file+".richtext-probe.tmp";
                File.WriteAllText(temp,root.ToJsonString(ConfigManager.HumanReadableJson));
                File.Move(temp,file,true);
            }
            SetTarget("<size=100%>Desativar tudo</size>");
            if(!ReloadWithToast()) throw new Exception("F10 first formatted reload failed");
            probe=new GameObject("PvZTranslationRichTextFileProbe");
            var text=probe.AddComponent<TextMeshProUGUI>(); text.richText=false; text.text="全部禁用";
            if(text.text!="<size=100%>Desativar tudo</size>" || !text.richText) throw new Exception("F10 first formatted target did not reach TMP");
            SetTarget("<size=70%><color=#FF0000>Desativar tudo</color></size>");
            if(!ReloadWithToast()) throw new Exception("F10 changed formatted reload failed");
            Refresh(text);
            if(text.text!="<size=70%><color=#FF0000>Desativar tudo</color></size>" || !text.richText) throw new Exception("F10 changed formatting not visible to TMP");
            MelonLogger.Msg("F10 RICH TEXT FILE PROBE PASS: edited external target loaded twice without DLL rebuild; TMP richText enabled; visual NOT TESTED");
        }
        finally {
            var temp=file+".richtext-restore.tmp";
            File.WriteAllBytes(temp,original);
            File.Move(temp,file,true);
            if(!Reload()) MelonLogger.Error("RICH TEXT FILE PROBE: production pack did not reload after restoration; backup="+backup);
            if(probe!=null) UnityEngine.Object.Destroy(probe);
            MelonLogger.Msg("RICH TEXT FILE PROBE: original exact.json bytes restored from backup="+backup);
        }
    }
}
