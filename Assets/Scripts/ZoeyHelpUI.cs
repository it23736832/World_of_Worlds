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
    [SerializeField] private float _zoeySpawnDistance  = 3.0f;
    [SerializeField] private string _promptMessage     = "✦  Press  [ H ]  to Call Zoey!  ✦";

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
            // Spawn Zoey between RUMI and the villain, offset from the villain's side
            // so she is close to the villain rather than crowding RUMI.
            Vector3 spawnPos;
            Quaternion spawnRot;

            if (_villainTransform != null)
            {
                // Flat direction pointing from the villain toward RUMI
                Vector3 toPlayer = _player.position - _villainTransform.position;
                toPlayer.y = 0f;
                Vector3 dir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized
                                                             : Vector3.forward;

                // Place Zoey in front of the villain (between them), facing the villain
                spawnPos = _villainTransform.position + dir * _zoeySpawnDistance;
                spawnRot = Quaternion.LookRotation(-dir); // -dir = toward villain
            }
            else
            {
                // Fallback when no villain is found: spawn in front of RUMI
                Vector3 forward2D = new Vector3(_player.forward.x, 0f, _player.forward.z).normalized;
                spawnPos = _player.position + forward2D * _zoeySpawnDistance;
                spawnRot = Quaternion.Euler(0, _player.eulerAngles.y, 0);
            }

            spawnPos.y = _player.position.y;

            // Raycast to find the actual ground level at the spawn point.
            if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit groundHit, 50f))
                spawnPos.y = groundHit.point.y;

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
