using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MatchState
{
    PreLaunch,
    LaunchMinigame,
    Playing,
    KO,
    WinScreen,
    Restart
}

public class MatchFlowManager : MonoBehaviour
{
    public static MatchFlowManager Instance { get; private set; }

    [Header("State Durations (seconds)")]
    public float preLaunchDuration = 0f;
    public float launchMinigameDuration = 0f;
    public float koDuration = 0.5f;
    public float restartDuration = 0f;

    public MatchState CurrentState { get; private set; } = MatchState.PreLaunch;
    public BeybladeController LastKO { get; private set; }
    public BeybladeController Winner { get; private set; }

    public int TotalCollisions { get; private set; }
    public float HighestImpact { get; private set; }
    public float TimeSurvived { get; private set; }
    public List<float> SpinHistory { get; private set; } // Tracks spin speed over time

    public string WinnerDisplayName
    {
        get
        {
            if (Winner == null)
                return "Unknown";
            if (GameMode.Instance != null)
                return GameMode.Instance.GetDisplayName(Winner.gameObject);
            return Winner.name;
        }
    }

    public event Action<MatchState, MatchState> StateChanged;

    bool _flowActive;
    int _generation;
    Coroutine _pending;
    float _playingStartedAt;
    Coroutine _spinTrackingCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        BeginFlow();
    }

    public void BeginFlow()
    {
        if (_flowActive)
            return;

        _flowActive = true;
        Enter(MatchState.PreLaunch);
    }

    public void NotifyKO(BeybladeController ko)
    {
        if (CurrentState != MatchState.Playing)
            return;

        LastKO = ko;
        Winner = ResolveWinner(ko);
        TimeSurvived = Mathf.Max(0f, Time.unscaledTime - _playingStartedAt);
        Enter(MatchState.KO);
    }

    public void RecordCollision(float impact)
    {
        if (CurrentState != MatchState.Playing)
            return;

        TotalCollisions++;
        if (impact > HighestImpact)
            HighestImpact = impact;
    }

    public void CompleteLaunchMinigame()
    {
        if (CurrentState != MatchState.LaunchMinigame)
            return;
        Enter(MatchState.Playing);
    }

    public void ContinueFromWinScreen()
    {
        if (CurrentState != MatchState.WinScreen)
            return;
        Enter(MatchState.Restart);
    }

    BeybladeController ResolveWinner(BeybladeController ko)
    {
        if (GameMode.Instance == null)
            return null;

        BeybladeController a = GameMode.Instance.a != null
            ? GameMode.Instance.a.GetComponent<BeybladeController>()
            : null;
        BeybladeController b = GameMode.Instance.b != null
            ? GameMode.Instance.b.GetComponent<BeybladeController>()
            : null;

        if (ko == a) return b;
        if (ko == b) return a;
        return null;
    }

    void Enter(MatchState next)
    {
        MatchState previous = CurrentState;
        CurrentState = next;
        _generation++;

        if (_pending != null)
        {
            StopCoroutine(_pending);
            _pending = null;
        }

        StateChanged?.Invoke(previous, next);

        switch (next)
        {
            case MatchState.PreLaunch:
                Time.timeScale = 1f;
                LastKO = null;
                Winner = null;
                TotalCollisions = 0;
                HighestImpact = 0f;
                TimeSurvived = 0f;
                SpinHistory = new List<float>();
                Schedule(preLaunchDuration, () => Enter(MatchState.LaunchMinigame));
                break;

            case MatchState.LaunchMinigame:
                if (LaunchMinigame.Instance != null)
                    LaunchMinigame.Instance.Begin();
                else
                {
                    GameMode.Instance?.StartMatch();
                    Schedule(0f, () => Enter(MatchState.Playing));
                }
                break;

            case MatchState.Playing:
                _playingStartedAt = Time.unscaledTime;
                StartSpinTracking();
                break;

            case MatchState.KO:
                StopSpinTracking();
                Schedule(koDuration, () => Enter(MatchState.WinScreen));
                break;

            case MatchState.WinScreen:
                {
                    Time.timeScale = 0f;
                    break;
                }


            case MatchState.Restart:
                Time.timeScale = 1f;
                Schedule(restartDuration, () =>
                {
                    _flowActive = false;
                    BeginFlow();
                });
                break;
        }
    }

    void Schedule(float delay, Action next)
    {
        _pending = StartCoroutine(DelayThen(_generation, delay, next));
    }

    IEnumerator DelayThen(int generation, float delay, Action next)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        else
            yield return null;

        _pending = null;
        if (generation != _generation)
            yield break;

        next();
    }

    void StartSpinTracking()
    {
        if (_spinTrackingCoroutine != null)
            StopCoroutine(_spinTrackingCoroutine);

        SpinHistory = new List<float>();
        _spinTrackingCoroutine = StartCoroutine(TrackSpinCoroutine());
    }

    void StopSpinTracking()
    {
        if (_spinTrackingCoroutine != null)
        {
            StopCoroutine(_spinTrackingCoroutine);
            _spinTrackingCoroutine = null;
        }
    }

    IEnumerator TrackSpinCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Sample every 0.5 seconds

            if (GameMode.Instance == null)
                continue;

            // Get spin from both beyblades and average them
            float totalSpin = 0f;
            int bladeCount = 0;

            if (GameMode.Instance.a != null)
            {
                var controller = GameMode.Instance.a.GetComponent<BeybladeController>();
                if (controller != null)
                {
                    var spinField = typeof(BeybladeController).GetField("spinSpeed",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (spinField != null)
                    {
                        totalSpin += (float)spinField.GetValue(controller);
                        bladeCount++;
                    }
                }
            }

            if (GameMode.Instance.b != null)
            {
                var controller = GameMode.Instance.b.GetComponent<BeybladeController>();
                if (controller != null)
                {
                    var spinField = typeof(BeybladeController).GetField("spinSpeed",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (spinField != null)
                    {
                        totalSpin += (float)spinField.GetValue(controller);
                        bladeCount++;
                    }
                }
            }

            if (bladeCount > 0)
            {
                float avgSpin = totalSpin / bladeCount;
                SpinHistory.Add(avgSpin);
            }
        }
    }
}
