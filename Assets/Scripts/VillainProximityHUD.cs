using UnityEngine;
using UnityEngine.UI;

public class VillainProximityHUD : MonoBehaviour
{
    // Using constants (not SerializeField) so stale scene-serialized values can never break the layout.
    private const float PanelW  = 240f;
    private const float PanelH  = 96f;
    private const float MarginX = 18f;
    private const float MarginY = 18f;

    private Transform         _player;
    private AStarVillainChase _astar;
    private UCSVillainChase   _ucs;
    private Text  _headerText;
    private Text  _dataText;
    private Text  _sealsText;
    private Image _bg;

    private float  _lastDist   = -1f;
    private int    _lastNodes  = -99;
    private int    _lastSeals  = -1;
    private string _lastHeader = "";

    private static readonly Color ColBgSafe   = new Color(0.04f, 0.02f, 0.14f, 0.85f);
    private static readonly Color ColBgClose  = new Color(0.22f, 0.04f, 0.04f, 0.88f);
    private static readonly Color ColBgDanger = new Color(0.48f, 0.01f, 0.01f, 0.92f);
    private static readonly Color ColCyan     = new Color(0.38f, 0.93f, 1.00f, 1.00f);
    private static readonly Color ColOrange   = new Color(1.00f, 0.62f, 0.10f, 1.00f);
    private static readonly Color ColRed      = new Color(1.00f, 0.28f, 0.28f, 1.00f);
    private static readonly Color ColGrey     = new Color(0.50f, 0.50f, 0.55f, 0.80f);

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
        // Canvas
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
        panelRT.sizeDelta        = new Vector2(PanelW, PanelH);
        panelRT.anchoredPosition = new Vector2(-MarginX, -MarginY);

        // Divide panel into 3 equal rows using percentage anchors — immune to size mismatches
        _headerText = MakeRow(panelGO, "Header", 17, ColCyan, 2f / 3f, 1f, 4f);
        _dataText   = MakeRow(panelGO, "Data",   19, ColCyan, 1f / 3f, 2f / 3f, 4f);
        _sealsText  = MakeRow(panelGO, "Seals",  13, ColGrey, 0f,      1f / 3f, 4f);
    }

    // Creates a Text object that occupies a horizontal band between anchorYMin and anchorYMax,
    // with a pixel inset on all sides. This approach is immune to absolute pixel size assumptions.
    private Text MakeRow(GameObject panel, string rowName, int fontSize, Color color,
                         float anchorYMin, float anchorYMax, float inset)
    {
        GameObject go = new GameObject(rowName);
        go.transform.SetParent(panel.transform, false);

        Text t = go.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) t.font = font;   // only override if we actually found one
        t.fontSize          = fontSize;
        t.fontStyle         = FontStyle.Bold;
        t.color             = color;
        t.alignment         = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;

        Outline ol = go.AddComponent<Outline>();
        ol.effectColor    = new Color(0f, 0f, 0f, 1f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow sh = go.AddComponent<Shadow>();
        sh.effectColor    = new Color(0.72f, 0.08f, 1.0f, 0.75f);
        sh.effectDistance = new Vector2(2f, -2f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorYMin);
        rt.anchorMax = new Vector2(1f, anchorYMax);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);

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
            UpdateSealsRow();
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

        float dispDist = Mathf.Round(dist * 10f) / 10f;

        if (header != _lastHeader)
        {
            _headerText.text  = header;
            _headerText.color = col;
            _dataText.color   = col;
            _lastHeader       = header;
        }

        if (dispDist != _lastDist || nodes != _lastNodes)
        {
            _dataText.text = nodes >= 0
                ? $"{dispDist:F1} m  ◆  {nodes} nodes"
                : $"{dispDist:F1} m";
            _lastDist  = dispDist;
            _lastNodes = nodes;
        }

        UpdateSealsRow();
        _bg.color = Color.Lerp(_bg.color, bgTarget, Time.deltaTime * 5f);
    }

    private void UpdateSealsRow()
    {
        int seals = SealBarricade.ActiveCount;
        if (seals == _lastSeals) return;
        _lastSeals = seals;

        _sealsText.text  = seals > 0
            ? $"◆  {seals} seal{(seals != 1 ? "s" : "")} active"
            : "◆  no seals active";
        _sealsText.color = seals > 0 ? ColCyan : ColGrey;
    }
}
