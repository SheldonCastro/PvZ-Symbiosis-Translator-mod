using System;
using Il2CppTMPro;
using UnityEngine;

namespace PvZSymbiosisTranslator.Diagnostics;

// Read-only: never activates objects, refreshes text, or touches graphics.
public sealed class RuntimeTextScanner
{
    public readonly struct Result { public readonly int Tmp, Legacy, Chinese; public Result(int tmp, int legacy, int chinese) { Tmp = tmp; Legacy = legacy; Chinese = chinese; } }
    public Result Scan(Func<Component,string,string> original, Action<Component,string> observe)
    {
        var tmpCount = 0; var legacyCount = 0; var chinese = 0;
        void Collect(Component component, string current)
        {
            var source = original(component, current);
            if (source == null) return;
            if (HanSourceDetector.ContainsHan(source)) chinese++;
            observe(component, source);
        }
        foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text == null || !text.gameObject.activeInHierarchy) continue;
            tmpCount++; Collect(text, text.text);
        }
        foreach (var text in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>())
        {
            if (text == null || !text.gameObject.activeInHierarchy) continue;
            legacyCount++; Collect(text, text.text);
        }
        return new Result(tmpCount, legacyCount, chinese);
    }
}
