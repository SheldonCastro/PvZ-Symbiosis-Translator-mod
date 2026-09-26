using System;
using System.Collections.Generic;
using System.Linq;

namespace PvZSymbiosisTranslator.Diagnostics;

public sealed class TranslatorSessionRecord
{
    public string Source { get; init; }
    public string NormalizedSource { get; init; }
    public string Target { get; set; }
    public string Context { get; init; }
    public string Scene { get; init; }
    public string ObjectName { get; init; }
    public string ComponentType { get; init; }
    public string Hierarchy { get; init; }
    public string Status { get; set; }
    public DateTimeOffset FirstSeen { get; init; }
    public DateTimeOffset LastSeen { get; set; }
    public long SeenCount { get; set; }
}

public sealed class TranslatorSessionSnapshot
{
    public DateTimeOffset StartedAt { get; init; }
    public string CurrentContext { get; init; }
    public IReadOnlyList<string> ContextHistory { get; init; }
    public IReadOnlyList<TranslatorSessionRecord> Records { get; init; }
    public int UniqueSeen => Records.Count;
    public int Translated => Records.Count(x=>x.Status=="exact"||x.Status=="dynamic"||x.Status=="context");
    public int Preserved => Records.Count(x=>x.Status=="preserved");
    public int KnownUntranslated => Records.Count(x=>x.Status=="known-untranslated");
    public int Unknown => Records.Count(x=>x.Status=="unknown");
}

public sealed class TranslatorSessionService
{
    private readonly Dictionary<string,TranslatorSessionRecord> records=new(StringComparer.Ordinal);
    private readonly List<string> contexts=new();
    public DateTimeOffset StartedAt { get; private set; }=DateTimeOffset.UtcNow;
    public string CurrentContext { get; private set; }="Unknown";
    public bool SetContext(string context)
    {
        context=string.IsNullOrWhiteSpace(context)?"Unknown":context.Trim();
        if(string.Equals(CurrentContext,context,StringComparison.Ordinal))return false;
        CurrentContext=context;if(contexts.Count==0||!string.Equals(contexts[^1],context,StringComparison.Ordinal))contexts.Add(context);return true;
    }
    public bool Observe(string source,string target,string context,string scene,string objectName,string componentType,string hierarchy,string status,bool detailed,DateTimeOffset? now=null)
    {
        if(IsNoise(source))return false;
        context=string.IsNullOrWhiteSpace(context)?CurrentContext:context;
        var identity=source+"\u001f"+context;var timestamp=now??DateTimeOffset.UtcNow;
        if(!records.TryGetValue(identity,out var record)) {
            record=new TranslatorSessionRecord {Source=source,NormalizedSource=Normalize(source),Target=target??source,Context=context,Scene=scene??"",ObjectName=detailed?objectName??"":"",ComponentType=detailed?componentType??"":"",Hierarchy=detailed?hierarchy??"":"",Status=status,FirstSeen=timestamp,LastSeen=timestamp,SeenCount=1};records.Add(identity,record);return true;
        }
        record.SeenCount++;record.LastSeen=timestamp;record.Target=target??source;record.Status=status;return false;
    }
    public void Clear(){records.Clear();contexts.Clear();StartedAt=DateTimeOffset.UtcNow;CurrentContext="Unknown";}
    public TranslatorSessionSnapshot Snapshot()=>new(){StartedAt=StartedAt,CurrentContext=CurrentContext,ContextHistory=contexts.ToArray(),Records=records.Values.OrderBy(x=>x.Source,StringComparer.Ordinal).ThenBy(x=>x.Context,StringComparer.Ordinal).Select(Clone).ToArray()};
    public static string Normalize(string value)=>value?.Replace("\u200B","").Trim()??"";
    public static bool IsNoise(string value)
    {
        if(string.IsNullOrWhiteSpace(value))return true;var normalized=Normalize(value);if(normalized.Length==0)return true;
        if(normalized.StartsWith("[NativeUI]",StringComparison.Ordinal)||normalized.StartsWith("[Translation]",StringComparison.Ordinal)||normalized.StartsWith("PvZ Symbiosis Translator",StringComparison.OrdinalIgnoreCase))return true;
        return !normalized.Any(char.IsLetterOrDigit);
    }
    private static TranslatorSessionRecord Clone(TranslatorSessionRecord x)=>new(){Source=x.Source,NormalizedSource=x.NormalizedSource,Target=x.Target,Context=x.Context,Scene=x.Scene,ObjectName=x.ObjectName,ComponentType=x.ComponentType,Hierarchy=x.Hierarchy,Status=x.Status,FirstSeen=x.FirstSeen,LastSeen=x.LastSeen,SeenCount=x.SeenCount};
}
