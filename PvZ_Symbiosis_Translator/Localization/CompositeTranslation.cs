using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PvZSymbiosisTranslator.Localization;

public readonly struct CompositeTranslationResult
{
    public readonly string Text;
    public readonly int Segments, Translated, Unknown;
    public CompositeTranslationResult(string text,int segments,int translated,int unknown)
    { Text=text; Segments=segments; Translated=translated; Unknown=unknown; }
}

public static class CompositeTranslation
{
    private static readonly Regex Separator = new(@"(\r?\n\r?\n)",RegexOptions.CultureInvariant);
    public static CompositeTranslationResult Translate(string source, Func<string,string> translate)
    {
        if(source==null) return new CompositeTranslationResult(null,0,0,0);
        var pieces=Separator.Split(source);
        var output=new StringBuilder(source.Length);
        var segments=0; var translated=0; var unknown=0;
        for(var i=0;i<pieces.Length;i++) {
            var piece=pieces[i];
            if(i%2==1 || piece.Length==0) { output.Append(piece); continue; }
            segments++;
            var target=translate(piece);
            if(string.Equals(target,piece,StringComparison.Ordinal)) {
                var separator=i+1<pieces.Length && pieces[i+1].Length>0 ? pieces[i+1] : "\n\n";
                var sourceWithSeparator=piece+separator;
                var translatedWithSeparator=translate(sourceWithSeparator);
                if(!string.Equals(translatedWithSeparator,sourceWithSeparator,StringComparison.Ordinal) &&
                    translatedWithSeparator.EndsWith(separator,StringComparison.Ordinal))
                    target=translatedWithSeparator.Substring(0,translatedWithSeparator.Length-separator.Length);
            }
            if(string.Equals(target,piece,StringComparison.Ordinal)) unknown++; else translated++;
            output.Append(target);
        }
        return new CompositeTranslationResult(output.ToString(),segments,translated,unknown);
    }
}
