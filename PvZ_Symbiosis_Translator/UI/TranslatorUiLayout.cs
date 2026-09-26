using System;

namespace PvZSymbiosisTranslator.UI;

public static class TranslatorUiLayout
{
    public const float ReferenceWidth=1920f;
    public const float ReferenceHeight=1080f;
    public const float ContentWidth=1240f;
    public const float TabY=335f;
    public const float TabWidth=226f;
    public const float TabHeight=62f;
    public const float PageY=5f;
    public const float PageWidth=1190f;
    public const float PageHeight=570f;
    public const float StatusY=-365f;
    public const float CloseY=-445f;
    public static readonly float[] TabX={-480f,-240f,0f,240f,480f};

    public static float ClampScale(float percent)=>Math.Clamp(percent,85f,115f);
    public static float ScaleFactor(float percent)=>ClampScale(percent)/100f;
}
