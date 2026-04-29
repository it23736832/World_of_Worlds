using UnityEngine;
using UnityEngine.InputSystem;

public class cameraControll : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private float horizontalSensitivity = 0.02f;
    [SerializeField] private float verticalSensitivity = 0.02f;
    [SerializeField] private float maxMouseDeltaPerFrame = 35f;
    [SerializeField] private float maxTurnDegreesPerFrame = 2.5f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private bool useThirdPersonOffset = true;

    [Header("Third Person")]
    [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0f, 1.8f, -3.5f);

    private float xRotation;

    private void Start()
    {
        if (transform.parent != null)
        {
            playerBody = transform.parent;
        }

        if (useThirdPersonOffset)
        {
            transform.localPosition = cameraLocalOffset;
        }

        if (lockCursorOnStart)
        {
            LockCursor(true);
        }
    }

    private void Update()
    {
        HandleCursorToggle();

        Vector2 lookDelta = ReadLookInput();
        ApplyLook(lookDelta);
    }

    private void LateUpdate()
    {
        if (playerBody == null || transform.parent != playerBody)
        {
            return;
        }

        if (useThirdPersonOffset)
        {
            transform.localPosition = cameraLocalOffset;
        }
        else
        {
            Vector3 localPos = transform.localPosition;
            localPos.x = 0f;
            localPos.z = 0f;
            transform.localPosition = localPos;
        }
    }

    private Vector2 ReadLookInput()
    {
        Vector2 lookDelta;

        if (Mouse.current != null)
        {
            lookDelta = Mouse.current.delta.ReadValue();
        }
        else
        {
            lookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        return Vector2.ClampMagnitude(lookDelta, maxMouseDeltaPerFrame);
    }

    private void ApplyLook(Vector2 lookDelta)
    {
        if (Cursor.lockState != CursorLockMode.Locked || playerBody == null)
        {
            return;
        }

        float mouseX = lookDelta.x * horizontalSensitivity;
        float mouseY = lookDelta.y * verticalSensitivity;

        mouseX = Mathf.Clamp(mouseX, -maxTurnDegreesPerFrame, maxTurnDegreesPerFrame);
        mouseY = Mathf.Clamp(mouseY, -maxTurnDegreesPerFrame, maxTurnDegreesPerFrame);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void HandleCursorToggle()
    {
        bool escapePressed = false;

        if (Keyboard.current != null)
        {
            escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            escapePressed = true;
        }

        if (!escapePressed)
        {
            return;
        }

        bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
        LockCursor(shouldLock);
    }

    private static void LockCursor(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }
}
