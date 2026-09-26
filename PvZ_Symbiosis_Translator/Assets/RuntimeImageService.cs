using System;
using System.IO;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace PvZSymbiosisTranslator.Assets;

public static class RuntimeImageService
{
    public static Texture2D LoadPng(string path, bool debug = false, bool markNonReadable = false)
    {
        var bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var il2cpp = new Il2CppStructArray<byte>(bytes.Length);
        for (var i = 0; i < bytes.Length; i++) il2cpp[i] = bytes[i];
        try
        {
            var ok = UnityEngine.Il2CppImageConversionManager.LoadImage(texture, il2cpp, markNonReadable);
            if (!ok || texture == null)
            {
                throw new InvalidDataException("LOAD IMAGE FAILED");
            }
            if(debug) MelonLogger.Msg($"[RuntimeImageService] decoded {Path.GetFileName(path)}: {bytes.Length} bytes -> {texture.width}x{texture.height}; nonReadable={markNonReadable}");
            // These external assets have no serialized scene owner. A managed cache alone does
            // not protect them from Resources.UnloadUnusedAssets during scene transitions.
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear; UnityEngine.Object.DontDestroyOnLoad(texture);
            return texture;
        }
        catch (Exception ex)
        {
            if(texture!=null) UnityEngine.Object.Destroy(texture);
            if(debug) MelonLogger.Warning($"[RuntimeImageService] {Path.GetFileName(path)}: {ex.Message}");
            throw;
        }
    }

    public static byte[] EncodePng(Texture2D texture)
    {
        var data = UnityEngine.Il2CppImageConversionManager.EncodeToPNG(texture);
        if (data == null) return null;
        var bytes = new byte[data.Length]; for (var i = 0; i < data.Length; i++) bytes[i] = data[i]; return bytes;
    }
}
