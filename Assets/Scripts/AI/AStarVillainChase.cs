using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AStarVillainChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private AStarGrid grid;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.5f;
    [SerializeField] private float runDistance = 8f;
    [SerializeField] private float waypointTolerance = 0.4f;
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float gravity = -19.62f;
    [SerializeField] private float groundSnapSpeed = 20f;
    [SerializeField] private float groundOffset = 0.02f;
    [SerializeField] private bool snapToClosestWalkableOnStart;

    [Header("Path Updates")]
    [SerializeField] private float repathRate = 0.3f;
    [SerializeField] private float targetMoveRepathDistance = 0.75f;

    [Header("Animator")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string swipingTrigger = "Swiping";
    [SerializeField] private string runToStopTrigger = "RunToStop";
    [SerializeField] private string runningToTurnTrigger = "RunningToTurn";
    [SerializeField] private string runningJumpTrigger = "RunningJump";
    [SerializeField] private float animatorDampTime = 0.1f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Debug")]
    [SerializeField] private bool drawPath;
    [SerializeField] private bool logPathProblems = true;
    [SerializeField] private bool logAnimatorProblems;
    [SerializeField] private bool logMissingReferences = true;
    [SerializeField] private bool logPathSuccess;

    [Header("Audio")]
    [SerializeField] private AudioClip roarSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private float roarDistance = 20f;
    [SerializeField] private float roarCooldown = 5f;
    [SerializeField] private float roarVolume = 1f;
    [SerializeField] private float closeSoundDistance = 5f;
    [SerializeField] private float closeSoundCooldown = 3f;
    [SerializeField] private float closeSoundVolume = 1f;

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
    private Vector3 _lastFramePosition;
    private float _stuckTimer;
    private int _jumpAttemptsAtWaypoint = 0;
    private Vector3 _lastWaypoint;
    private bool _isIdle = false;
    [SerializeField] private float stuckThreshold = 0.1f;
    [SerializeField] private float stuckDuration = 0.5f;
    [SerializeField] private float obstacleCheckDistance = 3f;
    [SerializeField] private int maxJumpAttemptsPerWaypoint = 2;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (pathfinder == null)
        {
            pathfinder = FindObjectOfType<AStarPathfinder>();
        }

        if (grid == null)
        {
            grid = pathfinder != null ? pathfinder.GetComponent<AStarGrid>() : FindObjectOfType<AStarGrid>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        if (logAnimatorProblems && animator == null)
        {
            Debug.LogWarning("[AStarVillainChase] No Animator found. Assign the villain Animator in the Inspector.", this);
        }
    }

    private void Start()
    {
        if (target == null && !string.IsNullOrWhiteSpace(targetTag))
        {
            GameObject targetObject = GameObject.FindWithTag(targetTag);
            if (targetObject != null)
            {
                target = targetObject.transform;
            }
        }

        _lastTargetPosition = target != null ? target.position : transform.position;
        _lastFramePosition = transform.position;
        if (snapToClosestWalkableOnStart)
        {
            SnapToClosestWalkableNode();
        }
        else
        {
            SnapToGround();
        }
        Repath();
    }

    private void Update()
    {
        ApplyGravity();
        SnapToGroundSmoothly();
        _attackTimer -= Time.deltaTime;
        _roarTimer -= Time.deltaTime;
        _closeSoundTimer -= Time.deltaTime;

        if (target == null || pathfinder == null)
        {
            if (logMissingReferences)
            {
                if (target == null) Debug.LogWarning("[AStarVillainChase] Target is missing. Assign Rumi or tag Rumi as Player.", this);
                if (pathfinder == null) Debug.LogWarning("[AStarVillainChase] Pathfinder is missing. Assign the AStarSystem object.", this);
            }

            SetAnimatorSpeed(0f);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Play sounds based on distance
        PlaySoundByDistance(distanceToTarget);
        
        if (distanceToTarget <= stoppingDistance)
        {
            FacePosition(target.position);
            SetAnimatorSpeed(0f);
            TryAttack();
            return;
        }

        _repathTimer -= Time.deltaTime;
        bool targetMoved = Vector3.Distance(target.position, _lastTargetPosition) >= targetMoveRepathDistance;
        if (_repathTimer <= 0f || targetMoved || _path.Count == 0 || _pathIndex >= _path.Count)
        {
            if (!_isIdle)  // Only repath if not idle
            {
                Repath();
            }
        }

        if (!_isIdle)  // Only follow path if not idle
        {
            FollowPath(distanceToTarget);
        }
    }

    private void PlaySoundByDistance(float distanceToTarget)
    {
        // Play close sound when really near
        if (distanceToTarget <= closeSoundDistance && _closeSoundTimer <= 0f && closeSound != null)
        {
            _audioSource.PlayOneShot(closeSound, closeSoundVolume);
            _closeSoundTimer = closeSoundCooldown;
            Debug.Log($"[AStarVillainChase] Playing foundYou sound at distance {distanceToTarget}" , this);
        }
        
        // Play roar when far away (beyond roarDistance)
        if (distanceToTarget > roarDistance && _roarTimer <= 0f && roarSound != null)
        {
            _audioSource.PlayOneShot(roarSound, roarVolume);
            _roarTimer = roarCooldown;
        }
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }

        _verticalVelocity += gravity * Time.deltaTime;
        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    private void Repath()
    {
        _repathTimer = repathRate;

        if (target == null || pathfinder == null)
        {
            return;
        }

        _path = pathfinder.FindPath(transform.position, target.position);
        _pathIndex = 0;
        _lastTargetPosition = target.position;

        if (_path == null || _path.Count == 0)
        {
            if (logPathProblems)
            {
                string reason = pathfinder != null && !string.IsNullOrWhiteSpace(pathfinder.LastFailureReason)
                    ? pathfinder.LastFailureReason
                    : "Check AStarGrid size, ground mask, obstacle mask, and that Rumi is inside the grid.";
                Debug.LogWarning($"[AStarVillainChase] No A* path found. {reason}", this);
            }
        }
        else if (logPathSuccess)
        {
            Debug.Log($"[AStarVillainChase] A* path found with {_path.Count} waypoints.", this);
        }
    }

    private void FollowPath(float distanceToTarget)
    {
        if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count)
        {
            SetAnimatorSpeed(0f);
            return;
        }

        Vector3 waypoint = _path[_pathIndex];
        if (grid != null && grid.TryGetGroundPoint(waypoint, out Vector3 groundedWaypoint))
        {
            waypoint = groundedWaypoint;
        }

        Vector3 direction = waypoint - transform.position;
        direction.y = 0f;

        // Check if we've moved to a new waypoint
        if (Vector3.Distance(waypoint, _lastWaypoint) > 0.1f)
        {
            _lastWaypoint = waypoint;
            _jumpAttemptsAtWaypoint = 0;
            _stuckTimer = 0f;
        }

        if (direction.magnitude <= waypointTolerance)
        {
            _pathIndex++;
            _stuckTimer = 0f;
            _jumpAttemptsAtWaypoint = 0;
            return;
        }

        // Check if blocked by obstacle
        if (IsBlockedByObstacle(direction))
        {
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer >= stuckDuration && !string.IsNullOrWhiteSpace(runningJumpTrigger))
            {
                animator.SetTrigger(runningJumpTrigger);
                _jumpAttemptsAtWaypoint++;
                _stuckTimer = 0f;

                // If we've jumped too many times at this waypoint, skip it
                if (_jumpAttemptsAtWaypoint > maxJumpAttemptsPerWaypoint)
                {
                    Debug.Log($"[AStarVillainChase] Too many jump attempts at waypoint. Skipping waypoint and recalculating.", this);
                    _pathIndex++;
                    _jumpAttemptsAtWaypoint = 0;
                    if (pathfinder != null && target != null)
                    {
                        _path = pathfinder.FindPath(transform.position, target.position);
                    }
                }
            }
        }
        else
        {
            _stuckTimer = 0f;
        }

        float speed = distanceToTarget >= runDistance ? runSpeed : walkSpeed;
        Vector3 move = direction.normalized * speed;
        _controller.Move(move * Time.deltaTime);
        FacePosition(waypoint);
        SetAnimatorSpeed(Mathf.InverseLerp(0f, runSpeed, speed));
        
        _lastFramePosition = transform.position;
    }

    private bool IsBlockedByObstacle(Vector3 moveDirection)
    {
        if (moveDirection.magnitude < 0.01f)
            return false;

        // Raycast forward to detect obstacles
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDirection.normalized, obstacleCheckDistance, LayerMask.GetMask("Obstacle"), QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        // Also check if villain hasn't moved much (stuck check)
        float movementThisFrame = Vector3.Distance(transform.position, _lastFramePosition);
        if (movementThisFrame < stuckThreshold)
        {
            return true;
        }

        return false;
    }

    private void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (_attackTimer > 0f || animator == null)
        {
            return;
        }

        string[] attackAnimations = { attackTrigger, swipingTrigger, runningJumpTrigger };
        string randomAttack = attackAnimations[Random.Range(0, attackAnimations.Length)];
        
        if (!string.IsNullOrWhiteSpace(randomAttack))
        {
            animator.SetTrigger(randomAttack);
        }
        
        _attackTimer = Mathf.Max(0.1f, attackCooldown);
    }

    public void OnPlayerSwordSwing()
    {
        if (animator == null)
        {
            return;
        }

        StartCoroutine(PlaySwordSwingReaction());
    }

    private IEnumerator PlaySwordSwingReaction()
    {
        if (!string.IsNullOrWhiteSpace(runToStopTrigger))
        {
            animator.SetTrigger(runToStopTrigger);
        }

        yield return new WaitForSeconds(1f);

        if (!string.IsNullOrWhiteSpace(swipingTrigger))
        {
            animator.SetTrigger(swipingTrigger);
        }
    }

    public void SetIdle()
    {
        _isIdle = true;
        _path.Clear();
        _pathIndex = 0;
        SetAnimatorSpeed(0f);
        Debug.Log("[AStarVillainChase] Villain set to idle.", this);
    }

    public void ResumeChase()
    {
        _isIdle = false;
        // Path will be recalculated in next Update() call
        _path.Clear();
        _pathIndex = 0;
        Debug.Log("[AStarVillainChase] Villain resumed chase.", this);
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator == null || string.IsNullOrWhiteSpace(speedParam))
        {
            if (logAnimatorProblems && animator == null)
            {
                Debug.LogWarning("[AStarVillainChase] Animator is missing, so Speed cannot be set.", this);
            }
            return;
        }

        animator.SetFloat(speedParam, speed, animatorDampTime, Time.deltaTime);
    }

    private void SnapToGround()
    {
        if (grid == null || !grid.TryGetGroundPoint(transform.position, out Vector3 groundPoint))
        {
            return;
        }

        Vector3 position = transform.position;
        position.y = groundPoint.y + groundOffset;

        _controller.enabled = false;
        transform.position = position;
        _controller.enabled = true;
    }

    private void SnapToClosestWalkableNode()
    {
        if (grid == null || !grid.TryGetClosestWalkablePoint(transform.position, out Vector3 walkablePoint))
        {
            SnapToGround();
            return;
        }

        Vector3 position = transform.position;
        position.x = walkablePoint.x;
        position.y = walkablePoint.y + groundOffset;
        position.z = walkablePoint.z;

        _controller.enabled = false;
        transform.position = position;
        _controller.enabled = true;
    }

    private void SnapToGroundSmoothly()
    {
        if (grid == null || !grid.TryGetGroundPoint(transform.position, out Vector3 groundPoint))
        {
            return;
        }

        float targetY = groundPoint.y + groundOffset;
        float yDelta = targetY - transform.position.y;
        if (Mathf.Abs(yDelta) <= 0.01f)
        {
            return;
        }

        float snap = Mathf.Clamp(yDelta, -groundSnapSpeed * Time.deltaTime, groundSnapSpeed * Time.deltaTime);
        _controller.Move(Vector3.up * snap);
    }

    private void OnDrawGizmos()
    {
        if (!drawPath || _path == null || _path.Count < 1)
        {
            return;
        }

        Gizmos.color = Color.green;
        for (int i = 0; i < _path.Count - 1; i++)
        {
            Gizmos.DrawLine(_path[i], _path[i + 1]);
            Gizmos.DrawSphere(_path[i], 0.2f);
        }
        Gizmos.DrawSphere(_path[_path.Count - 1], 0.2f);
    }
}
