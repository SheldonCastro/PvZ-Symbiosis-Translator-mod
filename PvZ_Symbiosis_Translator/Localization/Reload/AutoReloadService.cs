using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PvZSymbiosisTranslator.Localization.Reload;

public sealed class AutoReloadBatch
{
    public DateTimeOffset DetectedAt { get; init; }
    public IReadOnlyList<string> Files { get; init; } = Array.Empty<string>();
}

public sealed class AutoReloadService : IDisposable
{
    private readonly object gate = new();
    private readonly HashSet<string> pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan debounce;
    private FileSystemWatcher watcher;
    private DateTimeOffset readyAt;
    private string root;
    public bool Active { get; private set; }
    public string Error { get; private set; } = "";
    private DateTimeOffset? lastDetectedAt;
    private string lastDetectedFile = "";
    public DateTimeOffset? LastDetectedAt { get { lock(gate)return lastDetectedAt; } }
    public string LastDetectedFile { get { lock(gate)return lastDetectedFile; } }
    public int WatchedFileCount { get { try { return string.IsNullOrWhiteSpace(root) || !Directory.Exists(root) ? 0 : Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories).Count(IsRelevant); } catch { return 0; } } }

    public AutoReloadService(TimeSpan? debounce = null) => this.debounce = debounce ?? TimeSpan.FromMilliseconds(750);

    public void Start(string localeDirectory)
    {
        Stop();
        root=localeDirectory;
        try {
            if(!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
            watcher=new FileSystemWatcher(root) { IncludeSubdirectories=true, NotifyFilter=NotifyFilters.FileName|NotifyFilters.LastWrite|NotifyFilters.CreationTime, Filter="*.*", EnableRaisingEvents=false };
            watcher.Changed+=OnChanged;watcher.Created+=OnChanged;watcher.Deleted+=OnChanged;watcher.Renamed+=OnRenamed;
            watcher.EnableRaisingEvents=true;Active=true;Error="";
        } catch(Exception ex) { Stop();Error=ex.Message; }
    }

    public void Stop()
    {
        Active=false;
        if(watcher!=null) { watcher.EnableRaisingEvents=false;watcher.Changed-=OnChanged;watcher.Created-=OnChanged;watcher.Deleted-=OnChanged;watcher.Renamed-=OnRenamed;watcher.Dispose();watcher=null; }
        lock(gate) pending.Clear();
    }

    public void NotifyChange(string path,DateTimeOffset now)
    {
        if(!IsRelevant(path))return;
        lock(gate) { pending.Add(Path.GetFullPath(path));readyAt=now+debounce;lastDetectedAt=now;lastDetectedFile=Path.GetFileName(path); }
    }

    public bool TryDequeue(DateTimeOffset now,out AutoReloadBatch batch)
    {
        lock(gate) {
            if(pending.Count==0 || now<readyAt) {batch=null;return false;}
            batch=new AutoReloadBatch {DetectedAt=lastDetectedAt??now,Files=pending.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToArray()};pending.Clear();return true;
        }
    }

    public static bool IsRelevant(string path)
    {
        if(string.IsNullOrWhiteSpace(path))return false;
        var normalized=path.Replace('\\','/');var name=Path.GetFileName(normalized);
        if(name.EndsWith(".tmp",StringComparison.OrdinalIgnoreCase))return false;
        if(string.Equals(name,"ModStrings.json",StringComparison.OrdinalIgnoreCase))return true;
        if(normalized.Contains("/Textures/",StringComparison.OrdinalIgnoreCase)&&name.EndsWith(".png",StringComparison.OrdinalIgnoreCase))return true;
        if(string.Equals(name,"manifest.json",StringComparison.OrdinalIgnoreCase))return normalized.Contains("/Fonts/",StringComparison.OrdinalIgnoreCase)||normalized.Contains("/Textures/",StringComparison.OrdinalIgnoreCase)||normalized.Contains("/Audio/",StringComparison.OrdinalIgnoreCase);
        return normalized.Contains("/Strings/",StringComparison.OrdinalIgnoreCase)&&name.EndsWith(".json",StringComparison.OrdinalIgnoreCase) || normalized.Contains("/Almanac/",StringComparison.OrdinalIgnoreCase)&&string.Equals(name,"exact.json",StringComparison.OrdinalIgnoreCase);
    }

    private void OnChanged(object sender,FileSystemEventArgs e)=>NotifyChange(e.FullPath,DateTimeOffset.UtcNow);
    private void OnRenamed(object sender,RenamedEventArgs e){NotifyChange(e.OldFullPath,DateTimeOffset.UtcNow);NotifyChange(e.FullPath,DateTimeOffset.UtcNow);}
    public void Dispose()=>Stop();
}
