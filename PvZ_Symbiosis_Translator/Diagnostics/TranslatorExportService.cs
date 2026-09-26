using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using PvZSymbiosisTranslator.Configuration;

namespace PvZSymbiosisTranslator.Diagnostics;

public sealed class TranslatorExportService
{
    private readonly string root;
    public TranslatorExportService(string root)=>this.root=root;
    public string ExportUntranslated(TranslatorSessionSnapshot snapshot,string locale)
        =>Write("Untranslated",new {generatedAt=DateTimeOffset.UtcNow,locale,records=snapshot.Records.Where(x=>x.Status is "unknown" or "known-untranslated").ToArray()});
    public string ExportContext(TranslatorSessionSnapshot snapshot,string locale,string context,DateTimeOffset? seenAfter=null)
    {
        var records=snapshot.Records.Where(x=>string.Equals(x.Context,context,StringComparison.Ordinal)&&(!seenAfter.HasValue||x.LastSeen>seenAfter.Value)).ToArray();
        return seenAfter.HasValue&&records.Length==0?null:Write("Screens",new {generatedAt=DateTimeOffset.UtcNow,locale,context,records});
    }
    public string ExportSession(TranslatorSessionSnapshot snapshot,string locale,object reloadState)
        =>Write("Sessions",new {generatedAt=DateTimeOffset.UtcNow,locale,snapshot.StartedAt,snapshot.CurrentContext,snapshot.ContextHistory,snapshot.UniqueSeen,snapshot.Translated,snapshot.Preserved,snapshot.KnownUntranslated,snapshot.Unknown,records=snapshot.Records,reload=reloadState});
    private string Write(string category,object value)
    {
        var dir=Path.Combine(root,"TranslationExport",category,DateTime.Now.ToString("yyyy-MM-dd"));Directory.CreateDirectory(dir);
        var stem=DateTime.Now.ToString("HHmmss-fff");var path=Path.Combine(dir,stem+".json");var suffix=1;while(File.Exists(path))path=Path.Combine(dir,$"{stem}-{suffix++}.json");var temp=path+".tmp";
        File.WriteAllText(temp,JsonSerializer.Serialize(value,ConfigManager.HumanReadableJson));using(JsonDocument.Parse(File.ReadAllText(temp))){}File.Move(temp,path);return path;
    }
}
