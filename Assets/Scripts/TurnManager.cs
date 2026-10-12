using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnManager : MonoBehaviour
{
    [Header("GameState Metrics")]
    [SerializeField] public int currentTurn = 1;
    [SerializeField] public int budget = 10000;
    [SerializeField] public float reefHealth = 50.0f;

    [Header("UI References")]
    [SerializeField] public TMP_Text hudText;
    [SerializeField] public TMP_Text advisoryText;
    [SerializeField] public Button deployReefButton;

    [Header("Spawning References")]
    [SerializeField] public Sprite reefSprite;
    [SerializeField] public GameObject reefPrefab;
    [SerializeField] public Transform oceanWorldParent;

    // Available grid positions for spawning artificial reefs
    private List<Vector3Int> availableGridCoords = new List<Vector3Int>();
    private List<Vector3Int> occupiedGridCoords = new List<Vector3Int>();

    private void Awake()
    {
        InitializeGridPositions();
    }

    private void Start()
    {
        UpdateHUD();
        if (advisoryText != null && string.IsNullOrEmpty(advisoryText.text))
        {
            advisoryText.text = $"Turn {currentTurn}: OceanGuard monitoring online. Awaiting policy deployment.";
        }

        if (deployReefButton != null)
        {
            deployReefButton.onClick.RemoveListener(OnDeployReefClicked);
            deployReefButton.onClick.AddListener(OnDeployReefClicked);
        }
    }

    public void InitializeGridPositions()
    {
        availableGridCoords.Clear();
        // 6x6 grid coordinates (gx, gy)
        for (int gx = 0; gx < 6; gx++)
        {
            for (int gy = 0; gy < 6; gy++)
            {
                // Skip pre-occupied grid slots: (2,2), (3,2), (0,4), (5,1)
                if ((gx == 2 && gy == 2) || (gx == 3 && gy == 2) || (gx == 0 && gy == 4) || (gx == 5 && gy == 1))
                {
                    continue;
                }
                availableGridCoords.Add(new Vector3Int(gx, gy, 0));
            }
        }
    }

    public void OnDeployReefClicked()
    {
        if (budget < 2000)
        {
            if (advisoryText != null)
            {
                advisoryText.text = $"Turn {currentTurn}: Insufficient funds! Artificial reef module costs $2,000.";
            }
            return;
        }

        budget -= 2000;
        reefHealth = Mathf.Min(100.0f, reefHealth + 12.5f);
        currentTurn += 1;

        // Spawn Reef Module on seabed
        SpawnReefStructureOnGrid();

        UpdateHUD();

        if (advisoryText != null)
        {
            advisoryText.text = $"Turn {currentTurn}: Artificial reef module anchored. Substrate stability increased!";
        }
    }

    private void SpawnReefStructureOnGrid()
    {
        if (availableGridCoords.Count == 0) return;

        // Pick next available grid position
        Vector3Int gridCoord = availableGridCoords[0];
        availableGridCoords.RemoveAt(0);
        occupiedGridCoords.Add(gridCoord);

        // Convert grid coordinate to isometric world position
        int gx = gridCoord.x;
        int gy = gridCoord.y;
        float worldX = (gx - gy) * 0.875f;
        float worldY = (gx + gy) * 0.4375f - 2.1875f + 0.4f; // height offset above tile

        GameObject reefGO = null;
        if (reefPrefab != null)
        {
            reefGO = Instantiate(reefPrefab, new Vector3(worldX, worldY, 0f), Quaternion.identity, oceanWorldParent);
        }
        else
        {
            reefGO = new GameObject($"Reef_Structure_{gx}_{gy}");
            if (oceanWorldParent != null)
            {
                reefGO.transform.SetParent(oceanWorldParent, true);
            }
            reefGO.transform.position = new Vector3(worldX, worldY, 0f);

            SpriteRenderer sr = reefGO.AddComponent<SpriteRenderer>();
            if (reefSprite != null)
            {
                sr.sprite = reefSprite;
            }
#if UNITY_EDITOR
            else
            {
                sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/reef_structure.png");
            }
#endif
            sr.sortingOrder = (gx + gy) * 10 + 5;
        }

        // Pop-in scale animation
        StartCoroutine(AnimatePopIn(reefGO.transform));
    }

    private IEnumerator AnimatePopIn(Transform targetTransform)
    {
        if (targetTransform == null) yield break;

        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 finalScale = Vector3.one;
        Vector3 overshootScale = new Vector3(1.25f, 1.25f, 1.0f);

        targetTransform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (t < 0.7f)
            {
                float subT = t / 0.7f;
                targetTransform.localScale = Vector3.Lerp(Vector3.zero, overshootScale, subT);
            }
            else
            {
                float subT = (t - 0.7f) / 0.3f;
                targetTransform.localScale = Vector3.Lerp(overshootScale, finalScale, subT);
            }

            yield return null;
        }

        if (targetTransform != null)
        {
            targetTransform.localScale = finalScale;
        }
    }

    public void UpdateHUD()
    {
        if (hudText != null)
        {
            hudText.text = $"Turn: {currentTurn}  |  Budget: ${budget:N0}  |  Reef Health: {reefHealth:F1}%";
        }
    }
}
