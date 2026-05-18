using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to an empty child of RUMI at eye/shoulder height.
/// Cinemachine Virtual Camera's Follow and LookAt both point to this object.
/// Mouse input rotates this object; Cinemachine reads the rotation to orbit around it.
/// </summary>
public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float minPitch      = -30f;
    [SerializeField] private float maxPitch      =  60f;

    private float _pitch;
    private float _yaw;

    private void Start()
    {
        _yaw   = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void LateUpdate()
    {
        HandleCursor();
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = ReadMouseDelta();
        _yaw   += delta.x * rotationSpeed * Time.deltaTime;
        _pitch -= delta.y * rotationSpeed * Time.deltaTime;
        _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private static Vector2 ReadMouseDelta()
    {
        if (Mouse.current != null)
            return Mouse.current.delta.ReadValue() * 0.05f;
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
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
