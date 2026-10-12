using UnityEngine;

/// <summary>
/// Attached to interactive beach litter sprites.
/// Player clicks/taps to clean litter, spending energy and earning sundials.
/// Requires a Collider2D on the same GameObject.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LitterNode : MonoBehaviour
{
    [Header("Floating Text")]
    [Tooltip("Optional prefab for the floating \"+15\" feedback. " +
             "If null, a simple TextMesh is spawned at runtime.")]
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("Tuning")]
    [SerializeField] private int energyCost     = 2;
    [SerializeField] private int sundialReward  = 15;

    // Prevent double-clicks during destruction
    private bool isConsumed;

    // ───────────────────────────────────────────────────────────
    // Input
    // ───────────────────────────────────────────────────────────
    private void OnMouseDown()
    {
        if (isConsumed) return;

        IslandManager mgr = IslandManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[LitterNode] IslandManager.Instance is null.");
            return;
        }

        // Attempt the full transaction (energy check + rewards happen inside)
        // Note: CleanLitterTransaction already calls TrySpendEnergy internally.
        if (mgr.Energy < energyCost)
        {
            Debug.Log("[LitterNode] Not enough energy to clean this litter.");
            return;
        }

        isConsumed = true;
        mgr.CleanLitterTransaction(transform.position);

        // Visual feedback – floating "+15" text
        SpawnFloatingText($"+{sundialReward}");

        // Clean up
        Destroy(gameObject);
    }

    // ───────────────────────────────────────────────────────────
    // Floating feedback text
    // ───────────────────────────────────────────────────────────
    private void SpawnFloatingText(string message)
    {
        if (floatingTextPrefab != null)
        {
            GameObject go = Instantiate(floatingTextPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
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
/// then destroys the GameObject.
/// </summary>
public class FloatingTextAnimator : MonoBehaviour
{
    public float lifetime  = 1.0f;
    public float riseSpeed = 1.2f;

    private float elapsed;
    private TextMesh textMesh;
    private Color startColor;

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
