using UnityEngine;
using System.Collections;

public class SimpleDoorOpener : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 2.5f;
    [SerializeField] private float openAngle       = 90f;
    [SerializeField] private float duration        = 0.45f;

    [Header("Hinge")]
    [Tooltip("Local-space offset from this object's center to the hinge edge. " +
             "A yellow sphere shows it in the Scene view. " +
             "Typical value: (-0.5, 0, 0) for left edge or (0.5, 0, 0) for right edge.")]
    [SerializeField] private Vector3 hingeOffset = new Vector3(-0.5f, 0f, 0f);

    private Quaternion _closedRot;
    private Vector3    _closedPos;
    private float      _currentAngle;
    private bool       _isOpen;
    private Coroutine  _routine;
    private float      _timer;

    private void Awake()
    {
        _closedRot = transform.localRotation;
        _closedPos = transform.localPosition;

        // Disable any Animator that would override transform every frame.
        Animator anim = GetComponentInParent<Animator>();
        if (anim != null) anim.enabled = false;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < 0.15f) return;
        _timer = 0f;

        bool near = IsNear("Player") || IsNear("Villain");
        if (near && !_isOpen)      { _isOpen = true;  StartRotate(openAngle); }
        else if (!near && _isOpen) { _isOpen = false; StartRotate(0f); }
    }

    private bool IsNear(string tag)
    {
        GameObject[] chars;
        try { chars = GameObject.FindGameObjectsWithTag(tag); }
        catch { return false; }

        foreach (GameObject c in chars)
            if (Vector3.Distance(transform.position, c.transform.position) <= detectionRadius)
                return true;

        return false;
    }

    private void StartRotate(float toAngle)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(RotateRoutine(toAngle));
    }

    private IEnumerator RotateRoutine(float toAngle)
    {
        float fromAngle = _currentAngle;
        float elapsed   = 0f;

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
        _routine = null;
    }

    private void ApplyAngle()
    {
        // Reset to closed pose first so RotateAround accumulates cleanly each frame.
        transform.localRotation = _closedRot;
        transform.localPosition = _closedPos;

        if (Mathf.Abs(_currentAngle) < 0.001f) return;

        // Rotate around the hinge point (world space).
        Vector3 hingeWorld = transform.TransformPoint(hingeOffset);
        transform.RotateAround(hingeWorld, Vector3.up, _currentAngle);
    }

    // Yellow sphere in Scene view shows where the hinge is.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(hingeOffset), 0.05f);
    }
}
