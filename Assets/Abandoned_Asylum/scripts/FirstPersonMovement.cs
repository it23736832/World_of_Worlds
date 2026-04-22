using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 5.5f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform movementTransform;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.1f;
    [SerializeField] private float gravity = -19.62f;

    [Header("Controller Shape")]
    [SerializeField] private bool applyRecommendedControllerShape = true;
    [SerializeField] private float controllerHeight = 1.8f;
    [SerializeField] private float controllerRadius = 0.32f;
    [SerializeField] private float controllerCenterY = 0.9f;
    [SerializeField] private float controllerStepOffset = 0.35f;
    [SerializeField] private float controllerSkinWidth = 0.04f;

    private float verticalVelocity;

    private void Awake()
    {
        ResolveCharacterController();

        if (characterController == null)
        {
            Debug.LogError("FirstPersonMovement needs a CharacterController on the player object.");
            enabled = false;
            return;
        }

        if (movementTransform == null)
        {
            movementTransform = characterController.transform;
        }

        if (applyRecommendedControllerShape)
        {
            ApplyControllerShape();
        }
    }

    private void OnValidate()
    {
        ResolveCharacterController();

        if (characterController == null)
        {
            return;
        }

        if (movementTransform == null)
        {
            movementTransform = characterController.transform;
        }

        if (!applyRecommendedControllerShape)
        {
            return;
        }

        ApplyControllerShape();
    }

    private void Update()
    {
        Vector2 moveInput = ReadMoveInput();
        bool sprintPressed = IsSprintPressed();
        bool jumpPressed = IsJumpPressed();

        MoveCharacter(moveInput, sprintPressed, jumpPressed);
    }

    private void MoveCharacter(Vector2 moveInput, bool sprintPressed, bool jumpPressed)
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (characterController.isGrounded && jumpPressed)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        float speed = sprintPressed ? sprintSpeed : walkSpeed;
        Transform moveBasis = movementTransform != null ? movementTransform : characterController.transform;
        Vector3 right = Vector3.ProjectOnPlane(moveBasis.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(moveBasis.forward, Vector3.up).normalized;
        Vector3 move = right * moveInput.x + forward * moveInput.y;

        characterController.Move(move * speed * Time.deltaTime);

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private static Vector2 ReadMoveInput()
    {
        if (Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;

            if (Keyboard.current.aKey.isPressed)
            {
                x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                x += 1f;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                y += 1f;
            }

            return new Vector2(x, y).normalized;
        }

        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
    }

    private static bool IsSprintPressed()
    {
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        }

        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private static bool IsJumpPressed()
    {
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        return Input.GetKeyDown(KeyCode.Space);
    }

    private void ResolveCharacterController()
    {
        bool scriptIsOnCamera = TryGetComponent(out Camera _);
        CharacterController parentController = null;

        if (transform.parent != null)
        {
            parentController = transform.parent.GetComponentInParent<CharacterController>();
        }

        // If this script is on the camera, prefer the player's parent controller.
        if (scriptIsOnCamera && parentController != null)
        {
            characterController = parentController;
            return;
        }

        if (characterController != null)
        {
            return;
        }

        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController = parentController != null ? parentController : GetComponentInParent<CharacterController>();
        }
    }

    private void ApplyControllerShape()
    {
        controllerHeight = Mathf.Max(1f, controllerHeight);
        controllerRadius = Mathf.Clamp(controllerRadius, 0.2f, 0.6f);
        controllerCenterY = Mathf.Clamp(controllerCenterY, 0.6f, controllerHeight * 0.6f);
        controllerStepOffset = Mathf.Clamp(controllerStepOffset, 0.05f, controllerHeight * 0.5f);
        controllerSkinWidth = Mathf.Clamp(controllerSkinWidth, 0.01f, 0.1f);

        characterController.height = controllerHeight;
        characterController.radius = controllerRadius;
        characterController.center = new Vector3(0f, controllerCenterY, 0f);
        characterController.stepOffset = controllerStepOffset;
        characterController.skinWidth = controllerSkinWidth;
    }
}