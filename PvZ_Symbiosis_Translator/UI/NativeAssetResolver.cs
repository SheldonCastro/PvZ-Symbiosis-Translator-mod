using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace PvZSymbiosisTranslator.UI;

internal sealed class NativeAssetSet
{
    public GameObject SmallButtonTemplate;
    public GameObject BigButtonTemplate;
    public GameObject CloseButtonTemplate;
    public GameObject CheckboxTemplate;
    public GameObject SliderTrackTemplate;
    public GameObject SliderKnobTemplate;
    public readonly Dictionary<string,Sprite> Sprites=new(StringComparer.Ordinal);
    public Sprite Get(string id)=>Sprites.TryGetValue(id,out var sprite) && sprite!=null ? sprite : null;
    public bool Complete=>NativeAssetIds.All.All(id=>Get(id)!=null);
}

internal static class NativeAssetResolver
{
    private static readonly Dictionary<string,Sprite> SpriteCache=new(StringComparer.Ordinal);
    private static int exactLookupAttempts;

    public static NativeAssetSet Resolve(Il2Cpp.HomeManager manager)
    {
        if(manager==null || manager.setupWindowPrefab==null) return null;
        var setup=manager.setupWindowPrefab;
        var setupComponent=Get<Il2Cpp.SetupWindow>(setup);
        var dialog=setupComponent?.dialogPrefab;
        var result=new NativeAssetSet {
            SmallButtonTemplate=FindDirect(dialog?.transform,"Confirm"),
            BigButtonTemplate=FindDirect(setup.transform,"Help"),
            CloseButtonTemplate=FindDirect(setup.transform,"Button"),
            CheckboxTemplate=FindPath(setup.transform,"FullScreen","Content5"),
            SliderTrackTemplate=FindPath(setup.transform,"Sound","Content3"),
            SliderKnobTemplate=FindPath(setup.transform,"Sound","Content4","Sliding Area","Handle")
        };
        CacheTemplateSprite(NativeAssetIds.ButtonSmall,result.SmallButtonTemplate);
        CacheTemplateSprite(NativeAssetIds.ButtonBig,result.BigButtonTemplate);
        CacheTemplateSprite(NativeAssetIds.CloseButton,result.CloseButtonTemplate);
        CacheTemplateSprite(NativeAssetIds.CheckboxOff,result.CheckboxTemplate);
        CacheTemplateSprite(NativeAssetIds.SliderTrack,result.SliderTrackTemplate);
        CacheTemplateSprite(NativeAssetIds.SliderKnob,result.SliderKnobTemplate);
        ResolveExactSprites();
        foreach(var pair in SpriteCache) result.Sprites[pair.Key]=pair.Value;
        if(!result.Complete) MelonLogger.Warning("[NativeAssets] unresolved: "+string.Join(",",NativeAssetIds.All.Where(id=>result.Get(id)==null)));
        return result;
    }

    private static void CacheTemplateSprite(string id,GameObject template)
    {
        var sprite=Get<Image>(template)?.sprite;
        if(sprite!=null) SpriteCache[id]=sprite;
    }

    private static void ResolveExactSprites()
    {
        var expected=new Dictionary<string,string>(StringComparer.Ordinal) {
            ["ButtonSmall_0"]=NativeAssetIds.ButtonSmall,
            ["ButtonBig_0"]=NativeAssetIds.ButtonBig,
            ["ChallengeBackground"]=NativeAssetIds.Background,
            ["OptionsBacktogameButton"]=NativeAssetIds.CloseButton,
            ["OptionsCheckboxOpen"]=NativeAssetIds.CheckboxOn,
            ["OptionsCheckbox"]=NativeAssetIds.CheckboxOff,
            ["OptionsSliderslot"]=NativeAssetIds.SliderTrack,
            ["OptionsSliderknob"]=NativeAssetIds.SliderKnob
        };
        var scannedLoadedSprites=false;
        if(!NativeAssetIds.All.All(IsCached) && exactLookupAttempts<2)
        {
            exactLookupAttempts++;
            scannedLoadedSprites=true;
            foreach(var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                CacheExpected(sprite,expected);
        }
        var loadedCount=0;
        foreach(var pair in NativeAssetIds.ResourcePaths)
        {
            if(IsCached(pair.Key)) continue;
            try
            {
                var loaded=Resources.Load(pair.Value,Il2CppType.Of<Sprite>())?.TryCast<Sprite>();
                if(loaded!=null)
                {
                    SpriteCache[pair.Key]=loaded;
                    loadedCount++;
                }
            }
            catch(Exception ex)
            {
                MelonLogger.Warning($"[NativeAssets] load failed: {pair.Key}: {ex.GetType().Name}");
            }
        }
        if(loadedCount>0 || scannedLoadedSprites)
            MelonLogger.Msg($"[NativeAssets] exact lookup {exactLookupAttempts}/2; reloaded={loadedCount}; resolved={NativeAssetIds.All.Count(IsCached)}/{NativeAssetIds.All.Count}");
    }

    private static bool IsCached(string id)
        => SpriteCache.TryGetValue(id,out var sprite) && sprite!=null;

    internal static Sprite GetCachedSprite(string id)
        => IsCached(id) ? SpriteCache[id] : null;

    private static void CacheExpected(Sprite sprite,IReadOnlyDictionary<string,string> expected)
    {
        if(sprite!=null && expected.TryGetValue(sprite.name,out var id) && !SpriteCache.ContainsKey(id)) SpriteCache[id]=sprite;
    }

    private static GameObject FindDirect(Transform parent,string name)
    {
        if(parent==null)return null;
        for(var i=0;i<parent.childCount;i++){var child=parent.GetChild(i);if(child!=null && string.Equals(child.gameObject.name,name,StringComparison.Ordinal))return child.gameObject;}
        return null;
    }

    private static GameObject FindPath(Transform root,params string[] parts)
    {
        var current=root;
        foreach(var part in parts){var next=FindDirect(current,part);if(next==null)return null;current=next.transform;}
        return current?.gameObject;
    }

    private static T Get<T>(GameObject root) where T:Component
        => root?.GetComponent(Il2CppType.Of<T>())?.TryCast<T>();
}
