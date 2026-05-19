using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField] private Transform _character;

    [Header("Auto Follow")]
    [SerializeField] private float _yawSmoothSpeed  = 6f;   // how fast camera catches up to character facing
    [SerializeField] private float _pitch           = 10f;  // fixed downward tilt (positive = looks down slightly)

    [Header("Mouse Look (optional override)")]
    [SerializeField] private bool  _allowMouseLook      = true;
    [SerializeField] private float _mouseSensitivity    = 120f;
    [SerializeField] private float _minPitch            = -20f;
    [SerializeField] private float _maxPitch            =  40f;

    private float _currentYaw;
    private float _currentPitch;

    private void Start()
    {
        if (_character == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) _character = p.transform;
        }

        _currentYaw   = _character != null ? _character.eulerAngles.y : transform.eulerAngles.y;
        _currentPitch = _pitch;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void LateUpdate()
    {
        HandleCursor();
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (_character == null) return;

        // Smoothly follow character's yaw
        float targetYaw = _character.eulerAngles.y;
        _currentYaw = Mathf.LerpAngle(_currentYaw, targetYaw, _yawSmoothSpeed * Time.deltaTime);

        // Optional mouse pitch override
        if (_allowMouseLook)
        {
            float mouseY = Mouse.current != null
                ? Mouse.current.delta.ReadValue().y * 0.05f
                : Input.GetAxis("Mouse Y");
            _currentPitch -= mouseY * _mouseSensitivity * Time.deltaTime;
            _currentPitch  = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);
        }
        else
        {
            _currentPitch = _pitch;
        }

        transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
    }

    private static void HandleCursor()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        bool shouldLock  = Cursor.lockState != CursorLockMode.Locked;
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !shouldLock;
    }
}
