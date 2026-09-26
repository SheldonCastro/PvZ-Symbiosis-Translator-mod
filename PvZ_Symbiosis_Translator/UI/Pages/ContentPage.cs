using System;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;

namespace PvZSymbiosisTranslator.UI.Pages;

internal sealed class ContentPageState
{
    public string ActiveLanguage="—";
    public string LocaleFolder="—";
    public string PrimaryFont="—";
    public string FallbackFonts="—";
    public int Exact;
    public int Dynamic;
    public int TextureMappings;
    public int AudioMappings;
    public int InstalledLocales;
    public bool ReloadAttempted;
    public bool ReloadSucceeded;
    public string ReloadTime="—";
    public long ReloadDurationMilliseconds;
    public int ReloadFiles;
    public string ReloadError="";
}

internal sealed class ContentPageBindings
{
    public Func<ContentPageState> State;
    public Action ReloadAll;
    public Action ReloadTexts;
    public Action ReloadFonts;
    public Action ReloadTextures;
    public Action ReloadAudio;
    public Action OpenLocale;
    public Action OpenMod;
    public Action OpenExports;
    public Action OpenTextures;
    public Action OpenAudio;
}

internal static class ContentPage
{
    private const string Prefix="PvZTranslator.Native.Page.Content.";
    private const string PackInfo=Prefix+"PackInfo";
    private const string ReloadInfo=Prefix+"ReloadInfo";

    public static bool Build(GameObject pageRoot,GameObject textTemplate,NativeAssetSet assets,string locale,ContentPageBindings bindings)
    {
        if(pageRoot==null || textTemplate==null || assets?.SmallButtonTemplate==null || assets.BigButtonTemplate==null || bindings==null)return false;
        var pageTitle=FindFirstText(pageRoot);if(pageTitle!=null)pageTitle.text="";

        Label(pageRoot,textTemplate,"Label.content.activePackSection","content.activePackSection",locale,-440f,-15f,500f,42f,25f,true);
        var packInfo=Label(pageRoot,textTemplate,PackInfo,null,locale,-430f,-150f,540f,220f,20f);
        ConfigureInfo(packInfo,2f);
        Label(pageRoot,textTemplate,"Label.content.foldersSection","content.foldersSection",locale,-440f,-286f,500f,42f,25f,true);
        if(!SmallButton(pageRoot,assets,Prefix+"OpenLocale","actions.openLocaleFolder",locale,-410f,-344f,bindings.OpenLocale))return false;
        if(!SmallButton(pageRoot,assets,Prefix+"OpenMod","actions.openModFolder",locale,-120f,-344f,bindings.OpenMod))return false;
        if(!SmallButton(pageRoot,assets,Prefix+"OpenExports","actions.openExportFolder",locale,-410f,-406f,bindings.OpenExports))return false;
        if(!SmallButton(pageRoot,assets,Prefix+"OpenTextures","actions.openTexturesFolder",locale,-120f,-406f,bindings.OpenTextures))return false;
        if(!SmallButton(pageRoot,assets,Prefix+"OpenAudio","actions.openAudioFolder",locale,-265f,-468f,bindings.OpenAudio))return false;

        Label(pageRoot,textTemplate,"Label.content.reloadSection","content.reloadSection",locale,215f,-15f,500f,42f,25f,true);
        if(!BigButton(pageRoot,assets,Prefix+"ReloadAll","actions.reloadAll",locale,330f,-78f,bindings.ReloadAll))return false;
        if(!SmallButton(pageRoot,assets,Prefix+"ReloadTexts","actions.reloadTexts",locale,185f,-157f,bindings.ReloadTexts))return false;
        if(!SmallButton(pageRoot,assets,Prefix+"ReloadFonts","actions.reloadFonts",locale,475f,-157f,bindings.ReloadFonts))return false;
        if(!SmallButton(pageRoot,assets,Prefix+"ReloadTextures","actions.reloadTextures",locale,185f,-223f,bindings.ReloadTextures))return false;
        if(!SmallButton(pageRoot,assets,Prefix+"ReloadAudio","actions.reloadAudio",locale,475f,-223f,bindings.ReloadAudio))return false;
        Label(pageRoot,textTemplate,"Label.content.lastReloadSection","content.lastReloadSection",locale,300f,-286f,500f,42f,25f,true);
        var reloadInfo=Label(pageRoot,textTemplate,ReloadInfo,null,locale,310f,-376f,480f,150f,20f);
        ConfigureInfo(reloadInfo,3f);
        Refresh(pageRoot,locale,bindings);
        return true;
    }

    public static void Refresh(GameObject pageRoot,string locale,ContentPageBindings bindings)
    {
        if(pageRoot==null || bindings==null)return;
        var state=bindings.State?.Invoke() ?? new ContentPageState();
        SetText(pageRoot,PackInfo,
            $"{ModLabels.Get(locale,"content.activeLanguage")}: {state.ActiveLanguage}\n"+
            $"{ModLabels.Get(locale,"content.folder")}: {state.LocaleFolder}\n"+
            $"{ModLabels.Get(locale,"content.primaryFont")}: {state.PrimaryFont}\n"+
            $"{ModLabels.Get(locale,"content.fallback")}: {state.FallbackFonts}\n"+
            $"{ModLabels.Get(locale,"content.exact")}: {state.Exact}\n"+
            $"{ModLabels.Get(locale,"content.dynamic")}: {state.Dynamic}\n"+
            $"{ModLabels.Get(locale,"content.textures")}: {state.TextureMappings}\n"+
            $"{ModLabels.Get(locale,"content.audio")}: {state.AudioMappings}\n"+
            $"{ModLabels.Get(locale,"content.installedLocales")}: {state.InstalledLocales}");
        var result=!state.ReloadAttempted ? ModLabels.Get(locale,"content.never") : ModLabels.Get(locale,state.ReloadSucceeded?"content.pass":"content.fail");
        var error=string.IsNullOrWhiteSpace(state.ReloadError)?"—":Clip(state.ReloadError,96);
        SetText(pageRoot,ReloadInfo,
            $"{ModLabels.Get(locale,"content.result")}: {result}\n"+
            $"{ModLabels.Get(locale,"content.time")}: {state.ReloadTime}\n"+
            $"{ModLabels.Get(locale,"content.duration")}: {state.ReloadDurationMilliseconds} {ModLabels.Get(locale,"content.milliseconds")}\n"+
            $"{ModLabels.Get(locale,"content.files")}: {state.ReloadFiles}\n"+
            $"{ModLabels.Get(locale,"content.errors")}: {error}");
        Relabel(pageRoot,locale);
    }

    private static bool BigButton(GameObject root,NativeAssetSet assets,string name,string key,string locale,float x,float y,Action callback)
    {
        UnityAction click=callback;
        var button=NativeUiFactory.CloneNativeButton(assets.BigButtonTemplate,root.transform,name,click);if(button==null)return false;
        Place(button.Root,x,y,390f,76f);ConfigureButtonLabel(button.Label,365f,62f,18f,28f);button.Label.text=ModLabels.Get(locale,key);return true;
    }

    private static bool SmallButton(GameObject root,NativeAssetSet assets,string name,string key,string locale,float x,float y,Action callback)
    {
        UnityAction click=callback;
        var button=NativeUiFactory.CloneNativeButton(assets.SmallButtonTemplate,root.transform,name,click);if(button==null)return false;
        Place(button.Root,x,y,270f,58f);ConfigureButtonLabel(button.Label,250f,48f,15f,22f);button.Label.text=ModLabels.Get(locale,key);return true;
    }

    private static GameObject Label(GameObject root,GameObject template,string name,string key,string locale,float x,float y,float width,float height,float size,bool heading=false)
    {
        var clone=UnityEngine.Object.Instantiate<GameObject>(template,root.transform,false);clone.name=name;var text=FindFirstText(clone);
        if(text!=null){var rect=text.rectTransform;rect.anchorMin=new Vector2(.5f,.5f);rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;text.enableAutoSizing=true;text.fontSizeMin=heading?18f:14f;text.fontSizeMax=size;text.fontSize=size;text.alignment=TextAlignmentOptions.Left;text.color=new Color(.22f,.105f,.035f,1f);text.raycastTarget=false;text.text=key==null?"":ModLabels.Get(locale,key);}clone.SetActive(true);return clone;
    }

    private static void Relabel(GameObject root,string locale){for(var i=0;i<root.transform.childCount;i++){var child=root.transform.GetChild(i)?.gameObject;if(child==null||!child.name.StartsWith("Label.",StringComparison.Ordinal))continue;var text=FindFirstText(child);if(text!=null)text.text=ModLabels.Get(locale,child.name.Substring(6));}}
    private static string Clip(string value,int maximum)=>value.Length<=maximum?value:value.Substring(0,maximum-1)+"…";
    private static void SetText(GameObject root,string name,string value){var text=FindFirstText(FindDirect(root.transform,name));if(text!=null)text.text=value;}
    private static void Place(GameObject target,float x,float y,float width,float height){var rect=target.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.5f,.5f);rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;target.SetActive(true);}
    private static void ConfigureButtonLabel(TMP_Text label,float width,float height,float min,float max){if(label==null)return;var rect=label.rectTransform;rect.anchorMin=new Vector2(.5f,.5f);rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;label.enableAutoSizing=true;label.fontSizeMin=min;label.fontSizeMax=max;label.fontSize=max;label.color=new Color(.96f,.88f,.68f,1f);label.raycastTarget=false;}
    private static void ConfigureInfo(GameObject target,float lineSpacing){var text=FindFirstText(target);if(text==null)return;text.alignment=TextAlignmentOptions.TopLeft;text.lineSpacing=lineSpacing;}
    private static GameObject FindDirect(Transform parent,string name){if(parent==null)return null;for(var i=0;i<parent.childCount;i++){var child=parent.GetChild(i);if(child!=null&&string.Equals(child.gameObject.name,name,StringComparison.Ordinal))return child.gameObject;}return null;}
    private static TMP_Text FindFirstText(GameObject root){if(root==null)return null;var stack=new System.Collections.Generic.Stack<Transform>();stack.Push(root.transform);while(stack.Count>0){var node=stack.Pop();if(node==null)continue;var text=node.gameObject.GetComponent(Il2CppType.Of<TMP_Text>())?.TryCast<TMP_Text>();if(text!=null)return text;for(var i=node.childCount-1;i>=0;i--)stack.Push(node.GetChild(i));}return null;}
}
