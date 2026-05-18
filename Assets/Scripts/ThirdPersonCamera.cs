using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person camera that freely orbits a pivot point above the player.
/// Mouse X/Y orbit the camera only — player rotation is owned by ThirdPersonMovement.
///
/// Hierarchy:
///   RUMI  (player)
///   └─ camera_pivot  (empty child at ~eye height, e.g. local y = 1.6)
///   Camera  (root-level, attach this script here)
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;  // position-only reference (child of player)

    [Header("Distance")]
    [SerializeField] private float cameraDistance = 4f;
    [SerializeField] private float heightOffset   = 0f;

    [Header("Sensitivity")]
    [SerializeField] private float horizontalSensitivity = 0.1f;
    [SerializeField] private float verticalSensitivity   = 0.1f;

    [Header("Vertical Clamp")]
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle =  60f;

    [Header("Smoothing")]
    [SerializeField] private float followSmoothTime = 0.08f;

    [Header("Wall Collision")]
    [SerializeField] private float collisionRadius = 0.2f;
    [Tooltip("Assign every layer EXCEPT the Player layer. Excluding the player prevents the camera from being pulled inside RUMI's own mesh.")]
    [SerializeField] private LayerMask collisionMask = ~0;

    private float _yaw;
    private float _pitch;
    private Vector3 _followVelocity;

    // Expose camera yaw so ThirdPersonMovement can read the camera's facing direction.
    public float Yaw => _yaw;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _yaw   = transform.eulerAngles.y;
        _pitch = 10f;
    }

    private void LateUpdate()
    {
        HandleCursorToggle();

        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (cameraPivot == null) return;

        ReadMouseInput();
        OrbitAndFollow();
    }

    private void ReadMouseInput()
    {
        Vector2 delta = Mouse.current != null
            ? Mouse.current.delta.ReadValue()
            : Vector2.zero;

        _yaw   += delta.x * horizontalSensitivity;
        _pitch -= delta.y * verticalSensitivity;
        _pitch  = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);
    }

    private void OrbitAndFollow()
    {
        Quaternion orbitRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivotPos         = cameraPivot.position;
        Vector3 desiredOffset    = orbitRotation * new Vector3(0f, heightOffset, -cameraDistance);
        Vector3 desiredPosition  = pivotPos + desiredOffset;

        // Pull camera in front of any wall between pivot and desired position.
        Vector3 direction      = desiredPosition - pivotPos;
        float   desiredDist    = direction.magnitude;

        Vector3 targetPosition = desiredPosition;
        if (Physics.SphereCast(pivotPos, collisionRadius, direction.normalized,
                               out RaycastHit hit, desiredDist, collisionMask))
        {
            // Place camera just in front of the hit surface.
            targetPosition = pivotPos + direction.normalized * (hit.distance - collisionRadius);
        }

        // Skip smooth-damp when pulled in by collision so there's no lag into a wall.
        if (targetPosition != desiredPosition)
            transform.position = targetPosition;
        else
            transform.position = Vector3.SmoothDamp(
                transform.position, targetPosition, ref _followVelocity, followSmoothTime);

        transform.LookAt(pivotPos + Vector3.up * heightOffset);
    }

    private void HandleCursorToggle()
    {
        bool escPressed = Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame;

        if (!escPressed) return;

        bool shouldLock  = Cursor.lockState != CursorLockMode.Locked;
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !shouldLock;
    }
}
