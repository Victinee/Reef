#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

/// <summary>
/// First-Person Perspective Camera Configurator for the REEF / OceanGuard project.
///
/// <para>
/// Replaces the previous isometric (Orthographic 30/45/0) setup with a
/// Perspective camera parented to the player at eye-level, matching a
/// Minecraft-style first-person view optimised for Android.
/// </para>
///
/// <para>
/// <b>Target settings applied by this script</b>
/// <list type="bullet">
///   <item>Projection:       Perspective</item>
///   <item>Field of View:    75°</item>
///   <item>Near clip:        0.1</item>
///   <item>Far clip:         1000</item>
///   <item>Local Position:   (0, 1.7, 0)  — eye level above player root</item>
///   <item>Local Rotation:   (0, 0, 0)    — facing forward; pitch is controlled at runtime</item>
/// </list>
/// </para>
///
/// <para>
/// <b>How to apply</b>
/// <list type="number">
///   <item>
///     <b>Editor menu</b> → <c>OceanGuard ▸ Configure FPS Camera</c><br/>
///     Select the <b>Player</b> root GameObject first so the script knows
///     which Transform to parent the camera under. Applies instantly with
///     full Undo support.
///   </item>
///   <item>
///     <b>Runtime auto-apply</b> — add <see cref="FPSCameraRuntime"/> to the
///     Player root (or any scene GameObject that holds a reference to the player).
///     It configures the camera in <c>Awake()</c>.
///   </item>
/// </list>
/// </para>
/// </summary>
public static class IsometricCameraSetup   // class name kept for binary compatibility
{
    // ── Target values ──────────────────────────────────────────────────────
    public const float FieldOfView     = 75f;
    public const float NearClip        = 0.1f;
    public const float FarClip         = 1000f;

    /// <summary>Eye-level height above the player root (metres).</summary>
    public const float EyeHeight       = 1.7f;

    // ── Shared apply method ────────────────────────────────────────────────

    /// <summary>
    /// Configures <paramref name="cam"/> for first-person perspective and,
    /// if <paramref name="playerRoot"/> is provided, parents the camera's
    /// Transform under it at eye-level with zeroed local rotation.
    /// Safe to call from both Editor tooling and runtime MonoBehaviours.
    /// </summary>
    /// <param name="cam">The camera to configure. Must not be null.</param>
    /// <param name="playerRoot">
    ///   Optional player Transform. When supplied the camera is re-parented
    ///   under it at local position (0, <see cref="EyeHeight"/>, 0).
    /// </param>
    public static void ApplyTo(Camera cam, Transform playerRoot = null)
    {
        if (cam == null)
        {
            Debug.LogError("[FPSCameraSetup] ApplyTo() received a null Camera reference.");
            return;
        }

        // ── Projection ────────────────────────────────────────────────────
        cam.orthographic  = false;          // Perspective
        cam.fieldOfView   = FieldOfView;
        cam.nearClipPlane = NearClip;
        cam.farClipPlane  = FarClip;

        // ── Parenting & local transform ───────────────────────────────────
        if (playerRoot != null)
        {
            cam.transform.SetParent(playerRoot, worldPositionStays: false);
            cam.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            cam.transform.localRotation = Quaternion.identity;
        }

        Debug.Log($"[FPSCameraSetup] Applied to '{cam.gameObject.name}': " +
                  $"Perspective, FOV={FieldOfView}, Near={NearClip}, Far={FarClip}" +
                  (playerRoot != null ? $", parented under '{playerRoot.name}' at eye-height {EyeHeight}." : "."));
    }

#if UNITY_EDITOR
    // ── Editor menu ────────────────────────────────────────────────────────

    [MenuItem("OceanGuard/Configure FPS Camera")]
    public static void ConfigureCameraFromMenu()
    {
        Camera cam = Camera.main;
        if (cam == null)
            cam = Object.FindObjectOfType<Camera>();

        if (cam == null)
        {
            EditorUtility.DisplayDialog(
                "FPS Camera Setup",
                "No camera found in the active scene.\n\n" +
                "Add a Camera (tagged 'MainCamera') and try again.",
                "OK");
            return;
        }

        // Try to find the Player root from the current selection or scene search.
        Transform playerRoot = null;
        if (UnityEditor.Selection.activeGameObject != null)
        {
            // Use the selected GameObject if it looks like a player root
            // (has a CharacterController or PlayerController).
            GameObject sel = UnityEditor.Selection.activeGameObject;
            if (sel.GetComponent<CharacterController>() != null ||
                sel.GetComponent<PlayerController>()    != null)
            {
                playerRoot = sel.transform;
            }
        }

        if (playerRoot == null)
        {
            // Auto-find via PlayerController component
            PlayerController pc = Object.FindObjectOfType<PlayerController>();
            if (pc != null) playerRoot = pc.transform;
        }

        // Record Undo before making any changes.
        Undo.RecordObject(cam,                 "Configure FPS Camera");
        Undo.RecordObject(cam.transform,       "Configure FPS Camera Transform");
        if (playerRoot != null)
            Undo.SetTransformParent(cam.transform, playerRoot, "Parent Camera to Player");

        ApplyTo(cam, playerRoot);

        EditorUtility.SetDirty(cam.gameObject);
        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);

        string parentMsg = playerRoot != null
            ? $"Camera parented under '{playerRoot.name}'."
            : "No Player root found — camera NOT re-parented. " +
              "Select the Player GameObject and run this menu item again.";

        Debug.Log($"[FPSCameraSetup] ✅ {parentMsg} Save the scene (Ctrl+S) to persist.");
    }

    [MenuItem("OceanGuard/Configure FPS Camera", true)]
    private static bool ValidateMenu() => true;

    // ── Legacy alias — keeps the old menu item alive so muscle-memory works ──
    [MenuItem("OceanGuard/Configure Isometric Camera")]
    public static void LegacyMenuAlias() => ConfigureCameraFromMenu();

    [MenuItem("OceanGuard/Configure Isometric Camera", true)]
    private static bool ValidateLegacy() => true;
#endif
}

// ─────────────────────────────────────────────────────────────────────────────
// Runtime companion component
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Add this MonoBehaviour to the <b>Player root</b> GameObject.
/// On <c>Awake()</c> it locates <see cref="Camera.main"/> and calls
/// <see cref="IsometricCameraSetup.ApplyTo"/> with the player's own Transform,
/// ensuring the camera is Perspective / FOV 75 / parented at eye-level
/// every time the scene loads — no manual Inspector work required.
/// </summary>
public sealed class FPSCameraRuntime : MonoBehaviour
{
    private void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null)
            cam = FindObjectOfType<Camera>();

        if (cam == null)
        {
            Debug.LogWarning("[FPSCameraRuntime] No Camera found — skipping auto-configuration.");
            return;
        }

        IsometricCameraSetup.ApplyTo(cam, playerRoot: transform);
    }
}

/// <summary>
/// Kept for backwards compatibility so existing scene references to
/// <see cref="IsometricCameraRuntime"/> still compile.
/// Internally delegates to <see cref="FPSCameraRuntime"/>.
/// </summary>
public sealed class IsometricCameraRuntime : MonoBehaviour
{
    private void Awake()
    {
        // Re-use the full FPS logic — just find ourselves a FPSCameraRuntime proxy.
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam != null)
            IsometricCameraSetup.ApplyTo(cam, playerRoot: transform);
        else
            Debug.LogWarning("[IsometricCameraRuntime] No Camera found.");
    }
}
