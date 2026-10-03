using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using MelonLoader;
using PvZSymbiosisTranslator.Localization;

namespace PvZSymbiosisTranslator.Assets;

public sealed class AudioStore
{
    private readonly Dictionary<string, string> paths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AudioClip> cache = new(StringComparer.Ordinal);
    private string root;
    private int generation;
    public int Count => paths.Count;
    public int LoadedCount => cache.Count;
    public void Load(string localeDirectory, bool enabled)
    {
        paths.Clear();
        root = Path.Combine(localeDirectory, "Audio");
        if (!enabled) return;
        using var doc = JsonFile.Read(Path.Combine(root, "manifest.json"),message=>MelonLogger.Warning(message));
        var entries = doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("entries", out var wrapped) ? wrapped : doc.RootElement;
        foreach (var entry in entries.EnumerateArray())
        {
            var clip = entry.TryGetProperty("source",out var source) ? source.GetString() : entry.GetProperty("clip").GetString();
            var file = entry.TryGetProperty("replacement",out var replacement) ? replacement.GetString() : entry.GetProperty("file").GetString();
            if (string.IsNullOrWhiteSpace(clip) || paths.ContainsKey(clip)) throw new FormatException("Empty/conflicting audio clip identity");
            var resolved = Path.GetFullPath(Path.Combine(root, file));
            if (!resolved.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(resolved)) throw new FileNotFoundException("Missing audio replacement", resolved);
            paths.Add(clip, resolved);
        }
        MelonLogger.Msg($"[Audio] Loaded {paths.Count} clip mappings");
        var current=++generation;
        foreach(var pair in paths) MelonCoroutines.Start(Preload(pair.Key,pair.Value,current));
    }
    public bool TryGet(string clipName, out AudioClip clip)
    {
        return cache.TryGetValue(clipName,out clip) && clip != null;
    }
    private IEnumerator Preload(string clipName,string path,int version)
    {
        UnityWebRequest request=null;
        try
        {
            var type = Path.GetExtension(path).ToLowerInvariant() switch { ".wav" => AudioType.WAV, ".mp3" => AudioType.MPEG, ".ogg" => AudioType.OGGVORBIS, _ => AudioType.UNKNOWN };
            if (type == AudioType.UNKNOWN) yield break;
            request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type);
            request.SendWebRequest();
        }
        catch(Exception ex) {request?.Dispose();MelonLogger.Warning("Audio preload setup: "+ex.Message);yield break;}
        while(!request.isDone) { if(version!=generation) {request.Abort();request.Dispose();yield break;} yield return null; }
        try {
            if(version!=generation || request.result != UnityWebRequest.Result.Success) {MelonLogger.Warning("Audio preload failed: "+request.error);yield break;}
            var clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null) yield break;
            clip.name = clipName + " [PvZTranslation]";
            UnityEngine.Object.DontDestroyOnLoad(clip);
            cache[clipName] = clip;
        }
        finally {request.Dispose();}
    }
    public void ClearCache()
    {
        generation++;
        foreach (var clip in cache.Values) if (clip != null) UnityEngine.Object.Destroy(clip);
        cache.Clear();
    }
}
