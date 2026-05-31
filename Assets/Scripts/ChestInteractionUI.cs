using UnityEngine;
using UnityEngine.UI;

public class ChestInteractionUI : MonoBehaviour
{
    [SerializeField] private TreasureChestInteract _chest;

    private void Start()
    {
        if (_chest == null) _chest = FindObjectOfType<TreasureChestInteract>();
        if (_chest == null)
        {
            Debug.LogWarning("[ChestInteractionUI] No TreasureChestInteract found in scene.", this);
            return;
        }

        GameObject openPrompt   = BuildPrompt("✦  Press  [ E ]  to Open Chest  ✦");
        GameObject pickupPrompt = BuildPrompt("✦  Press  [ E ]  to Pick Up Mic  ✦");

        openPrompt.SetActive(false);
        pickupPrompt.SetActive(false);

        _chest.SetPromptUIObjects(openPrompt, pickupPrompt);
    }

    private static GameObject BuildPrompt(string message)
    {
        // Canvas root — TreasureChestInteract calls SetActive() on this object
        GameObject canvasGO = new GameObject("ChestPromptCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        // Panel — centered, lower third of screen (y = -200 from center)
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.02f, 0.14f, 0.88f);

        RectTransform rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(720f, 120f);
        rt.anchoredPosition = new Vector2(0f, -200f);

        // Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(panelGO.transform, false);

        Text t    = textGO.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) t.font = font;   // only override if found; never assign null
        t.fontSize  = 52;
        t.fontStyle = FontStyle.Bold;
        t.color     = new Color(0.38f, 0.93f, 1.0f, 1.0f);
        t.alignment = TextAnchor.MiddleCenter;
        t.text      = message;

        Outline ol = textGO.AddComponent<Outline>();
        ol.effectColor    = new Color(0f, 0f, 0f, 1f);
        ol.effectDistance = new Vector2(2.5f, -2.5f);

        Shadow sh = textGO.AddComponent<Shadow>();
        sh.effectColor    = new Color(0.72f, 0.08f, 1.0f, 0.85f);
        sh.effectDistance = new Vector2(3f, -3f);

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12f, 6f);
        textRT.offsetMax = new Vector2(-12f, -6f);

        return canvasGO;
    }
}
