using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class BeybladeBuilderUI : MonoBehaviour
{
    public static BeybladeBuilderUI Instance { get; private set; }

    [Header("UI References")]
    private GameObject _root;
    private TextMeshProUGUI _infoText;

    private int _currentPartType = 0; // 0=Core, 1=Ring, 2=Tip
    private bool _isPlayerSelection = true;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
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

    void OnStateChanged(MatchState previous, MatchState next)
    {
        if (next == MatchState.PreLaunch)
            Show();
        else
            Hide();
    }

    public void Show()
    {
        if (_root == null)
            Build();

        UpdateUI();
        _root.SetActive(true);
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    public void StartMatch()
    {
        Hide();
        if (MatchFlowManager.Instance != null)
            MatchFlowManager.Instance.CompleteLaunchMinigame();
    }

    public void RandomizeOpponent()
    {
        if (BeybladeBuilder.Instance != null)
            BeybladeBuilder.Instance.RandomizeOpponent();
        UpdateUI();
    }

    public void NextPart()
    {
        if (BeybladeBuilder.Instance == null) return;

        switch (_currentPartType)
        {
            case 0:
                CyclePart(ref BeybladeBuilder.Instance.selectedCoreIndex, BeybladeBuilder.Instance.availableCores);
                break;
            case 1:
                CyclePart(ref BeybladeBuilder.Instance.selectedRingIndex, BeybladeBuilder.Instance.availableRings);
                break;
            case 2:
                CyclePart(ref BeybladeBuilder.Instance.selectedTipIndex, BeybladeBuilder.Instance.availableTips);
                break;
        }
        UpdateUI();
    }

    public void PrevPart()
    {
        if (BeybladeBuilder.Instance == null) return;

        switch (_currentPartType)
        {
            case 0:
                CyclePart(ref BeybladeBuilder.Instance.selectedCoreIndex, BeybladeBuilder.Instance.availableCores, -1);
                break;
            case 1:
                CyclePart(ref BeybladeBuilder.Instance.selectedRingIndex, BeybladeBuilder.Instance.availableRings, -1);
                break;
            case 2:
                CyclePart(ref BeybladeBuilder.Instance.selectedTipIndex, BeybladeBuilder.Instance.availableTips, -1);
                break;
        }
        UpdateUI();
    }

    public void SelectCore() { _currentPartType = 0; UpdateUI(); }
    public void SelectRing() { _currentPartType = 1; UpdateUI(); }
    public void SelectTip() { _currentPartType = 2; UpdateUI(); }

    void CyclePart(ref int index, ScriptableObject[] parts, int direction = 1)
    {
        if (parts == null || parts.Length == 0) return;

        index += direction;
        if (index >= parts.Length) index = 0;
        if (index < 0) index = parts.Length - 1;
    }

    void UpdateUI()
    {
        if (BeybladeBuilder.Instance == null || _infoText == null)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("BEYBLADE BUILDER");
        sb.AppendLine();
        sb.AppendLine("YOUR BEYBLADE:");
        sb.AppendLine(GetPartInfo("Core", BeybladeBuilder.Instance.selectedCoreIndex, BeybladeBuilder.Instance.availableCores));
        sb.AppendLine(GetPartInfo("Ring", BeybladeBuilder.Instance.selectedRingIndex, BeybladeBuilder.Instance.availableRings));
        sb.AppendLine(GetPartInfo("Tip", BeybladeBuilder.Instance.selectedTipIndex, BeybladeBuilder.Instance.availableTips));
        sb.AppendLine();
        sb.AppendLine("OPPONENT:");
        sb.AppendLine(GetPartInfo("Core", BeybladeBuilder.Instance.opponentCoreIndex, BeybladeBuilder.Instance.availableCores));
        sb.AppendLine(GetPartInfo("Ring", BeybladeBuilder.Instance.opponentRingIndex, BeybladeBuilder.Instance.availableRings));
        sb.AppendLine(GetPartInfo("Tip", BeybladeBuilder.Instance.opponentTipIndex, BeybladeBuilder.Instance.availableTips));
        sb.AppendLine();
        sb.AppendLine("CONTROLS:");
        sb.AppendLine("1/2/3: Select Core/Ring/Tip");
        sb.AppendLine("A/D or Left/Right: Change part");
        sb.AppendLine("R: Randomize opponent");
        sb.AppendLine("ENTER: Start match");
        sb.AppendLine();
        sb.AppendLine($"Currently editing: {GetCurrentPartTypeName()}");

        _infoText.text = sb.ToString();
    }

    string GetCurrentPartTypeName()
    {
        switch (_currentPartType)
        {
            case 0: return "CORE";
            case 1: return "RING";
            case 2: return "TIP";
            default: return "NONE";
        }
    }

    string GetPartInfo(string partType, int index, ScriptableObject[] parts)
    {
        if (parts == null || parts.Length == 0 || index >= parts.Length)
            return $"{partType}: None";

        var part = parts[index];
        string description = "";

        if (part is BeybladeCore core)
            description = core.description;
        else if (part is BeybladeRing ring)
            description = ring.description;
        else if (part is BeybladeTip tip)
            description = tip.description;

        return $"{partType}: {part.name}";
    }

    void Update()
    {
        if (!_root || !_root.activeSelf) return;

        // Keyboard controls
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectCore();
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectRing();
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectTip();
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) PrevPart();
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) NextPart();
        if (Input.GetKeyDown(KeyCode.R)) RandomizeOpponent();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) StartMatch();
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
        if (_root != null) return;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        GameObject canvasGo = new GameObject("BuilderCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _root = CreateUIObject("BuilderPanel", canvasGo.transform);
        RectTransform panelRt = _root.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(800f, 600f);

        Image panelImg = _root.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.08f, 0.12f, 0.95f);

        _infoText = CreateText(_root.transform, "InfoText", "Builder Info", 18f, Color.white, font, Vector2.zero, new Vector2(700f, 500f));
        _infoText.alignment = TextAlignmentOptions.TopLeft;
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, Color color, TMP_FontAsset font, Vector2 pos, Vector2 sizeDelta)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }
}
