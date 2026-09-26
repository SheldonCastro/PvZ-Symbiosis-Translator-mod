using HarmonyLib;
using static PvZSymbiosisTranslator.PvZSymbiosisTranslatorMod;
namespace PvZSymbiosisTranslator.Patches.Game
{
        [HarmonyPatch(typeof(Il2Cpp.HomeManager), "Start")]
        static class HomeManager_Start_NativeUi_Patch
        {
            static void Postfix(Il2Cpp.HomeManager __instance)
            {
                try { if(__instance!=null) PvZSymbiosisTranslatorMod.InstallNativeHomeLauncher(__instance); }
                catch(System.Exception ex) { MelonLoader.MelonLogger.Warning("[NativeUI] Home launcher install failed: "+ex.Message); }
            }
        }

        [HarmonyPatch(typeof(Il2Cpp.HomeManager), "OnIllustratedClick")]
        static class HomeManager_Illustrated_NativeUi_Patch
        {
            static void Prefix(Il2Cpp.HomeManager __instance) => PvZSymbiosisTranslatorMod.TraceHomeLauncherSnapshot("before-almanac",__instance);
        }

        [HarmonyPatch(typeof(Il2Cpp.ButtonJump), "CloseSceneButtonClick", new System.Type[] { typeof(string) })]
        static class ButtonJump_CloseScene_NativeUi_Patch
        {
            static void Postfix(string sceneName) => PvZSymbiosisTranslatorMod.OnAlmanacSceneClosing(sceneName);
        }

        [HarmonyPatch(typeof(Il2Cpp.HelpWindow), "Start")]
        static class HelpWindow_Start_NativeUi_Patch
        {
            static void Postfix(Il2Cpp.HelpWindow __instance)
            {
                try { if(__instance!=null) PvZSymbiosisTranslatorMod.TryInstallNativeTranslatorMenu(__instance); }
                catch(System.Exception ex) { MelonLoader.MelonLogger.Warning("[NativeUI] translator menu install failed: "+ex.Message); }
            }
        }

        [HarmonyPatch(typeof(Il2Cpp.HelpWindow), "OnClickClose")]
        static class HelpWindow_Close_NativeUi_Patch
        {
            static void Prefix(Il2Cpp.HelpWindow __instance) { if(__instance!=null) PvZSymbiosisTranslatorMod.CloseNativeTranslatorMenu(__instance.gameObject); }
        }

        [HarmonyPatch(typeof(Il2Cpp.SetupWindow), "Start")]
        static class SetupWindow_Start_Patch
        {
            static void Prefix(Il2Cpp.SetupWindow __instance) => PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.Start","before");
            static void Postfix(Il2Cpp.SetupWindow __instance)
            {
                try { if (__instance != null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.Start","afterGame"); PvZSymbiosisTranslatorMod.RegisterUiRoot(__instance.gameObject, PvZSymbiosisTranslator.UI.GameUiRootKind.Setup); PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.Start","afterTranslator"); } }
                catch (System.Exception ex) { MelonLoader.MelonLogger.Warning("[UIRefresh] SetupWindow.Start wrapper invalid: "+ex.Message); }
            }
        }

        [HarmonyPatch(typeof(Il2Cpp.SetupWindow), "SetInGame", new System.Type[] { typeof(bool) })]
        static class SetupWindow_SetInGame_Patch
        {
            static void Prefix(Il2Cpp.SetupWindow __instance) => PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.SetInGame","before");
            static void Postfix(Il2Cpp.SetupWindow __instance)
            {
                try { if (__instance != null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.SetInGame","afterGame"); PvZSymbiosisTranslatorMod.RegisterUiRoot(__instance.gameObject, PvZSymbiosisTranslator.UI.GameUiRootKind.Setup); PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.SetInGame","afterTranslator"); } }
                catch (System.Exception ex) { MelonLoader.MelonLogger.Warning("[UIRefresh] SetupWindow.SetInGame wrapper invalid: "+ex.Message); }
            }
        }

        [HarmonyPatch(typeof(Il2Cpp.SetupWindow), "OnClickClose")]
        static class SetupWindow_Close_Patch
        {
            static void Prefix(Il2Cpp.SetupWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.OnClickClose","before"); PvZSymbiosisTranslatorMod.CloseUiRoot(__instance.gameObject); } }
            static void Postfix(Il2Cpp.SetupWindow __instance) { if(__instance!=null) PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.OnClickClose","afterGame"); }
        }

        [HarmonyPatch(typeof(Il2Cpp.SetupWindow), "OnDestroy")]
        static class SetupWindow_Destroy_Patch
        {
            static void Prefix(Il2Cpp.SetupWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.OnDestroy","before"); PvZSymbiosisTranslatorMod.DestroyUiRoot(__instance.gameObject); } }
        }

        [HarmonyPatch(typeof(Il2Cpp.SetupWindow), "OnChangeMusicVolume")]
        static class SetupWindow_Music_Patch
        { static void Prefix(Il2Cpp.SetupWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.OnChangeMusicVolume","before"); static void Postfix(Il2Cpp.SetupWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.OnChangeMusicVolume","afterGame"); }

        [HarmonyPatch(typeof(Il2Cpp.SetupWindow), "OnChangeSoundVolume")]
        static class SetupWindow_Sound_Patch
        { static void Prefix(Il2Cpp.SetupWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.OnChangeSoundVolume","before"); static void Postfix(Il2Cpp.SetupWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.OnChangeSoundVolume","afterGame"); }

        [HarmonyPatch(typeof(Il2Cpp.SetupWindow), "OnToggleFullscreen")]
        static class SetupWindow_Fullscreen_Patch
        { static void Prefix(Il2Cpp.SetupWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.OnToggleFullscreen","before"); static void Postfix(Il2Cpp.SetupWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"SetupWindow.OnToggleFullscreen","afterGame"); }

        [HarmonyPatch(typeof(Il2Cpp.DifficultyWindow), "Init")]
        static class DifficultyWindow_Init_Patch
        {
            static void Prefix(Il2Cpp.DifficultyWindow __instance) => PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.Init","before");
            static void Postfix(Il2Cpp.DifficultyWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.Init","afterGame"); PvZSymbiosisTranslatorMod.RegisterUiRoot(__instance.gameObject, PvZSymbiosisTranslator.UI.GameUiRootKind.Difficulty); PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.Init","afterTranslator"); } }
        }

        [HarmonyPatch(typeof(Il2Cpp.DifficultyWindow), "OnClickChangeDifficulty")]
        static class DifficultyWindow_Change_Patch
        {
            static void Prefix(Il2Cpp.DifficultyWindow __instance) => PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.OnClickChangeDifficulty","before");
            static void Postfix(Il2Cpp.DifficultyWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.OnClickChangeDifficulty","afterGame"); PvZSymbiosisTranslatorMod.RegisterUiRoot(__instance.gameObject, PvZSymbiosisTranslator.UI.GameUiRootKind.Difficulty); PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.OnClickChangeDifficulty","afterTranslator"); } }
        }

        [HarmonyPatch(typeof(Il2Cpp.DifficultyWindow), "OnClickClose")]
        static class DifficultyWindow_Close_Patch
        { static void Prefix(Il2Cpp.DifficultyWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.OnClickClose","before"); PvZSymbiosisTranslatorMod.CloseUiRoot(__instance.gameObject); } } static void Postfix(Il2Cpp.DifficultyWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.OnClickClose","afterGame"); }

        [HarmonyPatch(typeof(Il2Cpp.DifficultyWindow), "OnClickConfirm")]
        static class DifficultyWindow_Confirm_Patch
        { static void Prefix(Il2Cpp.DifficultyWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.OnClickConfirm","before"); PvZSymbiosisTranslatorMod.CloseUiRoot(__instance.gameObject); } } static void Postfix(Il2Cpp.DifficultyWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"DifficultyWindow.OnClickConfirm","afterGame"); }

        [HarmonyPatch(typeof(Il2Cpp.CustomModeWindow), "Start")]
        static class CustomModeWindow_Start_Patch
        {
            static void Prefix(Il2Cpp.CustomModeWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.Start","before");
            static void Postfix(Il2Cpp.CustomModeWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.Start","afterGame"); PvZSymbiosisTranslatorMod.RegisterUiRoot(__instance.gameObject, PvZSymbiosisTranslator.UI.GameUiRootKind.CustomMode); } }
        }

        [HarmonyPatch(typeof(Il2Cpp.CustomModeWindow), "SetMenuIndex")]
        static class CustomModeWindow_SetMenuIndex_Patch
        {
            static void Prefix(Il2Cpp.CustomModeWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.SetMenuIndex","before");
            static void Postfix(Il2Cpp.CustomModeWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.SetMenuIndex","afterGame"); PvZSymbiosisTranslatorMod.RegisterUiRoot(__instance.gameObject, PvZSymbiosisTranslator.UI.GameUiRootKind.CustomMode); } }
        }

        [HarmonyPatch(typeof(Il2Cpp.CustomModeWindow), "FillEventDescription")]
        static class CustomModeWindow_FillEventDescription_Patch
        {
            static void Prefix(Il2Cpp.CustomModeWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.FillEventDescription","before");
            static void Postfix(Il2Cpp.CustomModeWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.FillEventDescription","afterGame"); PvZSymbiosisTranslatorMod.RegisterUiRoot(__instance.gameObject, PvZSymbiosisTranslator.UI.GameUiRootKind.CustomMode); PvZSymbiosisTranslatorMod.RefreshCustomEventDescription(__instance); PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.FillEventDescription","afterTranslator"); } }
        }

        [HarmonyPatch(typeof(Il2Cpp.CustomModeWindow), "OnEventToggleChanged")]
        static class CustomModeWindow_EventToggle_Patch
        { static void Prefix(Il2Cpp.CustomModeWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.OnEventToggleChanged","before"); static void Postfix(Il2Cpp.CustomModeWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.OnEventToggleChanged","afterGame"); PvZSymbiosisTranslatorMod.RefreshCustomEventDescription(__instance); PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.OnEventToggleChanged","afterTranslator"); } } }

        [HarmonyPatch(typeof(Il2Cpp.CustomModeWindow), "OnCloseClick")]
        static class CustomModeWindow_Close_Patch
        { static void Prefix(Il2Cpp.CustomModeWindow __instance) { if(__instance!=null) { PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.OnCloseClick","before"); PvZSymbiosisTranslatorMod.CloseUiRoot(__instance.gameObject); } } static void Postfix(Il2Cpp.CustomModeWindow __instance)=>PvZSymbiosisTranslatorMod.TraceUiMethod(__instance,"CustomModeWindow.OnCloseClick","afterGame"); }

        [HarmonyPatch(typeof(Il2Cpp.HomeManager), "OnOptionClick")]
        static class HomeManager_OptionTrace_Patch
        { static void Prefix(Il2Cpp.HomeManager __instance)=>PvZSymbiosisTranslatorMod.TraceHomeRequest(__instance,"HomeManager.OnOptionClick","before"); static void Postfix(Il2Cpp.HomeManager __instance)=>PvZSymbiosisTranslatorMod.TraceHomeRequest(__instance,"HomeManager.OnOptionClick","afterGame"); }

        [HarmonyPatch(typeof(Il2Cpp.HomeManager), "OnDifficultyClick")]
        static class HomeManager_DifficultyTrace_Patch
        { static void Prefix(Il2Cpp.HomeManager __instance)=>PvZSymbiosisTranslatorMod.TraceHomeRequest(__instance,"HomeManager.OnDifficultyClick","before"); static void Postfix(Il2Cpp.HomeManager __instance)=>PvZSymbiosisTranslatorMod.TraceHomeRequest(__instance,"HomeManager.OnDifficultyClick","afterGame"); }

        [HarmonyPatch(typeof(Il2Cpp.PromptText), "SetText", new System.Type[] { typeof(string), typeof(float), typeof(string), typeof(Il2CppSystem.Action), typeof(bool) })]
        static class PromptText_SetText_Patch
        {
            static void Prefix(Il2Cpp.PromptText __instance, ref string text)
            {
                if (!Config.Enabled || text == null) return;
                ObserveGameText(text);
            }
        }

        [HarmonyPatch(typeof(Il2Cpp.DialogWindow), "Init", new System.Type[] { typeof(string), typeof(Il2CppSystem.Action), typeof(Il2CppSystem.Action), typeof(string), typeof(string) })]
        static class DialogWindow_Init_Patch
        {
            static void Prefix(Il2Cpp.DialogWindow __instance, ref string text, ref string leftText, ref string rightText)
            {
                if (!Config.Enabled) return;
                
                if (!string.IsNullOrEmpty(text))
                    ObserveGameText(text);
                
                if (!string.IsNullOrEmpty(leftText))
                    ObserveGameText(leftText);
                
                if (!string.IsNullOrEmpty(rightText))
                    ObserveGameText(rightText);
            }
        }

        [HarmonyPatch(typeof(Il2Cpp.PromptWindow), "SetText", new System.Type[] { typeof(string) })]
        static class PromptWindow_SetText_Patch
        {
            static void Prefix(Il2Cpp.PromptWindow __instance, ref string text)
            {
                if (!Config.Enabled || text == null) return;
                ObserveGameText(text);
            }
        }


}
