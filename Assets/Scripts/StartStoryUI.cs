using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

public class StartStoryUI : MonoBehaviour
{
    public static bool IsActive { get; private set; } = false;

    [Header("Story")]
    [TextArea(3, 6)]
    [SerializeField] private string[] paragraphs =
    {
        "You wake in the asylum's cold corridor, the air heavy with ash and whispers. The doors are locked, the halls twisted, and every step feels watched.",
        "Somewhere ahead, a portal flickers. It is your only way out, but Jinu has already sensed you and will close the distance fast.",
        "Find the portal to reach the other world before he catches you. Call Zoey when the fight turns against you; her blade buys you a breath."
    };

    [SerializeField] private float secondsPerParagraph = 6f;

    [Header("Pause Jinu")]
    [SerializeField] private bool pauseVillainChase = true;

    private readonly List<Behaviour> _pausedChase = new List<Behaviour>();
    private Canvas _canvas;
    private GameObject _panel;
    private Text _titleText;
    private Text[] _paragraphTexts;
    private GameObject _buttonContainer;
    private Button _startButton;
    private Text _buttonText;

    private void Start()
    {
        BuildUI();

        // Unlock cursor for the story introduction so the user can see and click the Start button
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        IsActive = true;

        if (pauseVillainChase)
        {
            PauseChase();
        }

        StartCoroutine(StorySequence());
    }

    private void Update()
    {
        // Keep cursor unlocked and visible while story introduction is active
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("StartStoryCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGO.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        // 1. Outer Glow Border
        GameObject borderGO = new GameObject("StoryCardBorder");
        borderGO.transform.SetParent(canvasGO.transform, false);

        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(0.2f, 0.4f, 0.95f, 0.85f); // Glowing Neon Cyan/Purple border

        RectTransform borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0.5f, 0.5f);
        borderRT.anchorMax = new Vector2(0.5f, 0.5f);
        borderRT.pivot = new Vector2(0.5f, 0.5f);
        borderRT.sizeDelta = new Vector2(1100f, 620f);
        borderRT.anchoredPosition = new Vector2(0f, -20f);

        // 2. Main Story Card Panel (nested inside the border for a perfect 3px outline)
        _panel = new GameObject("StoryCard");
        _panel.transform.SetParent(borderGO.transform, false);

        Image panelBg = _panel.AddComponent<Image>();
        panelBg.color = new Color(0.03f, 0.02f, 0.09f, 0.96f); // Deep navy/dark space background

        RectTransform panelRT = _panel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.offsetMin = new Vector2(3f, 3f);
        panelRT.offsetMax = new Vector2(-3f, -3f);

        // 3. Dynamic Vertical Layout
        VerticalLayoutGroup layout = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(60, 60, 45, 45);
        layout.spacing = 25f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 4. Header Title
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(_panel.transform, false);

        _titleText = titleGO.AddComponent<Text>();
        if (font != null) _titleText.font = font;
        _titleText.fontSize = 38;
        _titleText.fontStyle = FontStyle.Bold;
        _titleText.alignment = TextAnchor.MiddleCenter;
        _titleText.color = new Color(1f, 0.8f, 0.2f, 1f); // Vibrant gold color
        _titleText.text = "✦ THE STORY SO FAR ✦";

        Outline titleOl = titleGO.AddComponent<Outline>();
        titleOl.effectColor = new Color(0f, 0f, 0f, 0.95f);
        titleOl.effectDistance = new Vector2(2f, -2f);

        Shadow titleSh = titleGO.AddComponent<Shadow>();
        titleSh.effectColor = new Color(0.6f, 0.1f, 0.8f, 0.65f); // Glowing purple shadow
        titleSh.effectDistance = new Vector2(3f, -3f);

        ContentSizeFitter titleFitter = titleGO.AddComponent<ContentSizeFitter>();
        titleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        titleFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        _titleText.gameObject.SetActive(false);

        // 5. Paragraphs
        _paragraphTexts = new Text[paragraphs.Length];
        for (int i = 0; i < paragraphs.Length; i++)
        {
            GameObject textGO = new GameObject($"Paragraph_{i + 1}");
            textGO.transform.SetParent(_panel.transform, false);

            Text t = textGO.AddComponent<Text>();
            if (font != null) t.font = font;
            t.fontSize = 24;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.UpperLeft;
            t.color = new Color(0.85f, 0.94f, 1.0f, 1f); // Soft blue-white
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = paragraphs[i];

            Outline ol = textGO.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.95f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);

            Shadow sh = textGO.AddComponent<Shadow>();
            sh.effectColor = new Color(0.3f, 0.1f, 0.6f, 0.65f);
            sh.effectDistance = new Vector2(2f, -2f);

            ContentSizeFitter fitter = textGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            t.gameObject.SetActive(false);
            _paragraphTexts[i] = t;
        }

        // 6. Centered Button Container
        _buttonContainer = new GameObject("ButtonContainer");
        _buttonContainer.transform.SetParent(_panel.transform, false);

        LayoutElement containerLe = _buttonContainer.AddComponent<LayoutElement>();
        containerLe.preferredHeight = 85f;
        containerLe.flexibleHeight = 0f;

        _startButton = BuildStartButton(_buttonContainer.transform, font);
        _buttonContainer.SetActive(false);
    }

    private IEnumerator StorySequence()
    {
        // Title Fade
        if (_titleText != null)
        {
            yield return StartCoroutine(FadeInText(_titleText, 1.0f));
            yield return new WaitForSeconds(0.5f);
        }

        // Paragraph Fades
        int count = Mathf.Min(paragraphs.Length, _paragraphTexts.Length);
        for (int i = 0; i < count; i++)
        {
            if (_paragraphTexts[i] != null)
            {
                yield return StartCoroutine(FadeInText(_paragraphTexts[i], 1.5f));
                float waitTime = secondsPerParagraph - 1.5f;
                yield return new WaitForSeconds(waitTime > 0.5f ? waitTime : 0.5f);
            }
        }

        // Start Button Fade
        if (_buttonContainer != null && _startButton != null && _buttonText != null)
        {
            _buttonContainer.SetActive(true);
            yield return StartCoroutine(FadeInButton(_startButton, _buttonText, 1.0f));
        }
    }

    private Button BuildStartButton(Transform parent, Font font)
    {
        GameObject buttonGO = new GameObject("StartGameButton");
        buttonGO.transform.SetParent(parent, false);

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.08f, 0.3f, 0.95f);

        Outline btnOutline = buttonGO.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0f, 0.8f, 1f, 0.8f);
        btnOutline.effectDistance = new Vector2(2f, -2f);

        Button btn = buttonGO.AddComponent<Button>();
        btn.onClick.AddListener(OnStartGame);

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.12f, 0.08f, 0.3f, 0.95f);
        cb.highlightedColor = new Color(0.25f, 0.15f, 0.6f, 1f);
        cb.pressedColor = new Color(0.08f, 0.04f, 0.2f, 1f);
        cb.selectedColor = new Color(0.25f, 0.15f, 0.6f, 1f);
        cb.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300f, 65f);
        rt.anchoredPosition = Vector2.zero;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        _buttonText = textGO.AddComponent<Text>();
        if (font != null) _buttonText.font = font;
        _buttonText.fontSize = 26;
        _buttonText.fontStyle = FontStyle.Bold;
        _buttonText.alignment = TextAnchor.MiddleCenter;
        _buttonText.color = new Color(0.4f, 1f, 0.7f, 1f);
        _buttonText.text = "START ESCAPE";

        Outline ol = textGO.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.95f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return btn;
    }

    private IEnumerator FadeInText(Text text, float duration)
    {
        text.gameObject.SetActive(true);
        Color startColor = text.color;
        startColor.a = 0f;
        text.color = startColor;

        Outline ol = text.GetComponent<Outline>();
        Shadow sh = text.GetComponent<Shadow>();
        Color startOl = ol != null ? ol.effectColor : Color.clear;
        Color startSh = sh != null ? sh.effectColor : Color.clear;
        if (ol != null) { startOl.a = 0f; ol.effectColor = startOl; }
        if (sh != null) { startSh.a = 0f; sh.effectColor = startSh; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = Mathf.Clamp01(elapsed / duration);

            Color c = text.color;
            c.a = pct;
            text.color = c;

            if (ol != null)
            {
                Color oc = ol.effectColor;
                oc.a = pct * 0.95f;
                ol.effectColor = oc;
            }

            if (sh != null)
            {
                Color sc = sh.effectColor;
                sc.a = pct * 0.65f;
                sh.effectColor = sc;
            }

            yield return null;
        }

        Color finalC = text.color;
        finalC.a = 1f;
        text.color = finalC;
        if (ol != null) { Color oc = ol.effectColor; oc.a = 0.95f; ol.effectColor = oc; }
        if (sh != null) { Color sc = sh.effectColor; sc.a = 0.65f; sh.effectColor = sc; }
    }

    private IEnumerator FadeInButton(Button button, Text btnText, float duration)
    {
        button.gameObject.SetActive(true);
        Image img = button.GetComponent<Image>();
        Outline ol = button.GetComponent<Outline>();

        Color startImg = img.color;
        startImg.a = 0f;
        img.color = startImg;

        Color startText = btnText.color;
        startText.a = 0f;
        btnText.color = startText;

        Color startOl = ol != null ? ol.effectColor : Color.clear;
        if (ol != null) { startOl.a = 0f; ol.effectColor = startOl; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = Mathf.Clamp01(elapsed / duration);

            Color ic = img.color;
            ic.a = pct * 0.95f;
            img.color = ic;

            Color tc = btnText.color;
            tc.a = pct;
            btnText.color = tc;

            if (ol != null)
            {
                Color oc = ol.effectColor;
                oc.a = pct * 0.8f;
                ol.effectColor = oc;
            }

            yield return null;
        }

        Color finalImg = img.color;
        finalImg.a = 0.95f;
        img.color = finalImg;

        Color finalTxt = btnText.color;
        finalTxt.a = 1f;
        btnText.color = finalTxt;

        if (ol != null) { Color oc = ol.effectColor; oc.a = 0.8f; ol.effectColor = oc; }
    }

    private void OnStartGame()
    {
        IsActive = false;

        if (pauseVillainChase)
        {
            ResumeChase();
        }

        // Re-lock the cursor for gameplay so mouse look works
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_canvas != null)
        {
            Destroy(_canvas.gameObject);
        }

        Destroy(gameObject);
    }

    private void PauseChase()
    {
        PauseBehaviourType<AStarVillainChase>();
        PauseBehaviourType<UCSVillainChase>();
        PauseBehaviourType<VillainAI>();
    }

    private void PauseBehaviourType<T>() where T : Behaviour
    {
        T[] list = FindObjectsOfType<T>(true);
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == null || !list[i].enabled) continue;
            list[i].enabled = false;
            _pausedChase.Add(list[i]);
        }
    }

    private void ResumeChase()
    {
        for (int i = 0; i < _pausedChase.Count; i++)
        {
            if (_pausedChase[i] != null)
                _pausedChase[i].enabled = true;
        }
        _pausedChase.Clear();
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing != null)
        {
            StandaloneInputModule oldModule = existing.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                DestroyImmediate(oldModule);
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                {
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }
}
