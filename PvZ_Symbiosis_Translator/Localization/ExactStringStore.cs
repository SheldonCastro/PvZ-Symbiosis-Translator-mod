using System;
using System.Collections.Generic;

namespace PvZSymbiosisTranslator.Localization;

public sealed class ExactStringStore
{
    private readonly Dictionary<string, string> entries = new(StringComparer.Ordinal);
    public int Count => entries.Count;
    public IReadOnlyDictionary<string,string> Entries => entries;
    public void Add(string source, string target)
    {
        ExactSectionMarker.Validate(source, target);
        if (ExactSectionMarker.IsMarker(source, target)) return;
        TranslationValidator.Validate(source, target);
        if (entries.TryGetValue(source, out var previous) && previous != target)
            throw new FormatException("Conflicting mapping: " + source);
        entries[source] = target;
    }
    public bool TryGet(string source, out string target)
    {
        target = null;
        return source != null && entries.TryGetValue(source, out target);
    }
    public string Translate(string source) => TryGet(source, out var target) ? target : source;
}
