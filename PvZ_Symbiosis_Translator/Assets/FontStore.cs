using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using Il2CppTMPro;
using UnityEngine;
using MelonLoader;
using PvZSymbiosisTranslator.Localization;

namespace PvZSymbiosisTranslator.Assets;

public sealed class FontStore
{
    private readonly Dictionary<string, TMP_FontAsset> cache = new(StringComparer.Ordinal);
    private sealed class FontState { public TMP_Text Component; public TMP_FontAsset Original; public TMP_FontAsset Applied; }
    private readonly Dictionary<int, FontState> states = new();
    private readonly List<TMP_FontAsset> activeFallbacks = new();
    private readonly List<TMP_FontAsset> replacementFallbacksAdded = new();
    private TMP_FontAsset primaryReplacement;
    private bool enabled;
    public bool Ready => primaryReplacement != null || activeFallbacks.Count > 0;
    public int AppliedCount { get { PruneDead(); return states.Count; } }
    public int FallbackCount => activeFallbacks.Count;
    public TMP_FontAsset PrimaryReplacement => primaryReplacement;
    public void SetEnabled(bool value) { if (!value) RestoreAll(); enabled = value && Ready; }
    public void Load(string localeDirectory, bool shouldEnable)
    {
        RestoreAll();
        activeFallbacks.Clear(); primaryReplacement = null; replacementFallbacksAdded.Clear(); enabled = false;
        // Settings still needs its fonts when replacement on game text is disabled.
        if (File.Exists(Path.Combine(localeDirectory, "Fonts", "manifest.json")))
        {
            var manifestPath = Path.Combine(localeDirectory, "Fonts", "manifest.json");
            using var manifest = JsonFile.Read(manifestPath,message=>MelonLogger.Warning(message));
            foreach (var entry in manifest.RootElement.EnumerateArray().OrderByDescending(e => e.TryGetProperty("priority", out var p) ? p.GetInt32() : 0))
            {
                var path = Path.GetFullPath(Path.Combine(localeDirectory, "Fonts", entry.GetProperty("file").GetString()));
                var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
                if (!cache.TryGetValue(hash, out var fontAsset))
                {
                    fontAsset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                    if (fontAsset == null) throw new InvalidOperationException("TMP_FontAsset creation failed: " + path);
                    UnityEngine.Object.DontDestroyOnLoad(fontAsset); cache.Add(hash, fontAsset);
                }
                if (fontAsset == null) throw new InvalidOperationException("TMP_FontAsset creation failed: " + path);
                var mode = entry.TryGetProperty("applyMode", out var m) ? m.GetString() : "fallback";
                if (mode == "replace" && primaryReplacement == null) primaryReplacement = fontAsset; else if (mode == "fallback") activeFallbacks.Add(fontAsset);
            }
            enabled = shouldEnable && Ready;
            MelonLogger.Msg($"[Fonts] Loaded {activeFallbacks.Count} fallbacks; primary replacement: {(primaryReplacement == null ? "none" : "configured")}");
        }
    }
    public void Apply(TMP_Text text)
    {
        ApplyCore(text,false);
    }
    public void ApplyTranslatorUi(TMP_Text text)
    {
        ApplyCore(text,true);
    }
    private void ApplyCore(TMP_Text text,bool force)
    {
        if ((!force && !enabled) || !Ready || text == null || text.font == null) return;
        var font = text.font;
        if (!states.TryGetValue(text.GetInstanceID(), out var state)) { state = new FontState { Component = text, Original = font }; states[text.GetInstanceID()] = state; }
        if (primaryReplacement != null)
        {
            if (primaryReplacement.fallbackFontAssetTable == null) primaryReplacement.fallbackFontAssetTable = new Il2CppSystem.Collections.Generic.List<TMP_FontAsset>();
            // Repeated refresh sees the replacement in text.font. Never add it to its own fallback table.
            var original = state.Original;
            if (original != null && original != primaryReplacement && !primaryReplacement.fallbackFontAssetTable.Contains(original)) { primaryReplacement.fallbackFontAssetTable.Add(original); replacementFallbacksAdded.Add(original); }
            foreach (var fallback in activeFallbacks) if (!primaryReplacement.fallbackFontAssetTable.Contains(fallback)) { primaryReplacement.fallbackFontAssetTable.Add(fallback); replacementFallbacksAdded.Add(fallback); }
            text.font = primaryReplacement; state.Applied = primaryReplacement; return;
        }
        if (font.fallbackFontAssetTable == null) font.fallbackFontAssetTable = new Il2CppSystem.Collections.Generic.List<TMP_FontAsset>();
        foreach (var fallback in activeFallbacks) if (!font.fallbackFontAssetTable.Contains(fallback)) font.fallbackFontAssetTable.Add(fallback);
        state.Applied = null;
    }
    public void PruneDead()
    {
        foreach (var id in states.Keys.ToArray())
            if (states[id].Component == null) states.Remove(id);
    }
    private void RestoreAll()
    {
        foreach (var state in states.Values)
        {
            if (state.Component == null) continue;
            if (state.Applied != null && state.Component.font == state.Applied) state.Component.font = state.Original;
            if (state.Original?.fallbackFontAssetTable != null) foreach (var fallback in activeFallbacks) state.Original.fallbackFontAssetTable.Remove(fallback);
        }
        if (primaryReplacement?.fallbackFontAssetTable != null) foreach (var fallback in replacementFallbacksAdded) primaryReplacement.fallbackFontAssetTable.Remove(fallback);
        replacementFallbacksAdded.Clear();
        states.Clear();
        enabled = false;
    }
}
