using System.Collections.Generic;

namespace PvZSymbiosisTranslator.UI;

public static class NativeAssetIds
{
    public const string ButtonSmall="Translator.Button.Small";
    public const string ButtonBig="Translator.Button.Big";
    public const string Background="Translator.Background";
    public const string CloseButton="Translator.Button.Close";
    public const string CheckboxOn="Translator.Checkbox.On";
    public const string CheckboxOff="Translator.Checkbox.Off";
    public const string SliderTrack="Translator.Slider.Track";
    public const string SliderKnob="Translator.Slider.Knob";
    public static readonly IReadOnlyList<string> All=new[] {ButtonSmall,ButtonBig,Background,CloseButton,CheckboxOn,CheckboxOff,SliderTrack,SliderKnob};
    public static readonly IReadOnlyDictionary<string,string> ResourcePaths=new Dictionary<string,string>(System.StringComparer.Ordinal) {
        [ButtonSmall]="image/interface/buttonsmall",
        [ButtonBig]="image/interface/buttonbig",
        [Background]="image/interface/challengebackground",
        [CloseButton]="image/interface/optionsbacktogamebutton",
        [CheckboxOn]="image/interface/optionscheckboxopen",
        [CheckboxOff]="image/interface/optionscheckbox",
        [SliderTrack]="image/interface/optionssliderslot",
        [SliderKnob]="image/interface/optionssliderknob"
    };
}
