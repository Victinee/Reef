using UnityEngine;
using TMPro;

/// <summary>
/// Reactively binds IslandManager + TimeSystem telemetry to a single
/// Canvas TextMeshPro element.
/// Format: "Time: HH:mm | Day: N | Sundials: N | Energy: N/N | Heat: N.N% | Appeal: N"
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class HUDBinding : MonoBehaviour
{
    private TMP_Text hudLabel;

    private void Awake()
    {
        hudLabel = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        // Subscribe to reactive events
        if (IslandManager.Instance != null)
            IslandManager.Instance.OnStatsChanged += RefreshHUD;

        if (TimeSystem.Instance != null)
            TimeSystem.Instance.OnTimeChanged += RefreshHUD;
    }

    private void OnDisable()
    {
        if (IslandManager.Instance != null)
            IslandManager.Instance.OnStatsChanged -= RefreshHUD;

        if (TimeSystem.Instance != null)
            TimeSystem.Instance.OnTimeChanged -= RefreshHUD;
    }

    private void Start()
    {
        // Late-bind in case singletons initialised after OnEnable
        if (IslandManager.Instance != null)
        {
            IslandManager.Instance.OnStatsChanged -= RefreshHUD;
            IslandManager.Instance.OnStatsChanged += RefreshHUD;
        }

        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.OnTimeChanged -= RefreshHUD;
            TimeSystem.Instance.OnTimeChanged += RefreshHUD;
        }

        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (hudLabel == null) return;

        var mgr  = IslandManager.Instance;
        var time = TimeSystem.Instance;

        string timeStr   = time != null ? time.FormattedTime           : "--:--";
        int    day       = time != null ? time.CurrentDay              : 0;
        int    sundials  = mgr  != null ? mgr.Sundials                : 0;
        int    energy    = mgr  != null ? mgr.Energy                  : 0;
        int    maxEnergy = mgr  != null ? mgr.MaxEnergy               : 0;
        float  heat      = mgr  != null ? mgr.Heat                    : 0f;
        int    appeal    = mgr  != null ? mgr.Appeal                  : 0;

        hudLabel.text = $"Time: {timeStr}  |  Day: {day}  |  ☀ Sundials: {sundials}  |  " +
                        $"⚡ Energy: {energy}/{maxEnergy}  |  🔥 Heat: {heat:F1}%  |  ✦ Appeal: {appeal}";
    }
}
