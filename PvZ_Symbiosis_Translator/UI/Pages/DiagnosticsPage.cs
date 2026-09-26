using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using PvZSymbiosisTranslator.Diagnostics;
using UnityEngine;
using UnityEngine.Events;

namespace PvZSymbiosisTranslator.UI.Pages;

internal sealed class DiagnosticsPageBindings
{
    public Func<DiagnosticsSnapshot> State;
    public Action Refresh,Copy,Export,OpenLogs,OpenMod,OpenExports;
}

internal static class DiagnosticsPage
{
    private const string Prefix="PvZTranslator.Native.Page.Diagnostics.";
    private const string Runtime=Prefix+"Runtime",Health=Prefix+"Health",Live=Prefix+"Live",Ui=Prefix+"Ui";
    public static bool Build(GameObject root,GameObject textTemplate,NativeAssetSet assets,string locale,DiagnosticsPageBindings bindings)
    {
        if(root==null||textTemplate==null||assets?.SmallButtonTemplate==null||bindings==null)return false;var old=FindText(root);if(old!=null)old.text="";
        Label(root,textTemplate,"Label.diagnostics.runtime","diagnostics.runtime",locale,-440f,-12f,520f,42f,25f,true);Info(Label(root,textTemplate,Runtime,null,locale,-420f,-112f,550f,165f,17f));
        Label(root,textTemplate,"Label.diagnostics.health","diagnostics.health",locale,-440f,-205f,520f,42f,25f,true);Info(Label(root,textTemplate,Health,null,locale,-420f,-365f,550f,280f,16f));
        Label(root,textTemplate,"Label.diagnostics.live","diagnostics.live",locale,150f,-12f,520f,42f,25f,true);Info(Label(root,textTemplate,Live,null,locale,280f,-118f,600f,175f,17f));
        Label(root,textTemplate,"Label.diagnostics.ui","diagnostics.ui",locale,150f,-214f,520f,42f,25f,true);Info(Label(root,textTemplate,Ui,null,locale,280f,-273f,600f,82f,17f));
        Label(root,textTemplate,"Label.diagnostics.actions","diagnostics.actions",locale,150f,-338f,520f,42f,25f,true);
        if(!Button(root,assets,Prefix+"Refresh","diagnostics.refresh",locale,195f,-390f,bindings.Refresh))return false;
        if(!Button(root,assets,Prefix+"Copy","diagnostics.copy",locale,465f,-390f,bindings.Copy))return false;
        if(!Button(root,assets,Prefix+"Export","diagnostics.export",locale,195f,-450f,bindings.Export))return false;
        if(!Button(root,assets,Prefix+"Logs","diagnostics.openLogs",locale,465f,-450f,bindings.OpenLogs))return false;
        if(!Button(root,assets,Prefix+"Mod","diagnostics.openMod",locale,195f,-510f,bindings.OpenMod))return false;
        if(!Button(root,assets,Prefix+"Exports","diagnostics.openExports",locale,465f,-510f,bindings.OpenExports))return false;
        RefreshPage(root,locale,bindings);return true;
    }
    public static void RefreshPage(GameObject root,string locale,DiagnosticsPageBindings bindings)
    {
        if(root==null||bindings==null)return;var s=bindings.State?.Invoke();if(s==null){Set(root,Runtime,ModLabels.Get(locale,"diagnostics.unavailable"));Set(root,Health,"—");Set(root,Live,"—");Set(root,Ui,"—");Relabel(root,locale);return;}var x=s.State;
        Set(root,Runtime,$"{ModLabels.Get(locale,"diagnostics.mod")}: {x.ModVersion}    •    {ModLabels.Get(locale,"diagnostics.game")}: {x.GameVersion}\nUnity: {x.UnityVersion}    •    MelonLoader: {x.MelonVersion}\nIL2CPP: {(x.Il2Cpp?"PASS":"FAIL")}    •    {ModLabels.Get(locale,"diagnostics.language")}: {x.ActiveLanguage}\n{ModLabels.Get(locale,"diagnostics.scene")}: {x.Scene}    •    {ModLabels.Get(locale,"diagnostics.context")}: {x.Context}\n{ModLabels.Get(locale,"diagnostics.features")}: T:{On(x.TranslatorEnabled)} F:{On(x.CustomFontEnabled)} X:{On(x.TextureEnabled)} A:{On(x.AudioEnabled)}");
        Set(root,Health,string.Join("\n",s.Health.Take(14).Select(h=>$"{Status(locale,h.Status),-6}  {ModLabels.Get(locale,"diagnostics.check."+h.Id)}: {Clip(h.Detail,36)}")));
        Set(root,Live,$"{ModLabels.Get(locale,"diagnostics.observed")}: {x.StringsObserved}    •    {ModLabels.Get(locale,"diagnostics.translated")}: {x.RuntimeTranslations}\n{ModLabels.Get(locale,"diagnostics.preserved")}: {x.Preserved}    •    {ModLabels.Get(locale,"diagnostics.unknown")}: {x.Unknown}\n{ModLabels.Get(locale,"diagnostics.watched")}: {x.WatchedFiles}    •    {ModLabels.Get(locale,"diagnostics.lastChange")}: {Time(x.LastFileChange)}\n{ModLabels.Get(locale,"diagnostics.autoReload")}: {Time(x.LastAutoReload)}    •    {ModLabels.Get(locale,"diagnostics.manualReload")}: {Time(x.LastManualReload)}\n{ModLabels.Get(locale,"diagnostics.reload")}: {x.LastReloadResult} / {x.LastReloadDurationMilliseconds} ms\n{ModLabels.Get(locale,"diagnostics.lastError")}: {Clip(string.IsNullOrWhiteSpace(x.LastReloadError)?"—":x.LastReloadError,64)}");
        Set(root,Ui,$"{ModLabels.Get(locale,"diagnostics.canvas")}: {x.TranslatorCanvasCount}    •    {ModLabels.Get(locale,"diagnostics.settings")}: {(x.SettingsActive?"ON":"OFF")}    •    {ModLabels.Get(locale,"diagnostics.tab")}: {x.CurrentTab}\n{ModLabels.Get(locale,"diagnostics.launchers")}: {x.LauncherCount}    •    {ModLabels.Get(locale,"diagnostics.roots")}: {x.SettingsRootCount}    •    {Status(locale,s.OverallStatus)}");Relabel(root,locale);
    }
    private static string On(bool value)=>value?"ON":"OFF";
    private static string Time(DateTimeOffset? value)=>value?.ToLocalTime().ToString("HH:mm:ss")??"—";
    private static string Status(string locale,RuntimeHealthStatus value)=>ModLabels.Get(locale,"qa.status."+(value switch{RuntimeHealthStatus.Pass=>"pass",RuntimeHealthStatus.Warning=>"warn",RuntimeHealthStatus.Fail=>"fail",RuntimeHealthStatus.NotApplicable=>"na",_=>"info"}));
    private static string Clip(string value,int max)=>string.IsNullOrEmpty(value)||value.Length<=max?value:value.Substring(0,max)+"…";
    private static void Info(TMP_Text text){if(text!=null){text.alignment=TextAlignmentOptions.TopLeft;text.lineSpacing=2f;}}
    private static bool Button(GameObject root,NativeAssetSet assets,string name,string key,string locale,float x,float y,Action action){UnityAction click=action;var b=NativeUiFactory.CloneNativeButton(assets.SmallButtonTemplate,root.transform,name,click);if(b==null)return false;Place(b.Root,x,y,245f,50f);var rect=b.Label.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(225f,42f);b.Label.enableAutoSizing=true;b.Label.fontSizeMin=13f;b.Label.fontSizeMax=19f;b.Label.fontSize=19f;b.Label.color=new Color(.96f,.88f,.68f,1f);b.Label.text=ModLabels.Get(locale,key);return true;}
    private static TMP_Text Label(GameObject root,GameObject template,string name,string key,string locale,float x,float y,float width,float height,float size,bool heading=false){var clone=UnityEngine.Object.Instantiate<GameObject>(template,root.transform,false);clone.name=name;var text=FindText(clone);if(text!=null){var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;text.enableAutoSizing=true;text.fontSizeMin=heading?17f:11f;text.fontSizeMax=size;text.fontSize=size;text.alignment=TextAlignmentOptions.Left;text.color=new Color(.22f,.105f,.035f,1f);text.raycastTarget=false;text.text=key==null?"":ModLabels.Get(locale,key);}clone.SetActive(true);return text;}
    private static void Relabel(GameObject root,string locale){for(var i=0;i<root.transform.childCount;i++){var child=root.transform.GetChild(i)?.gameObject;if(child==null||!child.name.StartsWith("Label.",StringComparison.Ordinal))continue;var text=FindText(child);if(text!=null)text.text=ModLabels.Get(locale,child.name.Substring(6));}}
    private static void Set(GameObject root,string name,string value){var text=FindText(Find(root.transform,name));if(text!=null)text.text=value;}
    private static void Place(GameObject target,float x,float y,float width,float height){var rect=target.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;target.SetActive(true);}
    private static GameObject Find(Transform parent,string name){if(parent==null)return null;for(var i=0;i<parent.childCount;i++){var child=parent.GetChild(i);if(child!=null&&child.gameObject.name==name)return child.gameObject;}return null;}
    private static TMP_Text FindText(GameObject root){if(root==null)return null;var stack=new Stack<Transform>();stack.Push(root.transform);while(stack.Count>0){var node=stack.Pop();var text=node.gameObject.GetComponent(Il2CppType.Of<TMP_Text>())?.TryCast<TMP_Text>();if(text!=null)return text;for(var i=node.childCount-1;i>=0;i--)stack.Push(node.GetChild(i));}return null;}
}
