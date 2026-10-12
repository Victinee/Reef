#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility: sets up the REEF core architecture in the active scene.
/// Creates GameManager (IslandManager + TimeSystem), HUD Canvas, and sample litter nodes.
/// Menu: OceanGuard ▸ Setup REEF Core
/// </summary>
public static class ReefCoreSetup
{
    [MenuItem("OceanGuard/Setup REEF Core")]
    public static void SetupReefCore()
    {
        // ── 1. GameManager ────────────────────────────────────
        GameObject gm = GameObject.Find("GameManager");
        if (gm == null)
        {
            gm = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(gm, "Create GameManager");
        }

        // Remove legacy TurnManager if present
        TurnManager legacy = gm.GetComponent<TurnManager>();
        if (legacy != null)
        {
            Undo.DestroyObjectImmediate(legacy);
            Debug.Log("[ReefCoreSetup] Removed legacy TurnManager from GameManager.");
        }

        // Add IslandManager
        if (gm.GetComponent<IslandManager>() == null)
        {
            Undo.AddComponent<IslandManager>(gm);
            Debug.Log("[ReefCoreSetup] Added IslandManager to GameManager.");
        }

        // Add TimeSystem
        if (gm.GetComponent<TimeSystem>() == null)
        {
            Undo.AddComponent<TimeSystem>(gm);
            Debug.Log("[ReefCoreSetup] Added TimeSystem to GameManager.");
        }

        // ── 2. HUD Canvas ─────────────────────────────────────
        GameObject canvasGO = GameObject.Find("ReefHUD_Canvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("ReefHUD_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create ReefHUD Canvas");

            Canvas canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // HUD Text
        Transform hudTextTf = canvasGO.transform.Find("HUD_Text");
        GameObject hudTextGO;
        if (hudTextTf == null)
        {
            hudTextGO = new GameObject("HUD_Text");
            hudTextGO.transform.SetParent(canvasGO.transform, false);
            Undo.RegisterCreatedObjectUndo(hudTextGO, "Create HUD Text");
        }
        else
        {
            hudTextGO = hudTextTf.gameObject;
        }

        // Configure TMP_Text
        TMP_Text tmpText = hudTextGO.GetComponent<TMP_Text>();
        if (tmpText == null)
        {
            tmpText = hudTextGO.AddComponent<TextMeshProUGUI>();
        }
        tmpText.fontSize   = 18;
        tmpText.alignment  = TextAlignmentOptions.TopLeft;
        tmpText.color      = Color.white;
        tmpText.text       = "Time: 06:00  |  Day: 1  |  ☀ Sundials: 100  |  ⚡ Energy: 50/50  |  🔥 Heat: 15.0%  |  ✦ Appeal: 20";

        // Position at top-left
        RectTransform rt = hudTextGO.GetComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0, 1);
        rt.anchorMax       = new Vector2(1, 1);
        rt.pivot           = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -10);
        rt.sizeDelta       = new Vector2(0, 40);

        // Add HUDBinding
        if (hudTextGO.GetComponent<HUDBinding>() == null)
        {
            hudTextGO.AddComponent<HUDBinding>();
        }

        // ── 3. Sample Litter Nodes ────────────────────────────
        SpawnSampleLitter(new Vector3(-2f, -1f, 0f), "Litter_Bottle");
        SpawnSampleLitter(new Vector3( 0f, -1.5f, 0f), "Litter_Can");
        SpawnSampleLitter(new Vector3( 1.5f, -0.5f, 0f), "Litter_Bag");

        EditorUtility.SetDirty(gm);
        Debug.Log("[ReefCoreSetup] ✅ REEF Core Architecture set up successfully. " +
                  "GameManager, HUD Canvas, and 3 sample litter nodes created.");
    }

    private static void SpawnSampleLitter(Vector3 pos, string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return; // don't duplicate

        GameObject litter = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(litter, "Create " + name);
        litter.transform.position = pos;

        // Sprite renderer (placeholder colour square)
        SpriteRenderer sr = litter.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.55f, 0.35f, 0.15f); // brown tint
        sr.sortingOrder = 50;

        // A simple 1x1 white sprite so it's visible
        Texture2D tex = new Texture2D(4, 4);
        Color[] cols = new Color[16];
        for (int i = 0; i < 16; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);

        // Collider for click detection
        BoxCollider2D col = litter.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        // LitterNode script
        litter.AddComponent<LitterNode>();

        // Scale for visibility
        litter.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
    }
}
#endif
