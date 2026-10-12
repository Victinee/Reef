using UnityEngine;
using UnityEditor;
using System.IO;

public class OceanGuardLevelBuilder
{
    [MenuItem("OceanGuard/Build Level with PBR and Models")]
    public static void BuildLevel()
    {
        // 1. Create PBR Materials
        Material sandMat = CreatePBRMaterial("M_Sand", "Assets/Art/Textures/Sand", new Vector2(4, 4));
        Material grassMat = CreatePBRMaterial("M_Grass", "Assets/Art/Textures/Grass", new Vector2(4, 4));
        Material rockMat = CreatePBRMaterial("M_Rock", "Assets/Art/Textures/Rock", new Vector2(3, 3));

        // 2. Update Island Geometry & Materials
        GameObject oceanWorld = GameObject.Find("OceanWorld");
        if (oceanWorld != null)
        {
            foreach (Transform child in oceanWorld.transform)
            {
                if (child.name.StartsWith("Forest Area"))
                {
                    AssignMaterial(child.gameObject, grassMat);
                }
                else if (child.name.StartsWith("Beach Area"))
                {
                    AssignMaterial(child.gameObject, sandMat);
                }
                else if (child.name.StartsWith("Reef Area"))
                {
                    AssignMaterial(child.gameObject, rockMat);
                }
                
                // Remove the old untextured sphere stems and procedural noise overlays
                for (int i = child.childCount - 1; i >= 0; i--)
                {
                    Transform c = child.GetChild(i);
                    if (c.name.ToLower().Contains("stem") || c.name.ToLower().Contains("noise"))
                    {
                        GameObject.DestroyImmediate(c.gameObject);
                    }
                }
            }
        }

        // 3. Place Models & Litter
        PlaceModelsOnArea("Forest Area", "Assets/Art/Models/Nature", 3);
        PlaceLitterOnBeach("Beach Area 1", "Assets/Art/Models/Survival", 3, 5);
        PlaceModelsOnArea("Reef Area", "Assets/Art/Models/Pirate", 2);

        // 4. Water Plane
        CreateWaterPlane();

        // 5. Refresh
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Level Build Complete!");
    }

    static Material CreatePBRMaterial(string matName, string texPath, Vector2 tiling)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/Materials"))
        {
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
        }

        string matPath = $"Assets/Art/Materials/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }

        Texture2D albedo = LoadTexture(texPath, "_Color", "Albedo", "Diffuse");
        Texture2D normal = LoadTexture(texPath, "_NormalDX", "Normal");
        Texture2D roughness = LoadTexture(texPath, "_Roughness", "Roughness");

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

    static void AssignMaterial(GameObject obj, Material mat)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static void PlaceModelsOnArea(string areaNamePrefix, string modelPath, int countPerArea)
    {
        GameObject oceanWorld = GameObject.Find("OceanWorld");
        if (!oceanWorld) return;

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { modelPath });
        if (guids.Length == 0) return;

        foreach (Transform child in oceanWorld.transform)
        {
            if (child.name.StartsWith(areaNamePrefix))
            {
                for (int i = 0; i < countPerArea; i++)
                {
                    string guid = guids[Random.Range(0, guids.Length)];
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    if (prefab)
                    {
                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        inst.transform.SetParent(child);
                        inst.transform.localPosition = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                    }
                }
            }
        }
    }

    static void PlaceLitterOnBeach(string beachName, string modelPath, int minCount, int maxCount)
    {
        GameObject oceanWorld = GameObject.Find("OceanWorld");
        if (!oceanWorld) return;
        Transform beach = oceanWorld.transform.Find(beachName);
        if (!beach) return;

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { modelPath });
        if (guids.Length == 0) return;

        int count = Random.Range(minCount, maxCount + 1);
        for (int i = 0; i < count; i++)
        {
            string guid = guids[Random.Range(0, guids.Length)];
            string p = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.SetParent(beach);
                inst.transform.localPosition = new Vector3(Random.Range(-1f, 1f), 0.2f, Random.Range(-1f, 1f)); 
                
                if (inst.GetComponent<Collider2D>() == null && inst.GetComponent<Collider>() == null)
                {
                    inst.AddComponent<BoxCollider>();
                }
                
                System.Type litterType = System.Type.GetType("LitterNode, Assembly-CSharp");
                if (litterType != null && inst.GetComponent(litterType) == null)
                {
                    inst.AddComponent(litterType);
                }
            }
        }
    }

    static void CreateWaterPlane()
    {
        GameObject water = GameObject.Find("WaterPlane");
        if (water == null)
        {
            water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "WaterPlane";
        }
        
        water.transform.position = new Vector3(0, 0.15f, 0);
        water.transform.localScale = new Vector3(20, 1, 20); 

        Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Water.mat");
        if (waterMat == null)
        {
            waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(waterMat, "Assets/Art/Materials/M_Water.mat");
        }

        waterMat.SetFloat("_Smoothness", 0.95f);
        
        // URP transparent setup
        waterMat.SetFloat("_Surface", 1);
        waterMat.SetColor("_BaseColor", new Color(0.2f, 0.8f, 0.9f, 0.5f));
        waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        waterMat.SetInt("_ZWrite", 0);
        waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        
        waterMat.DisableKeyword("_ALPHATEST_ON");
        waterMat.EnableKeyword("_ALPHABLEND_ON");
        waterMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        water.GetComponent<Renderer>().sharedMaterial = waterMat;
    }
}
