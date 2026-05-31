using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
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
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] private float gravity = -19.62f;

    [Header("Path Updates")]
    [SerializeField] private float repathRate = 0.5f;
    [SerializeField] private float targetMoveRepathDistance = 1.5f;
    [SerializeField] private float minRepathInterval = 0.15f;

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

    [Header("Debug")]
    [SerializeField] private bool logPathProblems = true;
    [SerializeField] private bool drawPath = true;

    private NavMeshAgent _navAgent;
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

    private Vector3 _lastStuckCheckPos;
    private float _stuckTimer;
    private const float StuckCheckInterval = 1.5f;
    private const float StuckMoveThreshold = 0.25f;

    private float _lastRepathTime = -999f;
    private float _lastWarnTime = -999f;
    private const float WarnThrottle = 5f;
    private bool _engagedInFight;

    public List<Vector3> CurrentPath    => _path;
    public int           PathNodesRemaining => _path != null ? Mathf.Max(0, _path.Count - _pathIndex) : 0;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _navAgent = GetComponent<NavMeshAgent>();
        _navAgent.updatePosition = false; // NavMeshAgent steers; CharacterController handles collisions like Rumi.
        _navAgent.updateRotation = false;
        _navAgent.stoppingDistance = 0f;

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
        _lastStuckCheckPos = transform.position;
        SyncAgentToController();

        Debug.Log($"[UCSVillainChase] Started on '{gameObject.name}'. Target: {(target != null ? target.name : "NULL")}. Pathfinder: {(pathfinder != null ? "OK" : "NULL — assign in Inspector!")}. Animator: {(animator != null ? "OK" : "NULL")}.");

        Repath();
    }

    private void Update()
    {
        _attackTimer     -= Time.deltaTime;
        _roarTimer       -= Time.deltaTime;
        _closeSoundTimer -= Time.deltaTime;

        if (_engagedInFight) { _navAgent.isStopped = true; return; }

        if (target == null || pathfinder == null)
        {
            _navAgent.isStopped = true;
            SetAnimatorSpeed(0f);
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, target.position);
        PlayProximitySounds(distToTarget);

        if (distToTarget <= stoppingDistance)
        {
            _navAgent.isStopped = true;
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
        if (Time.time - _lastRepathTime < minRepathInterval) return;
        _lastRepathTime = Time.time;
        _repathTimer = repathRate;
        if (target == null || pathfinder == null) return;

        Vector3 startPos  = GetNearestNavMeshPosition(transform.position);
        Vector3 targetPos = GetNearestNavMeshPosition(target.position);
        List<Vector3> newPath = pathfinder.FindPath(startPos, targetPos);

        if (newPath != null && newPath.Count > 0)
        {
            newPath.Add(targetPos);
            _path = newPath;
            _pathIndex = 0;
        }
        else if (logPathProblems && Time.time - _lastWarnTime >= WarnThrottle)
        {
            _lastWarnTime = Time.time;
            string reason = !string.IsNullOrWhiteSpace(pathfinder.LastFailureReason)
                ? pathfinder.LastFailureReason : "Unknown. Check NavMeshGraph and NavMesh bake.";
            Debug.LogWarning($"[UCSVillainChase] Repath failed, keeping last path. {reason}", this);
        }

        _lastTargetPosition = target.position;
    }

    // UCS gives us the waypoints; NavMeshAgent steers, while CharacterController enforces world collision.
    private void FollowPath(float distToTarget)
    {
        if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count)
        {
            _navAgent.isStopped = true;
            SetAnimatorSpeed(0f);
            if (target != null) FacePosition(target.position);
            return;
        }

        Vector3 waypoint  = _path[_pathIndex];
        Vector3 toWaypoint = waypoint - transform.position;
        float flatDist = new Vector2(toWaypoint.x, toWaypoint.z).magnitude;

        if (flatDist <= waypointTolerance)
        {
            _pathIndex++;
            return;
        }

        float speed = distToTarget >= runDistance ? runSpeed : walkSpeed;
        _navAgent.speed = speed;
        _navAgent.isStopped = false;
        _navAgent.SetDestination(waypoint);

        MoveWithCollision(waypoint, speed);

        FacePosition(distToTarget < runDistance ? target.position : waypoint);
        SetAnimatorSpeed(Mathf.InverseLerp(0f, runSpeed, speed));
    }

    private void MoveWithCollision(Vector3 waypoint, float speed)
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 desiredVelocity = _navAgent.desiredVelocity;
        desiredVelocity.y = 0f;

        if (desiredVelocity.sqrMagnitude < 0.01f)
        {
            Vector3 fallback = waypoint - transform.position;
            fallback.y = 0f;
            desiredVelocity = fallback.sqrMagnitude > 0.01f ? fallback.normalized * speed : Vector3.zero;
        }

        CollisionFlags flags = _controller.Move((desiredVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
        if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
            _verticalVelocity = 0f;

        SyncAgentToController();
    }

    private Vector3 GetNearestNavMeshPosition(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas)
            && Mathf.Abs(hit.position.y - position.y) <= 1f)
            return hit.position;
        return position;
    }

    private void SyncAgentToController()
    {
        if (_navAgent == null || !_navAgent.enabled)
            return;

        if (_navAgent.isOnNavMesh)
        {
            _navAgent.nextPosition = transform.position;
            return;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            _navAgent.Warp(hit.position);
    }

    // Triggered when Jinu walks into the barricade's trigger collider.
    // Don't stop him — clear the stale path so Update() immediately requests a new one around the barrier.
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<SealBarricade>() == null) return;

        _path.Clear();
        _pathIndex = 0;
        _repathTimer = 0f;          // skip the cooldown, repath on the very next Update
        _lastRepathTime = -999f;    // bypass minRepathInterval guard too

        if (animator != null && !string.IsNullOrWhiteSpace(hitBarricadeTrigger) && HasAnimatorParam(hitBarricadeTrigger))
            animator.SetTrigger(hitBarricadeTrigger);
    }

    private void CheckStuck()
    {
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer < StuckCheckInterval) return;
        _stuckTimer = 0f;

        if (Vector3.Distance(transform.position, _lastStuckCheckPos) < StuckMoveThreshold
            && _path.Count > 0 && _pathIndex < _path.Count)
        {
            Repath();
            if (logPathProblems) Debug.Log("[UCSVillainChase] Stuck — forcing repath.", this);
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
        if (dist <= roarDistance && dist > closeSoundDistance && _roarTimer <= 0f && roarSound != null)
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

    public void SetIdle()    { _path.Clear(); _pathIndex = 0; _navAgent.isStopped = true; SetAnimatorSpeed(0f); }
    public void ResumeChase(){ _path.Clear(); _pathIndex = 0; }
    public void ForceRepath() => Repath();

    public void EnterFight()
    {
        _engagedInFight = true;
        _path.Clear();
        _pathIndex = 0;
        _navAgent.isStopped = true;
        SetAnimatorSpeed(0f);
    }

    public void ExitFight()
    {
        _engagedInFight = false;
        ResumeChase();
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
