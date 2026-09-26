using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PvZSymbiosisTranslator.Diagnostics;
using PvZSymbiosisTranslator.Localization;
using PvZSymbiosisTranslator.Core;

namespace PvZSymbiosisTranslator.QA;

public sealed class LocalizationQaService
{
    private static readonly Regex Placeholder = new(@"\{[a-zA-Z0-9_]+\}|%(?:\d+\$)?[sdif]", RegexOptions.CultureInvariant);
    private static readonly Regex Tag = new(@"<\s*(/)?\s*([a-zA-Z][a-zA-Z0-9-]*)(?:=[^<>\s]+|\s+[^<>]*)?/?>", RegexOptions.CultureInvariant);
    private static readonly Regex NamedGroup = new(@"\(\?<([a-zA-Z][a-zA-Z0-9_]*)>", RegexOptions.CultureInvariant);
    private static readonly Regex UsedGroup = new(@"\$\{([a-zA-Z][a-zA-Z0-9_]*)\}", RegexOptions.CultureInvariant);
    private static readonly string[] RequiredJson = {
        "manifest.json", "glossary.json", "ModStrings.json", "Strings/exact.json", "Strings/context_overrides.json",
        "Strings/dynamic_rules.json", "Almanac/exact.json", "Fonts/manifest.json", "Textures/manifest.json", "Audio/manifest.json"
    };

    public QaSnapshot RunFullScan(string localeDirectory, string locale, string gameVersion, QaRuntimeMetrics runtime = null)
    {
        var issues = new List<QaIssue>();
        var counts = new Counts();
        var exactTargets = new HashSet<string>(StringComparer.Ordinal);
        var exactSources = new HashSet<string>(StringComparer.Ordinal);
        var parsed = new Dictionary<string, JsonDocument>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var root = Path.GetFullPath(localeDirectory ?? "");
            if (!Directory.Exists(root)) {
                Add(issues, QaSeverity.Error, "paths", "", "", "Localization directory does not exist");
                return Build(locale, gameVersion, runtime, counts, issues);
            }

            foreach (var relative in RequiredJson)
            {
                var path = SafePath(root, relative);
                if (path == null) { Add(issues,QaSeverity.Error,"paths",relative,"","Invalid relative path"); continue; }
                if (!File.Exists(path)) { Add(issues,QaSeverity.Error,"json",relative,"","Required JSON file is missing"); continue; }
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    var json = new UTF8Encoding(false, true).GetString(bytes);
                    var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas=false, CommentHandling=JsonCommentHandling.Disallow, MaxDepth=64 });
                    parsed[relative]=doc;
                    FindDuplicateProperties(doc.RootElement, relative, issues);
                    FindReplacementCharacters(doc.RootElement, relative, issues);
                }
                catch (DecoderFallbackException ex) { Add(issues,QaSeverity.Error,"encoding",relative,"",ex.Message); }
                catch (JsonException ex) { Add(issues,QaSeverity.Error,"json",relative,"",ex.Message,ex.LineNumber,ex.BytePositionInLine); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Add(issues,QaSeverity.Error,"json",relative,"",ex.Message); }
            }

            InspectManifest(parsed, root, locale, gameVersion, issues);
            InspectMap(parsed,"Strings/exact.json",issues,counts,exactSources,exactTargets,true);
            InspectMap(parsed,"Almanac/exact.json",issues,counts,exactSources,exactTargets,true);
            InspectMap(parsed,"ModStrings.json",issues,counts,exactSources,exactTargets,false);
            InspectContexts(parsed,issues,counts,exactSources,exactTargets);
            InspectDynamic(parsed,issues,counts);
            InspectAssets(parsed,root,"Textures/manifest.json","textures",new[]{".png"},issues,value=>counts.Textures=value,validatePng:true);
            InspectAssets(parsed,root,"Audio/manifest.json","audio",new[]{".wav",".mp3",".ogg"},issues,value=>counts.Audio=value,validatePng:false);
            InspectTargetContamination(exactSources,exactTargets,issues);
        }
        catch (Exception ex)
        {
            Add(issues,QaSeverity.Error,"scan","","","QA scan failed safely: "+ex.Message);
        }
        finally { foreach (var doc in parsed.Values) doc.Dispose(); }
        return Build(locale, gameVersion, runtime, counts, issues);
    }

    private static QaSnapshot Build(string locale,string gameVersion,QaRuntimeMetrics runtime,Counts counts,List<QaIssue> issues)
    {
        var checks = new[] {"json","placeholders","markup","dynamic","conflicts","cjk","whitespace","length","encoding","textures","audio","paths","contamination"}
            .Select(id => Check(id,issues)).ToArray();
        return new QaSnapshot {Timestamp=DateTimeOffset.UtcNow,Locale=locale??"",GameVersion=gameVersion??"",ExactTranslations=counts.Exact,
            DynamicRules=counts.Dynamic,ContextOverrides=counts.Context,TextureMappings=counts.Textures,AudioMappings=counts.Audio,
            Runtime=runtime??new QaRuntimeMetrics(),Checks=checks,Issues=issues.OrderByDescending(x=>x.Severity).ThenBy(x=>x.Check,StringComparer.Ordinal).ThenBy(x=>x.File,StringComparer.Ordinal).ThenBy(x=>x.Source,StringComparer.Ordinal).ToArray()};
    }

    private static QaCheckResult Check(string id,IEnumerable<QaIssue> all)
    {
        var issues=all.Where(x=>x.Check==id).ToArray();
        var status=issues.Any(x=>x.Severity==QaSeverity.Error)?QaStatus.Fail:issues.Any(x=>x.Severity==QaSeverity.Warning)?QaStatus.Warning:issues.Any()?QaStatus.Info:QaStatus.Pass;
        return new QaCheckResult {Id=id,Status=status,Findings=issues.Length,Summary=issues.Length==0?"No findings":issues.Length+" finding(s)"};
    }

    private static void InspectManifest(Dictionary<string,JsonDocument> parsed,string root,string locale,string gameVersion,List<QaIssue> issues)
    {
        if(!parsed.TryGetValue("manifest.json",out var doc))return;
        var value=doc.RootElement;
        if(value.ValueKind!=JsonValueKind.Object){Add(issues,QaSeverity.Error,"json","manifest.json","","Expected object");return;}
        if(!StringProperty(value,"locale",out var actual)||actual!=locale)Add(issues,QaSeverity.Error,"paths","manifest.json","locale","Locale does not match active directory");
        if(!StringProperty(value,"sourceLocale",out var source)||source!="zh-CN")Add(issues,QaSeverity.Error,"json","manifest.json","sourceLocale","Expected zh-CN source locale");
        if(value.TryGetProperty("gameVersion",out var version)&&version.ValueKind==JsonValueKind.String&&version.GetString()!=gameVersion)Add(issues,QaSeverity.Warning,"json","manifest.json","gameVersion","Active game version is not listed");
    }

    private static void InspectMap(Dictionary<string,JsonDocument> parsed,string file,List<QaIssue> issues,Counts counts,HashSet<string> sources,HashSet<string> targets,bool countExact)
    {
        if(!parsed.TryGetValue(file,out var doc))return;
        if(doc.RootElement.ValueKind!=JsonValueKind.Object){Add(issues,QaSeverity.Error,"json",file,"","Expected object of source/target strings");return;}
        foreach(var entry in doc.RootElement.EnumerateObject())
        {
            if(entry.Value.ValueKind!=JsonValueKind.String){Add(issues,QaSeverity.Error,"json",file,entry.Name,"Target must be a string");continue;}
            var target=entry.Value.GetString()??"";
            try { ExactSectionMarker.Validate(entry.Name,target); }
            catch(FormatException ex) { Add(issues,QaSeverity.Error,"json",file,entry.Name,ex.Message); continue; }
            if(ExactSectionMarker.IsMarker(entry.Name,target))continue;
            if(countExact)counts.Exact++;sources.Add(entry.Name);targets.Add(target);InspectPair(file,entry.Name,target,issues);
        }
    }

    private static void InspectContexts(Dictionary<string,JsonDocument> parsed,List<QaIssue> issues,Counts counts,HashSet<string> sources,HashSet<string> targets)
    {
        const string file="Strings/context_overrides.json";if(!parsed.TryGetValue(file,out var doc))return;
        if(doc.RootElement.ValueKind!=JsonValueKind.Object){Add(issues,QaSeverity.Error,"json",file,"","Expected context object");return;}
        foreach(var context in doc.RootElement.EnumerateObject())
        {
            if(context.Value.ValueKind!=JsonValueKind.Object){Add(issues,QaSeverity.Error,"json",file,context.Name,"Context must be an object");continue;}
            foreach(var entry in context.Value.EnumerateObject())
            {
                if(entry.Value.ValueKind!=JsonValueKind.String){Add(issues,QaSeverity.Error,"json",file,entry.Name,"Target must be a string");continue;}
                counts.Context++;sources.Add(entry.Name);var target=entry.Value.GetString()??"";targets.Add(target);InspectPair(file+"#"+context.Name,entry.Name,target,issues);
            }
        }
    }

    private static void InspectPair(string file,string source,string target,List<QaIssue> issues)
    {
        var sourceTokens=Placeholder.Matches(source).Select(x=>x.Value).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        var targetTokens=Placeholder.Matches(target).Select(x=>x.Value).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        if(!sourceTokens.SequenceEqual(targetTokens,StringComparer.Ordinal))Add(issues,QaSeverity.Error,"placeholders",file,source,"Placeholder set/count differs from source");
        if(!MarkupBalanced(target))Add(issues,QaSeverity.Error,"markup",file,source,"Malformed or unbalanced rich-text tag");
        var targetWithoutCredits=Regex.Replace(target,@"<align=right>.*?</align>","",RegexOptions.Singleline|RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
        if(source!=target&&HanSourceDetector.ContainsHan(targetWithoutCredits))Add(issues,HanRatio(targetWithoutCredits)>.35?QaSeverity.Warning:QaSeverity.Info,"cjk",file,source,HanRatio(targetWithoutCredits)>.35?"Suspicious CJK residue in translated target":"CJK proper name or credit retained");
        else if(source==target&&HanSourceDetector.ContainsHan(target))Add(issues,QaSeverity.Info,"cjk",file,source,"Explicit identity entry is intentionally preserved");
        var boundaryMismatch=target.Length>0&&((char.IsWhiteSpace(target[0])&&!char.IsWhiteSpace(source.FirstOrDefault()))||(char.IsWhiteSpace(target[^1])&&!char.IsWhiteSpace(source.LastOrDefault())));
        var unexpectedDouble=target.Contains("  ",StringComparison.Ordinal)&&!source.Contains("  ",StringComparison.Ordinal)&&!target.Contains('\n');
        if(boundaryMismatch||unexpectedDouble||target.Contains('\t')||target.Contains('\u200B')||target.Contains('\uFEFF'))
            Add(issues,QaSeverity.Warning,"whitespace",file,source,"Suspicious whitespace or invisible control character");
        if(target.Contains('\uFFFD')||Regex.IsMatch(target,@"(?:Ã[¡-¿]|Â[\u0080-\u00BF]|â[\u0080-\u00BF]{2})",RegexOptions.CultureInvariant))Add(issues,QaSeverity.Error,"encoding",file,source,"Replacement character or likely mojibake detected");
        var sourceVisible=StripMarkup(source).Length;var targetVisible=StripMarkup(target).Length;
        if(!target.Contains('\n')&&sourceVisible>=4&&sourceVisible<=7&&targetVisible>=36&&targetVisible>sourceVisible*5)Add(issues,QaSeverity.Warning,"length",file,source,"Very large single-line expansion may risk UI overflow; runtime clipping was not asserted");
    }

    private static bool MarkupBalanced(string value)
    {
        var stack=new Stack<string>();
        foreach(Match match in Tag.Matches(value??""))
        {
            var token=match.Value;var closing=match.Groups[1].Success;var name=match.Groups[2].Value.ToLowerInvariant();
            if(token.EndsWith("/>",StringComparison.Ordinal)||name is "br" or "sprite" or "space" or "quad")continue;
            if(closing){if(stack.Count==0||stack.Pop()!=name)return false;}else stack.Push(name);
        }
        var without=Tag.Replace(value??"","");
        var malformedKnownTag=Regex.IsMatch(without,@"<\s*/?\s*(?:color|size|b|i|u|s|align|cspace|voffset|mark|sup|sub|nobr|noparse|sprite)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
        return stack.Count==0&&!malformedKnownTag;
    }

    private static void InspectDynamic(Dictionary<string,JsonDocument> parsed,List<QaIssue> issues,Counts counts)
    {
        const string file="Strings/dynamic_rules.json";if(!parsed.TryGetValue(file,out var doc))return;
        if(doc.RootElement.ValueKind!=JsonValueKind.Array){Add(issues,QaSeverity.Error,"json",file,"","Expected array");return;}
        var ids=new HashSet<string>(StringComparer.Ordinal);var patterns=new HashSet<string>(StringComparer.Ordinal);
        foreach(var item in doc.RootElement.EnumerateArray())
        {
            counts.Dynamic++;if(item.ValueKind!=JsonValueKind.Object){Add(issues,QaSeverity.Error,"dynamic",file,"","Rule must be an object");continue;}
            var id=StringProperty(item,"id",out var idValue)?idValue:"#"+counts.Dynamic;
            var pattern=StringProperty(item,"pattern",out var p)?p:"";var target=StringProperty(item,"target",out var t)?t:"";
            if(!ids.Add(id))Add(issues,QaSeverity.Error,"conflicts",file,id,"Duplicate dynamic rule ID");
            if(!patterns.Add(pattern))Add(issues,QaSeverity.Error,"conflicts",file,id,"Duplicate dynamic pattern");
            if(!pattern.StartsWith('^')||!pattern.EndsWith('$'))Add(issues,QaSeverity.Warning,"dynamic",file,id,"Dynamic regex is not anchored");
            if(pattern is "^.*$" or "^.+$")Add(issues,QaSeverity.Warning,"dynamic",file,id,"Dynamic regex is dangerously broad");
            try
            {
                _=new Regex(pattern,RegexOptions.CultureInvariant|RegexOptions.ExplicitCapture,TimeSpan.FromMilliseconds(20));
                var defined=NamedGroup.Matches(pattern).Select(x=>x.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
                var used=UsedGroup.Matches(target).Select(x=>x.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
                if(!defined.SetEquals(used))Add(issues,QaSeverity.Error,"dynamic",file,id,"Replacement group references do not match named captures");
            }
            catch(ArgumentException ex){Add(issues,QaSeverity.Error,"dynamic",file,id,"Invalid regex: "+ex.Message);}
        }
    }

    private static void InspectAssets(Dictionary<string,JsonDocument> parsed,string localeRoot,string file,string check,string[] extensions,List<QaIssue> issues,Action<int> setCount,bool validatePng)
    {
        if(!parsed.TryGetValue(file,out var doc))return;var root=doc.RootElement;
        if(root.ValueKind==JsonValueKind.Object&&root.TryGetProperty("entries",out var entries))root=entries;
        if(root.ValueKind!=JsonValueKind.Array){Add(issues,QaSeverity.Error,check,file,"","Manifest must be an array or {entries:[]}");return;}
        var count=0;var ids=new HashSet<string>(StringComparer.Ordinal);var mappedPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var explicitNames=new HashSet<string>(StringComparer.Ordinal);var explicitSources=new HashSet<string>(StringComparer.Ordinal);var folder=Path.Combine(localeRoot,check=="textures"?"Textures":"Audio");
        foreach(var item in root.EnumerateArray())
        {
            count++;if(item.ValueKind!=JsonValueKind.Object){Add(issues,QaSeverity.Error,check,file,"","Entry must be an object");continue;}
            var id=StringProperty(item,"id",out var x)?x:StringProperty(item,"source",out x)?x:"#"+count;
            if(!ids.Add(id))Add(issues,QaSeverity.Error,check,file,id,"Duplicate mapping identity");
            if(validatePng&&item.TryGetProperty("match",out var match)&&match.ValueKind==JsonValueKind.Object)
            {
                var textureName=StringProperty(match,"textureName",out var tn)?tn:"";var spriteName=StringProperty(match,"spriteName",out var sn)?sn:"*";var scene=StringProperty(match,"scene",out var sc)?sc:"*";var context=StringProperty(match,"context",out var cx)?cx:"*";
                var width=match.TryGetProperty("width",out var w)&&w.TryGetInt32(out var wi)?wi:0;var height=match.TryGetProperty("height",out var h)&&h.TryGetInt32(out var hi)?hi:0;
                if(!string.IsNullOrWhiteSpace(textureName)){explicitNames.Add(textureName);var identity=$"{textureName}|{spriteName}|{width}x{height}|{scene}|{context}";if(!explicitSources.Add(identity))Add(issues,QaSeverity.Error,check,file,id,"Duplicate explicit texture source identity: "+identity);}
                else if(!StringProperty(match,"id",out _))Add(issues,QaSeverity.Error,check,file,id,"Texture match requires textureName/width/height or metadata ID");
            }
            var replacement=StringProperty(item,"replacement",out var r)?r:StringProperty(item,"file",out r)?r:"";
            var path=SafePath(folder,replacement);
            if(path==null){Add(issues,QaSeverity.Error,check,file,id,"Invalid or escaping replacement path");continue;}
            mappedPaths.Add(path);
            if(!extensions.Contains(Path.GetExtension(path),StringComparer.OrdinalIgnoreCase)){Add(issues,QaSeverity.Error,check,file,id,"Unsupported replacement file type");continue;}
            if(!File.Exists(path)){Add(issues,QaSeverity.Error,check,file,id,"Replacement file does not exist");continue;}
            if(validatePng)try{var png=PngCodec.Decode(File.ReadAllBytes(path));if(png.Width<=0||png.Height<=0)throw new FormatException("PNG dimensions must be positive");}catch(Exception ex){Add(issues,QaSeverity.Error,check,file,id,"PNG decode failed: "+ex.Message);}
        }
        if(validatePng&&Directory.Exists(folder))
        {
            foreach(var unsupported in Directory.EnumerateFiles(folder,"*",SearchOption.AllDirectories).Where(x=>!string.Equals(Path.GetExtension(x),".png",StringComparison.OrdinalIgnoreCase)&&!string.Equals(Path.GetFileName(x),"manifest.json",StringComparison.OrdinalIgnoreCase)).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
                Add(issues,QaSeverity.Warning,check,Path.GetRelativePath(localeRoot,unsupported),"","Unsupported file in texture pack");
            var automaticIdentities=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var path in Directory.EnumerateFiles(folder,"*.png",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
            {
                var full=Path.GetFullPath(path);if(mappedPaths.Contains(full))continue;count++;var relative=Path.GetRelativePath(folder,full).Replace('\\','/');var id="auto:"+relative;var textureName=Path.GetFileNameWithoutExtension(full);
                if(string.IsNullOrWhiteSpace(textureName)){Add(issues,QaSeverity.Error,check,file,id,"Automatic texture requires a Texture2D filename");continue;}
                try
                {
                    var png=PngCodec.Decode(File.ReadAllBytes(full));var identity=$"{textureName}|{png.Width}x{png.Height}";
                    if(automaticIdentities.TryGetValue(identity,out var previous))Add(issues,QaSeverity.Error,check,file,id,$"Duplicate automatic texture identity: name={textureName}; size={png.Width}x{png.Height}; first={previous}");else automaticIdentities.Add(identity,relative);
                    if(explicitNames.Contains(textureName))Add(issues,QaSeverity.Info,check,file,id,"Automatic texture is shadowed by an explicit mapping with the same Texture2D name");
                }
                catch(Exception ex){Add(issues,QaSeverity.Error,check,file,id,"PNG decode failed: "+ex.Message);}
            }
        }
        setCount(count);
    }

    private static void InspectTargetContamination(HashSet<string> sources,HashSet<string> targets,List<QaIssue> issues)
    {
        foreach(var source in sources.Where(x=>targets.Contains(x)&&!HanSourceDetector.ContainsHan(x)).Take(50))
            Add(issues,QaSeverity.Warning,"contamination","Strings","",$"A source equals a known rendered target: {Clip(source,80)}");
    }

    private static void FindDuplicateProperties(JsonElement value,string file,List<QaIssue> issues)
    {
        if(value.ValueKind==JsonValueKind.Object){var names=new HashSet<string>(StringComparer.Ordinal);foreach(var p in value.EnumerateObject()){if(!names.Add(p.Name))Add(issues,QaSeverity.Error,"conflicts",file,p.Name,"Duplicate JSON key");FindDuplicateProperties(p.Value,file,issues);}}
        else if(value.ValueKind==JsonValueKind.Array)foreach(var item in value.EnumerateArray())FindDuplicateProperties(item,file,issues);
    }

    private static void FindReplacementCharacters(JsonElement value,string file,List<QaIssue> issues)
    {
        if(value.ValueKind==JsonValueKind.String&&(value.GetString()??"").Contains('\uFFFD'))Add(issues,QaSeverity.Error,"encoding",file,"","Unicode replacement character detected");
        else if(value.ValueKind==JsonValueKind.Object)foreach(var p in value.EnumerateObject())FindReplacementCharacters(p.Value,file,issues);
        else if(value.ValueKind==JsonValueKind.Array)foreach(var item in value.EnumerateArray())FindReplacementCharacters(item,file,issues);
    }

    private static bool StringProperty(JsonElement value,string name,out string result){result="";if(!value.TryGetProperty(name,out var p)||p.ValueKind!=JsonValueKind.String)return false;result=p.GetString()??"";return true;}
    private static string SafePath(string root,string relative){try{if(string.IsNullOrWhiteSpace(relative)||Path.IsPathRooted(relative))return null;var prefix=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;var path=Path.GetFullPath(Path.Combine(prefix,relative.Replace('/',Path.DirectorySeparatorChar)));return path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)?path:null;}catch{return null;}}
    private static string StripMarkup(string value)=>Tag.Replace(value??"","");
    private static double HanRatio(string value){if(string.IsNullOrEmpty(value))return 0;var visible=StripMarkup(value);var letters=visible.Count(char.IsLetter);return letters==0?0:(double)visible.Count(c=>HanSourceDetector.ContainsHan(c.ToString()))/letters;}
    private static string Clip(string value,int max)=>value.Length<=max?value:value.Substring(0,max)+"…";
    private static void Add(List<QaIssue> issues,QaSeverity severity,string check,string file,string source,string message,long? line=null,long? position=null)=>issues.Add(new QaIssue{Severity=severity,Check=check,File=(file??"").Replace('\\','/'),Source=source??"",Message=message??"",Line=line,Position=position});
    private sealed class Counts{public int Exact,Dynamic,Context,Textures,Audio;}
}
