using System;

namespace PvZSymbiosisTranslator.Localization;

public enum TranslationClassification
{
    ExactTranslated,
    DynamicTranslated,
    ContextTranslated,
    Preserved,
    KnownUntranslated,
    Unknown,
    RenderedTarget
}

public sealed class ClassifiedTranslation
{
    public string Source { get; init; }
    public string Target { get; init; }
    public TranslationClassification Classification { get; init; }
    public bool IsKnown => Classification is not TranslationClassification.Unknown and not TranslationClassification.RenderedTarget;
    public bool IsTranslated => Classification is TranslationClassification.ExactTranslated or TranslationClassification.DynamicTranslated or TranslationClassification.ContextTranslated;
    public string StatusCode => Classification switch {
        TranslationClassification.ExactTranslated => "exact",
        TranslationClassification.DynamicTranslated => "dynamic",
        TranslationClassification.ContextTranslated => "context",
        TranslationClassification.Preserved => "preserved",
        TranslationClassification.KnownUntranslated => "known-untranslated",
        TranslationClassification.RenderedTarget => "rendered-target",
        _ => "unknown"
    };
}
