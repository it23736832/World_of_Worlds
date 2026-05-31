using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WinSequenceManager : MonoBehaviour
{
    [Header("Win Message Timing")]
    [SerializeField] private float _fadeInDuration  = 0.6f;
    [SerializeField] private float _holdDuration    = 3.5f;
    [SerializeField] private float _fadeOutDuration = 0.6f;

    // ── internal UI ──────────────────────────────────────────────────────────
    private Canvas    _canvas;
    private Image     _overlay;
    private Text      _headlineText;
    private Text      _subtitleText;
    private CanvasGroup _panelGroup;

    private void Start()
    {
        TreasureChestInteract chest = FindObjectOfType<TreasureChestInteract>();
        if (chest != null)
            chest.OnMicPickedUp += OnMicPickedUp;
        else
            Debug.LogWarning("[WinSequence] No TreasureChestInteract found in scene.", this);

        BuildWinUI();
    }

    private void OnDestroy()
    {
        TreasureChestInteract chest = FindObjectOfType<TreasureChestInteract>();
        if (chest != null)
            chest.OnMicPickedUp -= OnMicPickedUp;
    }

    private void OnMicPickedUp()
    {
        StartCoroutine(WinSequenceCoroutine());
    }

    private IEnumerator WinSequenceCoroutine()
    {
        // ── Step 2: show the win message ─────────────────────────────────────
        yield return StartCoroutine(ShowWinMessage());

        // ── Step 3 placeholder: spawn Zoey dancing ───────────────────────────
        // SpawnZoeyDancing();

        // ── Step 4 placeholder: trigger RUMI's dance animation ───────────────
        // TriggerRumiDance();

        // ── Step 5 placeholder: play Golden audio ────────────────────────────
        // PlayGoldenSong();
    }

    // ── Win Message UI ───────────────────────────────────────────────────────

    private IEnumerator ShowWinMessage()
    {
        _canvas.gameObject.SetActive(true);
        _panelGroup.alpha = 0f;

        // Fade in
        yield return StartCoroutine(FadeGroup(_panelGroup, 0f, 1f, _fadeInDuration));

        // Hold
        yield return new WaitForSeconds(_holdDuration);

        // Fade out
        yield return StartCoroutine(FadeGroup(_panelGroup, 1f, 0f, _fadeOutDuration));

        _canvas.gameObject.SetActive(false);
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }

    // Builds the win UI entirely in code — same approach as ChestInteractionUI
    private void BuildWinUI()
    {
        // Root canvas
        GameObject canvasGO = new GameObject("WinMessageCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Dim overlay behind the text panel
        GameObject overlayGO = new GameObject("DimOverlay");
        overlayGO.transform.SetParent(canvasGO.transform, false);
        _overlay = overlayGO.AddComponent<Image>();
        _overlay.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        // Panel (centred, upper-middle of screen)
        GameObject panelGO = new GameObject("WinPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        _panelGroup = panelGO.AddComponent<CanvasGroup>();

        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.02f, 0.14f, 0.92f);

        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(900f, 220f);
        panelRT.anchoredPosition = new Vector2(0f, 60f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Headline
        _headlineText = CreateText(panelGO.transform, "HeadlineText",
            "You did it, RUMI!  🎤",
            font, 64, FontStyle.Bold,
            new Color(1.0f, 0.88f, 0.25f, 1f),
            new Vector2(0f, 35f),
            new Vector2(860f, 90f));

        AddOutline(_headlineText.gameObject, new Color(0.55f, 0.15f, 0f, 1f), 2.5f);
        AddShadow(_headlineText.gameObject, new Color(0.72f, 0.08f, 1.0f, 0.85f), 3f);

        // Subtitle
        _subtitleText = CreateText(panelGO.transform, "SubtitleText",
            "Now let's celebrate!",
            font, 38, FontStyle.Italic,
            new Color(0.38f, 0.93f, 1.0f, 1f),
            new Vector2(0f, -42f),
            new Vector2(860f, 60f));

        AddOutline(_subtitleText.gameObject, new Color(0f, 0f, 0f, 1f), 1.5f);

        canvasGO.SetActive(false);
    }

    private static Text CreateText(Transform parent, string goName, string message,
        Font font, int size, FontStyle style, Color color,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);

        Text t = go.AddComponent<Text>();
        if (font != null) t.font = font;
        t.text      = message;
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = TextAnchor.MiddleCenter;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;

        return t;
    }

    private static void AddOutline(GameObject go, Color color, float dist)
    {
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor    = color;
        ol.effectDistance = new Vector2(dist, -dist);
    }

    private static void AddShadow(GameObject go, Color color, float dist)
    {
        Shadow sh = go.AddComponent<Shadow>();
        sh.effectColor    = color;
        sh.effectDistance = new Vector2(dist, -dist);
    }
}
