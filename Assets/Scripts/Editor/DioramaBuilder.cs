using UnityEngine;
using UnityEditor;

public class DioramaBuilder
{
    [MenuItem("OceanGuard/Build Diorama Level")]
    public static void BuildDiorama()
    {
        // 1. Wipe Procedural Placeholders
        string[] oldNames = new[] { "OceanWorld", "ProceduralNoisePlane", "WaferSlabs", "LollipopTrees", "Island_Diorama", "WaterPlane" };
        foreach (string n in oldNames)
        {
            GameObject oldObj = GameObject.Find(n);
            if (oldObj) GameObject.DestroyImmediate(oldObj);
        }

        // 2. Island Topography & Seabed Base
        GameObject diorama = new GameObject("Island_Diorama");
        diorama.transform.position = Vector3.zero;

        Material sandMat = CreatePBRMaterial("M_Sand", "Assets/Art/Textures/Sand", new Vector2(4, 4));
        Material grassMat = CreatePBRMaterial("M_Grass", "Assets/Art/Textures/Grass", new Vector2(4, 4));
        Material rockMat = CreatePBRMaterial("M_Rock", "Assets/Art/Textures/Rock", new Vector2(3, 3));

        // Create composite mesh structure
        GameObject seabed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        seabed.name = "Deep_Seabed_Base";
        seabed.transform.SetParent(diorama.transform);
        seabed.transform.localPosition = new Vector3(0, -0.85f, 0);
        seabed.transform.localScale = new Vector3(15f, 0.35f, 15f); // Submerged base Y = -1.2 to -0.5
        AssignMaterial(seabed, rockMat);

        GameObject shore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shore.name = "Sloping_Sandy_Shoreline";
        shore.transform.SetParent(diorama.transform);
        shore.transform.localPosition = new Vector3(0, 0.05f, 0);
        shore.transform.localScale = new Vector3(10f, 0.25f, 10f); // Sand Y = -0.2 to 0.3
        AssignMaterial(shore, sandMat);

        GameObject plateau = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plateau.name = "Elevated_Forest_Plateau";
        plateau.transform.SetParent(diorama.transform);
        plateau.transform.localPosition = new Vector3(0, 0.75f, 0);
        plateau.transform.localScale = new Vector3(7f, 0.25f, 7f); // Forest Y = 0.5 to 1.0
        AssignMaterial(plateau, grassMat);

        // 3. Realistic Ocean Water
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "OceanWater";
        water.transform.SetParent(diorama.transform);
        water.transform.localPosition = new Vector3(0, 0, 0);
        water.transform.localScale = new Vector3(2f, 1f, 2f); 

        Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(waterMat, "Assets/Art/Materials/M_OceanWater.mat");
        waterMat.SetFloat("_Smoothness", 0.92f);
        waterMat.SetFloat("_Surface", 1); 
        Color cyanColor;
        ColorUtility.TryParseHtmlString("#00B4D8", out cyanColor);
        cyanColor.a = 0.42f;
        waterMat.SetColor("_BaseColor", cyanColor);
        waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        waterMat.SetInt("_ZWrite", 0);
        waterMat.DisableKeyword("_ALPHATEST_ON");
        waterMat.EnableKeyword("_ALPHABLEND_ON");
        waterMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        AssignMaterial(water, waterMat);

        water.AddComponent<WaterWaveMotion>();

        // 4. Populate Models
        PlaceModels(plateau.transform, "Assets/Art/Models/Nature", 10, 3f);
        PlaceLitter(shore.transform, "Assets/Art/Models/Survival", 4, 4.5f);
        PlaceModels(seabed.transform, "Assets/Art/Models", 15, 7f, true);

        // 5. Lighting, Camera
        Light light = GameObject.FindObjectOfType<Light>();
        if (light != null && light.type == LightType.Directional)
        {
            Color lightCol;
            ColorUtility.TryParseHtmlString("#FFF3D6", out lightCol);
            light.color = lightCol;
            light.intensity = 1.25f;
            light.transform.rotation = Quaternion.Euler(45, -35, 0);
            light.shadows = LightShadows.Soft;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(12, 14, -12);
            cam.transform.rotation = Quaternion.Euler(30, 45, 0);
            cam.orthographic = true;
            cam.orthographicSize = 8;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Diorama Overhaul Complete!");
    }

    static void AssignMaterial(GameObject obj, Material mat)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static Material CreatePBRMaterial(string matName, string texPath, Vector2 tiling)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/Materials"))
            AssetDatabase.CreateFolder("Assets/Art", "Materials");

        string matPath = $"Assets/Art/Materials/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }

        Texture2D albedo = LoadTexture(texPath, "_Color", "Albedo", "Diffuse");
        Texture2D normal = LoadTexture(texPath, "_NormalDX", "Normal");
        
        if (albedo) mat.SetTexture("_BaseMap", albedo);
        if (normal) 
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        
        mat.mainTextureScale = tiling;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Texture2D LoadTexture(string path, params string[] nameHints)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            foreach (string hint in nameHints)
            {
                if (p.Contains(hint)) return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
        }
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }

    static void PlaceModels(Transform parent, string path, int count, float radius, bool submerged = false)
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { path });
        if (guids.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            string guid = guids[Random.Range(0, guids.Length)];
            string p = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.SetParent(parent);
                
                Vector2 randPos = Random.insideUnitCircle * radius;
                float yOffset = submerged ? 0.51f : 0.51f; // Since parent is scaled, we adjust relative to parent height. Cylinder is 2m high.
                inst.transform.localPosition = new Vector3(randPos.x / parent.localScale.x, yOffset, randPos.y / parent.localScale.z);
                inst.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
        }
    }

    static void PlaceLitter(Transform parent, string path, int count, float radius)
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { path });
        if (guids.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            string guid = guids[Random.Range(0, guids.Length)];
            string p = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.SetParent(parent);
                
                // Ring placement for shore
                Vector2 randPos = Random.insideUnitCircle.normalized * radius;
                inst.transform.localPosition = new Vector3(randPos.x / parent.localScale.x, 0.51f, randPos.y / parent.localScale.z);
                inst.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                
                if (inst.GetComponent<Collider2D>() == null && inst.GetComponent<Collider>() == null)
                    inst.AddComponent<BoxCollider>();
                
                System.Type litterType = System.Type.GetType("LitterNode, Assembly-CSharp");
                if (litterType != null && inst.GetComponent(litterType) == null)
                    inst.AddComponent(litterType);
            }
        }
    }
}
