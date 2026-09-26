using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PvZSymbiosisTranslator.Localization;

public static class TranslationValidator
{
    // Functional placeholders only. Numbers, line breaks and presentation belong to the target locale.
    private static readonly Regex Placeholders = new(@"\{\d+(?:,[^{}:]+)?(?::[^{}]+)?\}|(?<!\d)%(?:\d+\$)?[-+0 #]*\d*(?:\.\d+)?[sdifouxXeEgGc%]", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly Regex Numbers = new(@"(?<![0-9])[0-9]+(?:[.,][0-9]+)?(?![0-9])", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly Regex Tag = new(@"\A<(?<end>/)?(?<name>[A-Za-z][A-Za-z0-9-]*)(?:\s[^<>]*|=[^<>]*)?(?<self>/)?>\z", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    // This list is advisory. Unknown or newer TMP tags are passed through unchanged.
    private static readonly HashSet<string> KnownTags = new(StringComparer.OrdinalIgnoreCase) {
        "size", "color", "alpha", "b", "i", "u", "s", "align", "font", "font-weight", "gradient", "mark",
        "uppercase", "lowercase", "smallcaps", "sub", "sup", "cspace", "mspace", "space", "voffset", "pos",
        "indent", "line-indent", "line-height", "margin", "margin-left", "margin-right", "width", "nobr", "noparse",
        "rotate", "sprite", "link", "style", "page", "br", "quad", "allcaps"
    };

    public static void Validate(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) throw new FormatException("Source key must not be empty");
        if (target == null) throw new FormatException("Target must not be null");
        if (target.Length > 0 && string.IsNullOrWhiteSpace(target)) throw new FormatException("Whitespace-only target is not a useful translation; use an empty string to hide text");
        var expected = PlaceholderCounts(source);
        var actual = PlaceholderCounts(target);
        if (expected.Count != actual.Count || expected.Any(pair => !actual.TryGetValue(pair.Key, out var count) || count != pair.Value))
            throw new FormatException("Functional placeholder identity/count mismatch");
    }

    public static IReadOnlyList<string> Warnings(string source, string target)
    {
        Validate(source, target);
        var warnings = new List<string>();
        if (target.Length == 0) warnings.Add("Empty target intentionally hides the source text.");
        warnings.AddRange(InspectMarkup(target));
        var sourceNumbers = NumberValues(WithoutMarkup(source));
        var targetNumbers = NumberValues(WithoutMarkup(target));
        sourceNumbers.Sort(StringComparer.Ordinal);
        targetNumbers.Sort(StringComparer.Ordinal);
        if (sourceNumbers.Count > 0 && targetNumbers.Count > 0 && !sourceNumbers.SequenceEqual(targetNumbers, StringComparer.Ordinal))
            warnings.Add("Possible mechanic/value change; review numbers in context.");
        return warnings;
    }

    private static Dictionary<string, int> PlaceholderCounts(string text)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match match in Placeholders.Matches(text)) {
            result.TryGetValue(match.Value, out var count);
            result[match.Value] = count + 1;
        }
        return result;
    }

    private static List<string> NumberValues(string text) => Numbers.Matches(text).Select(m => m.Value.Replace(',', '.')).ToList();

    // Advisory lexing only. It does not implement TMP rendering or reject state-style tags.
    private static IEnumerable<string> InspectMarkup(string text)
    {
        var warnings = new List<string>();
        var stack = new Stack<string>();
        for (int cursor = 0; cursor < text.Length;) {
            if (text[cursor] != '<') { cursor++; continue; }
            var end = text.IndexOf('>', cursor + 1);
            if (end < 0) { warnings.Add("Unclosed angle bracket; check rich-text markup."); break; }
            var raw = text.Substring(cursor, end - cursor + 1);
            var match = Tag.Match(raw);
            cursor = end + 1;
            if (!match.Success) { warnings.Add("Unusual rich-text syntax: " + raw); continue; }
            var name = match.Groups["name"].Value.ToLowerInvariant();
            if (!KnownTags.Contains(name)) warnings.Add("Unknown TMP tag; verify in game: " + name);
            if (name == "noparse" && !match.Groups["end"].Success) {
                var close = text.IndexOf("</noparse>", cursor, StringComparison.OrdinalIgnoreCase);
                if (close < 0) { warnings.Add("Unclosed noparse tag."); break; }
                cursor = close + "</noparse>".Length;
                continue;
            }
            if (match.Groups["end"].Success) {
                if (stack.Count > 0 && stack.Peek() == name) stack.Pop();
                else warnings.Add("Unmatched closing rich-text tag: " + name);
            }
            else if (!match.Groups["self"].Success && name is not ("sprite" or "br" or "space" or "page" or "pos" or "quad")) stack.Push(name);
        }
        if (stack.Count > 0) warnings.Add("Rich-text tag may be unclosed: " + stack.Peek());
        return warnings;
    }

    private static string WithoutMarkup(string text)
    {
        var output = new System.Text.StringBuilder();
        for (var cursor = 0; cursor < text.Length;) {
            if (text[cursor] != '<') { output.Append(text[cursor++]); continue; }
            var end = text.IndexOf('>', cursor + 1);
            if (end < 0) { output.Append(text.AsSpan(cursor)); break; }
            var match = Tag.Match(text.Substring(cursor, end - cursor + 1));
            if (!match.Success) { output.Append(text[cursor++]); continue; }
            cursor = end + 1;
            if (match.Groups["name"].Value.Equals("noparse", StringComparison.OrdinalIgnoreCase) && !match.Groups["end"].Success) {
                var close = text.IndexOf("</noparse>", cursor, StringComparison.OrdinalIgnoreCase);
                if (close < 0) { output.Append(text.AsSpan(cursor)); break; }
                output.Append(text.AsSpan(cursor, close - cursor));
                cursor = close + "</noparse>".Length;
            }
        }
        return output.ToString();
    }
}
