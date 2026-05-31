using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TreasureChestInteract : MonoBehaviour
{
    [Header("Lid Parts (both open together)")]
    [SerializeField] private Transform lidTransform;
    [SerializeField] private float     openAngle      = -110f;
    [SerializeField] private Transform upperCaseTransform;
    [SerializeField] private float     upperCaseAngle = -110f;
    [SerializeField] private float     openDuration   = 0.5f;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";

    [Header("Prompt")]
    [SerializeField] private GameObject openPromptUI;
    [SerializeField] private GameObject pickupPromptUI;

    [Header("Mic Pickup")]
    [SerializeField] private GameObject micInChest;
    [SerializeField] private string     pickupAnimTrigger = "PickupItem";
    [SerializeField] private Vector3    micHandPosition   = Vector3.zero;
    [SerializeField] private Vector3    micHandRotation   = Vector3.zero;

    // Serialized so they survive recompiles — set via "Capture Closed State" context menu
    [SerializeField, HideInInspector] private Quaternion _closedRot      = Quaternion.identity;
    [SerializeField, HideInInspector] private Quaternion _closedUpperRot = Quaternion.identity;
    [SerializeField, HideInInspector] private bool       _closedCaptured;

    // Saved mic world state so "5. Return Mic To Chest" can restore it
    [SerializeField, HideInInspector] private Vector3    _micWorldPos;
    [SerializeField, HideInInspector] private Quaternion _micWorldRot;
    [SerializeField, HideInInspector] private Vector3    _micWorldScale;
    [SerializeField, HideInInspector] private bool       _micStateSaved;

    // Fired once when RUMI successfully picks up the mic
    public event Action OnMicPickedUp;

    private bool      _isOpen;
    private bool      _micPickedUp;
    private bool      _playerNear;
    private float     _currentAngle;
    private float     _currentUpperAngle;
    private Coroutine _lidRoutine;

    private static readonly string[] _handBoneNames =
        { "hand_r", "RightHand", "Hand_R", "Hand.R", "mixamorig:RightHand" };

    private void Awake()
    {
        if (!_closedCaptured)
        {
            if (lidTransform      != null) _closedRot      = lidTransform.localRotation;
            if (upperCaseTransform != null) _closedUpperRot = upperCaseTransform.localRotation;
        }

        if (openPromptUI   != null) openPromptUI.SetActive(false);
        if (pickupPromptUI != null) pickupPromptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) _playerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) _playerNear = false;
    }

    private void Update()
    {
        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (!_isOpen)
        {
            if (openPromptUI != null) openPromptUI.SetActive(_playerNear);
            if (_playerNear && ePressed) Open();
        }
        else if (!_micPickedUp)
        {
            if (pickupPromptUI != null) pickupPromptUI.SetActive(_playerNear);
            if (_playerNear && ePressed) StartCoroutine(PickupMic());
        }
        else
        {
            if (pickupPromptUI != null) pickupPromptUI.SetActive(false);
        }
    }

    private void Open()
    {
        _isOpen = true;
        if (openPromptUI != null) openPromptUI.SetActive(false);
        if (_lidRoutine != null) StopCoroutine(_lidRoutine);
        _lidRoutine = StartCoroutine(LidRoutine());
    }

    private IEnumerator LidRoutine()
    {
        float fromAngle      = _currentAngle;
        float fromUpperAngle = _currentUpperAngle;
        float elapsed        = 0f;
        float duration       = Mathf.Max(0.01f, openDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _currentAngle      = Mathf.Lerp(fromAngle,      openAngle,      t);
            _currentUpperAngle = Mathf.Lerp(fromUpperAngle, upperCaseAngle, t);
            ApplyAngles();
            yield return null;
        }

        _currentAngle      = openAngle;
        _currentUpperAngle = upperCaseAngle;
        ApplyAngles();
    }

    private IEnumerator PickupMic()
    {
        _micPickedUp = true;
        if (pickupPromptUI != null) pickupPromptUI.SetActive(false);

        GameObject player = GameObject.FindWithTag(playerTag);
        if (player == null) { Debug.LogWarning("[TreasureChest] Player not found!", this); yield break; }

        Animator anim = player.GetComponentInChildren<Animator>();

        if (anim != null && !string.IsNullOrWhiteSpace(pickupAnimTrigger))
        {
            bool triggerExists = false;
            foreach (var p in anim.parameters)
                if (p.name == pickupAnimTrigger) { triggerExists = true; break; }

            if (triggerExists)
                anim.SetTrigger(pickupAnimTrigger);
            else
                Debug.LogWarning($"[TreasureChest] Trigger '{pickupAnimTrigger}' not found in Rumi's Animator.", this);
        }

        yield return new WaitForSeconds(0.4f);

        SwordHolder sword = player.GetComponentInChildren<SwordHolder>();
        sword?.HideSword();

        if (micInChest == null) { Debug.LogWarning("[TreasureChest] Mic In Chest not assigned!", this); yield break; }

        Transform rightHand = anim != null ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (rightHand == null)
            rightHand = FindBoneByName(player.transform, _handBoneNames);
        if (rightHand == null) { Debug.LogWarning("[TreasureChest] Right hand bone not found!", this); yield break; }

        AttachMicToHand(rightHand);
        Debug.Log($"[TreasureChest] Mic attached to '{rightHand.name}'.", this);

        OnMicPickedUp?.Invoke();
    }

    // Preserves the mic's current world size when reparenting to the hand bone,
    // so RUMI's 10× root scale doesn't shrink or bloat the mic.
    private void AttachMicToHand(Transform hand)
    {
        float micWorldSize  = micInChest.transform.lossyScale.x;
        float handWorldSize = Mathf.Abs(hand.lossyScale.x) > 1e-6f ? hand.lossyScale.x : 1f;

        micInChest.transform.SetParent(hand, false);
        micInChest.transform.localPosition = micHandPosition;
        micInChest.transform.localRotation = Quaternion.Euler(micHandRotation);
        micInChest.transform.localScale    = Vector3.one * (micWorldSize / handWorldSize);
    }

    private void ApplyAngles()
    {
        if (lidTransform != null)
            lidTransform.localRotation = _closedRot * Quaternion.AngleAxis(_currentAngle, Vector3.right);
        if (upperCaseTransform != null)
            upperCaseTransform.localRotation = _closedUpperRot * Quaternion.AngleAxis(_currentUpperAngle, Vector3.right);
    }

    // ── Step 1: with chest visually CLOSED, right-click component → Capture Closed State
    [ContextMenu("1. Capture Closed State")]
    private void CaptureClosedState()
    {
        if (lidTransform      != null) _closedRot      = lidTransform.localRotation;
        if (upperCaseTransform != null) _closedUpperRot = upperCaseTransform.localRotation;
        _closedCaptured = true;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("[TreasureChest] Closed state captured.", this);
    }

    [ContextMenu("2. Preview Open")]
    private void PreviewOpen()
    {
        if (!_closedCaptured) { Debug.LogWarning("[TreasureChest] Run '1. Capture Closed State' first!", this); return; }
        if (lidTransform      != null) lidTransform.localRotation       = _closedRot      * Quaternion.AngleAxis(openAngle,      Vector3.right);
        if (upperCaseTransform != null) upperCaseTransform.localRotation = _closedUpperRot * Quaternion.AngleAxis(upperCaseAngle, Vector3.right);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    [ContextMenu("3. Preview Closed")]
    private void PreviewClosed()
    {
        if (!_closedCaptured) { Debug.LogWarning("[TreasureChest] Run '1. Capture Closed State' first!", this); return; }
        if (lidTransform      != null) lidTransform.localRotation       = _closedRot;
        if (upperCaseTransform != null) upperCaseTransform.localRotation = _closedUpperRot;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    // ── Edit-mode preview: attaches mic to Rumi's right hand using the current
    //    Mic Hand Position/Rotation values so you can see and adjust them live.
    //    Run "5. Return Mic To Chest" to put it back.
    [ContextMenu("4. Attach Mic To Hand (Edit Mode Preview)")]
    private void AttachMicToHandPreview()
    {
        if (micInChest == null) { Debug.LogWarning("[TreasureChest] Mic In Chest not assigned!", this); return; }

        GameObject player = GameObject.FindWithTag(playerTag);
        if (player == null) { Debug.LogWarning("[TreasureChest] No Player tagged object found in scene.", this); return; }

        Animator anim = player.GetComponentInChildren<Animator>();
        Transform rightHand = anim != null ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (rightHand == null)
            rightHand = FindBoneByName(player.transform, _handBoneNames);
        if (rightHand == null) { Debug.LogWarning("[TreasureChest] Right hand bone not found on Player.", this); return; }

        // Save world state so "5. Return Mic To Chest" can restore it
        _micWorldPos   = micInChest.transform.position;
        _micWorldRot   = micInChest.transform.rotation;
        _micWorldScale = micInChest.transform.localScale;
        _micStateSaved = true;

        AttachMicToHand(rightHand);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(micInChest);
#endif
        Debug.Log($"[TreasureChest] Mic attached to '{rightHand.name}'. " +
                  "Tweak Mic Hand Position / Rotation in the Inspector, then run '5. Return Mic To Chest'.", this);
    }

    [ContextMenu("5. Return Mic To Chest")]
    private void ReturnMicToChest()
    {
        if (micInChest == null) return;
        if (!_micStateSaved) { Debug.LogWarning("[TreasureChest] Run '4. Attach Mic To Hand' first.", this); return; }

        micInChest.transform.SetParent(null, true);
        micInChest.transform.position   = _micWorldPos;
        micInChest.transform.rotation   = _micWorldRot;
        micInChest.transform.localScale = _micWorldScale;
        _micStateSaved = false;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(micInChest);
#endif
        Debug.Log("[TreasureChest] Mic returned to chest position.", this);
    }

    [ContextMenu("0. Reset Mic Into Cabin (use if mic is lost)")]
    private void ResetMicToCabin()
    {
        if (micInChest == null) { Debug.LogWarning("[TreasureChest] Mic In Chest not assigned!", this); return; }

        Transform cabin = transform.parent;
        micInChest.transform.SetParent(cabin, false);
        micInChest.transform.localPosition = new Vector3(-0.249f, 0.784f, -1.005f);
        micInChest.transform.localRotation = Quaternion.Euler(-88.68f, 195.82f, -105.74f);
        micInChest.transform.localScale    = Vector3.one * 0.015f;
        _micStateSaved = false;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(micInChest);
#endif
        Debug.Log("[TreasureChest] Mic reset into Cabin.", this);
    }

    private static Transform FindBoneByName(Transform root, string[] names)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            foreach (string n in names)
                if (string.Equals(t.name, n, System.StringComparison.OrdinalIgnoreCase))
                    return t;
        return null;
    }

    public void SetPromptUIObjects(GameObject openPrompt, GameObject pickupPrompt)
    {
        openPromptUI   = openPrompt;
        pickupPromptUI = pickupPrompt;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
