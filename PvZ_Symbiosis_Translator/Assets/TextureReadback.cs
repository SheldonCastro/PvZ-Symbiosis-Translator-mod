using System;
using UnityEngine;

namespace PvZSymbiosisTranslator.Assets;

public static class TextureReadback
{
    // Readback is opt-in because copying a large atlas stalls the rendering thread.
    public static byte[] Encode(Texture original)
    {
        if(original==null || original.width<=0 || original.height<=0) throw new ArgumentException("Missing texture");
        var previous=RenderTexture.active;RenderTexture temporary=null;Texture2D readable=null;
        try
        {
            temporary=RenderTexture.GetTemporary(original.width,original.height,0,RenderTextureFormat.ARGB32);
            Graphics.Blit(original,temporary);RenderTexture.active=temporary;
            readable=new Texture2D(original.width,original.height,TextureFormat.RGBA32,false);
            readable.ReadPixels(new Rect(0,0,original.width,original.height),0,0,false);readable.Apply(false,false);
            var result=RuntimeImageService.EncodePng(readable);
            if(result==null || result.Length<8) throw new InvalidOperationException("PNG encode failed");
            return result;
        }
        finally {RenderTexture.active=previous;if(temporary!=null)RenderTexture.ReleaseTemporary(temporary);if(readable!=null)UnityEngine.Object.Destroy(readable);}
    }
}
