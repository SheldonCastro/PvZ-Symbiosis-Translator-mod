using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using PvZSymbiosisTranslator.Assets;

namespace PvZSymbiosisTranslator.Patches.Game;

[HarmonyPatch(typeof(Tool), "AudioClipGet")]
static class AudioClipGetPatch
{
    static void Postfix(string name, ref AudioClip __result)
    {
        if (PvZSymbiosisTranslatorMod.Config.Enabled && PvZSymbiosisTranslatorMod.Config.AudioReplacement && PvZSymbiosisTranslatorMod.Audio.TryGet(name, out var replacement)) __result = replacement;
    }
}
