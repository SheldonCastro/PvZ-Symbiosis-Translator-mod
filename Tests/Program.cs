using PvZSymbiosisTranslator.Localization;
using PvZSymbiosisTranslator.Core;
using PvZSymbiosisTranslator.Configuration;
using System.Text.Json;

int passed = 0, failed = 0;
void Test(string name, Action action)
{
    try { action(); Console.WriteLine("PASS " + name); passed++; }
    catch (Exception ex) { Console.WriteLine("FAIL " + name + ": " + ex); failed++; }
}
void Equal(string expected, string actual) { if (expected != actual) throw new Exception($"Expected [{expected}], got [{actual}]"); }
void Reject(Action action) { try { action(); } catch { return; } throw new Exception("Expected rejection"); }
Test("diagnostic original after translation", () => Equal("植物！", PvZSymbiosisTranslator.Diagnostics.DiagnosticSource.Resolve("Plantar!", "植物！", "Plantar!")));
Test("diagnostic newly changed source", () => Equal("新文字", PvZSymbiosisTranslator.Diagnostics.DiagnosticSource.Resolve("新文字", "植物！", "Plantar!")));
Test("diagnostic untracked source", () => Equal("未知", PvZSymbiosisTranslator.Diagnostics.DiagnosticSource.Resolve("未知", null, null)));
Test("translator mode default independent", () => { var c = new ModConfig { DebugLogging = true }; if(c.TranslatorMode || !c.Enabled || !c.UiNotifications || c.UiScalePercent!=100f) throw new Exception("Unexpected defaults"); var copy=c.Clone(); if(copy.TranslatorMode || !copy.DebugLogging || !copy.UiNotifications || copy.UiScalePercent!=100f) throw new Exception("Clone lost flags"); });
Test("translator mode config persistence", () => { var path=Path.GetTempFileName(); try { var c=new ModConfig { TranslatorMode=true, DebugLogging=false }; ConfigManager.Save(path,c); var copy=ConfigManager.Load(path).Clone(); if(!copy.TranslatorMode || copy.DebugLogging || !copy.Enabled || copy.DiagnosticKey!="PageDown") throw new Exception("Mode affected independent settings"); } finally { File.Delete(path); } });
Test("translator mode localized labels", () => { Equal("Modo de tradutor", PvZSymbiosisTranslator.UI.ModLabels.Get("pt-BR", "translator")); Equal("Translator Mode", PvZSymbiosisTranslator.UI.ModLabels.Get("en-US", "translator")); });
Test("native language menu labels", () => { Equal("Tradução de textos: Ativada",PvZSymbiosisTranslator.UI.NativeTranslatorMenuPolicy.FormatFeature("pt-BR",PvZSymbiosisTranslator.UI.NativeMenuFeature.Translation,true)); Equal("Texturas traduzidas: Desativadas",PvZSymbiosisTranslator.UI.NativeTranslatorMenuPolicy.FormatFeature("pt-BR",PvZSymbiosisTranslator.UI.NativeMenuFeature.Textures,false)); Equal("Custom audio: Enabled",PvZSymbiosisTranslator.UI.NativeTranslatorMenuPolicy.FormatFeature("en-US",PvZSymbiosisTranslator.UI.NativeMenuFeature.Audio,true)); Equal("Idioma: Português Brasileiro",PvZSymbiosisTranslator.UI.NativeTranslatorMenuPolicy.FormatLanguage("pt-BR","Português Brasileiro")); });
Test("native language menu locale cycle", () => { var locales=new[]{"en-US","pt-BR"}; Equal("pt-BR",PvZSymbiosisTranslator.UI.NativeTranslatorMenuPolicy.NextLocale(locales,"en-US")); Equal("en-US",PvZSymbiosisTranslator.UI.NativeTranslatorMenuPolicy.NextLocale(locales,"pt-BR")); Equal("pt-BR",PvZSymbiosisTranslator.UI.NativeTranslatorMenuPolicy.NextLocale(new[]{"pt-BR"},"pt-BR")); });
Test("native launcher health policy", () => {
    if(PvZSymbiosisTranslator.UI.NativeHomeLauncherPolicy.Decide(false,false,false,false,false,false,false,false,false)!=PvZSymbiosisTranslator.UI.LauncherHealthAction.Unavailable) throw new Exception("Unavailable context accepted");
    if(PvZSymbiosisTranslator.UI.NativeHomeLauncherPolicy.Decide(true,false,false,false,false,false,false,false,false)!=PvZSymbiosisTranslator.UI.LauncherHealthAction.Install) throw new Exception("Missing launcher not installed");
    if(PvZSymbiosisTranslator.UI.NativeHomeLauncherPolicy.Decide(true,true,true,true,true,true,true,true,false)!=PvZSymbiosisTranslator.UI.LauncherHealthAction.Rebind) throw new Exception("Stale callback not rebound");
    if(PvZSymbiosisTranslator.UI.NativeHomeLauncherPolicy.Decide(true,true,true,true,true,true,true,true,true)!=PvZSymbiosisTranslator.UI.LauncherHealthAction.Healthy) throw new Exception("Healthy launcher rejected");
});
Test("native settings page state", () => {
    var pages=PvZSymbiosisTranslator.UI.NativeSettingsMenuModel.Pages;
    if(pages.Count!=5 || pages.Distinct().Count()!=5) throw new Exception("Expected five unique pages");
    foreach(var page in pages) {
        var prefix=PvZSymbiosisTranslator.UI.NativeSettingsMenuModel.PagePrefix(page);
        if(!PvZSymbiosisTranslator.UI.NativeSettingsMenuModel.BelongsToPage(prefix+"Control",page)) throw new Exception("Page ownership failed: "+page);
        if(PvZSymbiosisTranslator.UI.NativeSettingsMenuModel.Actions(page).Count==0) throw new Exception("Page has no mapped actions: "+page);
    }
    if(!PvZSymbiosisTranslator.UI.NativeSettingsMenuModel.Actions(PvZSymbiosisTranslator.UI.NativeSettingsPage.Translator).Contains(PvZSymbiosisTranslator.UI.NativeSettingsAction.ToggleDebugLogging)) throw new Exception("Debug action missing");
});
Test("native asset logical IDs", () => {
    var ids=PvZSymbiosisTranslator.UI.NativeAssetIds.All;
    if(ids.Count!=8 || ids.Distinct(StringComparer.Ordinal).Count()!=8 || ids.Any(x=>!x.StartsWith("Translator.",StringComparison.Ordinal))) throw new Exception("Invalid native asset IDs");
    var paths=PvZSymbiosisTranslator.UI.NativeAssetIds.ResourcePaths;
    if(paths.Count!=ids.Count || ids.Any(id=>!paths.TryGetValue(id,out var path) || !path.StartsWith("image/interface/",StringComparison.Ordinal) || path.Any(char.IsUpper))) throw new Exception("Invalid native resource path map");
    Equal("image/interface/challengebackground",paths[PvZSymbiosisTranslator.UI.NativeAssetIds.Background]);
    Equal("image/interface/optionscheckboxopen",paths[PvZSymbiosisTranslator.UI.NativeAssetIds.CheckboxOn]);
});
Test("translator UI layout", () => {
    if(PvZSymbiosisTranslator.UI.TranslatorUiLayout.TabX.Length!=5 || PvZSymbiosisTranslator.UI.TranslatorUiLayout.TabX.Distinct().Count()!=5) throw new Exception("Five tab positions required");
    if(PvZSymbiosisTranslator.UI.TranslatorUiLayout.ClampScale(50)!=85 || PvZSymbiosisTranslator.UI.TranslatorUiLayout.ClampScale(130)!=115 || PvZSymbiosisTranslator.UI.TranslatorUiLayout.ClampScale(100)!=100) throw new Exception("Scale clamp failed");
    if(Math.Abs(PvZSymbiosisTranslator.UI.TranslatorUiLayout.ScaleFactor(85)-.85f)>.001f || Math.Abs(PvZSymbiosisTranslator.UI.TranslatorUiLayout.ScaleFactor(115)-1.15f)>.001f) throw new Exception("Scale factor failed");
    if(PvZSymbiosisTranslator.UI.TranslatorUiLayout.ReferenceWidth!=1920f || PvZSymbiosisTranslator.UI.TranslatorUiLayout.ReferenceHeight!=1080f) throw new Exception("Fullscreen reference changed");
});
Test("ModStrings completeness", () => {
    var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../PvZ_Symbiosis_Translator/Localization"));
    using var pt=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"pt-BR","ModStrings.json")));
    using var template=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"_template","ModStrings.json")));
    var ptKeys=pt.RootElement.EnumerateObject().Select(x=>x.Name).ToHashSet(StringComparer.Ordinal);
    var templateKeys=template.RootElement.EnumerateObject().Select(x=>x.Name).ToHashSet(StringComparer.Ordinal);
    if(!ptKeys.SetEquals(templateKeys)) throw new Exception("Locale/template key mismatch");
    foreach(var key in new[]{"menu.title","common.close","common.enabled","common.disabled","tabs.general","tabs.content","tabs.translator","tabs.qa","tabs.diagnostics","general.languageSection","general.translationSection","general.interfaceSection","general.statusSection","general.language","general.translation","general.font","general.textures","general.audio","general.notifications","general.uiScale","general.pack","general.version","content.activePackSection","content.reloadSection","content.lastReloadSection","content.foldersSection","content.activeLanguage","content.exact","content.dynamic","content.result","content.errors","settings.debugLogging","actions.reloadAll","actions.reloadTexts","actions.reloadFonts","actions.reloadTextures","actions.reloadAudio","actions.openTexturesFolder","actions.openAudioFolder","actions.exportUntranslated","actions.exportDiagnostics","actions.exportCurrentScene","actions.openLocaleFolder","actions.openExportFolder","actions.openModFolder","actions.openLogs","status.reloadPreserved","stats.translatorCanvas"})
        if(!ptKeys.Contains(key)) throw new Exception("Missing UI key: "+key);
});
Test("JSON trailing comma loads with warning", () => {
    var path=Path.Combine(Path.GetTempPath(),"pvz-json-"+Guid.NewGuid()+".json");
    try {
        File.WriteAllText(path,"{\"value\":1,}");var warnings=new List<string>();
        using var doc=PvZSymbiosisTranslator.Localization.JsonFile.Read(path,warnings.Add);
        if(doc.RootElement.GetProperty("value").GetInt32()!=1 || warnings.Count!=1 || !warnings[0].Contains("Trailing comma",StringComparison.Ordinal)) throw new Exception("Compatibility warning missing");
        File.WriteAllText(path,"{\"value\":");Reject(()=>PvZSymbiosisTranslator.Localization.JsonFile.Read(path));
    } finally {if(File.Exists(path))File.Delete(path);}
});
Test("transactional reload preserves last good", () => {
    var service=new PvZSymbiosisTranslator.Localization.Reload.TransactionalReloadService();var active="last-good";
    var failed=service.Execute<string>("texts",7,()=>throw new FormatException("broken JSON"),candidate=>active=candidate);
    if(failed || active!="last-good" || !service.Last.Attempted || service.Last.Success || service.Last.FilesProcessed!=7 || !service.Last.Error.Contains("broken JSON")) throw new Exception("Failed candidate replaced last-good state");
    var passed=service.Execute("texts",7,()=>"candidate",candidate=>active=candidate);
    if(!passed || active!="candidate" || !service.Last.Success || service.Last.Scope!="texts") throw new Exception("Valid candidate was not activated");
});
Test("ModStrings candidate is complete without mutation", () => {
    var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../PvZ_Symbiosis_Translator/Localization/pt-BR"));
    var candidate=PvZSymbiosisTranslator.UI.ModLabels.LoadCandidate(root);
    if(candidate.Count<100 || !candidate.ContainsKey("content.activePackSection") || !candidate.ContainsKey("status.reloadPreserved")) throw new Exception("Incomplete ModStrings candidate");
});
var store = new ExactStringStore();
store.Add("植物！", "Plantar!");
Test("exact", () => Equal("Plantar!", store.Translate("植物！")));
foreach (var s in new[] { " 植物！", "植物！ ", "植物!", "植物！\n", "未知" })
    Test("ordinal fallback " + JsonSerializer.Serialize(s), () => Equal(s, store.Translate(s)));
Test("null", () => Equal(null, store.Translate(null)));
Test("empty target", () => { store.Add("测试", ""); Equal("", store.Translate("测试")); });
Test("whitespace target", () => Reject(() => store.Add("测试", " ")));
Test("conflict", () => Reject(() => store.Add("植物！", "Outra")));
Test("same duplicate", () => store.Add("植物！", "Plantar!"));
Test("exact section marker ignored by lookup and count", () => {
    var marker="==================== [UI / MAIN MENU] ====================";var before=store.Count;store.Add(marker,"");
    if(store.Count!=before||store.Entries.ContainsKey(marker))throw new Exception("Marker entered exact store");Equal(marker,store.Translate(marker));
});
Test("exact subsection marker ignored", () => {var s=new ExactStringStore();s.Add("---------- [NAVIGATION] ----------","");if(s.Count!=0)throw new Exception("Subsection entered store");});
Test("malformed exact marker rejected", () => Reject(()=>new ExactStringStore().Add("========== [UI] ==========","")));
Test("marker target must be empty", () => Reject(()=>new ExactStringStore().Add("==================== [UI] ====================","translation")));
Test("genuine dashed source is not a marker", () => {var s=new ExactStringStore();s.Add("---------- ataque ----------","Texto");if(s.Count!=1)throw new Exception("Genuine source ignored");});
Test("no unicode normalization", () => { store.Add("é", "accent"); Equal("e\u0301", store.Translate("e\u0301")); });
Test("placeholders preserved", () => TranslationValidator.Validate("值{0} %s\n<b>30%</b>", "Valor{0} %s\n<b>30%</b>"));
Test("placeholder missing", () => Reject(() => TranslationValidator.Validate("值{0}", "Valor")));
Test("mechanic change is advisory", () => { TranslationValidator.Validate("20点。", "200 de dano."); if(!TranslationValidator.Warnings("20点。", "200 de dano.").Any(x=>x.Contains("mechanic"))) throw new Exception("Missing numeric warning"); });
Test("decimal and multiplication localized", () => { TranslationValidator.Validate("20*18点/2.5秒。", "20 × 18 de dano/2,5 s."); if(TranslationValidator.Warnings("20*18点/2.5秒。", "20 × 18 de dano/2,5 s.").Any(x=>x.Contains("mechanic"))) throw new Exception("False numeric warning"); });
Test("numeric reordering is advisory-safe", () => { if(TranslationValidator.Warnings("韧性1500的1级", "Nv. 1 com 1500 de resistência").Any(x=>x.Contains("mechanic"))) throw new Exception("Numeric reorder warned"); });
Test("source tags may be removed", () => TranslationValidator.Validate("<color=#F81000>伤害：</color>", "Dano:"));
Test("source tags may change", () => TranslationValidator.Validate("<color=#F81000>伤害：</color>", "<b><color=#00FF00>Dano:</color></b>"));
Test("target newlines free", () => TranslationValidator.Validate("短文本", "Texto\nem português\nmais uma linha"));
Test("source newlines free", () => TranslationValidator.Validate("短\n文本", "Texto em português"));
Test("unknown tag advisory", () => { TranslationValidator.Validate("植物", "<future-tag>Planta</future-tag>"); if(!TranslationValidator.Warnings("植物", "<future-tag>Planta</future-tag>").Any()) throw new Exception("Expected advisory warning"); });
Test("malformed tags advisory", () => { TranslationValidator.Validate("植物", "<b><i>Planta</b>"); if(!TranslationValidator.Warnings("植物", "<b><i>Planta</b>").Any()) throw new Exception("Expected advisory warning"); });
Test("noparse content not parsed", () => { var warnings=TranslationValidator.Warnings("植物", "<noparse><size=999>literal</size></noparse>"); if(warnings.Count!=0) throw new Exception(string.Join(";",warnings)); });
foreach (var markup in new[] { "<size=75%>Texto</size>", "<size=+4>Texto</size>", "<size=-2>Texto</size>", "<size=1.2em>Texto</size>", "<color=#FF0000>Texto</color>", "<color=#FF000080>Texto</color>", "<b>Texto</b>", "<i>Texto</i>", "<u>Texto</u>", "<s>Texto</s>", "<align=center>Texto</align>", "<cspace=-0.05em>Texto</cspace>", "<voffset=0.1em>Texto</voffset>", "<mark=#FFFF0080>Texto</mark>", "<sup>2</sup>", "<sub>2</sub>", "<nobr>Texto longo</nobr>", "<noparse><size=999>literal</size></noparse>", "<color=#FFD700><size=110%><b>Almanaque</b></size></color>" })
    Test("target markup " + markup, () => TranslationValidator.Validate("图鉴", markup));
Test("placeholder inside rich target", () => TranslationValidator.Validate("数值 {0}", "<size=80%>Valor {0}</size>"));
Test("placeholder missing in rich target", () => Reject(() => TranslationValidator.Validate("数值 {0}", "<size=80%>Valor</size>")));
Test("placeholder extra in rich target", () => Reject(() => TranslationValidator.Validate("数值 {0}", "<size=80%>Valor {0} {1}</size>")));
Test("placeholder occurrence count", () => Reject(() => TranslationValidator.Validate("数值 {0} {0}", "Valor {0}")));
Test("printf strict", () => Reject(() => TranslationValidator.Validate("Vida %s %d", "Vida %s")));
Test("literal percentage versus printf", () => TranslationValidator.Validate("50%概率", "50% de chance"));
Test("intentional empty target", () => { TranslationValidator.Validate("植物", ""); if(!TranslationValidator.Warnings("植物", "").Any()) throw new Exception("Missing empty warning"); });
Test("null target", () => Reject(() => TranslationValidator.Validate("植物", null)));
Test("rich text policy enables target tags", () => { if(!RichTextPolicy.Desired(false, "全部禁用", "<size=75%>Desativar tudo</size>", true)) throw new Exception("TMP tags not enabled"); });
Test("rich text policy restores original false", () => { if(RichTextPolicy.Desired(false, "全部禁用", "全部禁用", false)) throw new Exception("F9 state not restored"); });
Test("rich text policy preserves original true", () => { if(!RichTextPolicy.Desired(true, "全部禁用", "全部禁用", false)) throw new Exception("Original rich text lost"); });
Test("dynamic formatted target", () => { var rules=new DynamicRuleStore(); rules.Add("sun", @"^阳光：(?<amount>\d+)$", "<color=#FFD700>Sol: ${amount}</color>"); if(!rules.TryTranslate("阳光：42", out var value)) throw new Exception("Rule did not match"); Equal("<color=#FFD700>Sol: 42</color>",value); });
Test("dynamic bad ID can be retried", () => { var rules=new DynamicRuleStore(); Reject(()=>rules.Add("sun", "bad", "${amount}")); rules.Add("sun", @"^阳光：(?<amount>\d+)$", "Sol: ${amount}"); });
Test("dynamic capture can repeat", () => { var rules=new DynamicRuleStore(); rules.Add("repeat", @"^(?<value>\d+)秒$", "${value} / valor ${value}"); if(!rules.TryTranslate("42秒",out var text)) throw new Exception("No match"); Equal("42 / valor 42",text); });
Test("dynamic unknown capture rejected", () => Reject(() => new DynamicRuleStore().Add("bad", @"^(?<value>\d+)秒$", "${value} ${unknown}")));
Test("dynamic seconds and multiplier preserve zero-width space", () => { var rules=new DynamicRuleStore(); rules.Add("seconds", @"^(?<value>\d+(?:[.,]\d+)?)秒(?<zwsp>\u200B?)$", "${value} s${zwsp}"); rules.Add("multiplier", @"^(?<value>\d+(?:[.,]\d+)?)倍(?<zwsp>\u200B?)$", "${value}×${zwsp}"); if(!rules.TryTranslate("31秒\u200B",out var seconds) || seconds!="31 s\u200B") throw new Exception("Seconds rule failed"); if(!rules.TryTranslate("4,6倍\u200B",out var multiple) || multiple!="4,6×\u200B") throw new Exception("Multiplier rule failed"); });
Test("dynamic exact audit keeps intentional overrides", () => { var rules=new DynamicRuleStore(); rules.Add("slots", @"^(?<value>\d+)槽$", "${value} espaços"); var exact=new Dictionary<string,string>{{"1槽","1 espaços"},{"2槽","2 espaços"},{"3槽","3 posições"}}; var matches=DynamicExactAudit.Inspect(exact,rules); if(matches.Count!=3 || matches.Count(x=>x.Redundant)!=2 || matches.Count(x=>!x.Redundant)!=1 || matches.Single(x=>!x.Redundant).Source!="3槽") throw new Exception("Override audit failed"); });
var pack = new LanguagePack { Locale = "pt-BR" };
pack.Exact.Add("阳光：123", "Sol: 123");
pack.Dynamic.Add("sun", @"^阳光：(?<amount>\d+)$", "Sóis: ${amount}");
var context = new ExactStringStore(); context.Add("阳光：123", "Energia: 123"); pack.Contexts.Add("Home/Counter", context);
var service = new TranslationService(pack);
Test("context priority", () => Equal("Energia: 123", service.Translate("阳光：123", "Home/Counter")));
Test("exact before dynamic", () => Equal("Sol: 123", service.Translate("阳光：123")));
Test("dynamic", () => Equal("Sóis: 42", service.Translate("阳光：42")));
Test("dynamic disabled", () => Equal("阳光：42", service.Translate("阳光：42", dynamicRules:false)));
Test("dynamic full string incl newline", () => Equal("阳光：42\n", service.Translate("阳光：42\n")));
Test("unanchored", () => Reject(() => new DynamicRuleStore().Add("bad", @"阳光：(\d+)", "$1")));
Test("invalid regex", () => Reject(() => new DynamicRuleStore().Add("bad", "^[$", "bad")));
Test("lost group", () => Reject(() => new DynamicRuleStore().Add("bad", @"^(?<amount>\d+)$", "lost")));
Test("dynamic ID required", () => Reject(() => new DynamicRuleStore().Add("", @"^(?<amount>\d+)$", "${amount}")));
Test("locale traversal", () => Reject(() => new ModPaths(".").Locale("../pt-BR")));
Test("safe multi-subtag locales", () => { foreach(var name in new[]{"pt-BR","es-419","zh-Hans","zh-Hant","sr-Latn-RS"}) if(!ModPaths.IsValidLocale(name)) throw new Exception(name); foreach(var name in new[]{"../pt-BR","pt/BR","pt-BR/../es-ES","_template"}) if(ModPaths.IsValidLocale(name)) throw new Exception("Unsafe locale: "+name); });
Test("config key persistence", () => {
    var path = Path.Combine(Path.GetTempPath(), "pvz-config-" + Guid.NewGuid() + ".json");
    try { var c = new ModConfig { Enabled = false, ToggleTranslationKey = "F7", ReloadTranslationKey = "F6" }; ConfigManager.Save(path, c); var loaded = ConfigManager.Load(path); if (loaded.Enabled || loaded.ToggleTranslationKey != "F7" || loaded.ReloadTranslationKey != "F6") throw new Exception("Config state lost"); }
    finally { if (File.Exists(path)) File.Delete(path); }
});
Test("general UI config persistence and clamp", () => {
    var path=Path.Combine(Path.GetTempPath(),"pvz-general-ui-"+Guid.NewGuid()+".json");
    try {
        ConfigManager.Save(path,new ModConfig {UiNotifications=false,UiScalePercent=110f});
        var loaded=ConfigManager.Load(path);
        if(loaded.UiNotifications || loaded.UiScalePercent!=110f) throw new Exception("General UI settings lost");
        File.WriteAllText(path,"{\"uiNotifications\":true,\"uiScalePercent\":900}");
        loaded=ConfigManager.Load(path);
        if(!loaded.UiNotifications || loaded.UiScalePercent!=115f) throw new Exception("UI scale not clamped");
    } finally {if(File.Exists(path))File.Delete(path);}
});
Test("available locales", () => { var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../PvZ_Symbiosis_Translator/Localization")); var available = LocaleManager.AvailableLanguages(root); if (!available.Contains("pt-BR")) throw new Exception("pt-BR missing"); if (available.Contains("_template")) throw new Exception("template exposed"); });
Test("legacy hotReload config is accepted then omitted", () => {
    var path=Path.Combine(Path.GetTempPath(),"pvz-legacy-config-"+Guid.NewGuid()+".json");
    try { File.WriteAllText(path,"{\"language\":\"pt-BR\",\"hotReload\":true}"); var config=ConfigManager.Load(path); ConfigManager.Save(path,config);
        if(config.Language!="pt-BR" || File.ReadAllText(path).Contains("hotReload")) throw new Exception("Dead setting persisted"); }
    finally { if(File.Exists(path)) File.Delete(path); }
});
Test("locale discovery rejects stale template and mismatched folder", () => { var root=Path.Combine(Path.GetTempPath(),"pvz-discovery-"+Guid.NewGuid()); try { Directory.CreateDirectory(Path.Combine(root,"_template")); Directory.CreateDirectory(Path.Combine(root,"es-ES")); Directory.CreateDirectory(Path.Combine(root,"en-US")); File.WriteAllText(Path.Combine(root,"_template","manifest.json"),"{\"locale\":\"es-ES\",\"displayName\":\"Español\"}"); File.WriteAllText(Path.Combine(root,"es-ES","manifest.json"),"{\"locale\":\"pt-BR\",\"displayName\":\"Wrong\"}"); File.WriteAllText(Path.Combine(root,"en-US","manifest.json"),"{\"locale\":\"en-US\",\"displayName\":\"English\"}"); var warnings=new List<string>(); var found=LocaleManager.AvailableLanguageInfos(root,warnings.Add); if(found.Count!=1 || found[0].Locale!="en-US" || warnings.Count!=1) throw new Exception("Ghost locale visible"); } finally {Directory.Delete(root,true);} });
Test("human dump escaping", () => { var s = PvZSymbiosisTranslator.Diagnostics.DiagnosticString.EscapeInvisible("乌鸦\n20秒\u200B"); if (s != "乌鸦\\n20秒\\u200B") throw new Exception(s); });
Test("Han source detection includes extensions only", () => { foreach(var value in new[]{"植物", "㐀", "𠀀"}) if(!PvZSymbiosisTranslator.Diagnostics.HanSourceDetector.ContainsHan(value)) throw new Exception(value); foreach(var value in new[]{"English", "カタカナ", "한글", "20 s"}) if(PvZSymbiosisTranslator.Diagnostics.HanSourceDetector.ContainsHan(value)) throw new Exception(value); });

if (args.Length > 0)
{
    var sourceDir = Path.GetFullPath(args[0]);
    Test("pt-BR pack validation", () => { var loaded = LocaleManager.Load(sourceDir, "pt-BR", "1.2.0"); Console.WriteLine($"Validated {loaded.Exact.Count} exact entries"); });
    Test("pt-BR slider migration and rapid sequence", () => {
        var loaded=LocaleManager.Load(sourceDir,"pt-BR","1.2.0");
        var translate=new TranslationService(loaded);
        for(var i=0;i<=30;i++) {
            var source=i+"秒\u200B";
            if(loaded.Exact.TryGet(source,out _)) throw new Exception("Redundant exact seconds: "+i);
            Equal(i+" s\u200B",translate.Translate(source));
        }
        foreach(var source in new[]{"0,1倍\u200B","1,5倍\u200B","5倍\u200B"}) {
            if(loaded.Exact.TryGet(source,out _)) throw new Exception("Redundant exact multiplier");
            Equal(source.Replace("倍","×"),translate.Translate(source));
        }
        foreach(var value in new[]{"5","10","100","1000","4400"}) {
            var source="一类:"+value;
            if(loaded.Exact.TryGet(source,out _)) throw new Exception("Redundant exact type-one value");
            Equal("Tipo um: "+value,translate.Translate(source));
        }
    });
    string scratch = Path.Combine(Path.GetTempPath(), "pvz-locale-tests-" + Guid.NewGuid());
    Directory.CreateDirectory(scratch);
    try
    {
        foreach (var f in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var dest = Path.Combine(scratch, Path.GetRelativePath(sourceDir, f));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)); File.Copy(f, dest);
        }
        var manifest = Path.Combine(scratch, "manifest.json");
        var original = File.ReadAllText(manifest);
        var exactFile = Path.Combine(scratch, "Strings", "exact.json");
        var originalExact = File.ReadAllText(exactFile);
        Test("supported game versions and legacy compatibility", () => {
            LocaleManager.Load(scratch,"pt-BR","1.2.0",false);
            var withVersions=original.Replace("\"gameVersion\": \"1.2.0\"","\"gameVersion\": \"1.2.0\", \"supportedGameVersions\": [\"1.2.0\"]");
            if(withVersions==original) throw new Exception("Manifest fixture changed");
            try { File.WriteAllText(manifest,withVersions); LocaleManager.Load(scratch,"pt-BR","1.2.0",false);
                Reject(()=>LocaleManager.Load(scratch,"pt-BR","1.3.0",false));
                File.WriteAllText(manifest,withVersions.Replace("[\"1.2.0\"]","[]"));
                Reject(()=>LocaleManager.Load(scratch,"pt-BR","1.2.0",false));
            } finally { File.WriteAllText(manifest,original); }
        });
        Test("failed candidate report leaves active report unchanged", () => {
            var activeReport=Path.Combine(scratch,"translation_validation.json");
            var attemptedReport=Path.Combine(scratch,"translation_validation.pt-BR.json");
            File.WriteAllText(activeReport,"active sentinel");
            Reject(()=>LocaleManager.Load(scratch,"pt-BR","9.9.9",false,attemptedReport));
            if(File.ReadAllText(activeReport)!="active sentinel") throw new Exception("Failed candidate replaced active report");
            using(var failed=JsonDocument.Parse(File.ReadAllText(attemptedReport))) if(failed.RootElement.GetProperty("fatals").GetInt32()==0) throw new Exception("No candidate failure report");
            var selected=LocaleManager.Load(scratch,"pt-BR","1.2.0",false,attemptedReport);
            selected.Validation.WriteIfRequested(activeReport);
            using var active=JsonDocument.Parse(File.ReadAllText(activeReport));
            if(active.RootElement.GetProperty("locale").GetString()!="pt-BR" || active.RootElement.GetProperty("fatals").GetInt32()!=0)
                throw new Exception("Active report not updated on success");
        });
        Test("external locale no rebuild", () => {
            File.WriteAllText(manifest, original.Replace("pt-BR", "es-ES"));
            File.WriteAllText(exactFile, "{\"植物！\":\"¡Plantar!\"}");
            Equal("¡Plantar!", new TranslationService(LocaleManager.Load(scratch, "es-ES", "1.2.0")).Translate("植物！"));
        });
        File.WriteAllText(exactFile, originalExact);
        File.WriteAllText(manifest, original);
        Test("duplicate manifest field", () => { File.WriteAllText(manifest, original.Replace("{", "{\"locale\":\"en-US\",")); Reject(() => LocaleManager.Load(scratch, "pt-BR", "1.2.0")); });
        File.WriteAllText(manifest, original);
        var glossaryFile = Path.Combine(scratch, "glossary.json");
        var originalGlossary = File.ReadAllText(glossaryFile);
        Test("invalid glossary JSON", () => { File.WriteAllText(glossaryFile, "{"); Reject(() => LocaleManager.Load(scratch, "pt-BR", "1.2.0")); });
        File.WriteAllText(glossaryFile, originalGlossary);
        var fontManifestFile = Path.Combine(scratch, "Fonts", "manifest.json");
        var originalFontManifest = File.ReadAllText(fontManifestFile);
        Test("missing font license", () => { File.WriteAllText(fontManifestFile, originalFontManifest.Replace("OFL.txt", "missing-license.txt")); Reject(() => LocaleManager.Load(scratch, "pt-BR", "1.2.0")); });
        File.WriteAllText(fontManifestFile, originalFontManifest);
        Test("wrong game version", () => Reject(() => LocaleManager.Load(scratch, "pt-BR", "9.0")));
        Test("invalid JSON", () => { File.WriteAllText(manifest, "{"); Reject(() => LocaleManager.Load(scratch, "pt-BR", "1.2.0")); });
        File.WriteAllText(manifest, original);
        foreach (var kind in new[] {"Fonts", "Textures", "Audio"})
        {
            var assetManifest = Path.Combine(scratch, kind, "manifest.json");
            Test("missing " + kind, () => { File.WriteAllText(assetManifest, "[{\"id\":\"test\",\"file\":\"missing.asset\"}]"); Reject(() => LocaleManager.Load(scratch, "pt-BR", "1.2.0")); });
            File.WriteAllText(assetManifest, "[]");
        }
        var almanacFile = Path.Combine(scratch, "Almanac", "exact.json");
        var originalAlmanac = File.ReadAllText(almanacFile);
        var contextFile = Path.Combine(scratch, "Strings", "context_overrides.json");
        var originalContexts = File.ReadAllText(contextFile);
        var dynamicFile = Path.Combine(scratch, "Strings", "dynamic_rules.json");
        var originalDynamic = File.ReadAllText(dynamicFile);
        var reportFile = Path.Combine(scratch, "translation_validation.json");
        Test("JSON duplicate first valid wins", () => {
            File.WriteAllText(exactFile, "{\"植物\":\"Planta\",\"植物\":\"Outra\"}");
            var loaded = LocaleManager.Load(scratch, "pt-BR", "1.2.0");
            Equal("Planta",new TranslationService(loaded).Translate("植物"));
            if(loaded.Validation.Rejected!=1 || loaded.Validation.Issues.Count(x=>x.Severity==ValidationSeverity.Error)!=1) throw new Exception("Conflict was not isolated");
        });
        File.WriteAllText(exactFile, originalExact);
        File.WriteAllText(dynamicFile,"[]");
        Test("100 valid plus one rejected and report", () => {
            var entries = Enumerable.Range(0,100).ToDictionary(i=>"源"+i,i=>"Valor "+i,StringComparer.Ordinal);
            entries.Add("数值 {0}","Valor sem placeholder");
            File.WriteAllText(almanacFile,"{}"); File.WriteAllText(exactFile,JsonSerializer.Serialize(entries));
            var loaded = LocaleManager.Load(scratch,"pt-BR","1.2.0",true,reportFile);
            if(loaded.Exact.Count!=100 || loaded.Validation.Loaded!=100 || loaded.Validation.Rejected!=1) throw new Exception("Entry isolation count incorrect");
            Equal("Valor 99",new TranslationService(loaded).Translate("源99"));
            Equal("数值 {0}",new TranslationService(loaded).Translate("数值 {0}"));
            using var report=JsonDocument.Parse(File.ReadAllText(reportFile));
            if(report.RootElement.GetProperty("rejected").GetInt32()!=1 || report.RootElement.GetProperty("issues")[0].GetProperty("severity").GetString()!="error" || report.RootElement.GetProperty("issues")[0].GetProperty("sourceFile").GetString()!="Strings/exact.json") throw new Exception("Validation report missing issue");
        });
        File.WriteAllText(exactFile,originalExact); File.WriteAllText(almanacFile,originalAlmanac);
        File.WriteAllText(dynamicFile,originalDynamic);
        Test("context errors isolated", () => {
            File.WriteAllText(contextFile,"{\"Home/A\":{\"数值 {0}\":\"Valor sem campo\",\"植物\":\"<size=75%>Planta</size>\"}}");
            var loaded=LocaleManager.Load(scratch,"pt-BR","1.2.0");
            Equal("<size=75%>Planta</size>",new TranslationService(loaded).Translate("植物","Home/A"));
            if(loaded.Validation.Rejected!=1) throw new Exception("Context entry rejected whole pack");
        });
        File.WriteAllText(contextFile,originalContexts);
        Test("dynamic errors isolated", () => {
            File.WriteAllText(dynamicFile,JsonSerializer.Serialize(new[] { new { id="bad", pattern="unanchored", target="${amount}" }, new { id="sun", pattern=@"^阳光：(?<amount>\d+)$", target="<color=#FFD700>Sol: ${amount}</color>" } }));
            var loaded=LocaleManager.Load(scratch,"pt-BR","1.2.0");
            Equal("<color=#FFD700>Sol: 42</color>",new TranslationService(loaded).Translate("阳光：42"));
            if(loaded.Validation.Rejected!=1 || loaded.Dynamic.Count!=1) throw new Exception("Dynamic rule isolation failed");
        });
        File.WriteAllText(dynamicFile,originalDynamic);
        Test("F10-style formatting reload without rebuild", () => {
            File.WriteAllText(exactFile,"{\"全部禁用\":\"<size=100%>Texto</size>\"}");
            var first=new TranslationService(LocaleManager.Load(scratch,"pt-BR","1.2.0"));
            Equal("<size=100%>Texto</size>",first.Translate("全部禁用"));
            File.WriteAllText(exactFile,"{\"全部禁用\":\"<size=70%><color=#FF0000>Texto</color></size>\"}");
            var second=new TranslationService(LocaleManager.Load(scratch,"pt-BR","1.2.0"));
            Equal("<size=70%><color=#FF0000>Texto</color></size>",second.Translate("全部禁用"));
        });
        File.WriteAllText(exactFile,originalExact);
        Test("invalid exact JSON fatal with report", () => {
            File.WriteAllText(exactFile,"{");
            Reject(()=>LocaleManager.Load(scratch,"pt-BR","1.2.0",true,reportFile));
            using var report=JsonDocument.Parse(File.ReadAllText(reportFile));
            if(report.RootElement.GetProperty("fatals").GetInt32()!=1) throw new Exception("Fatal JSON not reported");
        });
        File.WriteAllText(exactFile,originalExact);
        Test("runtime collector restart persistence", () => {
            var first = new PvZSymbiosisTranslator.Diagnostics.RuntimeStringCollector();
            first.Record("未知", "Home", "Label", "TMP", "Home/Label"); first.Save(scratch);
            var second = new PvZSymbiosisTranslator.Diagnostics.RuntimeStringCollector();
            second.Load(scratch); second.Record("未知", "Home", "Label", "TMP", "Home/Label"); second.Save(scratch);
            using var dump = JsonDocument.Parse(File.ReadAllText(Path.Combine(scratch, "runtime_strings.json")));
            if (dump.RootElement.GetArrayLength() != 1 || dump.RootElement[0].GetProperty("count").GetInt32() != 2) throw new Exception("Aggregation lost across restart");
        });
    }
    finally { Directory.Delete(scratch, true); }
}
Console.WriteLine($"RESULT: {passed} PASS, {failed} FAIL");
Test("export preserves work and exact contexts", () => {
    var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
        Directory.CreateDirectory(Path.Combine(root, "TranslationExport"));
        Directory.CreateDirectory(Path.Combine(root, "Dumps"));
        File.WriteAllText(Path.Combine(root,"TranslationExport","pending.json"), "{\"乌鸦\":\"Corvo\"}");
        File.WriteAllText(Path.Combine(root,"Dumps","source_catalog.json"), "[{\"source\":\"字表\",\"classification\":\"CHARACTER_SET\"}]");
        var collector = new PvZSymbiosisTranslator.Diagnostics.RuntimeStringCollector();
        collector.Record("乌鸦", "Home", "Title", "TMP", "Home/Title");
        collector.Record("字表", "Home", "Table", "TMP", "Home/Table");
        collector.Snapshot()[0].Contexts.Clear();
        PvZSymbiosisTranslator.Diagnostics.TranslationExportWriter.Write(root,root,collector,s=>s);
        using var pending = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"TranslationExport","pending.json")));
        Equal("Corvo",pending.RootElement.GetProperty("乌鸦").GetString());
        if(pending.RootElement.TryGetProperty("字表",out _)) throw new Exception("Technical data exported");
        if(!File.Exists(Path.Combine(root,"TranslationExport","ByContext","Home.json"))) throw new Exception("Scene fallback lost");
    } finally { Directory.Delete(root,true); }
});
Test("whole source dynamic candidates", () => {
    foreach(var source in new[]{"0秒\u200B","20秒\u200B","1倍\u200B"}) if(!PvZSymbiosisTranslator.Diagnostics.TranslationExportWriter.IsDynamicCandidate(source)) throw new Exception(source);
    foreach(var source in new[]{"持续20秒。","20秒\n"," 20秒","伤害1倍"}) if(PvZSymbiosisTranslator.Diagnostics.TranslationExportWriter.IsDynamicCandidate(source)) throw new Exception(source);
});
Test("dynamic family grouping is bounded and deterministic", () => {
    var entries=new[]{"0秒\u200B","1秒\u200B","2秒\u200B","0,1倍\u200B","0,2倍\u200B","一类:5","一类:10","比率:1,5","比率:2,5","持续20秒。"}
        .Select(source=>new PvZSymbiosisTranslator.Diagnostics.RuntimeStringCollector.Entry { Source=source, Count=2 });
    var families=PvZSymbiosisTranslator.Diagnostics.DynamicCandidateFamilies.Build(entries);
    if(families.Count!=4 || families[0].Family!="multiplier" || families[1].Family!="prefix_decimal" ||
        families[2].Family!="prefix_integer" || families[3].Family!="seconds") throw new Exception("Wrong families");
    if(families[2].Prefix!="一类:" || families[2].SuggestedPattern!="^一类:(?<value>\\d+)(?<zwsp>\\u200B?)$" ||
        families[2].SuggestedTarget!=null) throw new Exception("Wrong generic prefix suggestion");
    if(families[3].Occurrences!=6 || families[3].Values.Length!=3 || families[3].SuggestedTarget!="${value} s${zwsp}" ||
        families[3].SuggestedPattern!="^(?<value>\\d+)秒(?<zwsp>\\u200B?)$") throw new Exception("Wrong seconds suggestion");
});
Test("custom event descriptions compose in any order", () => {
    var targets=new Dictionary<string,string>(StringComparer.Ordinal);
    for(var i=1;i<=9;i++) targets["事件"+i]="Evento "+i;
    targets["富文本"]="<b>Texto</b>";
    string Translate(string value)=>targets.TryGetValue(value,out var target)?target:value;
    void Check(string source,string expected,int segments,int translated,int unknown) {
        var result=CompositeTranslation.Translate(source,Translate);
        Equal(expected,result.Text);
        if(result.Segments!=segments || result.Translated!=translated || result.Unknown!=unknown)
            throw new Exception($"counts {result.Segments}/{result.Translated}/{result.Unknown}");
    }
    Check("事件1","Evento 1",1,1,0);
    Check("事件1\n\n事件2\n\n","Evento 1\n\nEvento 2\n\n",2,2,0);
    Check("事件5\n\n事件2\n\n事件9\n\n事件1\n\n事件7","Evento 5\n\nEvento 2\n\nEvento 9\n\nEvento 1\n\nEvento 7",5,5,0);
    var all=string.Join("\n\n",Enumerable.Range(1,9).Select(i=>"事件"+i));
    var allTarget=string.Join("\n\n",Enumerable.Range(1,9).Select(i=>"Evento "+i));
    Check(all,allTarget,9,9,0);
    Check("事件3\n\n未知\n\n事件1\n\n","Evento 3\n\n未知\n\nEvento 1\n\n",3,2,1);
    Check("富文本\n\n事件1","<b>Texto</b>\n\nEvento 1",2,2,0);
    targets["事件2\n\n事件1"]="Substituição explícita";
    Check("事件2\n\n事件1","Evento 2\n\nEvento 1",2,2,0);
    targets.Remove("事件4"); targets["事件4\n\n"]="Evento 4\n\n";
    Check("事件4","Evento 4",1,1,0);
    Check("事件4\n\n事件1\n\n","Evento 4\n\nEvento 1\n\n",2,2,0);
});
Test("composite all 511 subsets bypass whole-combination exact", () => {
    for(int mask=1;mask<512;mask++) {
        var selected=Enumerable.Range(1,9).Where(i=>(mask & (1<<(i-1)))!=0).Reverse().ToArray();
        var source=string.Join("\r\n\r\n",selected.Select(i=>"事件"+i))+"\r\n\r\n";
        var expected=string.Join("\r\n\r\n",selected.Select(i=>"Evento "+i))+"\r\n\r\n";
        string Translate(string x) => x.Contains('\n') ? "OLD COMBINATION" : x.StartsWith("事件") ? "Evento "+x.Substring(2) : x;
        var result=CompositeTranslation.Translate(source,Translate);
        Equal(expected,result.Text);
        if(result.Segments!=selected.Length || result.Translated!=selected.Length || result.Unknown!=0) throw new Exception("Subset "+mask);
    }
});
Test("legacy hotkeys migrate once and custom keys survive", () => {
    var path=Path.Combine(Path.GetTempPath(),"pvz-key-migration-"+Guid.NewGuid()+".json");
    try {
        ConfigManager.Save(path,new ModConfig { ToggleTranslationKey="F9",ReloadTranslationKey="F10",DiagnosticKey="F8" });
        int notices=0; var config=ConfigManager.Load(path,_=>notices++);
        Equal("Insert",config.ToggleTranslationKey); Equal("PageUp",config.ReloadTranslationKey); Equal("PageDown",config.DiagnosticKey);
        ConfigManager.Load(path,_=>notices++); if(notices!=1) throw new Exception("Repeated migration");
        ConfigManager.Save(path,new ModConfig { ToggleTranslationKey="Home",ReloadTranslationKey="End",DiagnosticKey="Delete" });
        var custom=ConfigManager.Load(path); Equal("Home",custom.ToggleTranslationKey); Equal("End",custom.ReloadTranslationKey); Equal("Delete",custom.DiagnosticKey);
    } finally {File.Delete(path);}
});
Test("tracked refresh rejects inactive stale and translator-owned components", () => {
    for(int bits=0;bits<32;bits++) {
        bool alive=(bits&1)!=0, active=(bits&2)!=0, owned=(bits&4)!=0, current=(bits&8)!=0, modal=(bits&16)!=0;
        bool result=TextRefreshPolicy.ShouldRefresh(alive,active,owned,current,modal);
        if(result && (!alive || !active || owned || (!current && !modal))) throw new Exception("Unsafe refresh "+bits);
        if(alive && active && !owned && (current || modal) && !result) throw new Exception("Missed active text "+bits);
    }
});
Test("translator ownership names exclude game hierarchy names", () => {
    foreach(var name in new[]{"PvZTranslationUI","PvZTranslationSettingsRoot","PvZTranslationToast","PvZTranslationToastLabel","PvZTranslator.Native.Toggle"})
        if(!TextRefreshPolicy.IsTranslatorName(name)) throw new Exception(name);
    foreach(var name in new[]{"Canvas","HomeManager","SetupWindow(Clone)","DifficultyWindow(Clone)",null})
        if(TextRefreshPolicy.IsTranslatorName(name)) throw new Exception(name);
});
Console.WriteLine($"FINAL RESULT: {passed} PASS, {failed} FAIL");
Test("optional reload failure isolation", () => {
    var coordinator=new PvZSymbiosisTranslator.Core.ReloadCoordinator();var restored=false;var next=false;
    coordinator.Optional("font",()=>throw new Exception("bad asset"),()=>restored=true);
    coordinator.Optional("audio",()=>next=true,()=>{});
    if(!restored || !next || coordinator.Results[0].Status!="WARNING" || coordinator.Results[1].Status!="SUCCESS") throw new Exception("Reload isolation broken");
});
Console.WriteLine($"VERIFIED RESULT: {passed} PASS, {failed} FAIL");
Test("PNG pixels alpha and corruption",()=>{
    var pixels=new byte[]{255,0,0,255,0,255,0,128,0,0,255,0,40,50,60,70};
    var png=PvZSymbiosisTranslator.Core.PngCodec.Encode(2,2,pixels);
    var decoded=PvZSymbiosisTranslator.Core.PngCodec.Decode(png);
    if(decoded.Width!=2||decoded.Height!=2||!pixels.SequenceEqual(decoded.Rgba))throw new Exception("RGBA changed");
    png[20]^=1;Reject(()=>PvZSymbiosisTranslator.Core.PngCodec.Decode(png));
});
Console.WriteLine($"FINAL VERIFIED: {passed} PASS, {failed} FAIL");
Test("texture metadata exact and context match",()=>{
    var source=new PvZSymbiosisTranslator.Core.TextureSource {Scene="Home",Context="Canvas/A",TextureName="atlas",SpriteName="button",Width=124,Height=52,RuntimeAssetKey="1:2"};
    var match=new PvZSymbiosisTranslator.Core.TextureMatch {TextureName="atlas",SpriteName="button",Width=124,Height=52};
    if(match.Resolve(new[]{source}).Count!=1) throw new Exception("Unique source lost");
    match.TextureName="Atlas";if(match.Matches(source))throw new Exception("Case normalized");
    match.TextureName="atlas";match.Context="Canvas/B";if(match.Matches(source))throw new Exception("Context ignored");
});
Test("texture ambiguity rejected without pixel readback",()=>{
    var a=new PvZSymbiosisTranslator.Core.TextureSource {TextureName="atlas",Width=32,Height=32,Context="A",RuntimeAssetKey="1"};
    var b=new PvZSymbiosisTranslator.Core.TextureSource {TextureName="atlas",Width=32,Height=32,Context="B",RuntimeAssetKey="2"};
    var match=new PvZSymbiosisTranslator.Core.TextureMatch {TextureName="atlas",Width=32,Height=32};
    Reject(()=>match.Resolve(new[]{a,b}));match.Context="B";if(match.Resolve(new[]{a,b}).Single()!=1)throw new Exception("Context did not disambiguate");
    if(a.Id==b.Id)throw new Exception("Metadata IDs collide");
});
Test("texture repeated shared asset is allowed",()=>{
    var source=new PvZSymbiosisTranslator.Core.TextureSource {TextureName="atlas",Width=32,Height=32,RuntimeTextureKey="shared-texture",RuntimeAssetKey="shared"};
    var match=new PvZSymbiosisTranslator.Core.TextureMatch {TextureName="atlas",Width=32,Height=32};
    if(match.Resolve(new[]{source,source}).Count!=2)throw new Exception("Shared source rejected");
});
Test("texture-only atlas mapping allows distinct sprites on one Texture2D",()=>{
    var a=new PvZSymbiosisTranslator.Core.TextureSource {TextureName="Home",SpriteName="Options1",Width=4096,Height=8192,RuntimeTextureKey="texture:177",RuntimeAssetKey="texture:177:sprite:6237"};
    var b=new PvZSymbiosisTranslator.Core.TextureSource {TextureName="Home",SpriteName="Help1",Width=4096,Height=8192,RuntimeTextureKey="texture:177",RuntimeAssetKey="texture:177:sprite:3730"};
    var match=new PvZSymbiosisTranslator.Core.TextureMatch {TextureName="Home",Width=4096,Height=8192};
    if(match.Resolve(new[]{a,b}).Count!=2)throw new Exception("Shared atlas regions rejected");
});
Test("texture-only mapping rejects distinct Texture2D instances",()=>{
    var a=new PvZSymbiosisTranslator.Core.TextureSource {TextureName="Home",Width=4096,Height=8192,RuntimeTextureKey="texture:1",RuntimeAssetKey="texture:1:sprite:1"};
    var b=new PvZSymbiosisTranslator.Core.TextureSource {TextureName="Home",Width=4096,Height=8192,RuntimeTextureKey="texture:2",RuntimeAssetKey="texture:2:sprite:2"};
    var match=new PvZSymbiosisTranslator.Core.TextureMatch {TextureName="Home",Width=4096,Height=8192};
    Reject(()=>match.Resolve(new[]{a,b}));
});
Test("auto reload aggregates and debounces",()=>{
    using var reload=new PvZSymbiosisTranslator.Localization.Reload.AutoReloadService(TimeSpan.FromMilliseconds(750));
    var now=DateTimeOffset.Parse("2026-09-24T12:00:00Z");var path=Path.Combine(Path.GetTempPath(),"Localization","pt-BR","Strings","exact.json");
    reload.NotifyChange(path,now);reload.NotifyChange(path,now.AddMilliseconds(100));reload.NotifyChange(Path.Combine(Path.GetDirectoryName(path)!,"exact.json.tmp"),now.AddMilliseconds(200));
    if(reload.TryDequeue(now.AddMilliseconds(800),out _))throw new Exception("Debounce did not restart");
    if(!reload.TryDequeue(now.AddMilliseconds(851),out var batch)||batch.Files.Count!=1)throw new Exception("Aggregated batch missing");
    if(reload.TryDequeue(now.AddSeconds(2),out _))throw new Exception("Batch delivered twice");
});
Test("auto reload filters locale assets",()=>{
    foreach(var path in new[]{"x/Strings/exact.json","x/Almanac/exact.json","x/Fonts/manifest.json","x/Textures/manifest.json","x/Textures/Home.png","x/Audio/manifest.json","x/ModStrings.json"})if(!PvZSymbiosisTranslator.Localization.Reload.AutoReloadService.IsRelevant(path))throw new Exception(path);
    foreach(var path in new[]{"x/manifest.json","x/Strings/readme.txt","x/exact.json.tmp","x/texture.png"})if(PvZSymbiosisTranslator.Localization.Reload.AutoReloadService.IsRelevant(path))throw new Exception(path);
});
Test("translator session deduplicates and filters noise",()=>{
    var session=new PvZSymbiosisTranslator.Diagnostics.TranslatorSessionService();session.SetContext("Home");var now=DateTimeOffset.Parse("2026-09-24T12:00:00Z");
    if(!session.Observe("未知","未知","Home","Home","Label","TMP","Canvas/Label","unknown",true,now))throw new Exception("First capture missing");
    if(session.Observe("未知","未知","Home","Home","Label","TMP","Canvas/Label","unknown",true,now.AddSeconds(1)))throw new Exception("Duplicate identity added");
    session.Observe("植物","Planta","Home","Home","Plant","TMP","Canvas/Plant","exact",false,now);
    foreach(var noise in new[]{"","   ","\u200B","---","[NativeUI] trace"})session.Observe(noise,noise,"Home","Home","","","","unknown",true,now);
    var snapshot=session.Snapshot();if(snapshot.UniqueSeen!=2||snapshot.Unknown!=1||snapshot.Translated!=1||snapshot.Records.Single(x=>x.Source=="未知").SeenCount!=2)throw new Exception("Wrong session aggregation");
    if(snapshot.Records.Single(x=>x.Source=="植物").Hierarchy!="")throw new Exception("Light context leaked details");
});
Test("translator session context identity and clear",()=>{
    var session=new PvZSymbiosisTranslator.Diagnostics.TranslatorSessionService();session.SetContext("Home");session.Observe("未知","未知","Home","Home","","","","unknown",false);session.SetContext("Almanac");session.Observe("未知","未知","Almanac","IllustratedIndex","","","","unknown",false);
    var snapshot=session.Snapshot();if(snapshot.UniqueSeen!=2||snapshot.ContextHistory.Count!=2||snapshot.CurrentContext!="Almanac")throw new Exception("Context identity lost");session.Clear();if(session.Snapshot().UniqueSeen!=0)throw new Exception("Clear failed");
});
Test("translator session exports valid text JSON",()=>{
    var root=Path.Combine(Path.GetTempPath(),"pvz-session-"+Guid.NewGuid());try{var session=new PvZSymbiosisTranslator.Diagnostics.TranslatorSessionService();session.SetContext("Home");session.Observe("未知","未知","Home","Home","Label","TMP","Canvas/Label","unknown",true);var export=new PvZSymbiosisTranslator.Diagnostics.TranslatorExportService(root);var a=export.ExportUntranslated(session.Snapshot(),"pt-BR");var b=export.ExportContext(session.Snapshot(),"pt-BR","Home");var c=export.ExportSession(session.Snapshot(),"pt-BR",new{success=true});foreach(var path in new[]{a,b,c})using(JsonDocument.Parse(File.ReadAllText(path))){} }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
});
Test("QA valid pack and zero optional mappings",()=>{
    var root=QaPack("{\"植物 {0}\":\"Planta {0}\"}","[]");
    try {var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");if(qa.Errors!=0||qa.ExactTranslations!=1||qa.TextureMappings!=0||qa.AudioMappings!=0)throw new Exception($"{qa.Errors}/{qa.ExactTranslations}");}
    finally{Directory.Delete(root,true);}
});
Test("QA ignores exact markers in translation count and checks",()=>{
    var exact="{\"==================== [UI / MAIN MENU] ====================\":\"\",\"---------- [NAVIGATION] ----------\":\"\",\"植物\":\"Planta\"}";
    var root=QaPack(exact,"[]");
    try {var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");if(qa.Errors!=0||qa.ExactTranslations!=1||qa.Issues.Any(x=>x.Source.Contains("MAIN MENU",StringComparison.Ordinal)))throw new Exception($"{qa.Errors}/{qa.ExactTranslations}");}
    finally{Directory.Delete(root,true);}
});
Test("QA reports malformed exact marker",()=>{
    var root=QaPack("{\"========== [UI] ==========\":\"\"}","[]");
    try {var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");if(!qa.Issues.Any(x=>x.Check=="json"&&x.Message.Contains("Malformed exact section marker",StringComparison.Ordinal)))throw new Exception("Malformed marker not reported");}
    finally{Directory.Delete(root,true);}
});
Test("QA broken JSON reports position",()=>{
    var root=QaPack("{\"植物\":\"Planta\",}","[]");
    try {var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");var issue=qa.Issues.Single(x=>x.Check=="json"&&x.File=="Strings/exact.json");if(issue.Line is null||qa.OverallStatus!=PvZSymbiosisTranslator.QA.QaStatus.Fail)throw new Exception("Parse failure missing detail");}
    finally{Directory.Delete(root,true);}
});
Test("QA placeholders markup CJK whitespace encoding and length",()=>{
    var exact="{\"获得 {0} 阳光\":\"Receba sóis\",\"标签\":\"<b>Texto\",\"名称\":\"植物残留\",\"空格\":\" texto  \",\"编码\":\"FranÃ§a\",\"简短文本\":\"Uma tradução extremamente longa que pode ultrapassar o botão nativo\"}";
    var root=QaPack(exact,"[]");
    try {var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");foreach(var id in new[]{"placeholders","markup","cjk","whitespace","encoding","length"})if(!qa.Issues.Any(x=>x.Check==id))throw new Exception(id);}
    finally{Directory.Delete(root,true);}
});
Test("QA explicit CJK identity is informational",()=>{
    var root=QaPack("{\"Bilibili@厘子gg\":\"Bilibili@厘子gg\"}","[]");
    try {var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");var issue=qa.Issues.Single(x=>x.Check=="cjk");if(issue.Severity!=PvZSymbiosisTranslator.QA.QaSeverity.Info||qa.Errors!=0)throw new Exception("Identity was rejected");}
    finally{Directory.Delete(root,true);}
});
Test("QA invalid and broad dynamic rules",()=>{
    var dynamic="[{\"id\":\"bad\",\"pattern\":\"([\",\"target\":\"x\"},{\"id\":\"broad\",\"pattern\":\"^.*$\",\"target\":\"x\"}]";
    var root=QaPack("{}",dynamic);
    try {var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");if(!qa.Issues.Any(x=>x.Check=="dynamic"&&x.Severity==PvZSymbiosisTranslator.QA.QaSeverity.Error)||!qa.Issues.Any(x=>x.Check=="dynamic"&&x.Severity==PvZSymbiosisTranslator.QA.QaSeverity.Warning))throw new Exception("Dynamic findings missing");}
    finally{Directory.Delete(root,true);}
});
Test("QA texture path and PNG validation",()=>{
    var root=QaPack("{}","[]");
    try {
        File.WriteAllBytes(Path.Combine(root,"Textures","ok.png"),PvZSymbiosisTranslator.Core.PngCodec.Encode(2,2,new byte[16]));
        File.WriteAllText(Path.Combine(root,"Textures","manifest.json"),"[{\"id\":\"ok\",\"replacement\":\"ok.png\"}]");
        var valid=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");if(valid.Issues.Any(x=>x.Check=="textures")||valid.TextureMappings!=1)throw new Exception("Valid texture rejected");
        File.WriteAllText(Path.Combine(root,"Textures","manifest.json"),"[{\"id\":\"bad\",\"replacement\":\"../escape.png\"}]");
        var invalid=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");if(!invalid.Issues.Any(x=>x.Check=="textures"&&x.Severity==PvZSymbiosisTranslator.QA.QaSeverity.Error))throw new Exception("Escape accepted");
    } finally{Directory.Delete(root,true);}
});
Test("QA rejects truncated and duplicate automatic textures",()=>{
    var root=QaPack("{}","[]");
    try {
        Directory.CreateDirectory(Path.Combine(root,"Textures","A"));Directory.CreateDirectory(Path.Combine(root,"Textures","B"));
        var png=PvZSymbiosisTranslator.Core.PngCodec.Encode(2,2,new byte[16]);
        File.WriteAllBytes(Path.Combine(root,"Textures","A","Home.png"),png);File.WriteAllBytes(Path.Combine(root,"Textures","B","Home.png"),png);
        File.WriteAllBytes(Path.Combine(root,"Textures","Broken.png"),new byte[]{137,80,78,71,13,10,26,10});
        var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");
        if(!qa.Issues.Any(x=>x.Check=="textures"&&x.Message.Contains("Duplicate automatic texture identity",StringComparison.Ordinal)))throw new Exception("Duplicate auto identity accepted");
        if(!qa.Issues.Any(x=>x.Check=="textures"&&x.Message.Contains("PNG decode failed",StringComparison.Ordinal)))throw new Exception("Truncated PNG accepted");
    } finally{Directory.Delete(root,true);}
});
Test("QA reports explicit texture identity conflicts and unsupported files",()=>{
    var root=QaPack("{}","[]");
    try {
        var png=PvZSymbiosisTranslator.Core.PngCodec.Encode(2,2,new byte[16]);File.WriteAllBytes(Path.Combine(root,"Textures","one.png"),png);File.WriteAllBytes(Path.Combine(root,"Textures","two.png"),png);File.WriteAllText(Path.Combine(root,"Textures","notes.txt"),"x");
        File.WriteAllText(Path.Combine(root,"Textures","manifest.json"),"[{\"id\":\"one\",\"match\":{\"textureName\":\"Home\",\"width\":2,\"height\":2},\"replacement\":\"one.png\"},{\"id\":\"two\",\"match\":{\"textureName\":\"Home\",\"width\":2,\"height\":2},\"replacement\":\"two.png\"}]");
        var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");
        if(!qa.Issues.Any(x=>x.Message.Contains("Duplicate explicit texture source identity",StringComparison.Ordinal)))throw new Exception("Explicit conflict accepted");
        if(!qa.Issues.Any(x=>x.Message.Contains("Unsupported file in texture pack",StringComparison.Ordinal)))throw new Exception("Unsupported file hidden");
    } finally{Directory.Delete(root,true);}
});
Test("QA counts automatic texture PNG without manifest entry",()=>{
    var root=QaPack("{}","[]");
    try {
        var png=PvZSymbiosisTranslator.Core.PngCodec.Encode(2,2,new byte[16]);
        File.WriteAllBytes(Path.Combine(root,"Textures","Home.png"),png);
        var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");
        if(qa.Issues.Any(x=>x.Check=="textures")||qa.TextureMappings!=1)throw new Exception("Automatic texture not counted");
    } finally{Directory.Delete(root,true);}
});
Test("QA current pt-BR pack has no technical failures",()=>{
    var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","PvZ_Symbiosis_Translator","Localization","pt-BR"));
    var qa=new PvZSymbiosisTranslator.QA.LocalizationQaService().RunFullScan(root,"pt-BR","1.2.0");
    if(qa.Errors!=0)throw new Exception(string.Join(" | ",qa.Issues.Where(x=>x.Severity==PvZSymbiosisTranslator.QA.QaSeverity.Error).Take(5).Select(x=>$"{x.Check}:{x.File}:{x.Message}")));
    if(qa.ExactTranslations<1700||qa.DynamicRules!=3)throw new Exception($"Unexpected counts {qa.ExactTranslations}/{qa.DynamicRules}");
});
Test("QA report writer emits valid JSON and readable text",()=>{
    var root=Path.Combine(Path.GetTempPath(),"pvz-qa-report-"+Guid.NewGuid());
    try {var snapshot=new PvZSymbiosisTranslator.QA.QaSnapshot {Timestamp=DateTimeOffset.Parse("2026-09-24T12:00:00Z"),Locale="pt-BR",GameVersion="1.2.0",Checks=new[]{new PvZSymbiosisTranslator.QA.QaCheckResult{Id="json",Status=PvZSymbiosisTranslator.QA.QaStatus.Pass,Summary="No findings"}},Issues=Array.Empty<PvZSymbiosisTranslator.QA.QaIssue>()};var result=new PvZSymbiosisTranslator.QA.QaReportWriter(root).Write(snapshot,"1.1.0");using(JsonDocument.Parse(File.ReadAllText(result.JsonPath))){}if(!File.ReadAllText(result.TextPath).Contains("Status: Pass",StringComparison.Ordinal))throw new Exception("Text report incomplete");}
    finally{if(Directory.Exists(root))Directory.Delete(root,true);}
});
Test("translation classification distinguishes exact dynamic context preserved and unknown",()=>{
    var pack=new LanguagePack {Locale="pt-BR"};pack.Exact.Add("植物","Planta");pack.Exact.Add("Bilibili@厘子gg","Bilibili@厘子gg");pack.Dynamic.Add("seconds","^(?<value>\\d+)秒$","${value} s");var context=new ExactStringStore();context.Add("确定","Confirmar");pack.Contexts.Add("Menu",context);var service=new TranslationService(pack);
    if(service.Classify("植物").Classification!=TranslationClassification.ExactTranslated)throw new Exception("exact");
    if(service.Classify("20秒").Classification!=TranslationClassification.DynamicTranslated)throw new Exception("dynamic");
    if(service.Classify("确定","Menu").Classification!=TranslationClassification.ContextTranslated)throw new Exception("context");
    if(service.Classify("Bilibili@厘子gg").Classification!=TranslationClassification.Preserved)throw new Exception("preserved");
    if(service.Classify("已知",knownRuntimeSource:true).Classification!=TranslationClassification.KnownUntranslated)throw new Exception("known");
    if(service.Classify("未知").Classification!=TranslationClassification.Unknown)throw new Exception("unknown");
});
Test("rendered target is identified and preserved entry is never unknown",()=>{
    var pack=new LanguagePack {Locale="pt-BR"};pack.Exact.Add("植物","Planta");pack.Exact.Add("作者厘子","作者厘子");var service=new TranslationService(pack);
    if(service.Classify("Planta").Classification!=TranslationClassification.RenderedTarget||!service.IsRenderedTargetOnly("Planta"))throw new Exception("Rendered target contamination not detected");
    var session=new PvZSymbiosisTranslator.Diagnostics.TranslatorSessionService();session.Observe("作者厘子","作者厘子","Home","Home","","","","preserved",false);var snapshot=session.Snapshot();if(snapshot.Preserved!=1||snapshot.Unknown!=0||snapshot.KnownUntranslated!=0)throw new Exception("Preserved classification regressed");
});
Test("diagnostics active watcher and zero Canvas are healthy",()=>{
    var input=HealthyDiagnostics();input.WatcherConfigured=true;input.WatcherActive=true;input.WatchedFiles=8;var snapshot=new PvZSymbiosisTranslator.Diagnostics.DiagnosticsService().Capture(input);
    if(snapshot.Health.Single(x=>x.Id=="watcher").Status!=PvZSymbiosisTranslator.Diagnostics.RuntimeHealthStatus.Pass||snapshot.Health.Single(x=>x.Id=="translatorCanvas").Status!=PvZSymbiosisTranslator.Diagnostics.RuntimeHealthStatus.Pass)throw new Exception("Healthy runtime rejected");
});
Test("diagnostics inactive configured watcher and extra Canvas fail",()=>{
    var input=HealthyDiagnostics();input.WatcherConfigured=true;input.WatcherActive=false;input.TranslatorCanvasCount=1;var snapshot=new PvZSymbiosisTranslator.Diagnostics.DiagnosticsService().Capture(input);
    if(snapshot.Health.Single(x=>x.Id=="watcher").Status!=PvZSymbiosisTranslator.Diagnostics.RuntimeHealthStatus.Fail||snapshot.Health.Single(x=>x.Id=="translatorCanvas").Status!=PvZSymbiosisTranslator.Diagnostics.RuntimeHealthStatus.Fail||snapshot.Failures<2)throw new Exception("Runtime failure hidden");
});
Test("diagnostics optional zero mappings are not failures",()=>{
    var input=HealthyDiagnostics();input.TextureEnabled=false;input.AudioEnabled=true;input.AudioMappings=0;input.LastReloadResult="PASS";input.LastReloadDurationMilliseconds=77;var snapshot=new PvZSymbiosisTranslator.Diagnostics.DiagnosticsService().Capture(input);
    if(snapshot.Health.Single(x=>x.Id=="textures").Status!=PvZSymbiosisTranslator.Diagnostics.RuntimeHealthStatus.NotApplicable||snapshot.Health.Single(x=>x.Id=="audio").Status!=PvZSymbiosisTranslator.Diagnostics.RuntimeHealthStatus.Pass||snapshot.State.ActiveLanguage!="pt-BR"||snapshot.State.LastReloadDurationMilliseconds!=77)throw new Exception("Optional or reload state wrong");
});
Test("diagnostics report writer emits JSON and text",()=>{
    var root=Path.Combine(Path.GetTempPath(),"pvz-diagnostics-"+Guid.NewGuid());try{var snapshot=new PvZSymbiosisTranslator.Diagnostics.DiagnosticsService().Capture(HealthyDiagnostics());var result=new PvZSymbiosisTranslator.Diagnostics.DiagnosticsReportWriter(root).Write(snapshot);using(JsonDocument.Parse(File.ReadAllText(result.JsonPath))){}if(!File.ReadAllText(result.TextPath).Contains("Locale: pt-BR",StringComparison.Ordinal))throw new Exception("Text report incomplete");}finally{if(Directory.Exists(root))Directory.Delete(root,true);}
});
Test("QA and Diagnostics expose only implemented native actions",()=>{
    var qa=PvZSymbiosisTranslator.UI.NativeSettingsMenuModel.Actions(PvZSymbiosisTranslator.UI.NativeSettingsPage.Qa);var diagnostics=PvZSymbiosisTranslator.UI.NativeSettingsMenuModel.Actions(PvZSymbiosisTranslator.UI.NativeSettingsPage.Diagnostics);var translator=PvZSymbiosisTranslator.UI.NativeSettingsMenuModel.Actions(PvZSymbiosisTranslator.UI.NativeSettingsPage.Translator);
    if(qa.Count!=5||diagnostics.Count!=6||!qa.Contains(PvZSymbiosisTranslator.UI.NativeSettingsAction.RunQa)||!diagnostics.Contains(PvZSymbiosisTranslator.UI.NativeSettingsAction.RefreshDiagnostics))throw new Exception("Page actions incomplete");
    foreach(var page in new[]{PvZSymbiosisTranslator.UI.NativeSettingsPage.General,PvZSymbiosisTranslator.UI.NativeSettingsPage.Qa,PvZSymbiosisTranslator.UI.NativeSettingsPage.Diagnostics,PvZSymbiosisTranslator.UI.NativeSettingsPage.Translator})if(!PvZSymbiosisTranslator.UI.NativeSettingsMenuModel.Pages.Contains(page))throw new Exception("Page switch unavailable: "+page);
});
Console.WriteLine($"TOTAL: {passed} PASS, {failed} FAIL");
return failed == 0 ? 0 : 1;

static PvZSymbiosisTranslator.Diagnostics.DiagnosticsInput HealthyDiagnostics()=>new(){ModVersion="1.1.0",GameVersion="1.2.0",UnityVersion="6000.0.41f1",MelonVersion="0.7.3",ActiveLanguage="pt-BR",Scene="Home",Context="Home",Il2Cpp=true,TranslatorEnabled=true,CustomFontEnabled=true,TranslationStoreReady=true,CollectorReady=true,FontReady=true,HarmonyReady=true,ExportDirectoryReady=true,LanguageDirectoryReady=true,ExactEntries=1735,DynamicRules=3,SettingsActive=true,SettingsRootCount=1,LauncherCount=1,TranslatorCanvasCount=0};

static string QaPack(string exact,string dynamic)
{
    var root=Path.Combine(Path.GetTempPath(),"pvz-qa-"+Guid.NewGuid());
    foreach(var folder in new[]{"Strings","Almanac","Fonts","Textures","Audio"})Directory.CreateDirectory(Path.Combine(root,folder));
    File.WriteAllText(Path.Combine(root,"manifest.json"),"{\"locale\":\"pt-BR\",\"displayName\":\"Português Brasileiro\",\"sourceLocale\":\"zh-CN\",\"version\":\"1.0.0\",\"gameVersion\":\"1.2.0\"}");
    File.WriteAllText(Path.Combine(root,"glossary.json"),"{}");File.WriteAllText(Path.Combine(root,"ModStrings.json"),"{}");
    File.WriteAllText(Path.Combine(root,"Strings","exact.json"),exact);File.WriteAllText(Path.Combine(root,"Strings","context_overrides.json"),"{}");File.WriteAllText(Path.Combine(root,"Strings","dynamic_rules.json"),dynamic);
    File.WriteAllText(Path.Combine(root,"Almanac","exact.json"),"{}");File.WriteAllText(Path.Combine(root,"Fonts","manifest.json"),"[]");File.WriteAllText(Path.Combine(root,"Textures","manifest.json"),"[]");File.WriteAllText(Path.Combine(root,"Audio","manifest.json"),"[]");
    return root;
}
