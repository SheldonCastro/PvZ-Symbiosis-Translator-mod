using System;
using System.IO;
using System.Text.Json;

namespace PvZSymbiosisTranslator.Localization;

public static class JsonFile
{
    private static readonly JsonDocumentOptions Strict = new() {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64
    };
    private static readonly JsonDocumentOptions TrailingCommaCompatible = new() {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64
    };

    public static JsonDocument Read(string path,Action<string> warning=null)
    {
        string json;
        try { json=File.ReadAllText(path); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
        { throw new FormatException("Required JSON file failed: "+path+": "+ex.Message,ex); }

        try { return JsonDocument.Parse(json,Strict); }
        catch(JsonException strictFailure)
        {
            try {
                var compatible=JsonDocument.Parse(json,TrailingCommaCompatible);
                warning?.Invoke("Trailing comma accepted: "+path);
                return compatible;
            }
            catch(JsonException) {
                throw new FormatException("Required JSON file failed: "+path+": "+strictFailure.Message,strictFailure);
            }
        }
    }
}
