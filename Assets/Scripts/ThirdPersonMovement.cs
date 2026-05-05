using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person player controller.
/// Moves relative to the camera direction and rotates the player to face movement.
///
/// Attach to: RUMI (player root, same object as CharacterController)
/// Requires:  CharacterController on the same GameObject
///            Animator on this GameObject or a child
///            Main Camera in the scene (auto-resolved via Camera.main)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed           = 4f;
    [SerializeField] private float rotationSmoothTime  = 0.1f;   // seconds to turn to face direction
    [SerializeField] private float gravity             = -19.62f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string   speedParam       = "Speed";
    [SerializeField] private float    animatorDampTime = 0.1f;

    // ── private state ─────────────────────────────────────────────────────────
    private CharacterController _controller;
    private Transform           _cameraTransform;
    private float               _verticalVelocity;
    private float               _rotationVelocity;  // used by SmoothDampAngle

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        ApplyGravity();

        Vector2 input   = ReadMoveInput();
        bool    moving  = input.sqrMagnitude > 0.01f;

        if (moving)
            MoveAndRotate(input);

        UpdateAnimator(moving);
    }

    // ── gravity ───────────────────────────────────────────────────────────────
    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;
        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    // ── movement & rotation ───────────────────────────────────────────────────
    private void MoveAndRotate(Vector2 input)
    {
        // Project camera axes onto the horizontal plane so slope doesn't skew movement.
        Vector3 camForward = _cameraTransform != null
            ? Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized
            : Vector3.forward;

        Vector3 camRight = _cameraTransform != null
            ? Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized
            : Vector3.right;

        Vector3 moveDir = (camForward * input.y + camRight * input.x).normalized;

        // Smoothly rotate player to face the movement direction.
        float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y, targetAngle,
            ref _rotationVelocity, rotationSmoothTime);

        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

        _controller.Move(moveDir * moveSpeed * Time.deltaTime);
    }

    // ── animator ──────────────────────────────────────────────────────────────
    private void UpdateAnimator(bool moving)
    {
        if (animator == null) return;

        float targetSpeed = moving ? moveSpeed : 0f;
        animator.SetFloat(speedParam, targetSpeed, animatorDampTime, Time.deltaTime);
    }

    // ── input (new Input System + legacy fallback) ────────────────────────────
    private static Vector2 ReadMoveInput()
    {
        if (Keyboard.current != null)
        {
            float x = 0f, y = 0f;

            if (Keyboard.current.aKey.isPressed    || Keyboard.current.leftArrowKey.isPressed)  x -= 1f;
            if (Keyboard.current.dKey.isPressed    || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed    || Keyboard.current.downArrowKey.isPressed)  y -= 1f;
            if (Keyboard.current.wKey.isPressed    || Keyboard.current.upArrowKey.isPressed)    y += 1f;

            return new Vector2(x, y).normalized;
        }

        return new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")).normalized;
    }
}
