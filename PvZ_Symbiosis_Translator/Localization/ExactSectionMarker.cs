using System;
using System.Text.RegularExpressions;

namespace PvZSymbiosisTranslator.Localization;

public enum ExactSectionMarkerKind { None, Section, Subsection }

public static class ExactSectionMarker
{
    private static readonly Regex Section = new(
        @"\A={20} \[[^\[\]\r\n]+\] ={20}\z",
        RegexOptions.CultureInvariant);
    private static readonly Regex Subsection = new(
        @"\A-{10} \[[^\[\]\r\n]+\] -{10}\z",
        RegexOptions.CultureInvariant);

    public static ExactSectionMarkerKind Kind(string source)
    {
        if (source is null) return ExactSectionMarkerKind.None;
        if (Section.IsMatch(source)) return ExactSectionMarkerKind.Section;
        if (Subsection.IsMatch(source)) return ExactSectionMarkerKind.Subsection;
        return ExactSectionMarkerKind.None;
    }

    public static bool IsSection(string source) => Kind(source) == ExactSectionMarkerKind.Section;
    public static bool IsSubsection(string source) => Kind(source) == ExactSectionMarkerKind.Subsection;
    public static bool IsMarker(string source, string target) => Kind(source) != ExactSectionMarkerKind.None && target == "";

    public static bool LooksLikeMarker(string source)
    {
        if (string.IsNullOrEmpty(source)) return false;
        return Regex.IsMatch(source, @"\A(?:={5,}|-{5,}) \[[^\r\n]*\] (?:={5,}|-{5,})\z", RegexOptions.CultureInvariant);
    }

    public static void Validate(string source, string target)
    {
        var kind = Kind(source);
        if (kind != ExactSectionMarkerKind.None)
        {
            if (target != "") throw new FormatException("Section marker target must be an empty string");
            return;
        }
        if (LooksLikeMarker(source)) throw new FormatException("Malformed exact section marker");
    }
}
