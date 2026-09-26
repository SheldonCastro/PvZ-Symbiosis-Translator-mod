using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using PvZSymbiosisTranslator.Core;

namespace PvZSymbiosisTranslator.Localization;

public sealed class LanguagePack
{
    public string Locale { get; init; }
    public PackValidationReport Validation { get; init; }
    public ExactStringStore Exact { get; } = new();
    public Dictionary<string, ExactStringStore> Contexts { get; } = new(StringComparer.Ordinal);
    public DynamicRuleStore Dynamic { get; } = new();
}

public static class LocaleManager
{
    public sealed class LanguageInfo
    {
        public string Locale { get; }
        public string DisplayName { get; }
        public LanguageInfo(string locale, string displayName) { Locale = locale; DisplayName = displayName; }
    }
    public static IReadOnlyList<LanguageInfo> AvailableLanguageInfos(string localizationRoot, Action<string> warning = null)
    {
        var result = new List<LanguageInfo>();
        if (!Directory.Exists(localizationRoot)) return result;
        foreach (var dir in Directory.GetDirectories(localizationRoot))
        {
            var folder = Path.GetFileName(dir);
            if (folder == "_template") continue;
            try {
                if (!ModPaths.IsValidLocale(folder)) throw new FormatException("invalid locale directory name");
                using var doc = JsonFile.Read(Path.Combine(dir, "manifest.json"));
                var root = doc.RootElement;
                var locale = root.GetProperty("locale").GetString();
                var displayName = root.GetProperty("displayName").GetString();
                if (!ModPaths.IsValidLocale(locale) || locale != folder || string.IsNullOrWhiteSpace(displayName))
                    throw new FormatException("manifest locale/displayName does not match directory");
                result.Add(new LanguageInfo(locale, displayName));
            }
            catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException or KeyNotFoundException)
            { warning?.Invoke($"Skipping locale directory {folder}: {ex.Message}"); }
        }
        result.Sort((a,b) => StringComparer.Ordinal.Compare(a.Locale,b.Locale));
        return result;
    }
    public static IReadOnlyList<string> AvailableLanguages(string localizationRoot)
        => new List<string>(AvailableLanguageInfos(localizationRoot).Select(x => x.Locale));
    public static string DisplayName(string localizationRoot, string locale)
        => AvailableLanguageInfos(localizationRoot).FirstOrDefault(x => x.Locale == locale)?.DisplayName ?? locale;
    public static LanguagePack Load(string directory, string locale, string gameVersion, bool validateOptionalAssets = true, string validationReportPath = null)
    {
        var report = new PackValidationReport(locale);
        try { var pack = LoadCore(directory, locale, gameVersion, validateOptionalAssets, report);
              report.WriteIfRequested(validationReportPath);
              return pack; }
        catch (Exception ex) {
            report.Add(ValidationSeverity.Fatal, "pack", null, null, ex.Message);
            report.WriteIfRequested(validationReportPath);
            throw;
        }
    }
    private static LanguagePack LoadCore(string directory, string locale, string gameVersion, bool validateOptionalAssets, PackValidationReport report)
    {
        using var manifest = Read(Path.Combine(directory, "manifest.json"),report);
        var m = manifest.RootElement;
        var manifestFields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in m.EnumerateObject())
            if (!manifestFields.Add(field.Name)) throw new FormatException("Duplicate manifest field");
        if (m.GetProperty("locale").GetString() != locale || m.GetProperty("sourceLocale").GetString() != "zh-CN" ||
            string.IsNullOrWhiteSpace(m.GetProperty("displayName").GetString()) || !Version.TryParse(m.GetProperty("version").GetString(), out _))
            throw new FormatException("Invalid locale manifest");
        if (m.TryGetProperty("supportedGameVersions", out var supported)) {
            if (supported.ValueKind != JsonValueKind.Array) throw new FormatException("supportedGameVersions must be an array");
            var versions=new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in supported.EnumerateArray()) {
                if (value.ValueKind != JsonValueKind.String || !Version.TryParse(value.GetString(),out _) || !versions.Add(value.GetString()))
                    throw new FormatException("Invalid/duplicate supportedGameVersions entry");
            }
            if (versions.Count == 0 || !versions.Contains(gameVersion)) throw new FormatException("Incompatible game version");
        } else if (!m.TryGetProperty("gameVersion",out var legacy) || legacy.ValueKind != JsonValueKind.String || legacy.GetString() != gameVersion)
            throw new FormatException("Invalid/incompatible locale manifest");
        var pack = new LanguagePack { Locale = locale, Validation = report };
        using (var glossary = Read(Path.Combine(directory, "glossary.json"),report))
            foreach (var term in glossary.RootElement.EnumerateObject())
                if (string.IsNullOrWhiteSpace(term.Value.GetString())) throw new FormatException("Empty glossary target");
        LoadExact(directory, Path.Combine(directory, "Strings", "exact.json"), pack.Exact, report);
        LoadExact(directory, Path.Combine(directory, "Almanac", "exact.json"), pack.Exact, report);
        using (var contexts = Read(Path.Combine(directory, "Strings", "context_overrides.json"),report))
            foreach (var context in contexts.RootElement.EnumerateObject())
            {
                if (context.Value.ValueKind != JsonValueKind.Object) throw new FormatException("Context must contain an object of exact mappings: " + context.Name);
                var store = new ExactStringStore();
                foreach (var entry in context.Value.EnumerateObject())
                    LoadEntry(store, report, "Strings/context_overrides.json#" + context.Name, entry);
                if (!pack.Contexts.TryAdd(context.Name, store)) throw new FormatException("Duplicate context");
            }
        using (var dynamic = Read(Path.Combine(directory, "Strings", "dynamic_rules.json"),report))
            foreach (var rule in dynamic.RootElement.EnumerateArray())
            {
                string id = null, target = null;
                try {
                    if (rule.ValueKind != JsonValueKind.Object) throw new FormatException("Dynamic rule must be an object");
                    id = rule.GetProperty("id").GetString();
                    var pattern = rule.GetProperty("pattern").GetString();
                    target = rule.GetProperty("target").GetString();
                    pack.Dynamic.Add(id, pattern, target);
                    report.Loaded++;
                }
                catch (Exception ex) when (ex is FormatException or KeyNotFoundException or InvalidOperationException or ArgumentException) {
                    report.Rejected++;
                    report.Add(ValidationSeverity.Error, "Strings/dynamic_rules.json", id, target, ex.Message);
                }
            }
        foreach (string kind in validateOptionalAssets ? new[] { "Fonts", "Textures", "Audio" } : Array.Empty<string>())
        {
            using var assets = Read(Path.Combine(directory, kind, "manifest.json"),report);
            var assetRoot = assets.RootElement.ValueKind == JsonValueKind.Object && assets.RootElement.TryGetProperty("entries", out var entries) ? entries : assets.RootElement;
            if (assetRoot.ValueKind != JsonValueKind.Array) throw new FormatException(kind + " manifest must be an array or {entries:[]}");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var asset in assetRoot.EnumerateArray())
            {
                var id = kind == "Audio" && asset.TryGetProperty("source",out var sourceId) ? sourceId.GetString() : asset.GetProperty("id").GetString();
                if (string.IsNullOrWhiteSpace(id) || !keys.Add(id)) throw new FormatException("Empty/conflicting asset ID");
                var root = Path.GetFullPath(Path.Combine(directory, kind)) + Path.DirectorySeparatorChar;
                var path = Path.GetFullPath(Path.Combine(root, asset.TryGetProperty("replacement", out var replacementFile) ? replacementFile.GetString() : asset.GetProperty("file").GetString()));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw new FormatException("Missing/outside pack asset: " + path);
                if (kind == "Fonts")
                {
                    var distribution = asset.TryGetProperty("distribution", out var d) ? d.GetString() : "bundled";
                    if (distribution == "localOnly") continue;
                    if (!asset.TryGetProperty("license", out var l) || string.IsNullOrWhiteSpace(l.GetString())) throw new FormatException("Missing font license");
                    var license = Path.GetFullPath(Path.Combine(root, l.GetString()));
                    if (!license.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(license)) throw new FormatException("Missing font license");
                }
            }
        }
        return pack;
    }
    private static JsonDocument Read(string path,PackValidationReport report)
    {
        return JsonFile.Read(path,message=>report?.Add(ValidationSeverity.Warning,Path.GetFileName(path),null,null,message));
    }
    private static void LoadExact(string directory, string path, ExactStringStore store, PackValidationReport report)
    {
        using var doc = Read(path,report);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new FormatException("Exact mappings must be a JSON object: " + path);
        var relative = Path.GetRelativePath(directory, path).Replace('\\', '/');
        foreach (var entry in doc.RootElement.EnumerateObject())
        {
            var target = entry.Value.ValueKind == JsonValueKind.String ? entry.Value.GetString() : null;
            try
            {
                ExactSectionMarker.Validate(entry.Name, target);
                if (ExactSectionMarker.IsMarker(entry.Name, target)) continue;
                LoadEntry(store, report, relative, entry);
            }
            catch (FormatException ex)
            {
                report.Rejected++;
                report.Add(ValidationSeverity.Error, relative, entry.Name, target, ex.Message);
            }
        }
    }
    private static void LoadEntry(ExactStringStore store, PackValidationReport report, string file, JsonProperty entry)
    {
        var source = entry.Name;
        var target = entry.Value.ValueKind == JsonValueKind.String ? entry.Value.GetString() : null;
        try {
            var warnings = TranslationValidator.Warnings(source, target);
            if (store.TryGet(source, out var previous)) {
                if (previous != target) throw new FormatException("Conflicting mapping; first valid target retained");
                return;
            }
            store.Add(source, target);
            report.Loaded++;
            foreach (var warning in warnings) report.Add(ValidationSeverity.Warning, file, source, target, warning);
        }
        catch (FormatException ex) {
            report.Rejected++;
            report.Add(ValidationSeverity.Error, file, source, target, ex.Message);
        }
    }
}
