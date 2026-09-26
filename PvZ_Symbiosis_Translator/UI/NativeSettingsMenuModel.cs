using System;
using System.Collections.Generic;

namespace PvZSymbiosisTranslator.UI;

public enum NativeSettingsPage { General, Content, Translator, Qa, Diagnostics }

public enum NativeSettingsAction
{
    CycleLanguage,
    ToggleTranslation,
    ToggleFont,
    ToggleTextures,
    ToggleAudio,
    ReloadAll,
    ReloadFonts,
    ReloadTextures,
    ReloadAudio,
    OpenLocaleFolder,
    OpenExportFolder,
    OpenModFolder,
    ToggleTranslatorMode,
    ToggleDebugLogging,
    ToggleAutoReload,
    ToggleCaptureUnknown,
    ToggleAutoExport,
    ToggleDetailedContext,
    RefreshCurrentScreen,
    ClearCapture,
    CopySessionSummary,
    ExportSession,
    ExportUntranslated,
    ExportDiagnostics,
    ExportCurrentScene,
    RunQa,
    RefreshDiagnostics,
    CopyDiagnostics,
    OpenLogs
}

public static class NativeSettingsMenuModel
{
    public static readonly IReadOnlyList<NativeSettingsPage> Pages = new[] {
        NativeSettingsPage.General, NativeSettingsPage.Content,
        NativeSettingsPage.Translator, NativeSettingsPage.Qa,
        NativeSettingsPage.Diagnostics
    };

    public static string PageKey(NativeSettingsPage page) => page switch {
        NativeSettingsPage.General => "tabs.general",
        NativeSettingsPage.Content => "tabs.content",
        NativeSettingsPage.Translator => "tabs.translator",
        NativeSettingsPage.Qa => "tabs.qa",
        NativeSettingsPage.Diagnostics => "tabs.diagnostics",
        _ => throw new ArgumentOutOfRangeException(nameof(page))
    };

    public static string PagePrefix(NativeSettingsPage page)
        => "PvZTranslator.Native.Page." + page + ".";

    public static bool BelongsToPage(string objectName, NativeSettingsPage page)
        => objectName != null && objectName.StartsWith(PagePrefix(page), StringComparison.Ordinal);

    public static IReadOnlyList<NativeSettingsAction> Actions(NativeSettingsPage page) => page switch {
        NativeSettingsPage.General => new[] { NativeSettingsAction.CycleLanguage, NativeSettingsAction.ToggleTranslation, NativeSettingsAction.ToggleFont, NativeSettingsAction.ToggleTextures, NativeSettingsAction.ToggleAudio },
        NativeSettingsPage.Content => new[] { NativeSettingsAction.ReloadAll, NativeSettingsAction.ReloadFonts, NativeSettingsAction.ReloadTextures, NativeSettingsAction.ReloadAudio, NativeSettingsAction.OpenLocaleFolder, NativeSettingsAction.OpenExportFolder, NativeSettingsAction.OpenModFolder },
        NativeSettingsPage.Translator => new[] { NativeSettingsAction.ToggleTranslatorMode, NativeSettingsAction.ToggleDebugLogging, NativeSettingsAction.ToggleAutoReload, NativeSettingsAction.ToggleCaptureUnknown, NativeSettingsAction.ToggleAutoExport, NativeSettingsAction.ToggleDetailedContext, NativeSettingsAction.ReloadAll, NativeSettingsAction.RefreshCurrentScreen, NativeSettingsAction.ClearCapture, NativeSettingsAction.CopySessionSummary, NativeSettingsAction.ExportUntranslated, NativeSettingsAction.ExportCurrentScene, NativeSettingsAction.ExportSession, NativeSettingsAction.OpenExportFolder },
        NativeSettingsPage.Qa => new[] { NativeSettingsAction.RunQa, NativeSettingsAction.ExportDiagnostics, NativeSettingsAction.ExportUntranslated, NativeSettingsAction.OpenExportFolder, NativeSettingsAction.CopySessionSummary },
        NativeSettingsPage.Diagnostics => new[] { NativeSettingsAction.RefreshDiagnostics, NativeSettingsAction.CopyDiagnostics, NativeSettingsAction.ExportDiagnostics, NativeSettingsAction.OpenLogs, NativeSettingsAction.OpenModFolder, NativeSettingsAction.OpenExportFolder },
        _ => throw new ArgumentOutOfRangeException(nameof(page))
    };
}
