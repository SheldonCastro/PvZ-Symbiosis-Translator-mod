using System;
using System.IO;
using System.Text.RegularExpressions;

namespace PvZSymbiosisTranslator.Core;

public sealed class ModPaths
{
    public string Root { get; }
    public string ConfigFile => Path.Combine(Root, "Config", "translation_config.json");
    public string Dumps => Path.Combine(Root, "Dumps");
    public ModPaths(string modsDirectory) { Root = Path.Combine(modsDirectory, "PvZ_Symbiosis_Translator"); }
    public void EnsureDirectories()
    {
        foreach (var name in new[] { "Config", "Localization", "Dumps", "Logs", "Cache", "TranslationExport" }) Directory.CreateDirectory(Path.Combine(Root, name));
    }
    public static bool IsValidLocale(string locale) => locale != null &&
        Regex.IsMatch(locale, @"\A[a-z]{2,3}(?:-[A-Za-z0-9]{2,8})+\z", RegexOptions.CultureInvariant);
    public string Locale(string locale)
    {
        if (!IsValidLocale(locale)) throw new FormatException("Invalid locale identifier");
        return Path.Combine(Root, "Localization", locale);
    }
}
