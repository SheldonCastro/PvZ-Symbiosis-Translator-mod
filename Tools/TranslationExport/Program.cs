using PvZSymbiosisTranslator.Localization;
using PvZSymbiosisTranslator.Diagnostics;
using System.Text.Json;
using PvZSymbiosisTranslator.Configuration;
if(args.Length==2 && args[0]=="--audit-dynamic") {
    try {
        var locale=Path.GetFullPath(args[1]);
        using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(locale,"manifest.json")));
        var localeName=manifest.RootElement.GetProperty("locale").GetString();
        var gameVersion=manifest.RootElement.GetProperty("gameVersion").GetString();
        var pack=LocaleManager.Load(locale,localeName,gameVersion,false);
        var exact=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var name in new[]{"Strings","Almanac"}) {
            var file=Path.Combine(locale,name,"exact.json");
            if(!File.Exists(file)) continue;
            using var doc=JsonDocument.Parse(File.ReadAllText(file));
            foreach(var entry in doc.RootElement.EnumerateObject()) exact[entry.Name]=entry.Value.GetString();
        }
        var matches=DynamicExactAudit.Inspect(exact,pack.Dynamic);
        var report=matches.GroupBy(x=>x.RuleId).OrderBy(x=>x.Key,StringComparer.Ordinal)
            .Select(group=>new { ruleId=group.Key, matching=group.Count(), redundant=group.Count(x=>x.Redundant),
                overrides=group.Count(x=>!x.Redundant), entries=group.Select(x=>new { source=x.Source, exact=x.ExactTarget,
                    generated=x.DynamicTarget, redundant=x.Redundant }).ToArray() }).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(report,ConfigManager.HumanReadableJson));
        return 0;
    } catch(Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
}
if(args.Length!=2){Console.Error.WriteLine("TranslationExport <mod-root> <locale>");return 2;}
try {
    var root=Path.GetFullPath(args[0]);var locale=Path.GetFullPath(Path.Combine(root,"Localization",args[1]));
    if(!locale.StartsWith(Path.Combine(root,"Localization")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Invalid locale path");
    var service=new TranslationService(LocaleManager.Load(locale,args[1],"1.2.0",false));
    var collector=new RuntimeStringCollector();collector.Load(Path.Combine(root,"Dumps"));
    TranslationExportWriter.Write(root,locale,collector,s=>service.Translate(s));
    Console.WriteLine($"Export regenerated from {collector.Count} recorded sources; no live scan performed.");return 0;
}catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}
