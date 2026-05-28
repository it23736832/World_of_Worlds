using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField] private Transform _character;

    [Header("Auto Follow")]
    [SerializeField] private float _yawSmoothSpeed  = 4f;   // how fast camera catches up to character facing
    [SerializeField] private float _pitch           = 12f;  // fixed downward tilt (positive = looks down slightly)
    [SerializeField] private float _mouseFollowPause = 2f;  // seconds of no mouse input before auto-follow kicks in

    [Header("Mouse Look (optional override)")]
    [SerializeField] private bool  _allowMouseLook      = true;
    [SerializeField] private float _mouseSensitivity    = 220f;
    [SerializeField] private float _minPitch            = -40f;
    [SerializeField] private float _maxPitch            =  70f;

    private float _currentYaw;
    private float _currentPitch;
    private float _lastMouseLookTime;

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

        if (_allowMouseLook)
        {
            Vector2 mouseDelta = Mouse.current != null
                ? Mouse.current.delta.ReadValue()
                : new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            if (mouseDelta.sqrMagnitude > 0.001f)
                _lastMouseLookTime = Time.time;

            _currentYaw += mouseDelta.x * _mouseSensitivity * 0.05f * Time.deltaTime;

            float mouseY = Mouse.current != null
                ? mouseDelta.y * 0.05f
                : mouseDelta.y;
            _currentPitch -= mouseY * _mouseSensitivity * Time.deltaTime;
            _currentPitch  = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);
        }
        else
        {
            _currentPitch = _pitch;
        }

        if (Time.time - _lastMouseLookTime > _mouseFollowPause)
        {
            float targetYaw = _character.eulerAngles.y;
            _currentYaw = Mathf.LerpAngle(_currentYaw, targetYaw, _yawSmoothSpeed * Time.deltaTime);
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
