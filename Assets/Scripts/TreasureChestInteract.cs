using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TreasureChestInteract : MonoBehaviour
{
    [Header("Lid")]
    [SerializeField] private Transform lidTransform;
    [SerializeField] private float openAngle    = -110f;
    [SerializeField] private float openDuration = 0.5f;
    [Tooltip("Local-space offset from the lid's centre to its hinge edge, e.g. (0, 0, -0.5)")]
    [SerializeField] private Vector3 hingeOffset = new Vector3(0f, 0f, -0.5f);

    [Header("Interaction")]
    [SerializeField] private string playerTag       = "Player";
    [SerializeField] private float  detectionRadius = 5f;

    [Header("Prompt")]
    [SerializeField] private GameObject promptUI;

    private bool      _isOpen;
    private bool      _playerNear;
    private float     _currentAngle;
    private Quaternion _closedRot;
    private Vector3    _closedPos;
    private Coroutine  _lidRoutine;

    private void Awake()
    {
        if (lidTransform == null)
            lidTransform = transform;

        _closedRot = lidTransform.localRotation;
        _closedPos = lidTransform.localPosition;

        if (promptUI != null) promptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.GetComponentInParent<Collider>()?.CompareTag(playerTag) == true)
            _playerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.GetComponentInParent<Collider>()?.CompareTag(playerTag) == true)
            _playerNear = false;
    }

    private void Update()
    {
        if (_isOpen) return;

        if (promptUI != null) promptUI.SetActive(_playerNear);

        if (_playerNear && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Open();
    }

    private void Open()
    {
        _isOpen = true;
        if (promptUI != null) promptUI.SetActive(false);
        if (_lidRoutine != null) StopCoroutine(_lidRoutine);
        _lidRoutine = StartCoroutine(LidRoutine());
    }

    private IEnumerator LidRoutine()
    {
        float fromAngle = _currentAngle;
        float elapsed   = 0f;
        float duration  = Mathf.Max(0.01f, openDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _currentAngle = Mathf.Lerp(fromAngle, openAngle,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            ApplyAngle();
            yield return null;
        }

        _currentAngle = openAngle;
        ApplyAngle();
    }

    private void ApplyAngle()
    {
        if (lidTransform == null) return;
        lidTransform.localRotation = _closedRot;
        lidTransform.localPosition = _closedPos;
        if (Mathf.Abs(_currentAngle) < 0.001f) return;
        Vector3 hingeWorld = lidTransform.TransformPoint(hingeOffset);
        lidTransform.RotateAround(hingeWorld, lidTransform.right, _currentAngle);
    }


    private void OnDrawGizmosSelected()
    {
        if (lidTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(lidTransform.TransformPoint(hingeOffset), 0.05f);
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
