using System;
namespace PvZSymbiosisTranslator.Core;

public static class TextRefreshPolicy
{
    public static bool IsTranslatorName(string name) => name == "PvZTranslationUI" ||
        name == "PvZTranslationSettingsRoot" || (name?.StartsWith("PvZTranslationToast", StringComparison.Ordinal) ?? false) ||
        (name?.StartsWith("PvZTranslator.Native.", StringComparison.Ordinal) ?? false);

    public static bool ShouldRefresh(bool alive, bool active, bool translatorOwned, bool currentScene, bool openModal)
        => alive && active && !translatorOwned && (currentScene || openModal);
}
