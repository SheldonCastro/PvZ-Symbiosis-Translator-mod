using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using PvZSymbiosisTranslator.Configuration;

namespace PvZSymbiosisTranslator.Diagnostics;

public sealed class RuntimeStringCollector
{
    public sealed class Entry
    {
        public string Source { get; set; }
        public string Scene { get; set; }
        public string Object { get; set; }
        public string Component { get; set; }
        public string Hierarchy { get; set; }
        public long Count { get; set; }
        public string FirstSeen { get; set; }
        public string LastSeen { get; set; }
        public List<Context> Contexts { get; set; } = new();
        public string EscapedSource => DiagnosticString.EscapeInvisible(Source);
    }
    public sealed class Context
    {
        public string Scene { get; set; }
        public string Object { get; set; }
        public string Component { get; set; }
        public string Hierarchy { get; set; }
        public long Count { get; set; }
    }
    private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
    public int Count => entries.Count;
    public void Load(string directory)
    {
        var path = Path.Combine(directory, "runtime_strings.json");
        if (!File.Exists(path)) return;
        var saved = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(path), ConfigManager.Json);
        if (saved == null) return;
        foreach (var entry in saved)
            if (!string.IsNullOrEmpty(entry.Source) && entry.Count > 0 && entry.Object != "PvZTranslationSmokeProbe") { entry.Contexts ??= new(); entries.TryAdd(entry.Source, entry); }
    }
    public void Record(string source, string scene, string name, string component, string hierarchy)
    {
        if (!HanSourceDetector.ContainsHan(source)) return;
        var now = DateTimeOffset.UtcNow.ToString("O");
        if (!entries.TryGetValue(source, out var entry)) { entry = new Entry { Source = source, Scene = scene, Object = name, Component = component, Hierarchy = hierarchy, Count = 0, FirstSeen = now }; entries.Add(source, entry); }
        entry.Count++; entry.LastSeen = now;
        var context = entry.Contexts.FirstOrDefault(x => x.Scene == scene && x.Object == name && x.Component == component && x.Hierarchy == hierarchy);
        if (context == null) { if (entry.Contexts.Count >= 32) entry.Contexts.RemoveAt(0); context = new Context { Scene = scene, Object = name, Component = component, Hierarchy = hierarchy }; entry.Contexts.Add(context); }
        context.Count++;
    }
    public void Save(string directory)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "runtime_strings.json");
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(entries.Values.OrderBy(e => e.Source, StringComparer.Ordinal), ConfigManager.HumanReadableJson));
        File.Move(path + ".tmp", path, true);
    }
    public IReadOnlyList<Entry> Snapshot() => entries.Values.OrderBy(e => e.Source, StringComparer.Ordinal).ToList();
    public void SaveReports(string directory, Func<string, bool> translated, ISet<string> knownSources)
    {
        Directory.CreateDirectory(directory);
        var all = Snapshot();
        var known = all.Where(e => knownSources.Contains(e.Source) && !translated(e.Source)).ToList();
        var unknown = all.Where(e => !knownSources.Contains(e.Source) && !translated(e.Source)).ToList();
        var done = all.Where(e => translated(e.Source)).ToList();
        void Write(string name, object value) => File.WriteAllText(Path.Combine(directory, name), JsonSerializer.Serialize(value, ConfigManager.HumanReadableJson));
        Write("untranslated_known.json", known); Write("unknown_runtime_sources.json", unknown);
        Write("translation_coverage.json", new { rawCandidates = all.Count, translated = done.Count, knownUntranslated = known.Count, unknownRuntime = unknown.Count, coverage = all.Count == 0 ? 1d : (double)done.Count / all.Count });
        var todo = known.Select(e => new { source = e.Source, target = "", status = "KNOWN_UNTRANSLATED", context = e.Contexts.FirstOrDefault()?.Scene ?? e.Scene })
            .Concat(unknown.Select(e => new { source = e.Source, target = "", status = "UNKNOWN_RUNTIME_SOURCE", context = e.Contexts.FirstOrDefault()?.Scene ?? e.Scene }));
        Write("to_translate.json", todo);
    }
}
