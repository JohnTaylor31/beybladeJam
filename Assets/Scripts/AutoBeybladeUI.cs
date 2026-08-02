using UnityEngine;
using UnityEngine.UI;

public class AutoBeybladeUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject healthBarPrefab;

    private Canvas canvas;
    private BeybladeHealthBar barA;
    private BeybladeHealthBar barB;

    void Start()
    {
        SetupCanvas();
        CreateBars();
    }

    void Update()
    {
        AssignControllersIfNeeded();
    }

    void SetupCanvas()
    {
        canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject c = new GameObject("BeybladeCanvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }
    }

    void CreateBars()
    {
        GameObject aObj = Instantiate(healthBarPrefab, canvas.transform);
        aObj.name = "HealthBar_A";
        barA = aObj.GetComponent<BeybladeHealthBar>();

        GameObject bObj = Instantiate(healthBarPrefab, canvas.transform);
        bObj.name = "HealthBar_B";
        barB = bObj.GetComponent<BeybladeHealthBar>();

        RectTransform rtA = aObj.GetComponent<RectTransform>();
        RectTransform rtB = bObj.GetComponent<RectTransform>();

        rtA.anchorMin = new Vector2(0.05f, 0.9f);
        rtA.anchorMax = new Vector2(0.45f, 0.98f);
        rtA.offsetMin = rtA.offsetMax = Vector2.zero;

        rtB.anchorMin = new Vector2(0.55f, 0.9f);
        rtB.anchorMax = new Vector2(0.95f, 0.98f);
        rtB.offsetMin = rtB.offsetMax = Vector2.zero;
    }

    void AssignControllersIfNeeded()
    {
        if (barA.beybladeController == null || barB.beybladeController == null)
        {
            BeybladeController[] blades =
                Object.FindObjectsByType<BeybladeController>(FindObjectsSortMode.None);

            if (blades.Length >= 2)
            {
                barA.beybladeController = blades[0];
                barB.beybladeController = blades[1];
            }
        }
    }
}
