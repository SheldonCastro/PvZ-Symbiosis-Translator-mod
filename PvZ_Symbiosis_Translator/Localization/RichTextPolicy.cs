namespace PvZSymbiosisTranslator.Localization;

public static class RichTextPolicy
{
    // The renderer, not this policy, decides which TMP tags are supported.
    public static bool RequiresRichText(string source, string target, bool translationEnabled) =>
        translationEnabled && target != null && target != source && target.IndexOf('<') >= 0 && target.IndexOf('>') > target.IndexOf('<');

    public static bool Desired(bool original, string source, string target, bool translationEnabled) =>
        original || RequiresRichText(source, target, translationEnabled);
}
