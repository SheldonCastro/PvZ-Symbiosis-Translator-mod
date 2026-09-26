using System;
using UnityEngine;

namespace PvZSymbiosisTranslator.Core;

public sealed class HotkeyService
{
    private readonly Func<string, bool> toggle;
    private readonly Func<string, bool> reload;
    private readonly Action<string> dump;
    public HotkeyService(Func<string, bool> toggle, Func<string, bool> reload, Action<string> dump)
    { this.toggle = toggle; this.reload = reload; this.dump = dump; }
    public void Update(string toggleKey, string reloadKey, string diagnosticKey)
    {
        if (TryDown(toggleKey)) toggle(toggleKey);
        if (TryDown(reloadKey)) reload(reloadKey);
        if (TryDown(diagnosticKey)) dump(diagnosticKey);
    }
    private static bool TryDown(string value) => Enum.TryParse<KeyCode>(value, true, out var key) && Input.GetKeyDown(key);
}
