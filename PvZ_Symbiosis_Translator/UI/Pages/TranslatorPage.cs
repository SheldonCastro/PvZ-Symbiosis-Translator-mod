using System;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PvZSymbiosisTranslator.UI.Pages;

internal enum TranslatorSetting { Mode, Debug, AutoReload, CaptureUnknown, AutoExport, DetailedContext }

internal sealed class TranslatorPageState
{
    public bool Mode,Debug,AutoReload,CaptureUnknown,AutoExport,DetailedContext;
    public string Context="Unknown",Scene="—",LastChange="—",LastReload="—",Watcher="—";
    public int UniqueSeen,Translated,Preserved,KnownUntranslated,RuntimeUnknown,SessionUnknown,WatchedFiles;
}

internal sealed class TranslatorPageBindings
{
    public Func<TranslatorPageState> State;
    public Action<TranslatorSetting> Toggle;
    public Action ReloadNow,RefreshScreen,ClearCapture,CopySummary;
    public Action ExportUntranslated,ExportCurrentScreen,ExportSession,OpenExports;
}

internal static class TranslatorPage
{
    private const string Prefix="PvZTranslator.Native.Page.Translator.";
    private const string SessionInfo=Prefix+"SessionInfo";
    private static string ToggleName(TranslatorSetting setting)=>Prefix+setting;

    public static bool Build(GameObject pageRoot,GameObject textTemplate,NativeAssetSet assets,string locale,TranslatorPageBindings bindings)
    {
        if(pageRoot==null||textTemplate==null||assets?.CheckboxTemplate==null||assets.SmallButtonTemplate==null||assets.BigButtonTemplate==null||bindings==null)return false;
        var title=FindFirstText(pageRoot);if(title!=null)title.text="";
        Label(pageRoot,textTemplate,"Label.translator.settings","translator.settings",locale,-440f,-12f,520f,42f,25f,true);
        var y=-62f;
        foreach(var row in new[]{
            (TranslatorSetting.Mode,"translator.mode"),(TranslatorSetting.Debug,"translator.debug"),(TranslatorSetting.AutoReload,"translator.autoReload"),
            (TranslatorSetting.CaptureUnknown,"translator.captureUnknown"),
            (TranslatorSetting.AutoExport,"translator.autoExport"),(TranslatorSetting.DetailedContext,"translator.detailedContext")}) {
            Label(pageRoot,textTemplate,"Label."+row.Item2,row.Item2,locale,-405f,y,440f,38f,21f);
            Action action=()=>bindings.Toggle?.Invoke(row.Item1);UnityAction click=action;
            var toggle=NativeUiFactory.CloneNativeImageButton(assets.CheckboxTemplate,pageRoot.transform,ToggleName(row.Item1),click);if(toggle==null)return false;
            Place(toggle.Root,-175f,y,46f,43f);y-=47f;
        }
        Label(pageRoot,textTemplate,"Label.translator.actions","translator.actions",locale,-440f,-405f,520f,40f,24f,true);
        if(!Small(pageRoot,assets,Prefix+"ReloadNow","translator.reloadNow",locale,-375f,-452f,bindings.ReloadNow))return false;
        if(!Small(pageRoot,assets,Prefix+"RefreshScreen","translator.refreshCurrentScreen",locale,-115f,-452f,bindings.RefreshScreen))return false;
        if(!Small(pageRoot,assets,Prefix+"ClearCapture","translator.clearCapture",locale,-375f,-510f,bindings.ClearCapture))return false;
        if(!Small(pageRoot,assets,Prefix+"CopySummary","translator.copySessionSummary",locale,-115f,-510f,bindings.CopySummary))return false;

        Label(pageRoot,textTemplate,"Label.translator.session","translator.session",locale,145f,-12f,520f,42f,25f,true);
        var info=Label(pageRoot,textTemplate,SessionInfo,null,locale,260f,-184f,620f,290f,20f);if(info!=null){info.alignment=TextAlignmentOptions.TopLeft;info.lineSpacing=3f;}
        Label(pageRoot,textTemplate,"Label.translator.exports","translator.exports",locale,145f,-320f,520f,40f,24f,true);
        if(!Big(pageRoot,assets,Prefix+"ExportUntranslated","translator.exportUntranslated",locale,330f,-370f,bindings.ExportUntranslated))return false;
        if(!Small(pageRoot,assets,Prefix+"ExportCurrent","translator.exportCurrentScreen",locale,200f,-432f,bindings.ExportCurrentScreen))return false;
        if(!Small(pageRoot,assets,Prefix+"ExportSession","translator.exportSession",locale,460f,-432f,bindings.ExportSession))return false;
        if(!Small(pageRoot,assets,Prefix+"OpenExports","translator.openExports",locale,330f,-490f,bindings.OpenExports))return false;
        Refresh(pageRoot,locale,assets,bindings);return true;
    }

    public static void Refresh(GameObject root,string locale,NativeAssetSet assets,TranslatorPageBindings bindings)
    {
        if(root==null||bindings==null)return;var s=bindings.State?.Invoke()??new TranslatorPageState();
        SetToggle(root,assets,TranslatorSetting.Mode,s.Mode,true);SetToggle(root,assets,TranslatorSetting.Debug,s.Debug,true);SetToggle(root,assets,TranslatorSetting.AutoReload,s.AutoReload,true);
        SetToggle(root,assets,TranslatorSetting.CaptureUnknown,s.CaptureUnknown,s.Mode);SetToggle(root,assets,TranslatorSetting.AutoExport,s.AutoExport,s.Mode);SetToggle(root,assets,TranslatorSetting.DetailedContext,s.DetailedContext,s.Mode);
        SetText(root,SessionInfo,
            $"{ModLabels.Get(locale,"translator.currentContext")}: {s.Context}\n{ModLabels.Get(locale,"translator.scene")}: {s.Scene}\n"+
            $"{ModLabels.Get(locale,"translator.uniqueSeen")}: {s.UniqueSeen}    •    {ModLabels.Get(locale,"translator.runtimeTranslated")}: {s.Translated}\n"+
            $"{ModLabels.Get(locale,"translator.preserved")}: {s.Preserved}    •    {ModLabels.Get(locale,"translator.knownUntranslated")}: {s.KnownUntranslated}\n"+
            $"{ModLabels.Get(locale,"translator.runtimeUnknown")}: {s.RuntimeUnknown}    •    {ModLabels.Get(locale,"translator.sessionUnknown")}: {s.SessionUnknown}\n"+
            $"{ModLabels.Get(locale,"translator.watchedFiles")}: {s.WatchedFiles}\n"+
            $"{ModLabels.Get(locale,"translator.lastChange")}: {s.LastChange}\n{ModLabels.Get(locale,"translator.lastAutoReload")}: {s.LastReload}\n"+
            $"{ModLabels.Get(locale,"translator.watcher")}: {s.Watcher}\n{ModLabels.Get(locale,"translator.shortcutsInfo")}");
        Relabel(root,locale);
    }

    private static void SetToggle(GameObject root,NativeAssetSet assets,TranslatorSetting setting,bool enabled,bool available)
    {
        var item=FindDirect(root.transform,ToggleName(setting));var image=item?.GetComponent<Image>();var button=item?.GetComponent<Button>();
        if(image!=null){image.sprite=assets.Get(enabled?NativeAssetIds.CheckboxOn:NativeAssetIds.CheckboxOff);image.color=available?Color.white:new Color(.55f,.55f,.55f,.72f);}
        if(button!=null)button.interactable=available;
    }
    private static bool Big(GameObject root,NativeAssetSet assets,string name,string key,string locale,float x,float y,Action callback)=>Button(root,assets.BigButtonTemplate,name,key,locale,x,y,390f,62f,callback,17f,25f);
    private static bool Small(GameObject root,NativeAssetSet assets,string name,string key,string locale,float x,float y,Action callback)=>Button(root,assets.SmallButtonTemplate,name,key,locale,x,y,240f,50f,callback,13f,19f);
    private static bool Button(GameObject root,GameObject template,string name,string key,string locale,float x,float y,float width,float height,Action callback,float min,float max)
    {
        UnityAction click=callback;var b=NativeUiFactory.CloneNativeButton(template,root.transform,name,click);if(b==null)return false;Place(b.Root,x,y,width,height);var rect=b.Label.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(width-18f,height-8f);rect.localScale=Vector3.one;b.Label.enableAutoSizing=true;b.Label.fontSizeMin=min;b.Label.fontSizeMax=max;b.Label.fontSize=max;b.Label.color=new Color(.96f,.88f,.68f,1f);b.Label.raycastTarget=false;b.Label.text=ModLabels.Get(locale,key);return true;
    }
    private static TMP_Text Label(GameObject root,GameObject template,string name,string key,string locale,float x,float y,float width,float height,float size,bool heading=false)
    {
        var clone=UnityEngine.Object.Instantiate<GameObject>(template,root.transform,false);clone.name=name;var text=FindFirstText(clone);if(text!=null){var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;text.enableAutoSizing=true;text.fontSizeMin=heading?17f:12f;text.fontSizeMax=size;text.fontSize=size;text.alignment=TextAlignmentOptions.Left;text.color=new Color(.22f,.105f,.035f,1f);text.raycastTarget=false;text.text=key==null?"":ModLabels.Get(locale,key);}clone.SetActive(true);return text;
    }
    private static void Relabel(GameObject root,string locale){for(var i=0;i<root.transform.childCount;i++){var child=root.transform.GetChild(i)?.gameObject;if(child==null||!child.name.StartsWith("Label.",StringComparison.Ordinal))continue;var text=FindFirstText(child);if(text!=null)text.text=ModLabels.Get(locale,child.name.Substring(6));}}
    private static void SetText(GameObject root,string name,string value){var text=FindFirstText(FindDirect(root.transform,name));if(text!=null)text.text=value;}
    private static void Place(GameObject target,float x,float y,float width,float height){var rect=target.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;target.SetActive(true);}
    private static GameObject FindDirect(Transform parent,string name){if(parent==null)return null;for(var i=0;i<parent.childCount;i++){var child=parent.GetChild(i);if(child!=null&&string.Equals(child.gameObject.name,name,StringComparison.Ordinal))return child.gameObject;}return null;}
    private static TMP_Text FindFirstText(GameObject root){if(root==null)return null;var stack=new System.Collections.Generic.Stack<Transform>();stack.Push(root.transform);while(stack.Count>0){var node=stack.Pop();if(node==null)continue;var text=node.gameObject.GetComponent(Il2CppType.Of<TMP_Text>())?.TryCast<TMP_Text>();if(text!=null)return text;for(var i=node.childCount-1;i>=0;i--)stack.Push(node.GetChild(i));}return null;}
}
