using MelonLoader;
namespace PvZSymbiosisTranslator.UI;

public sealed class ToastService
{
    public bool Suppressed { get; set; }
    public void Show(string message, bool error = false)
    {
        if (Suppressed) return;
        if(error) MelonLogger.Warning("[NOTIFICATION] " + message);
        else MelonLogger.Msg("[NOTIFICATION] " + message);
    }
    public void Update() { }
}
