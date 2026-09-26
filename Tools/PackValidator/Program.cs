using System.Text.Json;
using PvZSymbiosisTranslator.Localization;

if (args.Length == 2 && args[0] == "--list-locales")
{
    Console.WriteLine(JsonSerializer.Serialize(LocaleManager.AvailableLanguageInfos(Path.GetFullPath(args[1]))
        .Select(x => new { locale = x.Locale, displayName = x.DisplayName }), new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
if (args.Length != 3 && !(args.Length == 5 && args[3] == "--runtime-report"))
{
    Console.Error.WriteLine("Usage: PackValidator <locale-directory> <locale> <game-version> [--runtime-report <report-path>] | --list-locales <localization-root>");
    return 2;
}
try
{
    var pack = LocaleManager.Load(Path.GetFullPath(args[0]), args[1], args[2], args.Length == 3,
        args.Length == 5 ? Path.GetFullPath(args[4]) : null);
    Console.WriteLine(JsonSerializer.Serialize(new {
        status = pack.Exact.Count == 0 ? "WARNING" : pack.Validation.Rejected > 0 ? "PARTIAL" : "PASS",
        locale = pack.Locale, exact = pack.Exact.Count,
        contexts = pack.Contexts.Count, dynamicRules = pack.Dynamic.Count,
        loaded = pack.Validation.Loaded, rejected = pack.Validation.Rejected, warnings = pack.Validation.Warnings,
        reportWriteError = pack.Validation.WriteError,
        issues = pack.Validation.Issues,
        note = pack.Exact.Count == 0 ? "Empty pack: all sources fall back to original." : "Structural checks passed. Entry errors are isolated; visual/linguistic review is separate."
    }, new JsonSerializerOptions { WriteIndented = true }));
    return pack.Validation.WriteError == null ? 0 : 1;
}
catch (Exception ex)
{
    Console.WriteLine(JsonSerializer.Serialize(new { status = "ERROR", message = ex.Message }));
    return 1;
}
