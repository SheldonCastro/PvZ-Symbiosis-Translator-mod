namespace PvZSymbiosisTranslator.UI;

public enum LauncherHealthAction { Unavailable, Install, Rebind, Healthy }

public static class NativeHomeLauncherPolicy
{
    public static LauncherHealthAction Decide(bool homeAvailable, bool launcherExists,
        bool activeSelf, bool activeInHierarchy, bool buttonEnabled, bool interactable,
        bool imageEnabled, bool raycastTarget, bool callbackCurrent)
    {
        if(!homeAvailable) return LauncherHealthAction.Unavailable;
        if(!launcherExists) return LauncherHealthAction.Install;
        return activeSelf && activeInHierarchy && buttonEnabled && interactable &&
               imageEnabled && raycastTarget && callbackCurrent
            ? LauncherHealthAction.Healthy
            : LauncherHealthAction.Rebind;
    }
}
