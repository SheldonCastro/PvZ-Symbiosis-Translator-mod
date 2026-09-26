using System;
using System.Collections.Generic;

namespace PvZSymbiosisTranslator.UI;

public enum NativeMenuFeature { Translation, Font, Textures, Audio, TranslatorMode }

public static class NativeTranslatorMenuPolicy
{
    public const string LauncherName = "PvZTranslator.Native.Languages";
    public const string WindowName = "PvZTranslator.Native.Menu";
    public const string LanguageName = "PvZTranslator.Native.Page.General.Language";
    public const string TranslationName = "PvZTranslator.Native.Page.General.Translation";
    public const string FontName = "PvZTranslator.Native.Page.General.Font";
    public const string TexturesName = "PvZTranslator.Native.Page.General.Textures";
    public const string AudioName = "PvZTranslator.Native.Page.General.Audio";
    public const string TranslatorModeName = "PvZTranslator.Native.Menu.TranslatorMode";
    public const string ReloadName = "PvZTranslator.Native.Menu.Reload";
    public const string ExportName = "PvZTranslator.Native.Menu.Export";
    public const string StatusName = "PvZTranslator.Native.Menu.Status";
    public const string TabPrefix = "PvZTranslator.Native.Tab.";
    public const string SummaryName = "PvZTranslator.Native.Menu.Summary";

    public static string FormatFeature(string locale, NativeMenuFeature feature, bool enabled)
    {
        var label = feature switch {
            NativeMenuFeature.Translation => "settings.translation",
            NativeMenuFeature.Font => "settings.font",
            NativeMenuFeature.Textures => "settings.textures",
            NativeMenuFeature.Audio => "settings.audio",
            NativeMenuFeature.TranslatorMode => "settings.translatorMode",
            _ => throw new ArgumentOutOfRangeException(nameof(feature))
        };
        var state = feature switch {
            NativeMenuFeature.Textures => enabled ? "settings.enabledPlural" : "settings.disabledPlural",
            NativeMenuFeature.Audio or NativeMenuFeature.TranslatorMode => enabled ? "settings.enabledMasculine" : "settings.disabledMasculine",
            _ => enabled ? "settings.enabled" : "settings.disabled"
        };
        return ModLabels.Get(locale,label) + ": " + ModLabels.Get(locale,state);
    }

    public static string FormatLanguage(string locale, string displayName)
        => ModLabels.Get(locale,"settings.language") + ": " + displayName;

    public static string NextLocale(IReadOnlyList<string> locales, string current)
    {
        if(locales==null || locales.Count==0) return current;
        for(var i=0;i<locales.Count;i++)
            if(string.Equals(locales[i],current,StringComparison.Ordinal)) return locales[(i+1)%locales.Count];
        return locales[0];
    }
}
