using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LevelStartFall : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float groundCheckPadding = 0.15f;
    [SerializeField] private bool debugAnimatorState = false;

    private CharacterController _controller;
    private bool _landed;
    private int _landingHash;
    private int _fallingIdleHash;
    private int _locomotionHash;
    private int _jumpHash;
    private int _pickupHash;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _landingHash = Animator.StringToHash("Landing");
        _fallingIdleHash = Animator.StringToHash("FallingIdle");
        _locomotionHash = Animator.StringToHash("Locomotion");
        _jumpHash = Animator.StringToHash("Jump");
        _pickupHash = Animator.StringToHash("pickup");
    }

    private void Start()
    {
        if (animator != null)
            animator.SetBool("IsFalling", true);
        else
            Debug.LogWarning("LevelStartFall: Animator not found on this object or its children.");

        Debug.Log($"LevelStartFall: animator='{(animator != null ? animator.name : "<null>")}', controller='{(animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "<null>")}'");
    }

    private void Update()
    {
        if (_landed || animator == null || _controller == null)
            return;

        if (IsGroundedByRaycast())
            FinishLanding();
    }

    // Fires directly from CharacterController physics — reliable at any scale
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (_landed || animator == null) return;

        // Only trigger on downward hits (i.e. landing on a floor)
        if (hit.moveDirection.y < -0.3f && hit.normal.y > 0.5f)
        {
            FinishLanding();
        }
    }

    private bool IsGroundedByRaycast()
    {
        Bounds bounds = _controller.bounds;
        Vector3 origin = bounds.center + Vector3.up * groundCheckPadding;
        float castDistance = bounds.extents.y + groundCheckPadding * 2f;

        return Physics.Raycast(origin, Vector3.down, castDistance);
    }

    private void FinishLanding()
    {
        _landed = true;
        animator.SetBool("IsFalling", false);
        Debug.Log("LevelStartFall: landed, IsFalling=false");
        if (debugAnimatorState)
            StartCoroutine(LogAnimatorStateThenDestroy());
        else
            Destroy(this);
    }

    private IEnumerator LogAnimatorStateThenDestroy()
    {
        float endTime = Time.time + 1f;
        while (Time.time < endTime)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            string stateName = ResolveStateName(info.shortNameHash);
            Debug.Log($"LevelStartFall: state='{stateName}' hash={info.shortNameHash} normalizedTime={info.normalizedTime:0.00}");
            yield return null;
        }

        Destroy(this);
    }

    private string ResolveStateName(int hash)
    {
        if (hash == _landingHash) return "Landing";
        if (hash == _fallingIdleHash) return "FallingIdle";
        if (hash == _locomotionHash) return "Locomotion";
        if (hash == _jumpHash) return "Jump";
        if (hash == _pickupHash) return "pickup";
        return "<unknown>";
    }
}
