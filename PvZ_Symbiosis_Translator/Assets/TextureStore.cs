using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Cryptography;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;
using System.Linq;
using PvZSymbiosisTranslator.Core;
using PvZSymbiosisTranslator.Localization;

namespace PvZSymbiosisTranslator.Assets;

public sealed class TextureStore : IDisposable
{
    private sealed class ActiveEntry
    {
        public string Id="",RelativePath="",Hash="";
        public bool Automatic,RetainedLastGood;
        public TextureMatch Match;
        public Texture2D Texture;
    }

    private readonly Dictionary<string,Texture2D> replacements=new(StringComparer.Ordinal);
    private readonly Dictionary<string,Texture2D> cache=new(StringComparer.Ordinal);
    private readonly Dictionary<string,ActiveEntry> active=new(StringComparer.Ordinal);
    private readonly List<Action> restore=new();
    private readonly List<Sprite> created=new();
    private readonly HashSet<int> applied=new();
    private readonly Dictionary<string,TextureMatch> matches=new(StringComparer.Ordinal);
    private readonly Dictionary<int,string> samplingProfiles=new();
    public int Count => replacements.Count;
    public int AppliedCount => applied.Count;
    public int ExplicitCount {get;private set;}
    public int AutomaticCount {get;private set;}
    public int CachedTextureCount => cache.Count;
    public int CreatedSpriteCount => created.Count;
    public int RetainedLastGoodCount {get;private set;}
    public long LastLoadDurationMilliseconds {get;private set;}
    public string LastError {get;private set;}="";

    public void Restore()
    {
        foreach(var action in restore) try { action(); } catch(Exception ex) { MelonLogger.Warning("Texture restore: "+ex.Message); }
        restore.Clear(); applied.Clear(); samplingProfiles.Clear();
        foreach(var sprite in created) if(sprite!=null) UnityEngine.Object.Destroy(sprite);
        created.Clear();
    }

    public void Load(string locale,bool enabled,bool debug=false)
    {
        var timer=Stopwatch.StartNew();
        var next=new Dictionary<string,ActiveEntry>(StringComparer.Ordinal);
        var newlyOwned=new Dictionary<string,Texture2D>(StringComparer.Ordinal);
        var explicitFiles=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var explicitTextureNames=new HashSet<string>(StringComparer.Ordinal);
        var sourceIdentities=new HashSet<string>(StringComparer.Ordinal);
        var explicitCount=0;var automaticCount=0;var retained=0;var candidateErrors=new List<string>();
        try
        {
            if(enabled)
            {
                var root=Path.GetFullPath(Path.Combine(locale,"Textures")).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                ActiveEntry Decode(string id,TextureMatch match,string path,bool automatic)
                {
                    var bytes=File.ReadAllBytes(path);var hash=Convert.ToHexString(SHA256.HashData(bytes));
                    Texture2D texture;
                    if(active.TryGetValue(id,out var previous)&&previous.Hash==hash&&previous.Texture!=null)texture=previous.Texture;
                    else if(cache.TryGetValue(hash,out var cached)&&cached!=null)texture=cached;
                    else if(newlyOwned.TryGetValue(hash,out var pending)&&pending!=null)texture=pending;
                    else{texture=RuntimeImageService.LoadPng(path,debug,false);newlyOwned.Add(hash,texture);}
                    return new ActiveEntry{Id=id,RelativePath=Path.GetRelativePath(root,path).Replace('\\','/'),Hash=hash,Automatic=automatic,Match=match,Texture=texture};
                }
                void AddOrRetain(string id,TextureMatch match,string path,bool automatic)
                {
                    try{next.Add(id,Decode(id,match,path,automatic));}
                    catch(Exception ex)
                    {
                        candidateErrors.Add(id+": "+ex.Message);
                        var relative=Path.GetRelativePath(root,path).Replace('\\','/');
                        if(active.TryGetValue(id,out var previous)&&previous.Texture!=null&&previous.RelativePath==relative&&MatchIdentity(previous.Match)==MatchIdentity(match)){previous.RetainedLastGood=true;next.Add(id,previous);retained++;MelonLogger.Warning($"Texture {id}: invalid candidate; retained last known good ({ex.Message})");}
                        else MelonLogger.Warning($"Texture {id}: candidate skipped ({ex.Message})");
                    }
                }

                using var doc=JsonFile.Read(Path.Combine(root,"manifest.json"),message=>MelonLogger.Warning(message));
                var entries=doc.RootElement.ValueKind==JsonValueKind.Array?doc.RootElement:doc.RootElement.GetProperty("entries");
                if(entries.ValueKind!=JsonValueKind.Array)throw new FormatException("Texture manifest entries must be an array");
                foreach(var entry in entries.EnumerateArray())
                {
                    if(entry.ValueKind!=JsonValueKind.Object)throw new FormatException("Texture manifest entry must be an object");
                    var id=entry.TryGetProperty("id",out var idValue)?idValue.GetString():"entry:"+explicitCount;
                    if(string.IsNullOrWhiteSpace(id)||next.ContainsKey(id))throw new FormatException("Duplicate or empty texture mapping ID: "+id);
                    var match=entry.TryGetProperty("match",out var matchJson)?JsonSerializer.Deserialize<TextureMatch>(matchJson.GetRawText(),new JsonSerializerOptions{PropertyNameCaseInsensitive=true}):new TextureMatch{Id=id};
                    if(match==null||(match.Id==null&&(match.TextureName==null||match.Width<=0||match.Height<=0)))throw new FormatException("Texture match requires textureName/width/height or metadata ID");
                    var identity=MatchIdentity(match);if(!sourceIdentities.Add(identity))throw new FormatException("Duplicate explicit texture source identity: "+identity);
                    var replacement=entry.TryGetProperty("replacement",out var file)?file.GetString():entry.GetProperty("file").GetString();
                    var path=SafePath(root,replacement);AddOrRetain(id,match,path,false);explicitFiles.Add(path);if(match.TextureName!=null)explicitTextureNames.Add(match.TextureName);explicitCount++;
                }

                var automaticIdentities=new HashSet<string>(StringComparer.Ordinal);
                foreach(var candidate in Directory.EnumerateFiles(root,"*.png",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
                {
                    var path=Path.GetFullPath(candidate);if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase)||explicitFiles.Contains(path))continue;
                    var textureName=Path.GetFileNameWithoutExtension(path);if(string.IsNullOrWhiteSpace(textureName)||explicitTextureNames.Contains(textureName))continue;
                    var relative=Path.GetRelativePath(root,path).Replace('\\','/');var id="auto:"+relative;ActiveEntry decoded;
                    try{decoded=Decode(id,new TextureMatch{TextureName=textureName},path,true);}
                    catch(Exception ex)
                    {
                        candidateErrors.Add(id+": "+ex.Message);
                        if(active.TryGetValue(id,out var previous)&&previous.Texture!=null){previous.RetainedLastGood=true;next.Add(id,previous);retained++;automaticCount++;MelonLogger.Warning($"Texture {id}: invalid candidate; retained last known good ({ex.Message})");}
                        else MelonLogger.Warning($"Texture {id}: candidate skipped ({ex.Message})");
                        continue;
                    }
                    decoded.Match.Width=decoded.Texture.width;decoded.Match.Height=decoded.Texture.height;var identity=MatchIdentity(decoded.Match);
                    if(!automaticIdentities.Add(identity))throw new FormatException("Duplicate automatic texture identity: "+identity);
                    if(!sourceIdentities.Add(identity))throw new FormatException("Explicit/automatic texture identity conflict: "+identity);
                    next.Add(id,decoded);automaticCount++;
                }
            }

            Restore();replacements.Clear();matches.Clear();active.Clear();
            foreach(var pair in next){active.Add(pair.Key,pair.Value);replacements.Add(pair.Key,pair.Value.Texture);matches.Add(pair.Key,pair.Value.Match);}
            foreach(var pair in newlyOwned)if(!cache.ContainsKey(pair.Key))cache.Add(pair.Key,pair.Value);
            foreach(var key in cache.Keys.ToArray())if(!replacements.ContainsValue(cache[key])){UnityEngine.Object.Destroy(cache[key]);cache.Remove(key);}
            ExplicitCount=enabled?explicitCount:0;AutomaticCount=enabled?automaticCount:0;RetainedLastGoodCount=enabled?retained:0;LastError=enabled?string.Join(" | ",candidateErrors.Take(5)):"";
            timer.Stop();LastLoadDurationMilliseconds=timer.ElapsedMilliseconds;
            if(enabled)MelonLogger.Msg($"Texture load: {ExplicitCount} explicit; {AutomaticCount} automatic; {Count} total; {RetainedLastGoodCount} retained; {LastLoadDurationMilliseconds} ms");
        }
        catch(Exception ex)
        {
            foreach(var texture in newlyOwned.Values.Distinct())if(texture!=null&&!cache.ContainsValue(texture))UnityEngine.Object.Destroy(texture);
            timer.Stop();LastLoadDurationMilliseconds=timer.ElapsedMilliseconds;LastError=ex.Message;MelonLogger.Error("Texture load rejected; retaining previous state: "+ex.Message);throw;
        }
    }

    private static string SafePath(string root,string relative)
    {
        if(string.IsNullOrWhiteSpace(relative)||Path.IsPathRooted(relative))throw new FormatException("Invalid texture replacement path");
        var path=Path.GetFullPath(Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar)));
        if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new FormatException("Outside pack texture");
        if(!string.Equals(Path.GetExtension(path),".png",StringComparison.OrdinalIgnoreCase))throw new FormatException("Texture replacement must be PNG");return path;
    }
    private static string MatchIdentity(TextureMatch match)=>match.Id!=null?"id:"+match.Id:$"texture:{match.TextureName}|sprite:{match.SpriteName??"*"}|size:{match.Width}x{match.Height}|scene:{match.Scene??"*"}|context:{match.Context??"*"}";

    private void PreserveSampling(Texture2D replacement,Texture source)
    {
        var profile=$"{(int)source.filterMode}|{(int)source.wrapMode}|{source.anisoLevel}";var id=replacement.GetInstanceID();
        if(samplingProfiles.TryGetValue(id,out var previous)&&previous!=profile)throw new InvalidOperationException("Replacement source sampling settings conflict");
        replacement.filterMode=source.filterMode;replacement.wrapMode=source.wrapMode;replacement.anisoLevel=source.anisoLevel;samplingProfiles[id]=profile;
    }

    private Sprite MakeSprite(Sprite original,Texture2D replacement,bool preserveCustomGeometry)
    {
        if(original.texture.width!=replacement.width||original.texture.height!=replacement.height)throw new FormatException("Replacement atlas dimensions differ from original");PreserveSampling(replacement,original.texture);
        var rect=original.rect;var sprite=Sprite.Create(replacement,rect,new Vector2(original.pivot.x/rect.width,original.pivot.y/rect.height),original.pixelsPerUnit,0,SpriteMeshType.FullRect,original.border);
        try
        {
            var a=original.vertices;var b=sprite.vertices;var at=original.triangles;var bt=sprite.triangles;bool same=a.Length==b.Length&&at.Length==bt.Length;
            for(int i=0;same&&i<a.Length;i++)same=a[i]==b[i];for(int i=0;same&&i<at.Length;i++)same=at[i]==bt[i];var rectangular=a.Length==4&&at.Length==6&&b.Length==4&&bt.Length==6;
            if(preserveCustomGeometry&&!same&&!rectangular)sprite.OverrideGeometry(a,at);
        }
        catch{UnityEngine.Object.Destroy(sprite);throw;}created.Add(sprite);return sprite;
    }

    public void Apply(TextureDiagnostics diagnostics)
    {
        if(replacements.Count==0)return;Restore();var targets=TextureDiagnostics.Targets(PvZSymbiosisTranslatorMod.CurrentScene);var sources=targets.Select(t=>t.Source).ToList();var assignments=new Dictionary<int,List<string>>();
        foreach(var pair in matches)try{foreach(var i in pair.Value.Resolve(sources)){if(!assignments.TryGetValue(i,out var ids)){ids=new List<string>();assignments.Add(i,ids);}ids.Add(pair.Key);}}catch(Exception ex){MelonLogger.Warning(pair.Key+": "+ex.Message);}
        foreach(var pair in assignments)try
        {
            if(pair.Value.Count!=1){MelonLogger.Warning("AMBIGUOUS MATCH: multiple texture mappings target "+sources[pair.Key].Id);continue;}var target=targets[pair.Key];var replacement=replacements[pair.Value[0]];
            if(target.Texture.width!=replacement.width||target.Texture.height!=replacement.height)throw new FormatException("Replacement dimensions differ from original");var image=target.Component.TryCast<Image>();var raw=target.Component.TryCast<RawImage>();var renderer=target.Component.TryCast<SpriteRenderer>();
            if(image!=null){var original=image.sprite;var overridden=image.overrideSprite;var sprite=MakeSprite(target.Sprite,replacement,image.useSpriteMesh);image.sprite=sprite;image.overrideSprite=sprite;restore.Add(()=>{if(image!=null&&image.sprite==sprite){image.sprite=original;image.overrideSprite=overridden;}});}
            else if(raw!=null){PreserveSampling(replacement,target.Texture);var original=raw.texture;raw.texture=replacement;restore.Add(()=>{if(raw!=null&&raw.texture==replacement)raw.texture=original;});}
            else if(renderer!=null){var original=renderer.sprite;var sprite=MakeSprite(original,replacement,true);renderer.sprite=sprite;restore.Add(()=>{if(renderer!=null&&renderer.sprite==sprite)renderer.sprite=original;});}applied.Add(target.Component.GetInstanceID());
        }
        catch(Exception ex){MelonLogger.Warning("Texture replacement skipped: "+ex.Message);}MelonLogger.Msg($"Texture apply: {applied.Count} components; {replacements.Count} mappings");
    }

    public void Dispose()
    {
        Restore();foreach(var texture in cache.Values.Distinct())if(texture!=null)UnityEngine.Object.Destroy(texture);cache.Clear();active.Clear();replacements.Clear();matches.Clear();ExplicitCount=0;AutomaticCount=0;RetainedLastGoodCount=0;LastError="";
    }
}
