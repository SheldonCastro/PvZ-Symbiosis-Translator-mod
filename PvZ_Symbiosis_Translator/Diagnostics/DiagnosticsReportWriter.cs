using System;
using System.IO;
using System.Text;
using System.Text.Json;
using PvZSymbiosisTranslator.Configuration;

namespace PvZSymbiosisTranslator.Diagnostics;

public sealed class DiagnosticsReportResult { public string JsonPath { get; init; } public string TextPath { get; init; } }

public sealed class DiagnosticsReportWriter
{
    private readonly string root;
    public DiagnosticsReportWriter(string root)=>this.root=root;
    public DiagnosticsReportResult Write(DiagnosticsSnapshot snapshot)
    {
        if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));var directory=Path.Combine(root,"TranslationExport","Diagnostics",DateTime.Now.ToString("yyyy-MM-dd"));Directory.CreateDirectory(directory);
        var stem=DateTime.Now.ToString("HHmmss-fff")+"-diagnostics";var json=Unique(directory,stem,".json");var text=Path.ChangeExtension(json,".txt");
        Atomic(json,JsonSerializer.Serialize(new{generatedAt=snapshot.Timestamp,modVersion=snapshot.State.ModVersion,gameVersion=snapshot.State.GameVersion,activeLanguage=snapshot.State.ActiveLanguage,summary=new{status=snapshot.OverallStatus,failures=snapshot.Failures,warnings=snapshot.Warnings},runtime=snapshot.State,health=snapshot.Health},ConfigManager.HumanReadableJson));
        var b=new StringBuilder().AppendLine("PvZ Symbiosis Translator Diagnostics").AppendLine($"Generated: {snapshot.Timestamp:O}").AppendLine($"Mod: {snapshot.State.ModVersion}").AppendLine($"Game: {snapshot.State.GameVersion}").AppendLine($"Locale: {snapshot.State.ActiveLanguage}").AppendLine($"Status: {snapshot.OverallStatus}").AppendLine();foreach(var check in snapshot.Health)b.AppendLine($"[{check.Status}] {check.Id}: {check.Detail}");
        Atomic(text,b.ToString());return new DiagnosticsReportResult{JsonPath=json,TextPath=text};
    }
    private static string Unique(string directory,string stem,string extension){var path=Path.Combine(directory,stem+extension);var n=1;while(File.Exists(path))path=Path.Combine(directory,$"{stem}-{n++}{extension}");return path;}
    private static void Atomic(string path,string value){var temp=path+".tmp";File.WriteAllText(temp,value,new UTF8Encoding(false));if(path.EndsWith(".json",StringComparison.OrdinalIgnoreCase))using(JsonDocument.Parse(File.ReadAllText(temp))){}File.Move(temp,path);}
}
