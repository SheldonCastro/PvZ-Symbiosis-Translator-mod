using System.Text;

namespace PvZSymbiosisTranslator.Diagnostics;

public static class DiagnosticString
{
    public static string EscapeInvisible(string value)
    {
        if (value == null) return null;
        var b = new StringBuilder();
        foreach (var c in value)
            b.Append(c switch { '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", '\u200B' => "\\u200B", '\uFEFF' => "\\uFEFF", _ => char.IsControl(c) ? $"\\u{(int)c:X4}" : c.ToString() });
        return b.ToString();
    }
}
