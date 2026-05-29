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
    [SerializeField] private float      micHandScale      = 1f;

    // Serialized so they survive recompiles — set via "Capture Closed State" context menu
    [SerializeField, HideInInspector] private Quaternion _closedRot      = Quaternion.identity;
    [SerializeField, HideInInspector] private Quaternion _closedUpperRot = Quaternion.identity;
    [SerializeField, HideInInspector] private bool       _closedCaptured;

    private bool      _isOpen;
    private bool      _micPickedUp;
    private bool      _playerNear;
    private float     _currentAngle;
    private float     _currentUpperAngle;
    private Coroutine _lidRoutine;

    private void Awake()
    {
        if (!_closedCaptured)
        {
            // Fallback: capture on first play if user forgot to run context menu
            if (lidTransform != null) _closedRot = lidTransform.localRotation;
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

        // Play pickup animation if trigger name is set and exists in the controller
        if (anim != null && !string.IsNullOrWhiteSpace(pickupAnimTrigger))
        {
            bool triggerExists = false;
            foreach (var p in anim.parameters)
                if (p.name == pickupAnimTrigger) { triggerExists = true; break; }

            if (triggerExists)
                anim.SetTrigger(pickupAnimTrigger);
            else
                Debug.LogWarning($"[TreasureChest] Trigger '{pickupAnimTrigger}' not found in Rumi's Animator. Add it in the Animator Controller.", this);
        }

        yield return new WaitForSeconds(0.4f);

        // Hide sword
        SwordHolder sword = player.GetComponentInChildren<SwordHolder>();
        sword?.HideSword();

        // Attach mic to right hand
        if (micInChest == null) { Debug.LogWarning("[TreasureChest] Mic In Chest not assigned!", this); yield break; }

        Transform rightHand = anim != null ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (rightHand == null) { Debug.LogWarning("[TreasureChest] Right hand bone not found!", this); yield break; }

        // Capture world scale before re-parenting so the bone's tiny scale doesn't shrink the mic
        Vector3 worldScale = micInChest.transform.lossyScale;

        micInChest.transform.SetParent(rightHand, false);
        micInChest.transform.localPosition = micHandPosition;
        micInChest.transform.localRotation = Quaternion.Euler(micHandRotation);

        // Re-apply world scale relative to the new parent so the mic stays visible
        Vector3 ps = rightHand.lossyScale;
        micInChest.transform.localScale = new Vector3(
            ps.x != 0 ? worldScale.x / ps.x : 1f,
            ps.y != 0 ? worldScale.y / ps.y : 1f,
            ps.z != 0 ? worldScale.z / ps.z : 1f) * micHandScale;

        Debug.Log("[TreasureChest] Mic attached to Rumi's right hand.", this);
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
        if (lidTransform != null)        _closedRot      = lidTransform.localRotation;
        if (upperCaseTransform != null)  _closedUpperRot = upperCaseTransform.localRotation;
        _closedCaptured = true;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("[TreasureChest] Closed state captured.", this);
    }

    // ── Step 2: adjust Open Angle values then click this to preview
    [ContextMenu("2. Preview Open")]
    private void PreviewOpen()
    {
        if (!_closedCaptured) { Debug.LogWarning("[TreasureChest] Run '1. Capture Closed State' first!", this); return; }
        if (lidTransform != null)       lidTransform.localRotation       = _closedRot      * Quaternion.AngleAxis(openAngle,      Vector3.right);
        if (upperCaseTransform != null) upperCaseTransform.localRotation = _closedUpperRot * Quaternion.AngleAxis(upperCaseAngle, Vector3.right);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    [ContextMenu("3. Preview Closed")]
    private void PreviewClosed()
    {
        if (!_closedCaptured) { Debug.LogWarning("[TreasureChest] Run '1. Capture Closed State' first!", this); return; }
        if (lidTransform != null)       lidTransform.localRotation       = _closedRot;
        if (upperCaseTransform != null) upperCaseTransform.localRotation = _closedUpperRot;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
