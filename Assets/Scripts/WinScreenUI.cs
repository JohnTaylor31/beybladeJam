using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinScreenUI : MonoBehaviour
{
    GameObject _root;
    TextMeshProUGUI _winnerName;
    TextMeshProUGUI _collisions;
    TextMeshProUGUI _impact;
    TextMeshProUGUI _timeSurvived;


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

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    void OnStateChanged(MatchState previous, MatchState next)
    {
        if (next == MatchState.WinScreen)
            Show();
        else
            Hide();
    }

    public void Rematch()
    {
        Time.timeScale = 1f;
        Hide();
        MatchFlowManager.Instance?.ContinueFromWinScreen();
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void Show()
    {
        MatchFlowManager flow = MatchFlowManager.Instance;
        if (flow == null || _root == null)
            return;

        _winnerName.text = flow.WinnerDisplayName;
        _collisions.text = $"Total collisions:  {flow.TotalCollisions}";
        _impact.text = $"Highest impact:  {flow.HighestImpact:0.0}";
        _timeSurvived.text = $"Time survived:  {FormatTime(flow.TimeSurvived)}";

        _root.SetActive(true);
        Time.timeScale = 0f;
    }

    void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    static string FormatTime(float seconds)
    {
        int whole = Mathf.FloorToInt(seconds);
        int minutes = whole / 60;
        float remainder = seconds - minutes * 60;
        if (minutes > 0)
            return $"{minutes}:{remainder:00.0}";
        return $"{seconds:0.0}s";
    }

    void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    void Build()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        GameObject canvasGo = new GameObject("WinScreenCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _root = CreateUIObject("WinScreen", canvasGo.transform);
        Stretch(_root.GetComponent<RectTransform>());

        Image overlay = _root.AddComponent<Image>();
        overlay.color = new Color(0.02f, 0.03f, 0.06f, 0.82f);
        overlay.raycastTarget = true;

        GameObject panel = CreateUIObject("Panel", _root.transform);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(720f, 640f);
        panelRt.anchoredPosition = Vector2.zero;

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.1f, 0.16f, 0.96f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 40, 40);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateText(panel.transform, "Title", "WINNER", 36f, new Color(0.75f, 0.78f, 0.85f), font, 40f);
        _winnerName = CreateText(panel.transform, "WinnerName", "—", 64f, new Color(1f, 0.84f, 0.28f), font, 80f);
        _winnerName.fontStyle = FontStyles.Bold;

        CreateSpacer(panel.transform, 8f);

        _collisions = CreateText(panel.transform, "Collisions", "Total collisions:  0", 32f, Color.white, font, 42f);
        _impact = CreateText(panel.transform, "Impact", "Highest impact:  0.0", 32f, Color.white, font, 42f);
        _timeSurvived = CreateText(panel.transform, "TimeSurvived", "Time survived:  0.0s", 32f, Color.white, font, 42f);

        CreateSpacer(panel.transform, 12f);

        GameObject buttonRow = CreateUIObject("Buttons", panel.transform);
        LayoutElement rowLe = buttonRow.AddComponent<LayoutElement>();
        rowLe.minHeight = 72f;
        rowLe.preferredHeight = 72f;

        HorizontalLayoutGroup row = buttonRow.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 24f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlHeight = true;
        row.childControlWidth = true;
        row.childForceExpandHeight = true;
        row.childForceExpandWidth = true;

        CreateButton(buttonRow.transform, "Rematch", "Rematch", new Color(0.15f, 0.62f, 0.38f), font, Rematch);
        CreateButton(buttonRow.transform, "ReturnToMenu", "Return to Menu", new Color(0.55f, 0.22f, 0.22f), font, ReturnToMenu);
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

    static void CreateSpacer(Transform parent, float height)
    {
        GameObject go = CreateUIObject("Spacer", parent);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, Color color, TMP_FontAsset font, float height)
    {
        GameObject go = CreateUIObject(name, parent);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

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

    static void CreateButton(Transform parent, string name, string label, Color color, TMP_FontAsset font, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreateUIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = color;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject textGo = CreateUIObject("Label", go.transform);
        Stretch(textGo.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 28f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }
}
