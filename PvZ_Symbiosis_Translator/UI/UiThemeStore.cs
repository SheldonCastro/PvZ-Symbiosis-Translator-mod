using System;
using System.IO;
using MelonLoader;
using UnityEngine;
using PvZSymbiosisTranslator.Core;
using PvZSymbiosisTranslator.Assets;

namespace PvZSymbiosisTranslator.UI;

/// <summary>Small, isolated cache for the two shipped UI theme images.</summary>
public sealed class UiThemeStore : IDisposable
{
    private readonly string _uiDirectory;
    private Texture2D _background;
    private Texture2D _button;

    private Sprite _backgroundSprite;
    private Sprite _buttonSprite;
    public enum ThemeState { NotLoaded, Loading, Ready, Failed }
    public ThemeState State { get; private set; }
    public bool IsReady => State == ThemeState.Ready && _background!=null && _button!=null;
    public Action ThemeReady;
    public Texture2D BackgroundTexture => _background;
    public Texture2D ButtonTexture => _button;
    public Sprite BackgroundSprite => _backgroundSprite;
    public Sprite ButtonSprite => _buttonSprite;

    public UiThemeStore(ModPaths paths)
    {
        if (paths == null) throw new ArgumentNullException(nameof(paths));
        _uiDirectory = Path.Combine(paths.Root, "UI");
        MelonLogger.Msg($"[UI Theme] root = {_uiDirectory}");
        MelonLogger.Msg($"[UI Theme] Background2.png exists = {File.Exists(Path.Combine(_uiDirectory, "Background2.png"))}");
        MelonLogger.Msg($"[UI Theme] buttonsmall.png exists = {File.Exists(Path.Combine(_uiDirectory, "buttonsmall.png"))}");
        MelonLogger.Msg("[UiThemeStore] Constructor called");
    }

    public void Load()
    {
        if(State==ThemeState.Loading || IsReady) return;
        State=ThemeState.Loading;
        MelonCoroutines.Start(LoadAsync());
    }

    public void Reload() { DisposeTextures(); State=ThemeState.NotLoaded; Load(); }

    public System.Collections.IEnumerator LoadAsync()
    {
        yield return null;
        DisposeTextures(); State = ThemeState.Loading;
        var failed=false;
        var bgPath=Path.Combine(_uiDirectory,"Background2.png"); var buttonPath=Path.Combine(_uiDirectory,"buttonsmall.png");
        MelonLogger.Msg($"[UI Theme] Background path: {bgPath}\n[UI Theme] Background URI: {new Uri(Path.GetFullPath(bgPath)).AbsoluteUri}");
        try { _background=RuntimeImageService.LoadPng(bgPath); _backgroundSprite=CreateSprite(_background,"Background2"); MelonLogger.Msg($"[UI Theme] Background texture: {_background.width}x{_background.height}; Background sprite: READY"); } catch(Exception ex) { failed=true; MelonLogger.Warning("[UI Theme] Background LOAD IMAGE FAILED: "+ex); }
        MelonLogger.Msg($"[UI Theme] Button path: {buttonPath}\n[UI Theme] Button URI: {new Uri(Path.GetFullPath(buttonPath)).AbsoluteUri}");
        try { _button=RuntimeImageService.LoadPng(buttonPath); _buttonSprite=CreateSprite(_button,"buttonsmall"); MelonLogger.Msg($"[UI Theme] Button texture: {_button.width}x{_button.height}; Button sprite: READY"); } catch(Exception ex) { failed=true; MelonLogger.Warning("[UI Theme] Button LOAD IMAGE FAILED: "+ex); }
        State = (!failed && _backgroundSprite != null && _buttonSprite != null) ? ThemeState.Ready : ThemeState.Failed;
        MelonLogger.Msg($"[UI Theme] UI Theme: {(IsReady ? "READY" : "FAILED")}"); if(IsReady) ThemeReady?.Invoke();
    }


    private void DisposeTextures()
    {
        if (_backgroundSprite != null) UnityEngine.Object.Destroy(_backgroundSprite);
        if (_buttonSprite != null) UnityEngine.Object.Destroy(_buttonSprite);
        if (_background != null) UnityEngine.Object.Destroy(_background);
        if (_button != null) UnityEngine.Object.Destroy(_button);
        _backgroundSprite = null;
        _buttonSprite = null;
        _background = null;
        _button = null;
    }

    private static Sprite CreateSprite(Texture2D texture, string name)
    {
        if (texture == null) return null;
        var sprite=Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
        if(sprite!=null) sprite.hideFlags=HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }

    public void Dispose() => DisposeTextures();
}
