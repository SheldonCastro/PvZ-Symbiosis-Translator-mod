using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PvZSymbiosisTranslator.UI;

public enum GameUiRootKind { Setup, Difficulty, CustomMode, TranslatorMenu }

public sealed class GameUiRootRegistry
{
    private sealed class Entry { public GameObject Root; public GameUiRootKind Kind; public bool Open; }
    private readonly Dictionary<int,Entry> entries = new();

    public void RegisterInstance(GameObject root, GameUiRootKind kind)
    {
        if(root==null) return;
        PruneDead();
        var id=root.GetInstanceID();
        if(entries.TryGetValue(id,out var existing)) { existing.Root=root; existing.Kind=kind; return; }
        entries[id]=new Entry { Root=root, Kind=kind };
    }
    public void MarkOpen(GameObject root, GameUiRootKind kind)
    {
        RegisterInstance(root,kind);
        if(root==null) return;
        try { if(entries.TryGetValue(root.GetInstanceID(),out var entry)) entry.Open=true; }
        catch { PruneDead(); }
    }
    public void MarkClosed(GameObject root)
    {
        if(ReferenceEquals(root,null)) return;
        try { if(entries.TryGetValue(root.GetInstanceID(),out var entry)) entry.Open=false; }
        catch { PruneDead(); }
    }
    public void UnregisterDestroyed(GameObject root)
    {
        if(ReferenceEquals(root,null)) return;
        try { entries.Remove(root.GetInstanceID()); } catch { PruneDead(); }
    }
    public bool IsRegistered(GameObject root)
    {
        if(root==null) return false;
        PruneDead();
        try { return entries.ContainsKey(root.GetInstanceID()); } catch { return false; }
    }
    public IReadOnlyList<GameObject> GetForegroundRoots()
    {
        PruneDead();
        return entries.Values.Where(x=>x.Open).Select(x=>x.Root).Where(x=>x!=null).ToArray();
    }
    public void Clear() => entries.Clear();
    public void PruneDead()
    {
        foreach(var pair in entries.ToArray()) {
            try { if(pair.Value.Root==null || pair.Value.Root.gameObject==null) entries.Remove(pair.Key); }
            catch { entries.Remove(pair.Key); }
        }
    }
}
