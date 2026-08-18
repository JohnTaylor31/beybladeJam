using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class LaunchMinigame : MonoBehaviour
{
    public static LaunchMinigame Instance { get; private set; }

    public float LaunchPowerA { get; private set; } = 1f;
    public float LaunchPowerB { get; private set; } = 1f;

    [Header("Timing")]
    public float markerSpeed = 1.35f;
    public float resultHoldSeconds = 0.55f;
    public float inputLockoutSeconds = 0.2f;

    bool _active;
    int _bladeIndex;
    float _markerT;
    float _ignoreUntil;
    float _resultUntil;
    bool _showingResult;

    GameObject _root;
    RectTransform _marker;
    TextMeshProUGUI _prompt;
    TextMeshProUGUI _result;
    Image _fill;
    BallSpawner[] _disabledSpawners;

    const float BarWidth = 640f;

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
        RestoreSpawners();
    }

    void OnEnable()
    {
        if (MatchFlowManager.Instance != null)
            MatchFlowManager.Instance.StateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        if (MatchFlowManager.Instance != null)
            MatchFlowManager.Instance.StateChanged -= OnStateChanged;
    }

    void Start()
    {
        EnsureEventSystem();
        Build();
        Hide();
    }

    public void Begin()
    {
        if (_root == null)
            Build();

        LaunchPowerA = 0f;
        LaunchPowerB = 0f;
        _bladeIndex = 0;
        _showingResult = false;
        _active = true;
        _ignoreUntil = Time.unscaledTime + inputLockoutSeconds;
        DisableSpawners();
        ShowPrompt();
        _root.SetActive(true);
    }

    void Update()
    {
        if (!_active || _root == null || !_root.activeSelf)
            return;

        if (_showingResult)
        {
            if (Time.unscaledTime >= _resultUntil)
                AdvanceAfterResult();
            return;
        }

        _markerT = Mathf.PingPong(Time.unscaledTime * markerSpeed, 1f);
        UpdateMarker();

        if (ConfirmPressed())
            LockCurrentBlade();
    }

    void OnStateChanged(MatchState previous, MatchState next)
    {
        if (next != MatchState.LaunchMinigame)
            Hide();
    }

    void LockCurrentBlade()
    {
        float power = Mathf.Clamp01(1f - Mathf.Abs(_markerT - 0.5f) * 2f);
        if (_bladeIndex == 0)
            LaunchPowerA = power;
        else
            LaunchPowerB = power;

        _showingResult = true;
        _resultUntil = Time.unscaledTime + resultHoldSeconds;
        _result.text = ResultLabel(power);
        _result.color = ResultColor(power);
        _fill.fillAmount = power;
        _prompt.text = BladeLabel(_bladeIndex) + "  locked";
    }

    void AdvanceAfterResult()
    {
        _showingResult = false;
        _result.text = "";
        _ignoreUntil = Time.unscaledTime + inputLockoutSeconds;

        if (_bladeIndex == 0)
        {
            _bladeIndex = 1;
            ShowPrompt();
            return;
        }

        Finish();
    }

    void Finish()
    {
        _active = false;
        Hide();
        RestoreSpawners();
        GameMode.Instance?.StartMatch();
        MatchFlowManager.Instance?.CompleteLaunchMinigame();
    }

    void ShowPrompt()
    {
        _fill.fillAmount = 0f;
        _result.text = "";
        _prompt.text = BladeLabel(_bladeIndex) + "  —  hit the center!";
        UpdateMarker();
    }

    string BladeLabel(int index)
    {
        if (GameMode.Instance == null)
            return index == 0 ? "Beyblade A" : "Beyblade B";

        if (index == 0)
            return GameMode.Instance.playerBeyblade != null
                ? GameMode.Instance.playerBeyblade.name
                : "Beyblade A";

        return GameMode.Instance.opponentBeyblade != null
            ? GameMode.Instance.opponentBeyblade.name
            : "Beyblade B";
    }

    static string ResultLabel(float power)
    {
        if (power >= 0.9f) return $"PERFECT  {power * 100f:0}%";
        if (power >= 0.7f) return $"GREAT  {power * 100f:0}%";
        if (power >= 0.4f) return $"OK  {power * 100f:0}%";
        return $"WEAK  {power * 100f:0}%";
    }

    static Color ResultColor(float power)
    {
        if (power >= 0.9f) return new Color(1f, 0.84f, 0.28f);
        if (power >= 0.7f) return new Color(0.45f, 0.9f, 0.5f);
        if (power >= 0.4f) return new Color(0.85f, 0.85f, 0.9f);
        return new Color(0.95f, 0.4f, 0.35f);
    }

    void UpdateMarker()
    {
        if (_marker == null)
            return;
        float x = Mathf.Lerp(-BarWidth * 0.5f, BarWidth * 0.5f, _markerT);
        _marker.anchoredPosition = new Vector2(x, 0f);
    }

    bool ConfirmPressed()
    {
        if (Time.unscaledTime < _ignoreUntil)
            return false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            return true;
        return false;
    }

    void Hide()
    {
        _active = false;
        _showingResult = false;
        if (_root != null)
            _root.SetActive(false);
        RestoreSpawners();
    }

    void DisableSpawners()
    {
        _disabledSpawners = Object.FindObjectsByType<BallSpawner>(FindObjectsSortMode.None);
        foreach (BallSpawner spawner in _disabledSpawners)
        {
            if (spawner != null)
                spawner.enabled = false;
        }
    }

    void RestoreSpawners()
    {
        if (_disabledSpawners == null)
            return;
        foreach (BallSpawner spawner in _disabledSpawners)
        {
            if (spawner != null)
                spawner.enabled = true;
        }
        _disabledSpawners = null;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    void Build()
    {
        if (_root != null)
            return;

        EnsureEventSystem();
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        GameObject canvasGo = new GameObject("LaunchMinigameCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _root = CreateUIObject("LaunchMinigame", canvasGo.transform);
        Stretch(_root.GetComponent<RectTransform>());
        Image overlay = _root.AddComponent<Image>();
        overlay.color = new Color(0.02f, 0.03f, 0.06f, 0.55f);

        GameObject panel = CreateUIObject("Panel", _root.transform);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.18f);
        panelRt.anchorMax = new Vector2(0.5f, 0.18f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(860f, 280f);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.1f, 0.16f, 0.94f);

        _prompt = CreateText(panel.transform, "Prompt", "Hit the center!", 34f, Color.white, font, new Vector2(0f, 90f), new Vector2(800f, 50f));
        _result = CreateText(panel.transform, "Result", "", 28f, new Color(1f, 0.84f, 0.28f), font, new Vector2(0f, 48f), new Vector2(800f, 40f));

        GameObject bar = CreateUIObject("Bar", panel.transform);
        RectTransform barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0.42f);
        barRt.sizeDelta = new Vector2(BarWidth, 36f);
        bar.AddComponent<Image>().color = new Color(0.15f, 0.18f, 0.24f, 1f);

        GameObject sweet = CreateUIObject("SweetSpot", bar.transform);
        RectTransform sweetRt = sweet.GetComponent<RectTransform>();
        sweetRt.anchorMin = new Vector2(0.4f, 0f);
        sweetRt.anchorMax = new Vector2(0.6f, 1f);
        sweetRt.offsetMin = sweetRt.offsetMax = Vector2.zero;
        sweet.AddComponent<Image>().color = new Color(1f, 0.84f, 0.28f, 0.35f);

        GameObject fillGo = CreateUIObject("PowerFill", panel.transform);
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = fillRt.anchorMax = new Vector2(0.5f, 0.22f);
        fillRt.sizeDelta = new Vector2(BarWidth, 14f);
        Image fillBg = fillGo.AddComponent<Image>();
        fillBg.color = new Color(0.15f, 0.18f, 0.24f, 1f);

        GameObject fillInner = CreateUIObject("Fill", fillGo.transform);
        Stretch(fillInner.GetComponent<RectTransform>());
        _fill = fillInner.AddComponent<Image>();
        _fill.color = new Color(0.2f, 0.75f, 0.45f, 1f);
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillAmount = 0f;

        GameObject markerGo = CreateUIObject("Marker", bar.transform);
        _marker = markerGo.GetComponent<RectTransform>();
        _marker.sizeDelta = new Vector2(10f, 56f);
        markerGo.AddComponent<Image>().color = Color.white;

        CreateText(panel.transform, "Hint", "Space / Click / A", 22f, new Color(0.75f, 0.78f, 0.85f), font, new Vector2(0f, -100f), new Vector2(800f, 32f));
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, Color color, TMP_FontAsset font, Vector2 pos, Vector2 sizeDelta)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }
}
