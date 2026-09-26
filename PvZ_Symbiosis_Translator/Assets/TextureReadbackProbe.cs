using System;
using System.IO;
using System.Linq;
using UnityEngine;
using MelonLoader;

namespace PvZSymbiosisTranslator.Assets;

// Opt-in process test only. Never invoked by F8 or normal gameplay.
internal static class TextureReadbackProbe
{
    public static void Run(string directory)
    {
        var target=TextureDiagnostics.Targets("Home").Where(t=>t.Texture.TryCast<Texture2D>()!=null && !t.Source.Context.StartsWith("TextureProbe",StringComparison.Ordinal) && t.Texture.width<=512 && t.Texture.height<=512).OrderBy(t=>t.Texture.TryCast<Texture2D>().isReadable).ThenBy(t=>t.Texture.width*t.Texture.height).FirstOrDefault();
        if(target==null) throw new InvalidOperationException("No small Home texture observed");
        MelonLogger.Msg($"READBACK PROBE: {target.Source.Id} {target.Source.TextureName} {target.Texture.width}x{target.Texture.height}; readable={target.Texture.TryCast<Texture2D>().isReadable}; context={target.Source.Context}");
        var previous=RenderTexture.active;
        var png=TextureReadback.Encode(target.Texture);
        if(RenderTexture.active!=previous) throw new Exception("RenderTexture.active not restored");
        var decoded=Core.PngCodec.Decode(png);
        if(decoded.Width!=target.Texture.width || decoded.Height!=target.Texture.height)throw new Exception("Encoded dimensions differ");
        Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory,"isolated-home.png"),png);
        var catalogDirectory=Path.Combine(directory,"CatalogProbe","Dumps","Textures");Directory.CreateDirectory(catalogDirectory);
        File.WriteAllText(Path.Combine(catalogDirectory,"dump_requests.json"),System.Text.Json.JsonSerializer.Serialize(new[]{target.Source.Id}));
        new TextureDiagnostics().Scan(catalogDirectory,"Home");
        var exports=Directory.GetFiles(Path.Combine(directory,"CatalogProbe","TextureExport","Home"),"*.png");
        if(exports.Length!=1)throw new Exception("Explicit request did not export exactly one texture");
        MelonLogger.Msg("TEXTURE READBACK RUNTIME PASS: original non-readable Home texture; PNG decoded; active restored; one explicit catalog dump");
    }
}
