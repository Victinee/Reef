using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReefVisualsSetup
{
    [MenuItem("OceanGuard/2. Setup 2.5D Realistic Diorama")]
    public static void SetupVisuals()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        // Clean up previous elements
        DestroyExisting("OceanWorld");
        DestroyExisting("Global Volume");
        DestroyExisting("Sun Directional Light");
        DestroyExisting("Caustics Light");
        DestroyExisting("Global Light 2D"); // cleanup old 2D light

        // 1. Configure Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            ColorUtility.TryParseHtmlString("#001219", out Color bg);
            mainCam.backgroundColor = bg;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6f;
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
        }

        // 2. Setup Materials
        Material matForest = CreateMaterial("Mat_Forest", "tile_forest", "tile_forest_n", false);
        Material matBeach = CreateMaterial("Mat_Beach", "tile_beach", "tile_beach_n", false);
        Material matSeabed = CreateMaterial("Mat_Seabed", "tile_seabed", "tile_seabed_n", false);
        Material matWater = CreateWaterMaterial();

        // 3. Post-Processing Stack
        GameObject globalVolGO = new GameObject("Global Volume");
        Undo.RegisterCreatedObjectUndo(globalVolGO, "Create Global Volume");
        Volume vol = globalVolGO.AddComponent<Volume>();
        vol.isGlobal = true;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, "Assets/Settings/ReefPostProcessing_Realistic.asset");
        vol.profile = profile;

        Tonemapping tm = profile.Add<Tonemapping>();
        tm.active = true;
        tm.mode.Override(TonemappingMode.ACES);

        Bloom bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(0.7f);

        DepthOfField dof = profile.Add<DepthOfField>();
        dof.active = true;
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(15f);
        dof.gaussianEnd.Override(30f);
        dof.gaussianMaxRadius.Override(1.5f);

        // 4. Lighting
        GameObject sun = new GameObject("Sun Directional Light");
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        ColorUtility.TryParseHtmlString("#FFF4E0", out Color sunColor);
        sunLight.color = sunColor;
        sunLight.intensity = 1.2f;
        sun.transform.rotation = Quaternion.Euler(50, -30, 0);
        sunLight.shadows = LightShadows.Soft;

        GameObject caustics = new GameObject("Caustics Light");
        Light causticsLightComp = caustics.AddComponent<Light>();
        causticsLightComp.type = LightType.Directional;
        causticsLightComp.color = Color.cyan;
        causticsLightComp.intensity = 0.6f;
        causticsLightComp.cookie = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/caustic_cookie.png");
        causticsLightComp.cookieSize2D = new Vector2(8f, 8f);
        caustics.transform.rotation = Quaternion.Euler(90, 0, 0);

        // 5. Build OceanWorld Layout
        GameObject oceanWorld = new GameObject("OceanWorld");
        Undo.RegisterCreatedObjectUndo(oceanWorld, "Create OceanWorld");

        Sprite spritePalm = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sprite_palm.png");
        Sprite spriteCoral = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sprite_coral.png");
        Sprite spriteLitter = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sprite_litter.png");
        
        Material matPalm = CreateMaterial("Mat_Palm", "sprite_palm", null, true);
        Material matCoral = CreateMaterial("Mat_Coral", "sprite_coral", null, true);
        Material matLitter = CreateMaterial("Mat_Litter", "sprite_litter", null, true);

        int litterCount = 0;

        for (int x = -4; x <= 4; x++)
        {
            for (int y = -4; y <= 4; y++)
            {
                int dist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                Material m = matSeabed;
                string type = "Seabed";
                float elevation = 0.0f;
                float baseZ = 0.0f;

                if (dist <= 1) { m = matForest; type = "Forest"; elevation = 0.8f; baseZ = -0.5f; }
                else if (dist == 2) { m = matBeach; type = "Beach"; elevation = 0.4f; baseZ = -0.2f; }

                GameObject tile = new GameObject($"Tile_{type}_{x}_{y}");
                tile.transform.parent = oceanWorld.transform;
                
                float posX = (x - y) * 0.93f; 
                float posY = (x + y) * 0.46f + elevation;
                float posZ = baseZ + posY * 0.01f; // Isometric Z-depth sorting
                
                tile.transform.position = new Vector3(posX, posY, posZ);

                var sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/tile_{type.ToLower()}.png");
                sr.material = m;
                sr.receiveShadows = true;
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                if (type == "Forest" && (x + y) % 2 == 0)
                {
                    GameObject palm = new GameObject($"Palm_{x}_{y}");
                    palm.transform.parent = tile.transform;
                    palm.transform.localPosition = new Vector3(0, 0.5f, -0.05f);
                    var palmSr = palm.AddComponent<SpriteRenderer>();
                    palmSr.sprite = spritePalm;
                    palmSr.material = matPalm;
                    palmSr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }

                if (type == "Beach" && litterCount < 5)
                {
                    GameObject litter = new GameObject($"LitterNode_{litterCount}");
                    litter.transform.parent = oceanWorld.transform;
                    litter.transform.position = new Vector3(posX, posY + 0.2f, posZ - 0.02f);
                    var lsr = litter.AddComponent<SpriteRenderer>();
                    lsr.sprite = spriteLitter;
                    lsr.material = matLitter;
                    
                    var col = litter.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(0.8f, 0.8f);
                    
                    var lnType = System.Type.GetType("LitterNode, Assembly-CSharp");
                    if (lnType != null) litter.AddComponent(lnType);

                    litterCount++;
                }

                if (type == "Seabed" && (x + y) % 2 != 0)
                {
                    GameObject coral = new GameObject($"Coral_{x}_{y}");
                    coral.transform.parent = oceanWorld.transform;
                    coral.transform.position = new Vector3(posX, posY + 0.2f, posZ - 0.02f);
                    var csr = coral.AddComponent<SpriteRenderer>();
                    csr.sprite = spriteCoral;
                    csr.material = matCoral;

                    var pLight = coral.AddComponent<Light>();
                    pLight.type = LightType.Point;
                    ColorUtility.TryParseHtmlString("#00F5D4", out Color coralLightCol);
                    pLight.color = coralLightCol;
                    pLight.intensity = 2.5f;
                    pLight.range = 3f;
                }
            }
        }

        // 6. Water Plane
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Quad);
        water.name = "Water_Plane";
        water.transform.parent = oceanWorld.transform;
        water.transform.position = new Vector3(0, 0.3f, -0.1f);
        water.transform.localScale = new Vector3(20f, 20f, 1f);
        Object.DestroyImmediate(water.GetComponent<Collider>());
        water.GetComponent<MeshRenderer>().material = matWater;

        // 7. Atmospheric VFX
        GameObject vfx = new GameObject("VFX_Bubbles");
        vfx.transform.parent = oceanWorld.transform;
        vfx.transform.position = new Vector3(0, -2, -0.15f);
        
        var ps = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        ColorUtility.TryParseHtmlString("#E0F7FA", out Color bubCol);
        bubCol.a = 0.45f;
        main.startColor = bubCol;
        main.startSize = 0.1f;
        main.startSpeed = 0.5f;
        main.startLifetime = 6f;
        main.gravityModifier = -0.05f; 

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(15f, 0f, 1f);

        var particleSr = vfx.GetComponent<ParticleSystemRenderer>();
        particleSr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

        Debug.Log("[ReefVisualsSetup] ✅ 2.5D Realistic Diorama fully set up!");
    }

    private static void DestroyExisting(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    private static Material CreateMaterial(string matName, string texName, string normalName, bool alphaClip)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Sprites/{texName}.png");
        if (albedo != null) mat.SetTexture("_BaseMap", albedo);

        if (normalName != null)
        {
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Sprites/{normalName}.png");
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1.5f);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        if (alphaClip)
        {
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
        }

        AssetDatabase.CreateAsset(mat, $"Assets/Settings/{matName}.mat");
        return mat;
    }

    private static Material CreateWaterMaterial()
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1); 
        mat.SetInt("_Blend", 0); 
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        
        ColorUtility.TryParseHtmlString("#00F5D4", out Color c);
        c.a = 0.5f;
        mat.SetColor("_BaseColor", c);
        mat.SetFloat("_Smoothness", 0.95f);
        mat.SetFloat("_Metallic", 0.1f);

        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/water_normal.png");
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 0.8f);
            mat.EnableKeyword("_NORMALMAP");
        }

        AssetDatabase.CreateAsset(mat, "Assets/Settings/Mat_Water.mat");
        return mat;
    }
}
