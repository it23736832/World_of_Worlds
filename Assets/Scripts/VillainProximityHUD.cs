using UnityEngine;
using UnityEngine.UI;

public class VillainProximityHUD : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 _panelSize = new Vector2(240f, 72f);
    [SerializeField] private Vector2 _margin    = new Vector2(18f, 18f);

    private Transform       _player;
    private AStarVillainChase _astar;
    private UCSVillainChase   _ucs;
    private Text  _headerText;
    private Text  _dataText;
    private Image _bg;

    private static readonly Color ColBgSafe   = new Color(0.04f, 0.02f, 0.14f, 0.85f);
    private static readonly Color ColBgClose  = new Color(0.22f, 0.04f, 0.04f, 0.88f);
    private static readonly Color ColBgDanger = new Color(0.48f, 0.01f, 0.01f, 0.92f);
    private static readonly Color ColCyan     = new Color(0.38f, 0.93f, 1.00f, 1.00f);
    private static readonly Color ColOrange   = new Color(1.00f, 0.62f, 0.10f, 1.00f);
    private static readonly Color ColRed      = new Color(1.00f, 0.28f, 0.28f, 1.00f);

    private void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
        _astar = FindObjectOfType<AStarVillainChase>();
        _ucs   = FindObjectOfType<UCSVillainChase>();

        BuildUI();
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("VillainProximityCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        // Panel — top-right corner
        GameObject panelGO = new GameObject("ProximityPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        _bg       = panelGO.AddComponent<Image>();
        _bg.color = ColBgSafe;

        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(1f, 1f);
        panelRT.anchorMax        = new Vector2(1f, 1f);
        panelRT.pivot            = new Vector2(1f, 1f);
        panelRT.sizeDelta        = _panelSize;
        panelRT.anchoredPosition = new Vector2(-_margin.x, -_margin.y);

        float halfH = _panelSize.y * 0.5f;

        // Header row (top half)
        _headerText = MakeText(panelGO, "Header", 17, FontStyle.Bold,
                               ColCyan, TextAnchor.MiddleCenter,
                               new Vector2(10f, halfH), new Vector2(-10f, -6f));

        // Data row (bottom half)
        _dataText = MakeText(panelGO, "Data", 20, FontStyle.Bold,
                             ColCyan, TextAnchor.MiddleCenter,
                             new Vector2(10f, 6f), new Vector2(-10f, -halfH));
    }

    private Text MakeText(GameObject parent, string name, int size, FontStyle style,
                          Color color, TextAnchor align, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        Text t    = go.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.font      = font;
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = align;

        Outline ol = go.AddComponent<Outline>();
        ol.effectColor    = new Color(0f, 0f, 0f, 1f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow sh = go.AddComponent<Shadow>();
        sh.effectColor    = new Color(0.72f, 0.08f, 1.0f, 0.75f);
        sh.effectDistance = new Vector2(2f, -2f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        return t;
    }

    private void Update()
    {
        if (_headerText == null) return;

        if (_player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
        }

        Transform villain = _astar != null ? _astar.transform
                          : _ucs   != null ? _ucs.transform
                          : null;

        if (villain == null || _player == null)
        {
            _headerText.text = "✦  VILLAIN";
            _dataText.text   = "--  ◆  --";
            return;
        }

        float dist  = Vector3.Distance(_player.position, villain.position);
        int   nodes = _astar != null ? _astar.PathNodesRemaining
                    : _ucs   != null ? _ucs.PathNodesRemaining
                    : -1;

        string header;
        Color  col;
        Color  bgTarget;

        if (dist <= 5f)
        {
            header   = "✦  DANGER — RUN!";
            col      = ColRed;
            bgTarget = ColBgDanger;
        }
        else if (dist <= 12f)
        {
            header   = "✦  VILLAIN CLOSING";
            col      = ColOrange;
            bgTarget = ColBgClose;
        }
        else
        {
            header   = "✦  VILLAIN";
            col      = ColCyan;
            bgTarget = ColBgSafe;
        }

        _headerText.text  = header;
        _headerText.color = col;

        _dataText.text  = nodes >= 0
            ? $"{dist:F1} m  ◆  {nodes} nodes"
            : $"{dist:F1} m";
        _dataText.color = col;

        _bg.color = Color.Lerp(_bg.color, bgTarget, Time.deltaTime * 5f);
    }
}
