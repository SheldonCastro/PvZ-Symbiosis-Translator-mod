using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using PvZSymbiosisTranslator.UI.Pages;

namespace PvZSymbiosisTranslator.UI;

internal static class NativeTranslatorShell
{
    public static bool Build(GameObject root,NativeAssetSet assets,GameObject title,GameObject status,
        GameObject spare,GameObject close,string locale,Action<NativeSettingsPage> selectPage,
        GeneralPageBindings generalBindings,ContentPageBindings contentBindings,TranslatorPageBindings translatorBindings,QaPageBindings qaBindings,DiagnosticsPageBindings diagnosticsBindings,float uiScalePercent,Action<TMP_Text> applyMenuFont)
    {
        if(root==null || assets==null || title==null || status==null || spare==null || close==null || selectPage==null) return false;
        var background=assets.Get(NativeAssetIds.Background);
        if(background==null || assets.SmallButtonTemplate==null || assets.CloseButtonTemplate==null) {
            MelonLogger.Warning("[NativeUI] final shell assets unavailable");
            return false;
        }

        var rootImage=Get<Image>(root);
        if(rootImage==null) return false;
        var rootRect=root.GetComponent<RectTransform>();
        if(rootRect==null) return false;
        rootRect.anchorMin=Vector2.zero;
        rootRect.anchorMax=Vector2.one;
        rootRect.pivot=new Vector2(.5f,.5f);
        rootRect.anchoredPosition=Vector2.zero;
        rootRect.sizeDelta=Vector2.zero;
        rootRect.localScale=Vector3.one;
        rootImage.sprite=background;
        rootImage.color=Color.white;
        rootImage.preserveAspect=false;
        rootImage.type=Image.Type.Simple;

        var oldBackground=FindDirect(root.transform,"Background");
        if(oldBackground!=null) oldBackground.SetActive(false);
        var obsoleteLanguage=FindDirect(root.transform,"Text2");
        if(obsoleteLanguage!=null) obsoleteLanguage.SetActive(false);
        spare.SetActive(false);

        ConfigureText(title,ModLabels.Get(locale,"menu.title"),new Vector2(0f,458f),new Vector2(1040f,72f),36f,44f,Color.white);
        var titleLabel=FindFirstText(title);
        if(titleLabel!=null)
        {
            titleLabel.alignment=TextAlignmentOptions.Center;
            titleLabel.rectTransform.localRotation=Quaternion.identity;
            titleLabel.rectTransform.localEulerAngles=Vector3.zero;
        }
        ConfigureText(status,"",new Vector2(0f,TranslatorUiLayout.StatusY),new Vector2(1060f,42f),18f,22f,new Color(.28f,.15f,.07f,1f));

        var closeImage=Get<Image>(close);
        var closeButton=Get<Button>(close);
        var closeLabel=FindFirstText(close);
        if(closeImage==null || closeButton==null || closeLabel==null) return false;
        closeImage.sprite=assets.Get(NativeAssetIds.CloseButton);
        closeImage.color=Color.white;
        closeImage.type=Image.Type.Simple;
        var closeRect=close.GetComponent<RectTransform>();
        closeRect.anchorMin=new Vector2(.5f,.5f);closeRect.anchorMax=new Vector2(.5f,.5f);closeRect.pivot=new Vector2(.5f,.5f);
        closeRect.anchoredPosition=new Vector2(0f,TranslatorUiLayout.CloseY);closeRect.sizeDelta=new Vector2(360f,100f);closeRect.localScale=Vector3.one;
        ConfigureLabel(closeLabel,330f,80f,24f,34f,new Color(.16f,.08f,.035f,1f));
        closeLabel.text=ModLabels.Get(locale,"common.close");

        for(var i=0;i<NativeSettingsMenuModel.Pages.Count;i++) {
            var page=NativeSettingsMenuModel.Pages[i];
            Action action=()=>selectPage(page);
            UnityEngine.Events.UnityAction click=action;
            var tab=NativeUiFactory.CloneNativeButton(assets.SmallButtonTemplate,root.transform,NativeTranslatorMenuPolicy.TabPrefix+page,click);
            if(tab==null) return false;
            var rect=tab.Root.GetComponent<RectTransform>();
            rect.anchorMin=new Vector2(.5f,.5f);rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(TranslatorUiLayout.TabX[i],TranslatorUiLayout.TabY);
            rect.sizeDelta=new Vector2(TranslatorUiLayout.TabWidth,TranslatorUiLayout.TabHeight);rect.localScale=Vector3.one;
            ConfigureLabel(tab.Label,TranslatorUiLayout.TabWidth-16f,TranslatorUiLayout.TabHeight-10f,18f,27f,new Color(.96f,.88f,.68f,1f));
            tab.Label.text=ModLabels.Get(locale,NativeSettingsMenuModel.PageKey(page));
            tab.Root.SetActive(true);

            var pageRoot=UnityEngine.Object.Instantiate<GameObject>(spare,root.transform,false);
            pageRoot.name=NativeSettingsMenuModel.PagePrefix(page)+"Root";
            var pageY=TranslatorUiLayout.PageY+(page==NativeSettingsPage.General?285f:215f);
            ConfigureText(pageRoot,ModLabels.Get(locale,NativeSettingsMenuModel.PageKey(page)),
                new Vector2(0f,pageY),new Vector2(TranslatorUiLayout.PageWidth,60f),26f,34f,new Color(.24f,.12f,.05f,1f));
            if(page==NativeSettingsPage.General && !GeneralPage.Build(pageRoot,spare,assets,locale,generalBindings)) return false;
            if(page==NativeSettingsPage.Content && !ContentPage.Build(pageRoot,spare,assets,locale,contentBindings)) return false;
            if(page==NativeSettingsPage.Translator && !TranslatorPage.Build(pageRoot,spare,assets,locale,translatorBindings)) return false;
            if(page==NativeSettingsPage.Qa && !QaPage.Build(pageRoot,spare,assets,locale,qaBindings)) return false;
            if(page==NativeSettingsPage.Diagnostics && !DiagnosticsPage.Build(pageRoot,spare,assets,locale,diagnosticsBindings)) return false;
            pageRoot.SetActive(false);
        }
        ApplyMenuFont(root,applyMenuFont);
        ApplyScale(root,uiScalePercent);
        MelonLogger.Msg("[NativeUI] final shell built; background=ChallengeBackground; fullscreen=True; tabs=5; close=OptionsBacktogameButton; canvasCreated=False");
        return true;
    }

    public static void ApplyMenuFont(GameObject root,Action<TMP_Text> apply)
    {
        if(root==null || apply==null)return;
        var stack=new Stack<Transform>();stack.Push(root.transform);
        while(stack.Count>0){var node=stack.Pop();if(node==null)continue;var text=Get<TMP_Text>(node.gameObject);if(text!=null)apply(text);for(var i=node.childCount-1;i>=0;i--)stack.Push(node.GetChild(i));}
    }

    public static void ApplyScale(GameObject root,float percent)
    {
        if(root==null)return;
        var scale=Vector3.one*TranslatorUiLayout.ScaleFactor(percent);
        for(var i=0;i<root.transform.childCount;i++) {
            var child=root.transform.GetChild(i);if(child==null)continue;
            var name=child.gameObject.name;
            if(name=="Text1" || name=="Image" || name==NativeTranslatorMenuPolicy.StatusName ||
               name.StartsWith(NativeTranslatorMenuPolicy.TabPrefix,StringComparison.Ordinal) ||
               NativeSettingsMenuModel.Pages.Any(page=>name.StartsWith(NativeSettingsMenuModel.PagePrefix(page),StringComparison.Ordinal)))
                child.localScale=scale;
        }
    }

    private static void ConfigureText(GameObject target,string value,Vector2 position,Vector2 size,float min,float max,Color color)
    {
        var label=FindFirstText(target);if(label==null)return;
        var rect=label.rectTransform;if(rect!=null){rect.anchorMin=new Vector2(.5f,.5f);rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;}
        label.enableAutoSizing=true;label.fontSizeMin=min;label.fontSizeMax=max;label.fontSize=max;label.color=color;label.raycastTarget=false;label.text=value;
    }

    private static void ConfigureLabel(TMP_Text label,float width,float height,float min,float max,Color color)
    {
        if(label==null)return;var rect=label.rectTransform;
        if(rect!=null){rect.anchorMin=new Vector2(.5f,.5f);rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;}
        label.enableAutoSizing=true;label.fontSizeMin=min;label.fontSizeMax=max;label.fontSize=max;label.color=color;label.raycastTarget=false;
    }

    private static GameObject FindDirect(Transform parent,string name)
    {
        if(parent==null)return null;for(var i=0;i<parent.childCount;i++){var child=parent.GetChild(i);if(child!=null && string.Equals(child.gameObject.name,name,StringComparison.Ordinal))return child.gameObject;}return null;
    }

    private static TMP_Text FindFirstText(GameObject root)
    {
        if(root==null)return null;var stack=new Stack<Transform>();stack.Push(root.transform);
        while(stack.Count>0){var node=stack.Pop();if(node==null)continue;var text=Get<TMP_Text>(node.gameObject);if(text!=null)return text;for(var i=node.childCount-1;i>=0;i--)stack.Push(node.GetChild(i));}return null;
    }

    private static T Get<T>(GameObject root) where T:Component
        => root?.GetComponent(Il2CppType.Of<T>())?.TryCast<T>();
}
