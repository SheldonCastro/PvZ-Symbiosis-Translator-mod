using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using PvZSymbiosisTranslator.Configuration;
using PvZSymbiosisTranslator.Localization;

namespace PvZSymbiosisTranslator.Diagnostics;

public static class TranslationExportWriter
{
    public static bool IsDynamicCandidate(string source) => Regex.IsMatch(source, @"\A[0-9]+(?:[.,][0-9]+)?(?:秒|倍)\u200B?\z", RegexOptions.CultureInvariant);
    public static void Write(string root, string localeDirectory, RuntimeStringCollector collector, Func<string, string> translate)
    {
        var export = Path.Combine(root, "TranslationExport"); Directory.CreateDirectory(Path.Combine(export, "ByContext"));
        var entries = collector.Snapshot();
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        var catalogPath = Path.Combine(root, "Dumps", "source_catalog.json");
        if (File.Exists(catalogPath)) using (var catalog = JsonDocument.Parse(File.ReadAllText(catalogPath)))
            foreach (var item in catalog.RootElement.EnumerateArray())
                if (item.TryGetProperty("classification", out var classification) &&
                    new[] { "TECHNICAL", "CHARACTER_SET", "EXCLUDED", "TECHNICAL_IDENTIFIER", "GLYPH_TABLE" }.Contains(classification.GetString()))
                    excluded.Add(item.GetProperty("source").GetString());
        var previous = new Dictionary<string,string>(StringComparer.Ordinal);
        var pendingPath = Path.Combine(export, "pending.json");
        if (File.Exists(pendingPath)) using (var saved = JsonDocument.Parse(File.ReadAllText(pendingPath)))
            foreach (var item in saved.RootElement.EnumerateObject()) previous[item.Name] = item.Value.GetString();
        var pending = new Dictionary<string,string>(StringComparer.Ordinal);
        foreach (var entry in entries.OrderBy(e => e.Source, StringComparer.Ordinal)) if (!excluded.Contains(entry.Source) && translate(entry.Source) == entry.Source)
            pending[entry.Source] = previous.TryGetValue(entry.Source, out var target) && !string.IsNullOrWhiteSpace(target) ? target : "";
        AtomicJson(Path.Combine(export, "pending.json"), pending);
        var exact = new Dictionary<string,string>(StringComparer.Ordinal);
        foreach (var file in new[] { Path.Combine(localeDirectory,"Strings","exact.json"), Path.Combine(localeDirectory,"Almanac","exact.json") }) if (File.Exists(file)) using (var doc = JsonDocument.Parse(File.ReadAllText(file))) foreach (var p in doc.RootElement.EnumerateObject())
        {
            var target=p.Value.ValueKind==JsonValueKind.String?p.Value.GetString():null;
            if(!ExactSectionMarker.IsMarker(p.Name,target))exact[p.Name]=target;
        }
        AtomicJson(Path.Combine(export, "current_translations.json"), exact);
        AtomicJson(Path.Combine(export, "all_runtime_sources.json"), entries.Select(e => e.Source).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToList());
        AtomicJson(Path.Combine(export, "dynamic_candidates.json"), pending.Keys.Where(IsDynamicCandidate).OrderBy(s => s, StringComparer.Ordinal).ToList());
        AtomicJson(Path.Combine(export, "dynamic_families.json"), DynamicCandidateFamilies.Build(entries));
        var byContext = new Dictionary<string, Dictionary<string,string>>(StringComparer.Ordinal);
        foreach (var entry in entries.OrderBy(e => e.Source, StringComparer.Ordinal)) if (pending.ContainsKey(entry.Source)) foreach (var context in entry.Contexts.Count > 0 ? entry.Contexts : new List<RuntimeStringCollector.Context> { new RuntimeStringCollector.Context { Scene = entry.Scene } })
        {
            var key = string.IsNullOrWhiteSpace(context.Scene) ? "Unknown" : context.Scene;
            if (!byContext.TryGetValue(key, out var map)) { map = new Dictionary<string,string>(StringComparer.Ordinal); byContext[key] = map; }
            map[entry.Source] = pending[entry.Source];
        }
        foreach (var pair in byContext.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var sorted = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in pair.Value.OrderBy(p => p.Key, StringComparer.Ordinal)) sorted[item.Key] = item.Value;
            AtomicJson(Path.Combine(export, "ByContext", pair.Key + ".json"), sorted);
        }
        File.WriteAllText(Path.Combine(export, "README.txt"), "Generated view from Dumps and the active locale. Edit source packs, then regenerate from Translator Settings or with PageDown.\r\n");
    }
    private static void AtomicJson(string path, object value)
    {
        var tmp = path + ".tmp"; File.WriteAllText(tmp, JsonSerializer.Serialize(value, ConfigManager.HumanReadableJson));
        using (JsonDocument.Parse(File.ReadAllText(tmp))) { }
        File.Move(tmp, path, true);
    }
}
