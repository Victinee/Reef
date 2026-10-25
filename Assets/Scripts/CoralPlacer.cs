using UnityEngine;
using TMPro; 

public class CoralPlacer : MonoBehaviour
{
    [Header("Placement Settings")]
    public GameObject coralPrefab;
    public LayerMask seabedLayer;
    public float waterLevelY = 0f; 

    [Header("Economy & UI")]
    public int coralCount = 10; 
    public int fixedSunDials = 500; // The fixed amount of SunDials
    public TextMeshProUGUI statsText; 

    void Start()
    {
        UpdateUI(); 
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            AttemptPlacement();
        }
    }

    void AttemptPlacement()
    {
        if (coralCount <= 0)
        {
            Debug.Log("Out of corals!");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, seabedLayer))
        {
            if (hit.point.y < waterLevelY)
            {
                Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Quaternion randomSpin = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                Instantiate(coralPrefab, hit.point, surfaceRotation * randomSpin);
                
                // Deduct inventory only
                coralCount--;
                UpdateUI();
                
                Debug.Log($"SUCCESS: Planted coral. Corals left: {coralCount}");
            }
        }
    }

    void UpdateUI()
    {
        if (statsText != null)
        {
            statsText.text = $"CORALS: {coralCount}   SUNDIALS: {fixedSunDials}";
        }
    }
}