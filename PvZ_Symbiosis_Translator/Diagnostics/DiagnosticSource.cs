namespace PvZSymbiosisTranslator.Diagnostics;

public static class DiagnosticSource
{
    // A changed visible value is fresh source; an unchanged rendering keeps its tracked original.
    public static string Resolve(string current, string source, string rendered) =>
        source != null && current == rendered ? source : current;
}
