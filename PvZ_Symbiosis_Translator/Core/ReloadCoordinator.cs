using System;
using System.Collections.Generic;
namespace PvZSymbiosisTranslator.Core;
public sealed class ReloadCoordinator
{
    public sealed class Result { public string Subsystem {get;set;} public string Status {get;set;} public string Message {get;set;} }
    public readonly List<Result> Results = new();
    public void Optional(string subsystem,Action load,Action fallback)
    {
        try {load();Results.Add(new Result{Subsystem=subsystem,Status="SUCCESS"});}
        catch(Exception ex) {
            var message=ex.Message;
            try {fallback();} catch(Exception failure) {message += "; fallback: "+failure.Message;}
            Results.Add(new Result{Subsystem=subsystem,Status="WARNING",Message=message});
        }
    }
}
