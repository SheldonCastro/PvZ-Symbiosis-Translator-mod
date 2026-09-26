using System;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace PvZSymbiosisTranslator.Configuration;

public sealed class ModConfig
{
    public string Language { get; set; } = "pt-BR";
    public bool Enabled { get; set; } = true;
    public bool DynamicRules { get; set; } = true;
    public bool TrackUntranslated { get; set; } = true;
    public bool FontReplacement { get; set; } = false;
    public bool TextureReplacement { get; set; } = false;
    public bool AudioReplacement { get; set; } = false;
    public bool TranslatorMode { get; set; } = false;
    public bool DebugLogging { get; set; } = false;
    public bool AutoReload { get; set; } = false;
    public bool ValidateBeforeApply { get; set; } = true;
    public bool CaptureUnknown { get; set; } = true;
    public bool AutoExportOnContextChange { get; set; } = false;
    public bool DetailedContext { get; set; } = true;
    public bool UiNotifications { get; set; } = true;
    public float UiScalePercent { get; set; } = 100f;
    public string ToggleTranslationKey { get; set; } = "Insert";
    public string ReloadTranslationKey { get; set; } = "PageUp";
    public string DiagnosticKey { get; set; } = "PageDown";
    public ModConfig Clone() => new() { Language = Language, Enabled = Enabled, DynamicRules = DynamicRules, TrackUntranslated = TrackUntranslated, FontReplacement = FontReplacement, TextureReplacement = TextureReplacement, AudioReplacement = AudioReplacement, TranslatorMode = TranslatorMode, DebugLogging = DebugLogging, AutoReload = AutoReload, ValidateBeforeApply = ValidateBeforeApply, CaptureUnknown = CaptureUnknown, AutoExportOnContextChange = AutoExportOnContextChange, DetailedContext = DetailedContext, UiNotifications = UiNotifications, UiScalePercent = UiScalePercent, ToggleTranslationKey = ToggleTranslationKey, ReloadTranslationKey = ReloadTranslationKey, DiagnosticKey = DiagnosticKey };
}

public static class ConfigManager
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    public static readonly JsonSerializerOptions HumanReadableJson = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
    public static ModConfig Load(string path, Action<string> migrated = null)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonSerializer.Serialize(new ModConfig(), Json));
        }
        var config = JsonSerializer.Deserialize<ModConfig>(File.ReadAllText(path), Json) ?? throw new FormatException("Null config");
        bool changed=false;
        if(config.ToggleTranslationKey=="F9") { config.ToggleTranslationKey="Insert"; changed=true; }
        if(config.ReloadTranslationKey=="F10") { config.ReloadTranslationKey="PageUp"; changed=true; }
        if(config.DiagnosticKey=="F8") { config.DiagnosticKey="PageDown"; changed=true; }
        var clampedScale=Math.Clamp(config.UiScalePercent,85f,115f);
        if(Math.Abs(config.UiScalePercent-clampedScale)>.001f) { config.UiScalePercent=clampedScale; changed=true; }
        if(changed) { Save(path,config); migrated?.Invoke("Legacy default hotkeys migrated to Insert/PageUp/PageDown"); }
        return config;
    }
    public static void Save(string path, ModConfig config)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(config, Json));
        File.Move(temp, path, true);
    }
}
