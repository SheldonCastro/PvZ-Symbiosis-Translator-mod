using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using MelonLoader;
using PvZSymbiosisTranslator.Configuration;
using PvZSymbiosisTranslator.Core;
namespace PvZSymbiosisTranslator.Assets;

public sealed class TextureDiagnostics
{
    // New GPU copy + manager encode path verified on one non-readable Home texture.
    // Historical GetPixels wrappers remain unused. F8 dumps explicit requests only.
    public static bool PixelReadbackVerified => true;
    public sealed class Entry {
        public string Id {get;set;} public string Scene {get;set;} public string Hierarchy {get;set;} public string Component {get;set;}
        public string SpriteName {get;set;} public string TextureName {get;set;} public int Width {get;set;} public int Height {get;set;}
        public string Hash {get;set;} public string Classification {get;set;} = "UNKNOWN_GRAPHIC"; public string Error {get;set;}
    }
    private readonly Dictionary<string,Texture> observed = new(StringComparer.Ordinal);
    private readonly Dictionary<int,string> identities = new();
    private string observedScene="Misc";
    public sealed class Target
    {
        public Component Component; public Texture Texture; public Sprite Sprite; public TextureSource Source;
    }
    public static List<Target> Targets(string scene)
    {
        var result=new List<Target>();
        void Add(Component c,Texture texture,Sprite sprite)
        {
            if(texture==null || !c.gameObject.activeInHierarchy || PvZSymbiosisTranslatorMod.IsModUi(c)) return;
            var names=new List<string>(); for(var t=c.transform;t!=null;t=t.parent) names.Add(t.name); names.Reverse();
            var actual=c.gameObject.scene;
            var actualScene=actual.IsValid() && !string.IsNullOrEmpty(actual.name) ? actual.name : scene;
            var textureKey=texture.GetInstanceID().ToString(System.Globalization.CultureInfo.InvariantCulture);
            result.Add(new Target {Component=c,Texture=texture,Sprite=sprite,Source=new TextureSource {Scene=actualScene,Context=string.Join("/",names),Component=c.GetType().Name,TextureName=texture.name,SpriteName=sprite?.name,Width=texture.width,Height=texture.height,RuntimeTextureKey=textureKey,RuntimeAssetKey=textureKey+":"+(sprite==null ? 0 : sprite.GetInstanceID())}});
        }
        foreach(var c in Resources.FindObjectsOfTypeAll<Image>()) if(c!=null) {var sprite=c.overrideSprite!=null ? c.overrideSprite : c.sprite;if(sprite!=null)Add(c,sprite.texture,sprite);}
        foreach(var c in Resources.FindObjectsOfTypeAll<RawImage>()) if(c!=null) Add(c,c.texture,null);
        foreach(var c in Resources.FindObjectsOfTypeAll<SpriteRenderer>()) if(c!=null && c.sprite!=null) Add(c,c.sprite.texture,c.sprite);
        return result;
    }
    public int Count {get;private set;}
    public static byte[] Encode(Texture original)
    {
        return TextureReadback.Encode(original);
    }
    public void Reset() {observed.Clear();identities.Clear();Count=0;}
    public void Scan(string directory,string scene)
    {
        Directory.CreateDirectory(directory);
        observedScene=scene;
        var rows=new List<Entry>();
        observed.Clear();
        foreach(var target in Targets(scene))
        {
            var s=target.Source;
            if(observed.TryGetValue(s.Id,out var previous) && (previous==null || previous.GetInstanceID()!=target.Texture.GetInstanceID())) observed[s.Id]=null;
            else observed[s.Id]=target.Texture;
            rows.Add(new Entry {Id=s.Id,Scene=s.Scene,Hierarchy=s.Context,Component=s.Component,SpriteName=s.SpriteName,TextureName=s.TextureName,Width=s.Width,Height=s.Height,Error=PixelReadbackVerified ? null : "Readback unverified; metadata ID only"});
        }
        Count=rows.Count;
        Save(Path.Combine(directory,"texture_catalog.json"),rows); Save(Path.Combine(directory,"graphic_text_candidates.json"),rows);
        var requests=Path.Combine(directory,"dump_requests.json");
        if(File.Exists(requests)) using(var doc=JsonDocument.Parse(File.ReadAllText(requests))) foreach(var id in doc.RootElement.EnumerateArray()) {
            try { DumpOriginal(directory,id.GetString()); } catch(Exception ex) {MelonLogger.Warning("Texture export: "+ex.Message);}
        }
    }
    public void DumpOriginal(string directory,string id)
    {
        if(!observed.TryGetValue(id,out var texture) || texture==null) throw new InvalidOperationException("Texture ID not observed in this run");
        var sceneFolder=System.Text.RegularExpressions.Regex.Replace(observedScene??"Misc",@"[^a-zA-Z0-9_-]","_");
        var modRoot=Directory.GetParent(Directory.GetParent(directory).FullName).FullName;
        var root=Path.Combine(modRoot,"TextureExport",sceneFolder); Directory.CreateDirectory(root);
        var safeName=id.Replace("metadata:","");
        File.WriteAllBytes(Path.Combine(root,safeName+".png"),Encode(texture));
        MelonLogger.Msg("Texture export: "+Path.Combine(root,safeName+".png"));
    }
    private static void Save(string path,object value) {File.WriteAllText(path+".tmp",JsonSerializer.Serialize(value,ConfigManager.HumanReadableJson));File.Move(path+".tmp",path,true);}
}
