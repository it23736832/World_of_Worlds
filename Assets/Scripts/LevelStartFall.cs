using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LevelStartFall : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float minFallTime = 0.3f;

    private static readonly int IsFalling = Animator.StringToHash("IsFalling");
    private static readonly int Land      = Animator.StringToHash("Land");

    private bool  _landed;
    private float _startTime;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        _startTime = Time.time;
        if (animator != null)
            animator.SetBool(IsFalling, true);
        else
            Debug.LogWarning("[LevelStartFall] No Animator found.");
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (_landed || animator == null) return;
        if (Time.time - _startTime < minFallTime) return;

        if (hit.moveDirection.y < -0.3f && hit.normal.y > 0.5f)
        {
            _landed = true;
            animator.SetBool(IsFalling, false);
            animator.SetTrigger(Land);
            Destroy(this);
        }
    }
}
