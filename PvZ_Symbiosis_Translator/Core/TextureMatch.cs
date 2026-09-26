using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PvZSymbiosisTranslator.Core;

public sealed class TextureSource
{
    public string Scene {get;set;}
    public string Context {get;set;}
    public string Component {get;set;}
    public string TextureName {get;set;}
    public string SpriteName {get;set;}
    public int Width {get;set;}
    public int Height {get;set;}
    // Session-only Texture2D identity. Unlike RuntimeAssetKey, this deliberately
    // ignores the Sprite so one atlas replacement can serve all of its regions.
    public string RuntimeTextureKey {get;set;}
    // Session-only identity is used solely to detect collisions, never persisted as an asset ID.
    public string RuntimeAssetKey {get;set;}
    public string Id => "metadata:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[]{Scene,Context,Component,TextureName,SpriteName,Width.ToString(System.Globalization.CultureInfo.InvariantCulture),Height.ToString(System.Globalization.CultureInfo.InvariantCulture)}))));
}
public sealed class TextureMatch
{
    public string Id {get;set;}
    public string TextureName {get;set;}
    public string SpriteName {get;set;}
    public string Scene {get;set;}
    public string Context {get;set;}
    public int Width {get;set;}
    public int Height {get;set;}
    public bool Matches(TextureSource source) =>
        (Id != null ? string.Equals(Id,source.Id,StringComparison.Ordinal) :
        TextureName != null && Width>0 && Height>0 && TextureName==source.TextureName && Width==source.Width && Height==source.Height && (SpriteName==null || SpriteName==source.SpriteName)) &&
        (Scene==null || Scene==source.Scene) && (Context==null || Context==source.Context);
    public List<int> Resolve(IReadOnlyList<TextureSource> sources)
    {
        var indices=new List<int>(); var assets=new HashSet<string>(StringComparer.Ordinal);
        for(int i=0;i<sources.Count;i++) if(Matches(sources[i])) {
            indices.Add(i);
            // A texture-only rule intentionally applies to every Sprite region backed
            // by the same Texture2D. Sprite-specific and metadata-ID rules retain the
            // narrower texture+sprite collision identity.
            var key=Id==null && SpriteName==null ? sources[i].RuntimeTextureKey : sources[i].RuntimeAssetKey;
            assets.Add(key??sources[i].RuntimeAssetKey??("source:"+i));
        }
        if(assets.Count>1) throw new InvalidOperationException("AMBIGUOUS MATCH: metadata identifies multiple runtime assets; add scene/context");
        return indices;
    }
}
