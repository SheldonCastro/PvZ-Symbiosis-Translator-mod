using HarmonyLib;
using Il2CppTMPro;
using UnityEngine.UI;

namespace PvZSymbiosisTranslator.Patches.Text;

[HarmonyPatch(typeof(TMP_Text), "text", MethodType.Setter)]
static class TmpSetter
{
    static void Prefix(TMP_Text __instance, ref string value) { PvZSymbiosisTranslatorMod.ObserveTextApi(__instance, value, "TMP_Text.text"); value = PvZSymbiosisTranslatorMod.TranslateComponent(__instance, value); }
}
[HarmonyPatch(typeof(TMP_Text), "SetText", new System.Type[] { typeof(string) })]
static class TmpSetTextStringProbe
{
    static void Prefix(TMP_Text __instance, string sourceText) => PvZSymbiosisTranslatorMod.ObserveTextApi(__instance, sourceText, "TMP_Text.SetText(string)");
}
[HarmonyPatch(typeof(TMP_Text), "SetText", new System.Type[] { typeof(string), typeof(bool) })]
static class TmpSetTextBoolProbe
{
    static void Prefix(TMP_Text __instance, string sourceText) => PvZSymbiosisTranslatorMod.ObserveTextApi(__instance, sourceText, "TMP_Text.SetText(string,bool)");
}
[HarmonyPatch(typeof(TMP_Text), "SetText", new System.Type[] { typeof(string), typeof(float) })]
static class TmpSetTextFloatProbe
{
    static void Prefix(TMP_Text __instance, string sourceText, float arg0) => PvZSymbiosisTranslatorMod.ObserveTextApi(__instance, sourceText, "TMP_Text.SetText(string,float)", arg0);
}
[HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
static class TmpUiEnable
{
    static void Postfix(TextMeshProUGUI __instance) => PvZSymbiosisTranslatorMod.Refresh(__instance);
}
[HarmonyPatch(typeof(TextMeshPro), "OnEnable")]
static class TmpMeshEnable
{
    static void Postfix(TextMeshPro __instance) => PvZSymbiosisTranslatorMod.Refresh(__instance);
}
[HarmonyPatch(typeof(UnityEngine.UI.Text), "text", MethodType.Setter)]
static class LegacySetter
{
    static void Prefix(UnityEngine.UI.Text __instance, ref string value) { PvZSymbiosisTranslatorMod.ObserveTextApi(__instance, value, "UI.Text.text"); value = PvZSymbiosisTranslatorMod.TranslateComponent(__instance, value); }
}
[HarmonyPatch(typeof(UnityEngine.UI.Text), "OnEnable")]
static class LegacyEnable
{
    static void Postfix(UnityEngine.UI.Text __instance) => PvZSymbiosisTranslatorMod.Refresh(__instance);
}
