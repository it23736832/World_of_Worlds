using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed          = 4f;
    [SerializeField] private float sprintSpeed        = 7f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [SerializeField] private float gravity            = -19.62f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string   speedParam       = "Speed";
    [SerializeField] private string   jumpParam        = "IsJumping";
    [SerializeField] private float    animatorDampTime = 0.1f;

    [Header("Camera")]
    [Tooltip("Assign the CameraFollowTarget child object. Falls back to Camera.main if left empty.")]
    [SerializeField] private Transform cameraFollowTarget;

    private CharacterController _controller;
    private Transform           _cameraTransform;
    private float               _verticalVelocity;
    private float               _rotationVelocity;
    private bool                _isJumping;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _cameraTransform = cameraFollowTarget != null
            ? cameraFollowTarget
            : Camera.main != null ? Camera.main.transform : null;
    }

    private void Update()
    {
        ApplyGravity();

        Vector2 input    = ReadMoveInput();
        bool    moving   = input.sqrMagnitude > 0.01f;
        bool    sprinting = moving && IsSprinting();

        if (moving)
            MoveAndRotate(input, sprinting);

        UpdateAnimator(moving, sprinting);
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded)
        {
            _isJumping = false;
            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;

            if (ReadJumpInput())
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _isJumping = true;
            }
        }

        _verticalVelocity += gravity * Time.deltaTime;
        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    private void MoveAndRotate(Vector2 input, bool sprinting)
    {
        Vector3 camForward = _cameraTransform != null
            ? Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized
            : Vector3.forward;

        Vector3 camRight = _cameraTransform != null
            ? Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized
            : Vector3.right;

        Vector3 moveDir = (camForward * input.y + camRight * input.x).normalized;

        float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y, targetAngle,
            ref _rotationVelocity, rotationSmoothTime);

        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

        float speed = sprinting ? sprintSpeed : walkSpeed;
        _controller.Move(moveDir * speed * Time.deltaTime);
    }

    private void UpdateAnimator(bool moving, bool sprinting)
    {
        if (animator == null) return;

        // 0 = idle, 0.5 = walk, 1 = sprint
        float targetSpeed = moving ? (sprinting ? 1f : 0.5f) : 0f;
        animator.SetFloat(speedParam, targetSpeed, animatorDampTime, Time.deltaTime);
        animator.SetBool(jumpParam, _isJumping);
    }

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
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
    }

    private static bool IsSprinting()
    {
        if (Keyboard.current != null)
            return Keyboard.current.leftShiftKey.isPressed;
        return Input.GetKey(KeyCode.LeftShift);
    }

    private static bool ReadJumpInput()
    {
        if (Keyboard.current != null)
            return Keyboard.current.spaceKey.wasPressedThisFrame;
        return Input.GetKeyDown(KeyCode.Space);
    }
}
