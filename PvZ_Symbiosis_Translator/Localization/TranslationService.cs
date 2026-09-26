namespace PvZSymbiosisTranslator.Localization;

public sealed class TranslationService
{
    public LanguagePack Pack { get; }
    public TranslationService(LanguagePack pack) { Pack = pack; }
    public string Translate(string source, string context = null, bool dynamicRules = true)
    {
        if (source == null) return null;
        if (context != null && Pack.Contexts.TryGetValue(context, out var store) && store.TryGet(source, out var contextual)) return contextual;
        if (Pack.Exact.TryGet(source, out var exact)) return exact;
        if (dynamicRules && Pack.Dynamic.TryTranslate(source, out var dynamic)) return dynamic;
        return source;
    }
    public ClassifiedTranslation Classify(string source,string context=null,bool dynamicRules=true,bool knownRuntimeSource=false)
    {
        if(source==null)return new ClassifiedTranslation {Source=null,Target=null,Classification=TranslationClassification.Unknown};
        if(context!=null&&Pack.Contexts.TryGetValue(context,out var contextual)&&contextual.TryGet(source,out var contextTarget))
            return Result(source,contextTarget,TranslationClassification.ContextTranslated);
        if(Pack.Exact.TryGet(source,out var exactTarget))return Result(source,exactTarget,TranslationClassification.ExactTranslated);
        if(dynamicRules&&Pack.Dynamic.TryTranslate(source,out var dynamicTarget))return Result(source,dynamicTarget,TranslationClassification.DynamicTranslated);
        if(IsRenderedTargetOnly(source))return new ClassifiedTranslation {Source=source,Target=source,Classification=TranslationClassification.RenderedTarget};
        return new ClassifiedTranslation {Source=source,Target=source,Classification=knownRuntimeSource?TranslationClassification.KnownUntranslated:TranslationClassification.Unknown};
    }
    public bool IsRenderedTargetOnly(string value)
    {
        if(value==null||Pack.Exact.TryGet(value,out _))return false;
        foreach(var context in Pack.Contexts.Values)if(context.TryGet(value,out _))return false;
        foreach(var target in Pack.Exact.Entries.Values)if(string.Equals(target,value,System.StringComparison.Ordinal))return true;
        foreach(var context in Pack.Contexts.Values)foreach(var target in context.Entries.Values)if(string.Equals(target,value,System.StringComparison.Ordinal))return true;
        return false;
    }
    private static ClassifiedTranslation Result(string source,string target,TranslationClassification translated)
        =>new(){Source=source,Target=target,Classification=string.Equals(source,target,System.StringComparison.Ordinal)?TranslationClassification.Preserved:translated};
}
