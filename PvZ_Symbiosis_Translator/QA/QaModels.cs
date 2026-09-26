using System;
using System.Collections.Generic;
using System.Linq;

namespace PvZSymbiosisTranslator.QA;

public enum QaSeverity { Info, Warning, Error }
public enum QaStatus { Pass, Warning, Fail, Info, NotApplicable }

public sealed class QaIssue
{
    public QaSeverity Severity { get; init; }
    public string Check { get; init; } = "";
    public string File { get; init; } = "";
    public string Source { get; init; } = "";
    public string Message { get; init; } = "";
    public long? Line { get; init; }
    public long? Position { get; init; }
}

public sealed class QaCheckResult
{
    public string Id { get; init; } = "";
    public QaStatus Status { get; init; }
    public string Summary { get; init; } = "";
    public int Findings { get; init; }
}

public sealed class QaRuntimeMetrics
{
    public int RuntimeObserved { get; init; }
    public int RuntimeTranslated { get; init; }
    public int Preserved { get; init; }
    public int KnownUntranslated { get; init; }
    public int Unknown { get; init; }
}

public sealed class QaSnapshot
{
    public DateTimeOffset Timestamp { get; init; }
    public string Locale { get; init; } = "";
    public string GameVersion { get; init; } = "";
    public int ExactTranslations { get; init; }
    public int DynamicRules { get; init; }
    public int ContextOverrides { get; init; }
    public int TextureMappings { get; init; }
    public int AudioMappings { get; init; }
    public QaRuntimeMetrics Runtime { get; init; } = new();
    public IReadOnlyList<QaCheckResult> Checks { get; init; } = Array.Empty<QaCheckResult>();
    public IReadOnlyList<QaIssue> Issues { get; init; } = Array.Empty<QaIssue>();
    public int Errors => Issues.Count(x => x.Severity == QaSeverity.Error);
    public int Warnings => Issues.Count(x => x.Severity == QaSeverity.Warning);
    public int Information => Issues.Count(x => x.Severity == QaSeverity.Info);
    public QaStatus OverallStatus => Errors > 0 ? QaStatus.Fail : Warnings > 0 ? QaStatus.Warning : QaStatus.Pass;
}
