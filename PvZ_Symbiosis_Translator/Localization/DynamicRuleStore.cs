using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PvZSymbiosisTranslator.Localization;

public sealed class DynamicRuleStore
{
    private readonly List<(string id, Regex regex, string target)> rules = new();
    private readonly HashSet<string> ids = new(StringComparer.Ordinal);
    private readonly HashSet<string> reportedFailures = new(StringComparer.Ordinal);
    public Action<string, string, string> FailureDiagnostic { get; set; }
    public int Count => rules.Count;
    public void Add(string id, string pattern, string target)
    {
        if (rules.Count >= 64 || pattern?.Length > 2048) throw new FormatException("Dynamic rule budget exceeded");
        if (string.IsNullOrWhiteSpace(id) || ids.Contains(id)) throw new FormatException("Missing/duplicate dynamic ID");
        if (pattern == null || !pattern.StartsWith("^", StringComparison.Ordinal) || !pattern.EndsWith("$", StringComparison.Ordinal)) throw new FormatException("Dynamic regex must be anchored ^...$");
        if (string.IsNullOrWhiteSpace(target)) throw new FormatException("Empty dynamic target");
        var regex = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(20));
        var names = regex.GetGroupNames().Where(n => n != "0").ToHashSet(StringComparer.Ordinal);
        var used = Regex.Matches(target, @"\$\{([a-zA-Z][a-zA-Z0-9_]*)\}").Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        if (names.Count == 0 || !names.SetEquals(used)) throw new FormatException("Dynamic targets must reference every named group and no unknown group");
        if (Regex.Replace(target, @"\$\{[a-zA-Z][a-zA-Z0-9_]*\}", "").Contains('$')) throw new FormatException("Unsupported dynamic replacement token");
        ids.Add(id);
        rules.Add((id, regex, target));
    }
    public bool TryTranslate(string source, out string target)
        => TryTranslate(source, out target, out _);
    public bool TryTranslate(string source, out string target, out string ruleId)
    {
        target = null;
        ruleId = null;
        foreach (var rule in rules)
        {
            try
            {
                var match = rule.regex.Match(source);
                if (!match.Success || match.Index != 0 || match.Length != source.Length) continue;
                var result = match.Result(rule.target);
                TranslationValidator.Validate(source, result);
                target = result;
                ruleId = rule.id;
                return true;
            }
            catch (RegexMatchTimeoutException) { Report(rule.id, source, "regex timeout"); }
            catch (FormatException ex) { Report(rule.id, source, "invalid result: " + ex.Message); }
        }
        return false;
    }
    private void Report(string id, string source, string failure)
    {
        if (reportedFailures.Add(id + ":" + failure)) FailureDiagnostic?.Invoke(id, source, failure);
    }
}
