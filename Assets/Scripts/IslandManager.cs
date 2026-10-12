using System;
using UnityEngine;

/// <summary>
/// Singleton state hub for the REEF island simulation.
/// Manages economy (Sundials), player stamina (Energy), environmental
/// pressure (Heat), and tourist attractiveness (Appeal).
/// Replaces the legacy TurnManager component.
/// </summary>
public class IslandManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    public static IslandManager Instance { get; private set; }

    // ── Events (UI and other systems subscribe) ───────────────
    /// <summary>Raised whenever any tracked stat changes.</summary>
    public event Action OnStatsChanged;

    // ── Currency ───────────────────────────────────────────────
    [Header("Economy")]
    [SerializeField] private int sundials = 100;
    public int Sundials
    {
        get => sundials;
        private set { sundials = value; OnStatsChanged?.Invoke(); }
    }

    // ── Player Stamina ────────────────────────────────────────
    [Header("Player Stamina")]
    [SerializeField] private int energy = 50;
    [SerializeField] private int maxEnergy = 50;
    public int Energy
    {
        get => energy;
        private set { energy = Mathf.Clamp(value, 0, maxEnergy); OnStatsChanged?.Invoke(); }
    }
    public int MaxEnergy
    {
        get => maxEnergy;
        set { maxEnergy = Mathf.Max(1, value); OnStatsChanged?.Invoke(); }
    }

    // ── Environmental Heat (0‥100) ────────────────────────────
    [Header("Environment")]
    [Tooltip("0 = pristine island, 100 = severe bleaching")]
    [Range(0f, 100f)]
    [SerializeField] private float heat = 15f;
    public float Heat
    {
        get => heat;
        private set { heat = Mathf.Clamp(value, 0f, 100f); OnStatsChanged?.Invoke(); }
    }

    // ── Tourist Appeal ────────────────────────────────────────
    [SerializeField] private int appeal = 20;
    public int Appeal
    {
        get => appeal;
        private set { appeal = Mathf.Max(0, value); OnStatsChanged?.Invoke(); }
    }

    // ── Progress Counters ─────────────────────────────────────
    [Header("Progress")]
    [SerializeField] private int plantedFloraCount;
    [SerializeField] private int plantedCoralCount;
    [SerializeField] private int cleanedLitterCount;
    public int PlantedFloraCount  => plantedFloraCount;
    public int PlantedCoralCount  => plantedCoralCount;
    public int CleanedLitterCount => cleanedLitterCount;

    // ── Tuning Constants ──────────────────────────────────────
    private const int   LitterEnergyCost    = 2;
    private const int   LitterSundialReward = 15;
    private const float LitterHeatReduction = 1f;
    private const float PlantHeatReduction  = 6f;
    private const int   PlantAppealBonus    = 5;

    // ───────────────────────────────────────────────────────────
    // Lifecycle
    // ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[IslandManager] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ───────────────────────────────────────────────────────────
    // Transactional API
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to deduct <paramref name="amount"/> energy.
    /// Returns true on success; false if player has insufficient stamina.
    /// </summary>
    public bool TrySpendEnergy(int amount)
    {
        if (amount <= 0) return true;
        if (energy < amount) return false;
        Energy -= amount;
        return true;
    }

    /// <summary>Deposits sundials (currency) to the player's wallet.</summary>
    public void AddSundials(int amount)
    {
        if (amount <= 0) return;
        Sundials += amount;
    }

    /// <summary>
    /// Full litter‑cleaning transaction:
    /// costs 2 energy, grants 15 sundials, cools heat by 1,
    /// and increments the cleaned‑litter counter.
    /// Caller should pre‑validate via <see cref="TrySpendEnergy"/>.
    /// </summary>
    public void CleanLitterTransaction(Vector3 worldPos)
    {
        if (!TrySpendEnergy(LitterEnergyCost))
        {
            Debug.Log("[IslandManager] Not enough energy to clean litter.");
            return;
        }

        sundials += LitterSundialReward;
        heat      = Mathf.Max(0f, heat - LitterHeatReduction);
        cleanedLitterCount++;

        OnStatsChanged?.Invoke();

        Debug.Log($"[IslandManager] Litter cleaned at {worldPos}. " +
                  $"+{LitterSundialReward}☀  Energy:{energy}/{maxEnergy}  Heat:{heat:F1}%");
    }

    /// <summary>
    /// Attempts a flora or coral planting transaction.
    /// Validates funds and energy first, then deducts costs
    /// and adjusts heat (−6) and appeal (+5).
    /// Returns true on success.
    /// </summary>
    public bool TryPlantItem(bool isCoral, int sundialCost, int energyCost)
    {
        // Validate
        if (sundials < sundialCost)
        {
            Debug.Log("[IslandManager] Not enough sundials to plant.");
            return false;
        }
        if (energy < energyCost)
        {
            Debug.Log("[IslandManager] Not enough energy to plant.");
            return false;
        }

        // Deduct costs
        sundials -= sundialCost;
        energy    = Mathf.Max(0, energy - energyCost);

        // Environmental effects
        heat   = Mathf.Max(0f, heat - PlantHeatReduction);
        appeal += PlantAppealBonus;

        // Track
        if (isCoral)
            plantedCoralCount++;
        else
            plantedFloraCount++;

        OnStatsChanged?.Invoke();

        string label = isCoral ? "Coral" : "Flora";
        Debug.Log($"[IslandManager] {label} planted. " +
                  $"☀{sundials} E:{energy}/{maxEnergy} Heat:{heat:F1}% Appeal:{appeal}");
        return true;
    }

    /// <summary>
    /// Fully restores the player's energy to <see cref="MaxEnergy"/>.
    /// Called by <see cref="TimeSystem"/> at dawn.
    /// </summary>
    public void RestoreEnergy()
    {
        Energy = maxEnergy;
        Debug.Log("[IslandManager] Energy fully restored at dawn.");
    }
}
