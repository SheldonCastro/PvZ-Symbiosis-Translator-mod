using System;
using System.Collections.Generic;
using System.Linq;

namespace PvZSymbiosisTranslator.Localization;

public static class DynamicExactAudit
{
    public sealed class Match
    {
        public string RuleId { get; }
        public string Source { get; }
        public string ExactTarget { get; }
        public string DynamicTarget { get; }
        public Match(string ruleId, string source, string exactTarget, string dynamicTarget)
        { RuleId=ruleId; Source=source; ExactTarget=exactTarget; DynamicTarget=dynamicTarget; }
        public bool Redundant => string.Equals(ExactTarget, DynamicTarget, StringComparison.Ordinal);
    }

    public static IReadOnlyList<Match> Inspect(IEnumerable<KeyValuePair<string, string>> exact, DynamicRuleStore rules)
    {
        var matches = new List<Match>();
        foreach (var entry in exact.OrderBy(e => e.Key, StringComparer.Ordinal))
            if (rules.TryTranslate(entry.Key, out var generated, out var ruleId))
                matches.Add(new Match(ruleId, entry.Key, entry.Value, generated));
        return matches;
    }
}
