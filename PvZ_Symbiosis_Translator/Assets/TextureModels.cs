using System.Collections.Generic;

namespace PvZSymbiosisTranslator.Assets;

public sealed class TextureCatalogEntry
{
    public string id { get; set; }
    public string textureName { get; set; }
    public string spriteName { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public string hash { get; set; }
    public string replacement { get; set; }
}

public sealed class TextureReplacementEntry
{
    public string id { get; set; }
    public string textureName { get; set; }
    public string spriteName { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public string hash { get; set; }
    public string replacement { get; set; }
}

public sealed class TextureManifest
{
    public List<TextureReplacementEntry> entries { get; set; } = new List<TextureReplacementEntry>();
}
