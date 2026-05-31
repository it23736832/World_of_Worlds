using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ZoeyHelpUI : MonoBehaviour
{
    [Header("Zoey Prefab")]
    [SerializeField] private GameObject _zoeyPrefab;

    [Header("UI")]
    [SerializeField] private GameObject _promptPanel;
    [SerializeField] private Text _promptText;

    [Header("Settings")]
    [SerializeField] private float _villainAlertRadius = 12f;
    [SerializeField] private float _zoeySpawnDistance  = 2.5f;
    [SerializeField] private string _promptMessage     = "[ H ]  Call Zoey to Fight!";

    private Transform _player;
    private Transform _villainTransform;
    private bool _zoeyUsed;

    private void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;

        AStarVillainChase astar = FindObjectOfType<AStarVillainChase>();
        UCSVillainChase   ucs   = FindObjectOfType<UCSVillainChase>();
        if (astar != null)    _villainTransform = astar.transform;
        else if (ucs != null) _villainTransform = ucs.transform;

        // Auto-find panel and text by name so no manual Inspector wiring is needed
        if (_promptPanel == null)
        {
            Transform t = transform.Find("ZoeyPromptPanel");
            if (t != null) _promptPanel = t.gameObject;
        }
        if (_promptText == null && _promptPanel != null)
            _promptText = _promptPanel.GetComponentInChildren<Text>(true);

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (_zoeyUsed || _player == null)
        {
            SetPromptVisible(false);
            return;
        }

        bool villainClose = _villainTransform != null
            && Vector3.Distance(_player.position, _villainTransform.position) <= _villainAlertRadius;

        SetPromptVisible(villainClose);

        bool helpPressed = Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
        if (villainClose && helpPressed)
            SpawnZoeyAndFight();
    }

    private void SpawnZoeyAndFight()
    {
        _zoeyUsed = true;
        SetPromptVisible(false);

        ZoeyFightSequence fight = null;

        if (_zoeyPrefab != null)
        {
            // Spawn Zoey 70% of the way between RUMI and the villain, facing the villain
            Vector3 spawnPos;
            Quaternion spawnRot;

            if (_villainTransform != null)
            {
                Vector3 toVillain = _villainTransform.position - _player.position;
                spawnPos = _player.position + toVillain * 0.7f;
                Vector3 faceDir = (_villainTransform.position - spawnPos);
                faceDir.y = 0f;
                spawnRot = faceDir.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(faceDir.normalized)
                    : Quaternion.Euler(0, _player.eulerAngles.y, 0);
            }
            else
            {
                spawnPos = _player.position + _player.forward * _zoeySpawnDistance;
                spawnRot = Quaternion.Euler(0, _player.eulerAngles.y, 0);
            }

            GameObject instance = Instantiate(_zoeyPrefab, spawnPos, spawnRot);
            FixGlbRotation(instance.transform);
            fight = instance.GetComponent<ZoeyFightSequence>();
        }
        else
        {
            // Fallback: find Zoey already placed in the scene
            fight = FindObjectOfType<ZoeyFightSequence>(true);
            if (fight != null)
                fight.gameObject.SetActive(true);
        }

        if (fight != null)
            fight.TriggerFight();
        else
            Debug.LogWarning("[ZoeyHelpUI] No Zoey found. Assign _zoeyPrefab or place ZoeyNPC in the scene.", this);
    }

    // GLB files from GLTF exporters embed a coordinate-system correction on the first child.
    // Find whichever direct child has a large X or Z tilt and zero it out.
    private static void FixGlbRotation(Transform root)
    {
        foreach (Transform child in root)
        {
            Vector3 e = child.localEulerAngles;
            // Normalise to -180..180
            float x = e.x > 180f ? e.x - 360f : e.x;
            float z = e.z > 180f ? e.z - 360f : e.z;
            if (Mathf.Abs(x) > 45f || Mathf.Abs(z) > 45f)
            {
                child.localEulerAngles = new Vector3(0f, e.y, 0f);
                return;
            }
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (_promptPanel != null) _promptPanel.SetActive(visible);
        if (_promptText  != null) _promptText.text = visible ? _promptMessage : string.Empty;
    }
}
