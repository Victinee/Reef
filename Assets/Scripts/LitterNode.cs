using UnityEngine;

/// <summary>
/// Attached to interactive beach-litter objects in the 3D first-person diorama.
/// Implements <see cref="IInteractable"/> so that <see cref="PlayerController"/>
/// can detect it via the centre-screen crosshair raycast and trigger
/// <see cref="PerformJob"/> on pressing 'E' or Left Mouse Button.
///
/// <para>
/// <b>First-person interaction flow</b>:<br/>
/// 1. <see cref="PlayerController"/> fires a short ray (≤4 units) from the camera centre each frame.<br/>
/// 2. When this object enters the crosshair, <see cref="PlayerController.CrosshairHighlightActive"/> turns true.<br/>
/// 3. Pressing 'E' or Left Click calls <see cref="PerformJob"/> immediately.
/// </para>
///
/// <para>
/// <b>Required setup</b>: the GameObject needs a 3-D <see cref="Collider"/>
/// (e.g. <see cref="BoxCollider"/>, <see cref="SphereCollider"/>) so the
/// camera raycast can hit it. Assign it to the same layer(s) specified in
/// <c>PlayerController.interactLayerMask</c>.
/// </para>
/// </summary>
public class LitterNode : MonoBehaviour, IInteractable
{
    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Floating Text")]
    [Tooltip("Optional prefab for the floating \"+15\" feedback. " +
             "If null, a simple TextMesh is spawned at runtime.")]
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("Tuning")]
    [SerializeField] private int   energyCost    = 2;
    [SerializeField] private int   sundialReward = 15;

    [Header("Interaction")]
    [Tooltip("Kept for IInteractable interface compatibility. In FPS mode the crosshair " +
             "raycast length in PlayerController is the effective reach — this value is unused.")]
    [SerializeField] private float interactionRadius = 4f;

    // ── IInteractable ──────────────────────────────────────────────────────
    /// <inheritdoc/>
    public float InteractionRadius => interactionRadius;

    // ── Private state ──────────────────────────────────────────────────────
    /// <summary>Guard flag — prevents the job from firing more than once.</summary>
    private bool isConsumed;

    // ── IInteractable implementation ───────────────────────────────────────
    /// <summary>
    /// Called by <see cref="PlayerController"/> when the player taps while
    /// this litter node is centred in the first-person crosshair.
    /// Spends energy, grants sundials, updates <see cref="IslandManager"/>,
    /// spawns floating feedback text, and destroys this GameObject.
    /// </summary>
    public void PerformJob()
    {
        if (isConsumed) return;

        IslandManager mgr = IslandManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[LitterNode] IslandManager.Instance is null — " +
                           "ensure a GameManager with IslandManager is in the scene.");
            return;
        }

        if (mgr.Energy < energyCost)
        {
            Debug.Log("[LitterNode] Not enough energy to clean this litter.");
            return;
        }

        isConsumed = true;

        // Full transactional update: energy deducted, sundials + heat handled inside.
        mgr.CleanLitterTransaction(transform.position);

        // Visual feedback — floating "+15" reward text
        SpawnFloatingText($"+{sundialReward}");

        Destroy(gameObject);
    }

    // ── Legacy Editor fallback ─────────────────────────────────────────────
    /// <summary>
    /// Retained for convenience in the Unity Editor so designers can still
    /// click litter nodes with the mouse when no player agent is present.
    /// On-device builds rely solely on <see cref="PerformJob"/>.
    /// </summary>
#if UNITY_EDITOR
    private void OnMouseDown()
    {
        PerformJob();
    }
#endif

    // ── Floating feedback text ─────────────────────────────────────────────
    private void SpawnFloatingText(string message)
    {
        if (floatingTextPrefab != null)
        {
            GameObject go = Instantiate(
                floatingTextPrefab,
                transform.position + Vector3.up * 0.3f,
                Quaternion.identity);

            // If the prefab has a TMP_Text we set it; otherwise assume it self-manages
            TMPro.TMP_Text tmp = go.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmp != null) tmp.text = message;
            Destroy(go, 1.2f);
            return;
        }

        // Fallback: procedurally spawn a lightweight TextMesh
        GameObject textGO = new GameObject("FloatingText");
        textGO.transform.position = transform.position + Vector3.up * 0.3f;

        TextMesh tm = textGO.AddComponent<TextMesh>();
        tm.text          = message;
        tm.characterSize = 0.15f;
        tm.fontSize      = 48;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.color         = new Color(1f, 0.85f, 0.2f); // warm gold

        // Float upward and fade
        FloatingTextAnimator anim = textGO.AddComponent<FloatingTextAnimator>();
        anim.lifetime  = 1.0f;
        anim.riseSpeed = 1.2f;
    }
}

/// <summary>
/// Tiny helper that floats a TextMesh upward and fades it out,
/// then destroys the GameObject. Unchanged from the original implementation.
/// </summary>
public class FloatingTextAnimator : MonoBehaviour
{
    public float lifetime  = 1.0f;
    public float riseSpeed = 1.2f;

    private float    elapsed;
    private TextMesh textMesh;
    private Color    startColor;

    private void Start()
    {
        textMesh   = GetComponent<TextMesh>();
        startColor = textMesh != null ? textMesh.color : Color.white;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t  = elapsed / lifetime;

        // Rise
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        // Fade
        if (textMesh != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            textMesh.color = c;
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
