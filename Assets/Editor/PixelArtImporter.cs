using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for everything under Assets/Art/Pixel: 16 pixels per unit, no filtering,
/// no compression. Pivots and 9-slice borders follow the folder and file name.
/// </summary>
public class PixelArtImporter : AssetPostprocessor
{
    public const string Root = "Assets/Art/Pixel/";
    public const int PixelsPerUnit = 16;

    public override uint GetVersion() => 2;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 0;
        settings.spriteAlignment = (int)(StandsOnGround() ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        importer.SetTextureSettings(settings);

        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (file == "Panel") importer.spriteBorder = new Vector4(6, 6, 6, 6);
        else if (file.StartsWith("Button_")) importer.spriteBorder = new Vector4(5, 5, 5, 5);
        else if (file == "Bar_Frame") importer.spriteBorder = new Vector4(3, 3, 3, 3);
    }

    // Characters, towers, trees, walls and the gate stand on the ground, so their pivot is at their feet.
    bool StandsOnGround()
    {
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (assetPath.Contains("/Towers/") || assetPath.Contains("/Props/") || assetPath.Contains("/Characters/")) return true;
        return assetPath.Contains("/Map/") && !file.StartsWith("BuildSpot");
    }
}
