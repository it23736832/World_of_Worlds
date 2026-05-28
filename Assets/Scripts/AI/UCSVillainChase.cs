using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class UCSVillainChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private UCSPathfinder pathfinder;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.5f;
    [SerializeField] private float runDistance = 8f;
    [SerializeField] private float waypointTolerance = 0.6f;
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float gravity = -19.62f;

    [Header("Path Updates")]
    [SerializeField] private float repathRate = 0.5f;
    [SerializeField] private float targetMoveRepathDistance = 0.3f;

    [Header("Animator")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string swipingTrigger = "Swiping";
    [SerializeField] private string runToStopTrigger = "RunToStop";
    [SerializeField] private float animatorDampTime = 0.1f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip roarSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private float roarDistance = 20f;
    [SerializeField] private float roarCooldown = 5f;
    [SerializeField] private float closeSoundDistance = 5f;
    [SerializeField] private float closeSoundCooldown = 3f;

    [Header("Barricade Hit")]
    [SerializeField] private string hitBarricadeTrigger = "HitBarricade";
    [SerializeField] private float hitRecoveryDuration = 3.5f;

    [Header("Debug")]
    [SerializeField] private bool logPathProblems = true;
    [SerializeField] private bool drawPath = true;

    private CharacterController _controller;
    private AudioSource _audioSource;
    private List<Vector3> _path = new List<Vector3>();
    private int _pathIndex;
    private float _verticalVelocity;
    private float _repathTimer;
    private float _attackTimer;
    private float _roarTimer;
    private float _closeSoundTimer;
    private Vector3 _lastTargetPosition;

    // Stuck detection
    private Vector3 _lastStuckCheckPos;
    private float _stuckTimer;
    private const float StuckCheckInterval = 1.5f;
    private const float StuckMoveThreshold = 0.25f;

    private float _lastWarnTime = -999f;
    private const float WarnThrottle = 5f;

    // Barricade hit
    private bool _isHit;
    private float _hitTimer;
    private float _hitCooldown; // prevents re-triggering while still touching the collider
    private float _hitGroundY;  // Y position when hit — keeps model from sinking during animation

    public List<Vector3> CurrentPath => _path;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        if (pathfinder == null) pathfinder = FindObjectOfType<UCSPathfinder>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject t = GameObject.FindWithTag(targetTag);
            if (t != null) target = t.transform;
        }

        _lastTargetPosition = target != null ? target.position : transform.position;

        Debug.Log($"[UCSVillainChase] Started on '{gameObject.name}'. Target: {(target != null ? target.name : "NULL")}. Pathfinder: {(pathfinder != null ? "OK" : "NULL — assign AI System in Inspector!")}. Animator: {(animator != null ? "OK" : "NULL")}.");

        Repath();
    }

    private void Update()
    {
        ApplyGravity();
        _attackTimer -= Time.deltaTime;
        _roarTimer -= Time.deltaTime;
        _closeSoundTimer -= Time.deltaTime;
        _hitCooldown -= Time.deltaTime;

        if (_isHit)
        {
            _hitTimer -= Time.deltaTime;
            if (_hitTimer <= 0f) _isHit = false;
            SetAnimatorSpeed(0f);
            return;
        }

        if (target == null || pathfinder == null)
        {
            SetAnimatorSpeed(0f);
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, target.position);
        PlayProximitySounds(distToTarget);

        if (distToTarget <= stoppingDistance)
        {
            FacePosition(target.position);
            SetAnimatorSpeed(0f);
            TryAttack();
            return;
        }

        _repathTimer -= Time.deltaTime;
        bool targetMoved = Vector3.Distance(target.position, _lastTargetPosition) >= targetMoveRepathDistance;
        if (_repathTimer <= 0f || targetMoved || _path.Count == 0 || _pathIndex >= _path.Count)
            Repath();

        CheckStuck();
        FollowPath(distToTarget);
    }

    private void Repath()
    {
        _repathTimer = repathRate;
        if (target == null || pathfinder == null) return;

        List<Vector3> newPath = pathfinder.FindPath(transform.position, target.position);

        if (newPath != null && newPath.Count > 0)
        {
            // Append exact target position so Jinu walks all the way to RUMI
            newPath.Add(target.position);
            _path = newPath;
            _pathIndex = 0;
        }
        else if (logPathProblems && Time.time - _lastWarnTime >= WarnThrottle)
        {
            _lastWarnTime = Time.time;
            string reason = !string.IsNullOrWhiteSpace(pathfinder.LastFailureReason)
                ? pathfinder.LastFailureReason
                : "Unknown. Check NavMeshGraph and NavMesh bake.";
            Debug.LogWarning($"[UCSVillainChase] Repath failed, keeping last path. {reason}", this);
        }

        _lastTargetPosition = target.position;
    }

    private void FollowPath(float distToTarget)
    {
        if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count)
        {
            SetAnimatorSpeed(0f);
            return;
        }

        Vector3 waypoint  = _path[_pathIndex];
        Vector3 toWaypoint = waypoint - transform.position;
        Vector3 flatDir   = new Vector3(toWaypoint.x, 0f, toWaypoint.z);

        if (flatDir.magnitude <= waypointTolerance)
        {
            _pathIndex++;
            return;
        }

        float speed = distToTarget >= runDistance ? runSpeed : walkSpeed;
        _controller.Move(flatDir.normalized * speed * Time.deltaTime);
        FacePosition(waypoint);
        SetAnimatorSpeed(Mathf.InverseLerp(0f, runSpeed, speed));
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;
        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    private void LateUpdate()
    {
        // Only prevent sinking below the floor — do not lock Y upward so gravity still works
        if (_isHit && transform.position.y < _hitGroundY)
        {
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, _hitGroundY, p.z);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (_isHit || _hitCooldown > 0f) return;
        if (hit.gameObject.GetComponent<SealBarricade>() == null) return;

        _isHit = true;
        _hitTimer = hitRecoveryDuration;
        _hitCooldown = hitRecoveryDuration + 1f;
        _hitGroundY = transform.position.y;
        _path.Clear();
        _pathIndex = 0;

        if (animator != null && !string.IsNullOrWhiteSpace(hitBarricadeTrigger) && HasAnimatorParam(hitBarricadeTrigger))
            animator.SetTrigger(hitBarricadeTrigger);
    }

    private void CheckStuck()
    {
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer < StuckCheckInterval) return;
        _stuckTimer = 0f;

        float moved = Vector3.Distance(transform.position, _lastStuckCheckPos);
        if (moved < StuckMoveThreshold && _path.Count > 0 && _pathIndex < _path.Count)
        {
            Repath();
            if (logPathProblems)
                Debug.Log("[UCSVillainChase] Stuck — forcing repath.", this);
        }
        _lastStuckCheckPos = transform.position;
    }

    private void FacePosition(Vector3 position)
    {
        Vector3 dir = position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), turnSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (_attackTimer > 0f || animator == null) return;
        string[] attacks = { attackTrigger, swipingTrigger };
        string pick = attacks[Random.Range(0, attacks.Length)];
        if (!string.IsNullOrWhiteSpace(pick)) animator.SetTrigger(pick);
        _attackTimer = Mathf.Max(0.1f, attackCooldown);
    }

    private void PlayProximitySounds(float dist)
    {
        if (dist <= closeSoundDistance && _closeSoundTimer <= 0f && closeSound != null)
        {
            _audioSource.PlayOneShot(closeSound);
            _closeSoundTimer = closeSoundCooldown;
        }
        if (dist > roarDistance && _roarTimer <= 0f && roarSound != null)
        {
            _audioSource.PlayOneShot(roarSound);
            _roarTimer = roarCooldown;
        }
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator == null || string.IsNullOrWhiteSpace(speedParam)) return;
        animator.SetFloat(speedParam, speed, animatorDampTime, Time.deltaTime);
    }

    public void SetIdle()
    {
        _path.Clear();
        _pathIndex = 0;
        SetAnimatorSpeed(0f);
    }

    public void ResumeChase()
    {
        _path.Clear();
        _pathIndex = 0;
    }

    public void OnPlayerSwordSwing()
    {
        if (animator == null) return;
        StartCoroutine(SwordReaction());
    }

    private IEnumerator SwordReaction()
    {
        if (!string.IsNullOrWhiteSpace(runToStopTrigger) && HasAnimatorParam(runToStopTrigger))
            animator.SetTrigger(runToStopTrigger);
        yield return new WaitForSeconds(1f);
        if (!string.IsNullOrWhiteSpace(swipingTrigger) && HasAnimatorParam(swipingTrigger))
            animator.SetTrigger(swipingTrigger);
    }

    private bool HasAnimatorParam(string paramName)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    // Called by SealBarricade when edges are severed or restored
    public void ForceRepath() => Repath();

    private void OnDrawGizmos()
    {
        if (!drawPath || _path == null || _path.Count < 1) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < _path.Count - 1; i++)
        {
            Gizmos.DrawLine(_path[i] + Vector3.up * 0.3f, _path[i + 1] + Vector3.up * 0.3f);
            Gizmos.DrawSphere(_path[i] + Vector3.up * 0.3f, 0.2f);
        }
        Gizmos.DrawSphere(_path[_path.Count - 1] + Vector3.up * 0.3f, 0.2f);
    }
}
