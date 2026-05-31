using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WinSequenceManager : MonoBehaviour
{
    [Header("Win Message Timing")]
    [SerializeField] private float _fadeInDuration  = 0.6f;
    [SerializeField] private float _holdDuration    = 3.5f;
    [SerializeField] private float _fadeOutDuration = 0.6f;

    [Header("Zoey Dance")]
    [SerializeField] private GameObject                _zoeyPrefab;
    [SerializeField] private RuntimeAnimatorController _danceController;
    [SerializeField] private float                     _zoeySpawnOffset   = 4.5f;
    [SerializeField] private float                     _minDanceDistance  = 3.0f;

    [Header("Golden Song")]
    [SerializeField] private AudioClip _goldenClip;
    [SerializeField] [Range(0f,1f)] private float _goldenVolume = 1f;

    private AudioSource _music;

    // ── internal UI ──────────────────────────────────────────────────────────
    private Canvas    _canvas;
    private Image     _overlay;
    private Text      _headlineText;
    private Text      _subtitleText;
    private CanvasGroup _panelGroup;

    private void Start()
    {
        // Auto-load assets if not assigned in Inspector
#if UNITY_EDITOR
        if (_zoeyPrefab == null)
            _zoeyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NPC/ZoeyNPC.prefab");
        if (_danceController == null)
            _danceController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/NPC/ZoeyDanceController.controller");
#endif

        TreasureChestInteract chest = FindObjectOfType<TreasureChestInteract>();
        if (chest != null)
            chest.OnMicPickedUp += OnMicPickedUp;
        else
            Debug.LogWarning("[WinSequence] No TreasureChestInteract found in scene.", this);

        Debug.Log("[WinSequence] Ready. Prefab=" + (_zoeyPrefab != null ? _zoeyPrefab.name : "NULL")
                  + " Controller=" + (_danceController != null ? _danceController.name : "NULL"));

        _music = gameObject.AddComponent<AudioSource>();
        _music.loop         = false;
        _music.playOnAwake  = false;
        _music.spatialBlend = 0f; // 2D

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
        SilenceVillain();
        StartCoroutine(WinSequenceCoroutine());
    }

    private void SilenceVillain()
    {
        FindAnyObjectByType<AStarVillainChase>()?.MuteAudio();
        FindAnyObjectByType<UCSVillainChase>()?.MuteAudio();
    }

    private IEnumerator WinSequenceCoroutine()
    {
        // ── Step 2: show the win message ─────────────────────────────────────
        yield return StartCoroutine(ShowWinMessage());

        // ── Step 3: spawn Zoey dancing ───────────────────────────────────────
        SpawnZoeyDancing();

        // ── Step 4: RUMI starts singing ──────────────────────────────────────
        TriggerRumiSinging();

        // ── Step 5: play Golden ──────────────────────────────────────────────
        PlayGoldenSong();
    }

    // ── Zoey Dance Spawn ─────────────────────────────────────────────────────

    private void SpawnZoeyDancing()
    {
        if (_zoeyPrefab == null)
        {
            Debug.LogWarning("[WinSequence] Zoey prefab is null — assign ZoeyNPC.prefab in Inspector.", this);
            return;
        }
        Debug.Log("[WinSequence] Spawning Zoey for celebration dance.");

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        // Spawn to RUMI's right side so they stand next to each other
        Vector3 right2D = new Vector3(player.transform.right.x, 0f, player.transform.right.z).normalized;
        Vector3 spawnPos = player.transform.position + right2D * _zoeySpawnOffset;
        spawnPos.y = player.transform.position.y;

        // Ground-snap: find actual terrain Y at the spawn XZ
        if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f))
            spawnPos.y = hit.point.y;

        // Face same direction as RUMI (side-by-side dance)
        Quaternion spawnRot = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);

        GameObject zoey = Instantiate(_zoeyPrefab, spawnPos, spawnRot);

        // Swap to the dance controller before the Animator evaluates its first frame
        Animator anim = zoey.GetComponentInChildren<Animator>();
        if (anim != null && _danceController != null)
            anim.runtimeAnimatorController = _danceController;

        // ZoeyFightSequence.Start() already handles the Humanoid foot-snap —
        // do NOT run a second snap here or Zoey sinks 54 m underground.
        Debug.Log("[WinSequence] Zoey instantiated — foot-snap handled by ZoeyFightSequence.");

        // Keep Zoey from walking into RUMI while she dances
        StartCoroutine(MaintainDanceDistance(zoey.transform, player.transform));
    }

    // ── RUMI Singing ─────────────────────────────────────────────────────────

    private void TriggerRumiSinging()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Animator anim = player.GetComponentInChildren<Animator>();
        if (anim == null) { Debug.LogWarning("[WinSequence] RUMI Animator not found.", this); return; }

        // Disable movement input so RUMI stays still while singing
        ThirdPersonMovement mov = player.GetComponent<ThirdPersonMovement>();
        if (mov != null) mov.enabled = false;

        anim.CrossFade("Singing", 0.3f);
        Debug.Log("[WinSequence] RUMI singing animation triggered.");
    }

    // ── Golden Song ──────────────────────────────────────────────────────────

    private void PlayGoldenSong()
    {
        if (_goldenClip == null)
        {
            Debug.LogWarning("[WinSequence] Golden audio clip not assigned — drag it into the Inspector on WinSequenceManager.", this);
            return;
        }
        _music.clip   = _goldenClip;
        _music.volume = _goldenVolume;
        _music.Play();
        Debug.Log("[WinSequence] Golden is playing.");
    }

    // Runs every frame — pushes Zoey's root away on XZ only if she gets too close to RUMI.
    // Y is never touched so the underground root (from the Humanoid foot-snap) stays correct.
    private IEnumerator MaintainDanceDistance(Transform zoeyRoot, Transform playerRoot)
    {
        while (zoeyRoot != null && playerRoot != null)
        {
            Vector3 toZoey = zoeyRoot.position - playerRoot.position;
            toZoey.y = 0f;
            float dist = toZoey.magnitude;

            if (dist < _minDanceDistance && dist > 0.001f)
            {
                Vector3 p      = zoeyRoot.position;
                Vector3 offset = toZoey.normalized * _minDanceDistance;
                p.x = playerRoot.position.x + offset.x;
                p.z = playerRoot.position.z + offset.z;
                // p.y intentionally unchanged
                zoeyRoot.position = p;
            }

            yield return null;
        }
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
