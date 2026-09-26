using System;
using System.IO;
using System.Text.Json;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;

namespace PvZSymbiosisTranslator.Assets;

internal static class TextureReplacementProbe
{
    public static void Run(string modRoot)
    {
        var root=Path.Combine(modRoot,"Cache","ReplacementProbe");var pack=Path.Combine(root,"Textures");Directory.CreateDirectory(pack);
        var png=Path.Combine(pack,"probe.png");File.Copy(Path.Combine(modRoot,"UI","buttonsmall.png"),png,true);
        var source=RuntimeImageService.LoadPng(png);source.name="ExternalProbeSource";source.filterMode=FilterMode.Point;source.wrapMode=TextureWrapMode.Repeat;source.anisoLevel=4;
        var sprite=Sprite.Create(source,new Rect(0,0,source.width,source.height),new Vector2(.3f,.6f),80,0,SpriteMeshType.FullRect,new Vector4(4,5,6,7));
        sprite.name="ExternalProbeSprite";
        var rawObject=new GameObject("TextureProbeRaw");var imageObject=new GameObject("TextureProbeImage");var rendererObject=new GameObject("TextureProbeRenderer");
        var raw=rawObject.AddComponent<RawImage>();raw.texture=source;
        var image=imageObject.AddComponent<Image>();image.sprite=sprite;image.type=Image.Type.Sliced;
        var renderer=rendererObject.AddComponent<SpriteRenderer>();renderer.sprite=sprite;
        var store=new TextureStore();
        try
        {
            var entries=new object[3];var names=new[]{"TextureProbeRaw","TextureProbeImage","TextureProbeRenderer"};
            for(int i=0;i<3;i++) entries[i]=new {id="probe"+i,match=new {textureName=source.name,width=source.width,height=source.height,context=names[i]},replacement="probe.png"};
            File.WriteAllText(Path.Combine(pack,"manifest.json"),JsonSerializer.Serialize(entries));
            store.Load(root,true);store.Apply(new TextureDiagnostics());
            if(store.AppliedCount!=3 || raw.texture==source || image.sprite.texture==source || renderer.sprite.texture==source) throw new Exception("Replacement did not reach all three renderer types");
            if(image.type!=Image.Type.Sliced || image.sprite.border!=sprite.border || image.sprite.rect!=sprite.rect || image.sprite.pivot!=sprite.pivot || image.sprite.pixelsPerUnit!=sprite.pixelsPerUnit || image.sprite.vertices.Length!=sprite.vertices.Length) throw new Exception("Sprite properties changed");
            var first=raw.texture.GetInstanceID();
            // A harmless byte after IEND changes the file hash without changing the source artwork.
            using(var stream=File.Open(png,FileMode.Append)) stream.WriteByte(0);
            store.Load(root,true);store.Apply(new TextureDiagnostics());
            if(store.AppliedCount!=3 || raw.texture.GetInstanceID()==first) throw new Exception("Changed PNG was not reloaded");
            if(raw.texture.filterMode!=source.filterMode||raw.texture.wrapMode!=source.wrapMode||raw.texture.anisoLevel!=source.anisoLevel)throw new Exception("Source sampling settings were not preserved");
            var lastGood=raw.texture.GetInstanceID();File.WriteAllBytes(png,new byte[]{137,80,78,71,13,10,26,10});
            store.Load(root,true);store.Apply(new TextureDiagnostics());
            if(store.RetainedLastGoodCount!=3||string.IsNullOrWhiteSpace(store.LastError)||store.AppliedCount!=3||raw.texture.GetInstanceID()!=lastGood)throw new Exception("Invalid save did not retain the last known good texture");
            store.Restore();if(raw.texture!=source || image.sprite!=sprite || renderer.sprite!=sprite)throw new Exception("Originals not restored");
            MelonLogger.Msg("TEXTURE REPLACEMENT PROBE PASS: Image/RawImage/SpriteRenderer; border/rect/pivot/PPU/geometry/sampling; changed-file reload; invalid-save last-known-good; restoration");
        }
        finally {store.Dispose();UnityEngine.Object.Destroy(rawObject);UnityEngine.Object.Destroy(imageObject);UnityEngine.Object.Destroy(rendererObject);UnityEngine.Object.Destroy(sprite);UnityEngine.Object.Destroy(source);}
    }
}
