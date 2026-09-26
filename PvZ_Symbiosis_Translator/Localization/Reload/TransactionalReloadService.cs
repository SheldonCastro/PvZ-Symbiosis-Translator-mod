using System;
using System.Diagnostics;

namespace PvZSymbiosisTranslator.Localization.Reload;

public sealed class ReloadSnapshot
{
    public string Scope { get; init; } = "none";
    public bool Attempted { get; init; }
    public bool Success { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public long DurationMilliseconds { get; init; }
    public int FilesProcessed { get; init; }
    public string Error { get; init; } = "";
}

public sealed class TransactionalReloadService
{
    public ReloadSnapshot Last { get; private set; } = new();

    public bool Execute<T>(string scope,int filesProcessed,Func<T> buildCandidate,Action<T> activate)
    {
        if(string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("Reload scope is required",nameof(scope));
        if(filesProcessed<0) throw new ArgumentOutOfRangeException(nameof(filesProcessed));
        if(buildCandidate==null) throw new ArgumentNullException(nameof(buildCandidate));
        if(activate==null) throw new ArgumentNullException(nameof(activate));

        var started=DateTimeOffset.Now;
        var timer=Stopwatch.StartNew();
        try {
            var candidate=buildCandidate();
            activate(candidate);
            timer.Stop();
            Last=new ReloadSnapshot {Scope=scope,Attempted=true,Success=true,Timestamp=started,DurationMilliseconds=timer.ElapsedMilliseconds,FilesProcessed=filesProcessed};
            return true;
        }
        catch(Exception ex)
        {
            timer.Stop();
            Last=new ReloadSnapshot {Scope=scope,Attempted=true,Success=false,Timestamp=started,DurationMilliseconds=timer.ElapsedMilliseconds,FilesProcessed=filesProcessed,Error=ex.Message};
            return false;
        }
    }
}
