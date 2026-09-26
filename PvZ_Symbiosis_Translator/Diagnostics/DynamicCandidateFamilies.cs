using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PvZSymbiosisTranslator.Diagnostics;

public static class DynamicCandidateFamilies
{
    private const string Affix = @"[%\u3400-\u9FFF\uF900-\uFAFF:：_+\-]{1,16}";
    private static readonly Regex Suffix = new(@"\A(?<value>\d+(?:[.,]\d+)?)(?<suffix>"+Affix+@")(?<zwsp>\u200B?)\z", RegexOptions.CultureInvariant);
    private static readonly Regex Prefix = new(@"\A(?<prefix>"+Affix+@")(?<value>\d+(?:[.,]\d+)?)(?<zwsp>\u200B?)\z", RegexOptions.CultureInvariant);
    public sealed class FamilyGroup
    {
        public string Family { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string[] Examples { get; set; }
        public string[] Values { get; set; }
        public long Occurrences { get; set; }
        public string SuggestedPattern { get; set; }
        public string SuggestedTarget { get; set; }
    }
    private sealed class Candidate
    {
        public string Name, Prefix, Suffix, Value, Source;
        public long Count;
    }
    public static IReadOnlyList<FamilyGroup> Build(IEnumerable<RuntimeStringCollector.Entry> entries)
    {
        var candidates = new List<Candidate>();
        foreach (var entry in entries) {
            if (entry?.Source == null) continue;
            var suffix = Suffix.Match(entry.Source);
            if (suffix.Success) {
                var unit = suffix.Groups["suffix"].Value;
                var value = suffix.Groups["value"].Value;
                var name = unit == "秒" ? "seconds" : unit == "倍" ? "multiplier" : unit == "%" ? "percentage" :
                    unit.Contains('槽') || unit.Contains('个') ? "slot_counter" : value.Contains('.') || value.Contains(',') ? "decimal_suffix" : "integer_suffix";
                candidates.Add(new Candidate { Name=name, Suffix=unit, Value=value, Source=entry.Source, Count=entry.Count });
                continue;
            }
            var prefix=Prefix.Match(entry.Source);
            if (prefix.Success) {
                var value=prefix.Groups["value"].Value;
                candidates.Add(new Candidate { Name=value.Contains('.') || value.Contains(',') ? "prefix_decimal" : "prefix_integer",
                    Prefix=prefix.Groups["prefix"].Value, Value=value, Source=entry.Source, Count=entry.Count });
            }
        }
        return candidates.GroupBy(x=>(x.Name,x.Prefix,x.Suffix)).Where(group=>group.Select(x=>x.Value).Distinct(StringComparer.Ordinal).Count()>1)
            .OrderBy(group=>group.Key.Name,StringComparer.Ordinal).ThenBy(group=>group.Key.Prefix,StringComparer.Ordinal).ThenBy(group=>group.Key.Suffix,StringComparer.Ordinal)
            .Select(group=> {
                var first=group.First();
                var decimalValue=group.Any(x=>x.Value.Contains('.') || x.Value.Contains(','));
                var number=decimalValue ? @"\d+(?:[.,]\d+)?" : @"\d+";
                var pattern=first.Prefix!=null ? "^"+Regex.Escape(first.Prefix)+"(?<value>"+number+")(?<zwsp>\\u200B?)$" :
                    "^(?<value>"+number+")"+Regex.Escape(first.Suffix)+"(?<zwsp>\\u200B?)$";
                var target=first.Name=="seconds" ? "${value} s${zwsp}" : first.Name=="multiplier" ? "${value}×${zwsp}" :
                    first.Name=="percentage" ? "${value}%${zwsp}" : null;
                return new FamilyGroup { Family=first.Name, Prefix=first.Prefix, Suffix=first.Suffix,
                    Examples=group.Select(x=>x.Source).Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).Take(12).ToArray(),
                    Values=group.Select(x=>x.Value).Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToArray(),
                    Occurrences=group.Sum(x=>x.Count), SuggestedPattern=pattern, SuggestedTarget=target };
            }).ToArray();
    }
}
