using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using PvZSymbiosisTranslator.Configuration;

namespace PvZSymbiosisTranslator.QA;

public sealed class QaReportResult
{
    public string JsonPath { get; init; }
    public string TextPath { get; init; }
}

public sealed class QaReportWriter
{
    private readonly string root;
    public QaReportWriter(string root)=>this.root=root;
    public QaReportResult Write(QaSnapshot snapshot,string modVersion)
    {
        if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
        var directory=Path.Combine(root,"TranslationExport","QA",DateTime.Now.ToString("yyyy-MM-dd"));Directory.CreateDirectory(directory);
        var stem=DateTime.Now.ToString("HHmmss-fff")+"-qa-report";var json=Unique(directory,stem,".json");var text=Path.ChangeExtension(json,".txt");
        var payload=new {generatedAt=snapshot.Timestamp,modVersion,gameVersion=snapshot.GameVersion,activeLanguage=snapshot.Locale,
            summary=new {status=snapshot.OverallStatus,errors=snapshot.Errors,warnings=snapshot.Warnings,information=snapshot.Information,exact=snapshot.ExactTranslations,dynamic=snapshot.DynamicRules,contexts=snapshot.ContextOverrides,textures=snapshot.TextureMappings,audio=snapshot.AudioMappings,runtime=snapshot.Runtime},
            checks=snapshot.Checks,findings=snapshot.Issues};
        Atomic(json,JsonSerializer.Serialize(payload,ConfigManager.HumanReadableJson));
        var lines=new StringBuilder().AppendLine("PvZ Symbiosis Translator QA").AppendLine($"Generated: {snapshot.Timestamp:O}").AppendLine($"Mod: {modVersion}").AppendLine($"Game: {snapshot.GameVersion}").AppendLine($"Locale: {snapshot.Locale}").AppendLine($"Status: {snapshot.OverallStatus}").AppendLine($"Errors: {snapshot.Errors}; Warnings: {snapshot.Warnings}; Info: {snapshot.Information}").AppendLine();
        foreach(var check in snapshot.Checks)lines.AppendLine($"[{check.Status}] {check.Id}: {check.Summary}");
        lines.AppendLine();foreach(var issue in snapshot.Issues)lines.AppendLine($"[{issue.Severity}] {issue.Check} | {issue.File} | {issue.Source} | {issue.Message}");
        Atomic(text,lines.ToString());return new QaReportResult{JsonPath=json,TextPath=text};
    }
    private static string Unique(string directory,string stem,string extension){var path=Path.Combine(directory,stem+extension);var suffix=1;while(File.Exists(path))path=Path.Combine(directory,$"{stem}-{suffix++}{extension}");return path;}
    private static void Atomic(string path,string contents){var temp=path+".tmp";File.WriteAllText(temp,contents,new UTF8Encoding(false));if(path.EndsWith(".json",StringComparison.OrdinalIgnoreCase))using(JsonDocument.Parse(File.ReadAllText(temp))){}File.Move(temp,path);}
}
