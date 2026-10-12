using System;
using UnityEngine;

/// <summary>
/// Accelerated day/night clock for REEF.
/// One real second = 8 in-game minutes.
/// Day runs from 06:00 to 03:00 (21 in-game hours).
/// At 03:00 the day ends: counter increments, clock resets
/// to 06:00, and player energy is fully restored.
/// </summary>
public class TimeSystem : MonoBehaviour
{
    // ── Singleton (lightweight; non-persistent) ───────────────
    public static TimeSystem Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────
    /// <summary>Raised every frame with the current time.</summary>
    public event Action OnTimeChanged;

    /// <summary>Raised once when the day ends (03:00).</summary>
    public event Action<int> OnDayEnd; // passes the NEW day number

    // ── Configuration ─────────────────────────────────────────
    [Header("Time Scale")]
    [Tooltip("In-game minutes that pass per 1 real second.")]
    [SerializeField] private float minutesPerRealSecond = 8f;

    [Header("Day Window")]
    [Tooltip("Hour when the day begins (default 6 = 06:00).")]
    [SerializeField] private int dayStartHour = 6;
    [Tooltip("Hour when the day ends (default 3 = 03:00 next day).")]
    [SerializeField] private int dayEndHour = 3;

    // ── Runtime State ─────────────────────────────────────────
    [Header("Current State (read‑only at runtime)")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int currentHour   = 6;
    [SerializeField] private int currentMinute = 0;

    /// <summary>Accumulated fractional minutes.</summary>
    private float minuteAccumulator;

    /// <summary>Total in-game minutes elapsed since day start (06:00).</summary>
    private float totalMinutesToday;

    /// <summary>
    /// Total in-game minutes in one full day cycle (06:00→03:00 = 21 hours = 1260 min).
    /// Handles wrapping across midnight.
    /// </summary>
    private float dayLengthMinutes;

    // ── Public Properties ─────────────────────────────────────
    public int   CurrentDay    => currentDay;
    public int   CurrentHour   => currentHour;
    public int   CurrentMinute => currentMinute;

    /// <summary>Formatted time string "HH:mm".</summary>
    public string FormattedTime => $"{currentHour:D2}:{currentMinute:D2}";

    // ── Pause support ─────────────────────────────────────────
    private bool isPaused;
    public bool IsPaused
    {
        get => isPaused;
        set => isPaused = value;
    }

    // ───────────────────────────────────────────────────────────
    // Lifecycle
    // ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CalculateDayLength();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Ensure clock starts at 06:00
        currentHour   = dayStartHour;
        currentMinute = 0;
        totalMinutesToday   = 0f;
        minuteAccumulator   = 0f;
    }

    private void Update()
    {
        if (isPaused) return;
        AdvanceTime(Time.deltaTime);
    }

    // ───────────────────────────────────────────────────────────
    // Core tick
    // ───────────────────────────────────────────────────────────
    private void AdvanceTime(float realDeltaSeconds)
    {
        float minutesElapsed = realDeltaSeconds * minutesPerRealSecond;
        minuteAccumulator  += minutesElapsed;
        totalMinutesToday  += minutesElapsed;

        // Roll over whole minutes
        while (minuteAccumulator >= 1f)
        {
            minuteAccumulator -= 1f;
            currentMinute++;

            if (currentMinute >= 60)
            {
                currentMinute = 0;
                currentHour++;
                if (currentHour >= 24)
                    currentHour = 0;
            }
        }

        OnTimeChanged?.Invoke();

        // Check for end-of-day
        if (totalMinutesToday >= dayLengthMinutes)
        {
            EndDay();
        }
    }

    // ───────────────────────────────────────────────────────────
    // Day-end logic
    // ───────────────────────────────────────────────────────────
    private void EndDay()
    {
        currentDay++;

        Debug.Log($"[TimeSystem] ── Day {currentDay - 1} ends at {FormattedTime}. " +
                  $"New day {currentDay} dawns. ──");

        // Reset clock to dawn
        currentHour         = dayStartHour;
        currentMinute       = 0;
        totalMinutesToday   = 0f;
        minuteAccumulator   = 0f;

        // Restore player stamina
        if (IslandManager.Instance != null)
        {
            IslandManager.Instance.RestoreEnergy();
        }

        OnDayEnd?.Invoke(currentDay);
        OnTimeChanged?.Invoke();
    }

    // ───────────────────────────────────────────────────────────
    // Helpers
    // ───────────────────────────────────────────────────────────
    private void CalculateDayLength()
    {
        // 06:00 → 03:00 (next day) = 21 hours
        // If end < start, it wraps past midnight.
        int hours = dayEndHour >= dayStartHour
            ? dayEndHour - dayStartHour
            : (24 - dayStartHour) + dayEndHour;

        dayLengthMinutes = hours * 60f;
        Debug.Log($"[TimeSystem] Day length = {hours}h ({dayLengthMinutes} in-game minutes). " +
                  $"Real-time duration ≈ {dayLengthMinutes / minutesPerRealSecond:F0}s");
    }
}
