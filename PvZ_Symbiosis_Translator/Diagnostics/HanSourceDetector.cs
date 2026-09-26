namespace PvZSymbiosisTranslator.Diagnostics;

public static class HanSourceDetector
{
    public static bool ContainsHan(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        foreach (var rune in value.EnumerateRunes())
        {
            var code=rune.Value;
            if (code is >= 0x3400 and <= 0x4DBF or >= 0x4E00 and <= 0x9FFF or
                >= 0xF900 and <= 0xFAFF or >= 0x20000 and <= 0x2FA1F or
                >= 0x30000 and <= 0x323AF) return true;
        }
        return false;
    }
}
