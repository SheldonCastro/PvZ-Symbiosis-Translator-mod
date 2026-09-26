using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace PvZSymbiosisTranslator.Localization;

public enum ValidationSeverity { Info, Warning, Error, Fatal }

public sealed class ValidationIssue
{
    public ValidationSeverity Severity { get; init; }
    public string SourceFile { get; init; }
    public string Source { get; init; }
    public string Target { get; init; }
    public string Reason { get; init; }
}

public sealed class PackValidationReport
{
    private static readonly JsonSerializerOptions Json = new() {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    public string Locale { get; }
    public string Timestamp { get; } = DateTimeOffset.UtcNow.ToString("O");
    public int Loaded { get; internal set; }
    public int Rejected { get; internal set; }
    public int Warnings { get; private set; }
    public int Fatals { get; private set; }
    public List<ValidationIssue> Issues { get; } = new();
    [JsonIgnore] public string WriteError { get; private set; }

    public PackValidationReport(string locale) { Locale = locale; }
    public void Add(ValidationSeverity severity, string file, string source, string target, string reason)
    {
        Issues.Add(new ValidationIssue { Severity = severity, SourceFile = file, Source = source, Target = target, Reason = reason });
        if (severity == ValidationSeverity.Warning) Warnings++;
        if (severity == ValidationSeverity.Fatal) Fatals++;
    }
    public void WriteIfRequested(string path)
    {
        if (path == null) return;
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(this, Json));
            File.Move(temp, path, true);
        }
        catch (Exception ex) { WriteError = ex.Message; }
    }
}
