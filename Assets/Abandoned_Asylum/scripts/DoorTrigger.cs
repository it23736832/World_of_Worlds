using UnityEngine;
using System.Collections;

public class DoorTrigger : MonoBehaviour
{
    [Header("Door Panel")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private float openAngle    = 90f;
    [SerializeField] private float openDuration = 0.45f;

    [Header("Hinge")]
    [Tooltip("Local-space offset from the door panel's center to its hinge edge. " +
             "Yellow sphere shows it in Scene view. e.g. (-0.5,0,0) or (0.5,0,0).")]
    [SerializeField] private Vector3 hingeOffset = new Vector3(-0.5f, 0f, 0f);

    [Header("Interaction")]
    [SerializeField] private string playerTag      = "Player";
    [SerializeField] private string villainTag     = "Villain";
    [SerializeField] private float  detectionRadius = 2.5f;

    private Quaternion _closedRot;
    private Vector3    _closedPos;
    private float      _currentAngle;
    private bool       _isOpen;
    private Coroutine  _doorRoutine;
    private float      _checkTimer;
    private Collider   _doorCollider;
    private const float CheckInterval = 0.15f;

    private void Awake()
    {
        if (doorTransform == null)
            doorTransform = FindDoorPanel(transform);

        if (doorTransform == null)
            doorTransform = transform.parent;

        if (doorTransform != null)
        {
            _closedRot = doorTransform.localRotation;
            _closedPos = doorTransform.localPosition;

            // Disable Animator so it doesn't override our rotation every frame.
            Animator anim = doorTransform.GetComponent<Animator>();
            if (anim == null) anim = doorTransform.GetComponentInParent<Animator>();
            if (anim != null) anim.enabled = false;

            // Cache the door panel collider so we can disable it while open.
            _doorCollider = doorTransform.GetComponent<Collider>();
        }
    }

    private void Update()
    {
        _checkTimer += Time.deltaTime;
        if (_checkTimer < CheckInterval) return;
        _checkTimer = 0f;

        bool anyoneNear = IsCharacterNear(playerTag) || IsCharacterNear(villainTag);

        if (anyoneNear && !_isOpen)
        {
            _isOpen = true;
            if (_doorCollider != null) _doorCollider.enabled = false;
            StartRotate(openAngle);
        }
        else if (!anyoneNear && _isOpen)
        {
            _isOpen = false;
            StartRotate(0f);
        }
    }

    private bool IsCharacterNear(string tag)
    {
        GameObject[] characters;
        try { characters = GameObject.FindGameObjectsWithTag(tag); }
        catch { return false; }

        foreach (GameObject character in characters)
            if (Vector3.Distance(transform.position, character.transform.position) <= detectionRadius)
                return true;

        return false;
    }

    private void StartRotate(float toAngle)
    {
        if (_doorRoutine != null) StopCoroutine(_doorRoutine);
        _doorRoutine = StartCoroutine(RotateRoutine(toAngle));
    }

    private IEnumerator RotateRoutine(float toAngle)
    {
        float fromAngle = _currentAngle;
        float elapsed   = 0f;
        float duration  = Mathf.Max(0.01f, openDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _currentAngle = Mathf.Lerp(fromAngle, toAngle,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            ApplyAngle();
            yield return null;
        }

        _currentAngle = toAngle;
        ApplyAngle();
        // Re-enable the collider only after the door is fully closed again.
        if (Mathf.Approximately(toAngle, 0f) && _doorCollider != null)
            _doorCollider.enabled = true;
        _doorRoutine = null;
    }

    private void ApplyAngle()
    {
        if (doorTransform == null) return;

        // Reset to closed pose so RotateAround starts clean every frame.
        doorTransform.localRotation = _closedRot;
        doorTransform.localPosition = _closedPos;

        if (Mathf.Abs(_currentAngle) < 0.001f) return;

        Vector3 hingeWorld = doorTransform.TransformPoint(hingeOffset);
        doorTransform.RotateAround(hingeWorld, Vector3.up, _currentAngle);
    }

    // Yellow sphere shows hinge position in Scene view.
    private void OnDrawGizmosSelected()
    {
        if (doorTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(doorTransform.TransformPoint(hingeOffset), 0.05f);
    }

    // ── Auto-find door panel (unchanged) ──────────────────────────────────────

    private static Transform FindDoorPanel(Transform triggerZone)
    {
        Transform parent = triggerZone.parent;
        if (parent == null) return null;

        Transform grandParent = parent.parent;
        if (grandParent == null) return parent;

        string parentName = parent.name;
        int lastUnderscore = parentName.LastIndexOf('_');
        string prefix = lastUnderscore > 0 ? parentName.Substring(0, lastUnderscore) : parentName;

        foreach (Transform child in grandParent)
        {
            if (child == parent) continue;
            if (!child.name.StartsWith(prefix)) continue;
            string lower = child.name.ToLowerInvariant();
            if (lower.Contains("right") || lower.Contains("left"))
                return child;
        }

        foreach (Transform child in grandParent)
        {
            if (child == parent) continue;
            if (!child.name.StartsWith(prefix)) continue;
            string lower = child.name.ToLowerInvariant();
            if (!lower.Contains("frame") && !lower.Contains("window") && !lower.Contains("trigger"))
                return child;
        }

        return parent;
    }
}
