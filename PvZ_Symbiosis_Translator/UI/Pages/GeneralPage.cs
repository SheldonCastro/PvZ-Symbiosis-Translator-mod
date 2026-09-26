using System;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PvZSymbiosisTranslator.UI.Pages;

internal sealed class GeneralPageBindings
{
    public Action CycleLanguage;
    public Action<NativeMenuFeature> ToggleFeature;
    public Action ToggleNotifications;
    public Action<float> SetUiScale;
    public Func<string> LanguageDisplayName;
    public Func<NativeMenuFeature,bool> FeatureEnabled;
    public Func<bool> NotificationsEnabled;
    public Func<float> UiScalePercent;
    public Func<string> Summary;
}

internal static class GeneralPage
{
    internal const string NotificationsName="PvZTranslator.Native.Page.General.Notifications";
    internal const string ScaleName="PvZTranslator.Native.Page.General.Scale";
    private const string ScaleValueName="PvZTranslator.Native.Page.General.ScaleValue";
    private const string SummaryName="PvZTranslator.Native.Page.General.Summary";

    public static bool Build(GameObject pageRoot,GameObject textTemplate,NativeAssetSet assets,string locale,GeneralPageBindings bindings)
    {
        if(pageRoot==null || textTemplate==null || assets?.BigButtonTemplate==null || assets.CheckboxTemplate==null ||
           assets.SliderTrackTemplate==null || assets.SliderKnobTemplate==null || bindings==null) return false;
        var pageTitle=FindFirstText(pageRoot);if(pageTitle!=null)pageTitle.text="";

        Label(pageRoot,textTemplate,"Label.general.languageSection","general.languageSection",locale,-430f,-42f,420f,44f,25f,true);
        Label(pageRoot,textTemplate,"Label.general.language","general.language",locale,-405f,-104f,360f,48f,24f);
        Action languageAction=bindings.CycleLanguage;
        UnityAction languageClick=languageAction;
        var language=NativeUiFactory.CloneNativeButton(assets.BigButtonTemplate,pageRoot.transform,NativeTranslatorMenuPolicy.LanguageName,languageClick);
        if(language==null) return false;
        Place(language.Root,155f,-104f,460f,72f);
        ConfigureButtonLabel(language.Label,430f,60f,19f,28f);

        Label(pageRoot,textTemplate,"Label.general.translationSection","general.translationSection",locale,-430f,-180f,420f,44f,25f,true);
        if(!Toggle(pageRoot,textTemplate,assets,bindings,locale,NativeTranslatorMenuPolicy.TranslationName,"general.translation",NativeMenuFeature.Translation,-365f,-240f)) return false;
        if(!Toggle(pageRoot,textTemplate,assets,bindings,locale,NativeTranslatorMenuPolicy.FontName,"general.font",NativeMenuFeature.Font,-365f,-300f)) return false;
        if(!Toggle(pageRoot,textTemplate,assets,bindings,locale,NativeTranslatorMenuPolicy.TexturesName,"general.textures",NativeMenuFeature.Textures,-365f,-360f)) return false;
        if(!Toggle(pageRoot,textTemplate,assets,bindings,locale,NativeTranslatorMenuPolicy.AudioName,"general.audio",NativeMenuFeature.Audio,-365f,-420f)) return false;

        Label(pageRoot,textTemplate,"Label.general.interfaceSection","general.interfaceSection",locale,250f,-180f,420f,44f,25f,true);
        Label(pageRoot,textTemplate,"Label.general.uiScale","general.uiScale",locale,165f,-240f,330f,48f,23f);
        if(!SliderControl(pageRoot,assets,bindings)) return false;
        Label(pageRoot,textTemplate,ScaleValueName,null,locale,485f,-298f,90f,40f,20f);
        Label(pageRoot,textTemplate,"Label.general.notifications","general.notifications",locale,205f,-355f,420f,48f,22f);
        if(!NotificationToggle(pageRoot,assets,bindings)) return false;

        Label(pageRoot,textTemplate,"Label.general.statusSection","general.statusSection",locale,250f,-420f,420f,42f,24f,true);
        Label(pageRoot,textTemplate,SummaryName,null,locale,265f,-485f,510f,92f,19f);
        Refresh(pageRoot,locale,bindings);
        return true;
    }

    public static void Refresh(GameObject pageRoot,string locale,GeneralPageBindings bindings)
    {
        if(pageRoot==null || bindings==null) return;
        SetText(pageRoot,NativeTranslatorMenuPolicy.LanguageName,bindings.LanguageDisplayName?.Invoke() ?? "—");
        SetToggle(pageRoot,NativeTranslatorMenuPolicy.TranslationName,bindings.FeatureEnabled?.Invoke(NativeMenuFeature.Translation) ?? false);
        SetToggle(pageRoot,NativeTranslatorMenuPolicy.FontName,bindings.FeatureEnabled?.Invoke(NativeMenuFeature.Font) ?? false);
        SetToggle(pageRoot,NativeTranslatorMenuPolicy.TexturesName,bindings.FeatureEnabled?.Invoke(NativeMenuFeature.Textures) ?? false);
        SetToggle(pageRoot,NativeTranslatorMenuPolicy.AudioName,bindings.FeatureEnabled?.Invoke(NativeMenuFeature.Audio) ?? false);
        SetToggle(pageRoot,NotificationsName,bindings.NotificationsEnabled?.Invoke() ?? true);
        var scale=TranslatorUiLayout.ClampScale(bindings.UiScalePercent?.Invoke() ?? 100f);
        var slider=FindDirect(pageRoot.transform,ScaleName)?.GetComponent<Slider>();
        if(slider!=null) slider.SetValueWithoutNotify(scale);
        SetText(pageRoot,ScaleValueName,$"{scale:0}%");
        SetText(pageRoot,SummaryName,bindings.Summary?.Invoke() ?? "");
        Relabel(pageRoot,locale);
    }

    private static bool Toggle(GameObject pageRoot,GameObject textTemplate,NativeAssetSet assets,GeneralPageBindings bindings,string locale,string name,string key,NativeMenuFeature feature,float labelX,float y)
    {
        Label(pageRoot,textTemplate,"Label."+key,key,locale,labelX,y,520f,48f,23f);
        Action action=()=>bindings.ToggleFeature?.Invoke(feature);
        UnityAction click=action;
        var toggle=NativeUiFactory.CloneNativeImageButton(assets.CheckboxTemplate,pageRoot.transform,name,click);
        if(toggle==null) return false;
        Place(toggle.Root,-175f,y,54f,50f);
        return true;
    }

    private static bool NotificationToggle(GameObject pageRoot,NativeAssetSet assets,GeneralPageBindings bindings)
    {
        Action action=bindings.ToggleNotifications;
        UnityAction click=action;
        var toggle=NativeUiFactory.CloneNativeImageButton(assets.CheckboxTemplate,pageRoot.transform,NotificationsName,click);
        if(toggle==null) return false;
        Place(toggle.Root,465f,-355f,54f,50f);
        return true;
    }

    private static bool SliderControl(GameObject pageRoot,NativeAssetSet assets,GeneralPageBindings bindings)
    {
        var track=NativeUiFactory.CloneNativeVisual(assets.SliderTrackTemplate,pageRoot.transform,ScaleName);
        if(track==null) return false;
        Place(track,300f,-298f,240f,20f);
        var trackImage=Get<Image>(track);
        if(trackImage==null) return false;
        trackImage.sprite=assets.Get(NativeAssetIds.SliderTrack);
        trackImage.type=Image.Type.Simple;
        var knob=NativeUiFactory.CloneNativeVisual(assets.SliderKnobTemplate,track.transform,"Handle");
        if(knob==null) return false;
        var knobRect=knob.GetComponent<RectTransform>();
        knobRect.anchorMin=new Vector2(.5f,.5f);knobRect.anchorMax=new Vector2(.5f,.5f);knobRect.pivot=new Vector2(.5f,.5f);
        knobRect.anchoredPosition=Vector2.zero;knobRect.sizeDelta=new Vector2(38f,50f);knobRect.localScale=Vector3.one;
        var knobImage=Get<Image>(knob);
        if(knobImage==null) return false;
        knobImage.sprite=assets.Get(NativeAssetIds.SliderKnob);
        var slider=track.GetComponent<Slider>() ?? track.AddComponent<Slider>();
        slider.minValue=85f;slider.maxValue=115f;slider.wholeNumbers=true;slider.direction=Slider.Direction.LeftToRight;
        slider.fillRect=null;slider.handleRect=knobRect;slider.targetGraphic=knobImage;
        slider.onValueChanged=new Slider.SliderEvent();
        Action<float> changeAction=value=>bindings.SetUiScale?.Invoke(value);
        UnityAction<float> changed=changeAction;
        slider.onValueChanged.AddListener(changed);
        return true;
    }

    private static GameObject Label(GameObject pageRoot,GameObject template,string name,string key,string locale,float x,float y,float width,float height,float size,bool heading=false)
    {
        var clone=UnityEngine.Object.Instantiate<GameObject>(template,pageRoot.transform,false);
        clone.name=name;
        var text=FindFirstText(clone);
        if(text!=null) {
            var rect=text.rectTransform;rect.anchorMin=new Vector2(.5f,.5f);rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;
            text.enableAutoSizing=true;text.fontSizeMin=heading?18f:16f;text.fontSizeMax=size;text.fontSize=size;
            text.alignment=TextAlignmentOptions.Left;text.color=new Color(.22f,.105f,.035f,1f);text.raycastTarget=false;
            text.text=key==null ? "" : ModLabels.Get(locale,key);
        }
        clone.SetActive(true);
        return clone;
    }

    private static void Relabel(GameObject pageRoot,string locale)
    {
        for(var i=0;i<pageRoot.transform.childCount;i++) {
            var child=pageRoot.transform.GetChild(i)?.gameObject;
            if(child==null || !child.name.StartsWith("Label.",StringComparison.Ordinal)) continue;
            var text=FindFirstText(child);if(text!=null) text.text=ModLabels.Get(locale,child.name.Substring(6));
        }
    }

    private static void SetToggle(GameObject root,string name,bool enabled)
    {
        var image=Get<Image>(FindDirect(root.transform,name));
        var sprite=NativeAssetResolver.GetCachedSprite(enabled?NativeAssetIds.CheckboxOn:NativeAssetIds.CheckboxOff);
        if(image!=null && sprite!=null) {image.sprite=sprite;image.color=Color.white;image.type=Image.Type.Simple;}
    }

    private static void SetText(GameObject root,string name,string value)
    {
        var text=FindFirstText(FindDirect(root.transform,name));if(text!=null)text.text=value;
    }

    private static void Place(GameObject target,float x,float y,float width,float height)
    {
        var rect=target.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.5f,.5f);rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;target.SetActive(true);
    }

    private static void ConfigureButtonLabel(TMP_Text label,float width,float height,float min,float max)
    {
        if(label==null)return;var rect=label.rectTransform;rect.anchorMin=new Vector2(.5f,.5f);rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;
        label.enableAutoSizing=true;label.fontSizeMin=min;label.fontSizeMax=max;label.fontSize=max;label.color=new Color(.96f,.88f,.68f,1f);label.raycastTarget=false;
    }

    private static GameObject FindDirect(Transform parent,string name)
    {
        if(parent==null)return null;for(var i=0;i<parent.childCount;i++){var child=parent.GetChild(i);if(child!=null&&string.Equals(child.gameObject.name,name,StringComparison.Ordinal))return child.gameObject;}return null;
    }

    private static TMP_Text FindFirstText(GameObject root)
    {
        if(root==null)return null;var stack=new System.Collections.Generic.Stack<Transform>();stack.Push(root.transform);
        while(stack.Count>0){var node=stack.Pop();if(node==null)continue;var text=Get<TMP_Text>(node.gameObject);if(text!=null)return text;for(var i=node.childCount-1;i>=0;i--)stack.Push(node.GetChild(i));}return null;
    }

    private static T Get<T>(GameObject root) where T:Component
        => root?.GetComponent(Il2CppType.Of<T>())?.TryCast<T>();
}
