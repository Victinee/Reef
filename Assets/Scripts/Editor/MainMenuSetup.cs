#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

/// <summary>
/// Editor utility — rebuilds the Reef main menu from scratch inside OceanGuard_Main.
/// Cleans up all previous menu objects, then creates:
///   • MenuCamera with MenuCameraOrbit targeting Tropical_Sketchfab
///   • Menu_GlobalVolume with URP Depth of Field
///   • MenuCanvas (Overlay, Android Canvas Scaler) with TMP title + styled Play button
///   • MenuController wiring (menuCamera, menuCanvas, Player)
///   • EventSystem with correct input module
///
/// Menu: OceanGuard ▸ Build Main Menu UI (GetStarted_Scene)
/// </summary>
public static class MainMenuSetup
{
    private const string ScenePath = "Assets/Scenes/OceanGuard_Main.unity";
    private const string VolumeProfilePath = "Assets/Settings/MenuPostProcessing.asset";

    // Names of objects to delete during clean slate
    private static readonly string[] CleanupNames =
    {
        "MenuCanvas", "MenuCamera", "MenuManager", "EventSystem",
        "Menu_GlobalVolume", "Canvas", "MenuContainer"
    };

    [MenuItem("OceanGuard/Build Main Menu UI (GetStarted_Scene)")]
    public static void BuildMenuUI()
    {
        // ═══════════════════════════════════════════════════════════════════
        //  0. Open the OceanGuard_Main scene
        // ═══════════════════════════════════════════════════════════════════
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != ScenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                currentScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("[MainMenuSetup] Aborted: save current scene cancelled.");
                return;
            }
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Rebuild Reef Main Menu");
        int undoGroup = Undo.GetCurrentGroup();

        // ═══════════════════════════════════════════════════════════════════
        //  1. CLEAN SLATE — destroy all previous menu objects
        // ═══════════════════════════════════════════════════════════════════
        foreach (string objName in CleanupNames)
        {
            // Destroy ALL instances, not just the first one found
            GameObject found;
            while ((found = GameObject.Find(objName)) != null)
            {
                Debug.Log($"[MainMenuSetup] Deleting existing '{found.name}'.");
                Undo.DestroyObjectImmediate(found);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  2. Locate scene references: Tropical_Sketchfab and Player
        // ═══════════════════════════════════════════════════════════════════
        GameObject islandTarget = GameObject.Find("Tropical_Sketchfab");
        if (islandTarget == null)
        {
            Debug.LogWarning("[MainMenuSetup] 'Tropical_Sketchfab' not found in scene. " +
                             "MenuCameraOrbit target will need to be assigned manually.");
        }

        // Player may be deactivated — FindObjectOfType won't find inactive objects.
        // Search all root GameObjects for one named "Player".
        GameObject playerObject = null;
        foreach (GameObject root in currentScene.GetRootGameObjects())
        {
            if (root.name == "Player")
            {
                playerObject = root;
                break;
            }
        }
        if (playerObject == null)
        {
            Debug.LogWarning("[MainMenuSetup] 'Player' not found in scene. " +
                             "MenuController.playerObject will need to be assigned manually.");
        }
        else
        {
            // Ensure the Player is DEACTIVATED by default
            Undo.RecordObject(playerObject, "Deactivate Player");
            playerObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  3. CINEMATIC CAMERA — MenuCamera with MenuCameraOrbit
        // ═══════════════════════════════════════════════════════════════════
        GameObject menuCameraGo = new GameObject("MenuCamera");
        Undo.RegisterCreatedObjectUndo(menuCameraGo, "Create MenuCamera");

        Camera menuCam = menuCameraGo.AddComponent<Camera>();
        menuCam.tag = "MainCamera";
        menuCam.clearFlags = CameraClearFlags.Skybox;
        menuCam.nearClipPlane = 0.3f;
        menuCam.farClipPlane = 1000f;
        menuCam.fieldOfView = 50f;

        // Add AudioListener
        menuCameraGo.AddComponent<AudioListener>();

        // Enable URP post-processing on this camera
        var camData = menuCameraGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // Position: high above water, looking down at the island
        // The island prefab is near origin; place the camera well above to avoid sand clipping
        Vector3 islandCenter = islandTarget != null ? islandTarget.transform.position : Vector3.zero;
        menuCameraGo.transform.position = islandCenter + new Vector3(15f, 18f, 15f);
        menuCameraGo.transform.LookAt(islandCenter + Vector3.up * 2f);

        // Attach MenuCameraOrbit
        MenuCameraOrbit orbit = menuCameraGo.AddComponent<MenuCameraOrbit>();

        // MenuCameraOrbit.target is private [SerializeField] — use SerializedObject
        if (islandTarget != null)
        {
            SerializedObject orbitSO = new SerializedObject(orbit);
            orbitSO.FindProperty("target").objectReferenceValue = islandTarget.transform;
            orbitSO.FindProperty("orbitSpeed").floatValue = 6f;
            orbitSO.FindProperty("orbitRadius").floatValue = 25f;
            orbitSO.FindProperty("orbitHeight").floatValue = 18f;
            orbitSO.FindProperty("lookAtYOffset").floatValue = 2f;
            orbitSO.FindProperty("lookSmoothSpeed").floatValue = 3f;
            orbitSO.ApplyModifiedProperties();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  4. URP EFFECTS — Menu_GlobalVolume with Depth of Field
        // ═══════════════════════════════════════════════════════════════════
        GameObject volGo = new GameObject("Menu_GlobalVolume");
        Undo.RegisterCreatedObjectUndo(volGo, "Create Menu_GlobalVolume");

        Volume volume = volGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;

        // Create or reuse VolumeProfile asset
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }
        volume.profile = profile;

        // Depth of Field — Gaussian for subtle background blur
        DepthOfField dof;
        if (!profile.TryGet(out dof))
        {
            dof = profile.Add<DepthOfField>();
        }
        dof.active = true;
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(10f);
        dof.gaussianEnd.Override(50f);
        dof.gaussianMaxRadius.Override(1.0f);

        // Tonemapping — ACES for cinematic color
        Tonemapping tm;
        if (!profile.TryGet(out tm))
        {
            tm = profile.Add<Tonemapping>();
        }
        tm.active = true;
        tm.mode.Override(TonemappingMode.ACES);

        // Bloom — subtle glow on water highlights
        Bloom bloom;
        if (!profile.TryGet(out bloom))
        {
            bloom = profile.Add<Bloom>();
        }
        bloom.active = true;
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.8f);
        bloom.scatter.Override(0.65f);

        // Vignette — cinematic framing with dark blue tint
        Vignette vignette;
        if (!profile.TryGet(out vignette))
        {
            vignette = profile.Add<Vignette>();
        }
        vignette.active = true;
        vignette.color.Override(new Color(0.02f, 0.05f, 0.15f));
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.4f);

        EditorUtility.SetDirty(profile);

        // ═══════════════════════════════════════════════════════════════════
        //  5. EVENT SYSTEM — ensure one exists
        // ═══════════════════════════════════════════════════════════════════
        GameObject esGo = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");

        // Add the correct input module depending on which input system is active
        System.Type inputSystemUI = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemUI != null)
        {
            esGo.AddComponent(inputSystemUI);
        }
        else
        {
            esGo.AddComponent<StandaloneInputModule>();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  6. CANVAS & UI — MenuCanvas with Android Canvas Scaler
        // ═══════════════════════════════════════════════════════════════════
        GameObject canvasGo = new GameObject("MenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create MenuCanvas");

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // Canvas Scaler — Scale With Screen Size for Android
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // ── Title: "Reef" — large bold white TMP ────────────────────────
        GameObject titleGo = new GameObject("Title_Reef",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(canvasGo.transform, false);
        Undo.RegisterCreatedObjectUndo(titleGo, "Create Title_Reef");

        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(800, 220);
        titleRect.anchoredPosition = new Vector2(0, -80);

        TextMeshProUGUI titleTMP = titleGo.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "Reef";
        titleTMP.fontSize = 130;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;
        titleTMP.raycastTarget = false;

        // ── Subtitle ────────────────────────────────────────────────────
        GameObject subGo = new GameObject("Subtitle",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(canvasGo.transform, false);
        Undo.RegisterCreatedObjectUndo(subGo, "Create Subtitle");

        RectTransform subRect = subGo.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.sizeDelta = new Vector2(750, 50);
        subRect.anchoredPosition = new Vector2(0, -290);

        TextMeshProUGUI subTMP = subGo.GetComponent<TextMeshProUGUI>();
        subTMP.text = "MARINE CONSERVATION & RESTORATION";
        subTMP.fontSize = 20;
        subTMP.characterSpacing = 8;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.80f, 0.92f, 1f, 0.65f);
        subTMP.raycastTarget = false;

        // ═══════════════════════════════════════════════════════════════════
        //  7. PLAY BUTTON — semi-transparent dark blue, bold white TMP text
        // ═══════════════════════════════════════════════════════════════════
        Color semiTransDarkBlue = new Color(0.04f, 0.10f, 0.28f, 0.65f);

        GameObject playBtnGo = new GameObject("Button_Play",
            typeof(RectTransform), typeof(Image), typeof(Button));
        playBtnGo.transform.SetParent(canvasGo.transform, false);
        Undo.RegisterCreatedObjectUndo(playBtnGo, "Create Button_Play");

        RectTransform playBtnRect = playBtnGo.GetComponent<RectTransform>();
        playBtnRect.anchorMin = new Vector2(0.5f, 0f);
        playBtnRect.anchorMax = new Vector2(0.5f, 0f);
        playBtnRect.pivot = new Vector2(0.5f, 0f);
        playBtnRect.sizeDelta = new Vector2(360, 85);
        playBtnRect.anchoredPosition = new Vector2(0, 260);

        Image playBtnImg = playBtnGo.GetComponent<Image>();
        playBtnImg.color = semiTransDarkBlue;
        playBtnImg.raycastTarget = true;

        Button playBtn = playBtnGo.GetComponent<Button>();
        ColorBlock playColors = playBtn.colors;
        playColors.normalColor      = Color.white;
        playColors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        playColors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        playColors.selectedColor    = new Color(1.1f, 1.1f, 1.1f, 1f);
        playColors.fadeDuration     = 0.12f;
        playBtn.colors = playColors;

        // Play button label — bold white
        GameObject playTxtGo = new GameObject("Text_Play",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        playTxtGo.transform.SetParent(playBtnGo.transform, false);
        Undo.RegisterCreatedObjectUndo(playTxtGo, "Create Text_Play");

        RectTransform playTxtRect = playTxtGo.GetComponent<RectTransform>();
        playTxtRect.anchorMin = Vector2.zero;
        playTxtRect.anchorMax = Vector2.one;
        playTxtRect.sizeDelta = Vector2.zero;
        playTxtRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI playTxtTMP = playTxtGo.GetComponent<TextMeshProUGUI>();
        playTxtTMP.text = "PLAY";
        playTxtTMP.fontSize = 38;
        playTxtTMP.fontStyle = FontStyles.Bold;
        playTxtTMP.characterSpacing = 6;
        playTxtTMP.alignment = TextAlignmentOptions.Center;
        playTxtTMP.color = Color.white;
        playTxtTMP.raycastTarget = false;

        // ═══════════════════════════════════════════════════════════════════
        //  8. QUIT BUTTON — matching style, smaller
        // ═══════════════════════════════════════════════════════════════════
        Color quitBgColor = new Color(0.04f, 0.08f, 0.20f, 0.50f);

        GameObject quitBtnGo = new GameObject("Button_Quit",
            typeof(RectTransform), typeof(Image), typeof(Button));
        quitBtnGo.transform.SetParent(canvasGo.transform, false);
        Undo.RegisterCreatedObjectUndo(quitBtnGo, "Create Button_Quit");

        RectTransform quitBtnRect = quitBtnGo.GetComponent<RectTransform>();
        quitBtnRect.anchorMin = new Vector2(0.5f, 0f);
        quitBtnRect.anchorMax = new Vector2(0.5f, 0f);
        quitBtnRect.pivot = new Vector2(0.5f, 0f);
        quitBtnRect.sizeDelta = new Vector2(270, 65);
        quitBtnRect.anchoredPosition = new Vector2(0, 160);

        Image quitBtnImg = quitBtnGo.GetComponent<Image>();
        quitBtnImg.color = quitBgColor;
        quitBtnImg.raycastTarget = true;

        Button quitBtn = quitBtnGo.GetComponent<Button>();
        ColorBlock qColors = quitBtn.colors;
        qColors.normalColor      = Color.white;
        qColors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        qColors.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
        qColors.fadeDuration     = 0.12f;
        quitBtn.colors = qColors;

        GameObject quitTxtGo = new GameObject("Text_Quit",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        quitTxtGo.transform.SetParent(quitBtnGo.transform, false);
        Undo.RegisterCreatedObjectUndo(quitTxtGo, "Create Text_Quit");

        RectTransform quitTxtRect = quitTxtGo.GetComponent<RectTransform>();
        quitTxtRect.anchorMin = Vector2.zero;
        quitTxtRect.anchorMax = Vector2.one;
        quitTxtRect.sizeDelta = Vector2.zero;
        quitTxtRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI quitTxtTMP = quitTxtGo.GetComponent<TextMeshProUGUI>();
        quitTxtTMP.text = "QUIT";
        quitTxtTMP.fontSize = 26;
        quitTxtTMP.fontStyle = FontStyles.Bold;
        quitTxtTMP.characterSpacing = 4;
        quitTxtTMP.alignment = TextAlignmentOptions.Center;
        quitTxtTMP.color = new Color(0.80f, 0.88f, 0.95f, 0.70f);
        quitTxtTMP.raycastTarget = false;

        // ═══════════════════════════════════════════════════════════════════
        //  9. FUNCTIONALITY — MenuController on MenuCanvas, wired up
        // ═══════════════════════════════════════════════════════════════════
        MenuController menuController = canvasGo.GetComponent<MenuController>();
        if (menuController == null)
        {
            menuController = canvasGo.AddComponent<MenuController>();
        }

        // MenuController fields are public — assign directly
        menuController.menuCamera   = menuCameraGo;
        menuController.menuCanvas   = canvasGo;
        menuController.playerObject = playerObject;

        // Wire Play Button OnClick → MenuController.StartGame()
        while (playBtn.onClick.GetPersistentEventCount() > 0)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(playBtn.onClick, 0);
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            playBtn.onClick, menuController.StartGame);

        // Wire Quit Button OnClick → MenuController.QuitGame()
        while (quitBtn.onClick.GetPersistentEventCount() > 0)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(quitBtn.onClick, 0);
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            quitBtn.onClick, menuController.QuitGame);

        // ═══════════════════════════════════════════════════════════════════
        //  10. Finalize — collapse undo, mark dirty, save
        // ═══════════════════════════════════════════════════════════════════
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log("[MainMenuSetup] ✅ Reef main menu rebuilt from scratch in OceanGuard_Main!");
        Debug.Log("  • MenuCamera — pos (15, 18, 15), orbiting Tropical_Sketchfab");
        Debug.Log("  • Menu_GlobalVolume — Gaussian DoF, ACES Tonemapping, Bloom, Vignette");
        Debug.Log("  • MenuCanvas — 1080×1920 Android scaler, TMP title, semi-transparent buttons");
        Debug.Log("  • MenuController — wired to MenuCamera, MenuCanvas, Player");
        Debug.Log("  • Button_Play → MenuController.StartGame()");
        Debug.Log("  • Player object is DEACTIVATED by default");
        Debug.Log("═══════════════════════════════════════════════════════════════");
    }
}
#endif
