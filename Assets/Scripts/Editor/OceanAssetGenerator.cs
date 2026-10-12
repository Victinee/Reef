using UnityEngine;
using UnityEditor;
using System.IO;

public static class OceanAssetGenerator
{
    [MenuItem("OceanGuard/1. Generate Realistic 2.5D Sprites")]
    public static void GenerateAllSprites()
    {
        string path = "Assets/Sprites";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        // Tiles
        GenerateTile(path + "/tile_forest.png", "#2D6A4F", "#1B4332", true, true);
        GenerateTile(path + "/tile_beach.png", "#E9D8A6", "#EE9B00", true, false);
        GenerateTile(path + "/tile_seabed.png", "#94D2BD", "#0A9396", true, false);

        // Normal Maps
        GenerateNormalMap(path + "/tile_forest_n.png", true);
        GenerateNormalMap(path + "/tile_beach_n.png", false);
        GenerateNormalMap(path + "/tile_seabed_n.png", false);

        // Objects
        GenerateFlora(path + "/sprite_palm.png", "#386641", "#6A994E", true);
        GenerateFlora(path + "/sprite_coral.png", "#FF007F", "#FF758F", false);
        GenerateLitter(path + "/sprite_litter.png", "#DDA15E");

        // Water Textures
        GenerateNoiseTexture(path + "/water_normal.png");
        GenerateVoronoiTexture(path + "/caustic_cookie.png");

        AssetDatabase.Refresh();

        string[] sprites = { "tile_forest", "tile_beach", "tile_seabed", "sprite_palm", "sprite_coral", "sprite_litter" };
        foreach (var s in sprites) ConfigureImporter(path + "/" + s + ".png", TextureImporterType.Sprite, true);
        
        string[] normals = { "tile_forest_n", "tile_beach_n", "tile_seabed_n", "water_normal" };
        foreach (var n in normals) ConfigureImporter(path + "/" + n + ".png", TextureImporterType.NormalMap, false);

        ConfigureImporter(path + "/caustic_cookie.png", TextureImporterType.Cookie, false);

        Debug.Log("[OceanAssetGenerator] Realistic 2.5D Diorama Assets Generated Successfully!");
    }

    private static void ConfigureImporter(string path, TextureImporterType type, bool isSprite)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = type;
            if (isSprite)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64;
                importer.alphaIsTransparency = true;
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.SetTextureSettings(settings);
            }
            if (type == TextureImporterType.Cookie)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                importer.wrapMode = TextureWrapMode.Repeat;
            }
            if (type == TextureImporterType.NormalMap)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
            }
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }

    private static void GenerateTile(string path, string topHex, string sideHex, bool addNoise, bool isElevated)
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        ColorUtility.TryParseHtmlString(topHex, out Color topCol);
        ColorUtility.TryParseHtmlString(sideHex, out Color sideCol);

        float cx = w / 2f;
        float cy = isElevated ? h * 0.65f : h * 0.5f;
        float hw = 120f;
        float hh = 60f;
        float thickness = isElevated ? 80f : 20f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                float dx = Mathf.Abs(x - cx) / hw;
                float dy = Mathf.Abs(y - cy) / hh;

                if (dx + dy <= 1.0f)
                {
                    c = Color.Lerp(sideCol, topCol, (y - (cy - hh)) / (2f * hh));
                    if (addNoise)
                    {
                        float noise = Mathf.Sin(x * 0.8f) * Mathf.Cos(y * 0.8f) * 0.05f;
                        c.r += noise; c.g += noise; c.b += noise;
                    }
                    if (dx + dy > 0.95f) c = Color.Lerp(c, Color.black, 0.3f); // border
                }
                else if (y < cy && dx + Mathf.Abs(y + thickness - cy) / hh <= 1.0f)
                {
                    c = (x < cx ? sideCol * 0.8f : sideCol * 0.5f);
                    if (isElevated) c *= Random.Range(0.85f, 1.0f); // rocky cliff texture
                    if (dx + Mathf.Abs(y + thickness - cy) / hh > 0.95f) c = Color.Lerp(c, Color.black, 0.4f);
                }
                px[y * w + x] = c;
            }
        }
        tex.SetPixels(px); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
    }

    private static void GenerateNormalMap(string path, bool isElevated)
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        
        float cx = w / 2f;
        float cy = isElevated ? h * 0.65f : h * 0.5f;
        float hw = 120f; float hh = 60f; float thickness = isElevated ? 80f : 20f;

        Color topNorm = new Color(0.5f, 0.5f, 1.0f);
        Color leftNorm = new Color(0.2f, 0.5f, 0.8f);
        Color rightNorm = new Color(0.8f, 0.5f, 0.8f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                float dx = Mathf.Abs(x - cx) / hw;
                float dy = Mathf.Abs(y - cy) / hh;

                if (dx + dy <= 1.0f)
                {
                    c = topNorm;
                    float noise = Random.Range(-0.05f, 0.05f);
                    c.r += noise; c.g += noise;
                }
                else if (y < cy && dx + Mathf.Abs(y + thickness - cy) / hh <= 1.0f)
                {
                    c = (x < cx ? leftNorm : rightNorm);
                    float noise = Random.Range(-0.15f, 0.15f);
                    c.r += noise; c.g += noise;
                }
                px[y * w + x] = c;
            }
        }
        tex.SetPixels(px); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
    }

    private static void GenerateFlora(string path, string hex1, string hex2, bool isPalm)
    {
        int w = 128, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        ColorUtility.TryParseHtmlString(hex1, out Color c1);
        ColorUtility.TryParseHtmlString(hex2, out Color c2);

        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        if (isPalm)
        {
            // Simple palm leaves
            Vector2 center = new Vector2(64, 80);
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d < 30f && (y > 70 || Mathf.Abs(x - 64) > 15)) {
                        px[y * w + x] = Color.Lerp(c1, c2, d / 30f);
                    }
                    // Trunk
                    if (y >= 20 && y <= 80 && Mathf.Abs(x - 64 - Mathf.Sin(y * 0.1f) * 5f) < 4f) {
                        px[y * w + x] = new Color(0.4f, 0.25f, 0.1f); // brown
                    }
                }
            }
        }
        else
        {
            // Coral
            Vector2[] branches = { new Vector2(64, 40), new Vector2(40, 70), new Vector2(88, 65), new Vector2(30, 95), new Vector2(100, 90), new Vector2(64, 110) };
            float[] radii = { 24f, 18f, 18f, 14f, 14f, 12f };
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    for (int i = 0; i < branches.Length; i++) {
                        float d = Vector2.Distance(new Vector2(x, y), branches[i]);
                        if (d < radii[i]) px[y * w + x] = Color.Lerp(c1, c2, d / radii[i]);
                    }
                    if (y > 10 && y <= 40 && Mathf.Abs(x - 64f) < 8f) px[y * w + x] = c2;
                }
            }
        }
        tex.SetPixels(px); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
    }

    private static void GenerateLitter(string path, string hex)
    {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        ColorUtility.TryParseHtmlString(hex, out Color col);

        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float dx = x - 32f, dy = y - 32f;
                float val = (dx * 0.86f + dy * 0.5f) * (dx * 0.86f + dy * 0.5f) / 80f + (-dx * 0.5f + dy * 0.86f) * (-dx * 0.5f + dy * 0.86f) / 12f;
                if (val <= 1.0f) {
                    px[y * w + x] = Color.Lerp(col, Color.black, val * 0.5f);
                    px[y * w + x].a = 1f;
                }
            }
        }
        tex.SetPixels(px); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
    }

    private static void GenerateNoiseTexture(string path)
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                // Convert noise to a valid normal map color
                px[y * w + x] = new Color(noise, noise, 1f); 
            }
        }
        tex.SetPixels(px); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
    }

    private static void GenerateVoronoiTexture(string path)
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        
        Random.InitState(42);
        Vector2[] pts = new Vector2[30];
        for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(Random.Range(0, w), Random.Range(0, h));

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float minDist = w;
                foreach (var p in pts) {
                    float d = Vector2.Distance(new Vector2(x, y), p);
                    if (d < minDist) minDist = d;
                }
                float intensity = Mathf.Clamp01(1f - minDist / 25f);
                intensity = Mathf.Pow(intensity, 3f); // sharp edges for caustics
                px[y * w + x] = new Color(intensity, intensity, intensity, 1f);
            }
        }
        tex.SetPixels(px); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
    }
}
