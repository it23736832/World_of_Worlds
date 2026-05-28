using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class VillainAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private AStarPathfinder pathfinding;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float waypointTolerance = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRaycastHeight = 2f;
    [SerializeField] private float groundRaycastDistance = 6f;
    [SerializeField] private float groundOffset;

    [Header("Path Recalc")]
    [SerializeField] private float repathRate = 1.0f;
    [SerializeField] private float repathDistance = 1.0f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private bool normalizeSpeed = true;
    [SerializeField] private float speedForMax = 5.0f;
    [SerializeField] private bool showDebugMessages = true;

    private List<Vector3> path;
    private int targetIndex;
    private float repathTimer;
    private float attackTimer;
    private Vector3 lastPosition;
    private Vector3 lastTargetPosition;

    private void Awake()
    {
        NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        lastPosition = transform.position;
        lastTargetPosition = target != null ? target.position : transform.position;

        SnapToGround();

        if (showDebugMessages)
        {
            Debug.Log($"[Villain] BFS initialized. Target: {target?.name ?? "NONE"}, Pathfinding: {pathfinding != null}, Animator: {animator != null}", this);
        }
    }

    private void Update()
    {
        if (target == null || pathfinding == null)
        {
            if (showDebugMessages)
            {
                if (target == null) Debug.LogWarning("[Villain] Target is NULL!", this);
                if (pathfinding == null) Debug.LogWarning("[Villain] Pathfinding is NULL!", this);
            }

            return;
        }

        attackTimer -= Time.deltaTime;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= attackRange)
        {
            TryAttack();
            UpdateAnimatorSpeed(0f);
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            bool targetMoved = Vector3.Distance(target.position, lastTargetPosition) >= repathDistance;
            bool needPath = path == null || path.Count == 0 || targetIndex >= path.Count;

            if (targetMoved || needPath)
            {
                path = pathfinding.FindPath(transform.position, target.position);
                targetIndex = 0;
                lastTargetPosition = target.position;

                if (showDebugMessages)
                {
                    Debug.Log($"[Villain] BFS path nodes: {path?.Count ?? 0}", this);
                }
            }

            repathTimer = repathRate;
        }

        FollowPath();
    }

    private void FollowPath()
    {
        if (path == null || path.Count == 0 || targetIndex >= path.Count)
        {
            UpdateAnimatorSpeed(0f);
            return;
        }

        Vector3 targetPosition = path[targetIndex];
        Vector3 currentPosition = transform.position;
        targetPosition = GetGroundedPosition(targetPosition, targetPosition.y);

        transform.position = Vector3.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                Time.deltaTime * 8f
            );
        }

        if (Vector3.Distance(transform.position, targetPosition) <= waypointTolerance)
        {
            targetIndex++;
        }

        UpdateAnimatorSpeedFromMotion();
        lastPosition = transform.position;
    }

    private void TryAttack()
    {
        if (attackTimer > 0f)
        {
            return;
        }

        if (animator != null && !string.IsNullOrWhiteSpace(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }

        attackTimer = Mathf.Max(0.1f, attackCooldown);
    }

    private void UpdateAnimatorSpeed(float speed)
    {
        if (animator == null || string.IsNullOrWhiteSpace(speedParam))
        {
            return;
        }

        animator.SetFloat(speedParam, speed);
    }

    private void UpdateAnimatorSpeedFromMotion()
    {
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float planarSpeed = delta.magnitude / dt;

        float speedValue = planarSpeed;
        if (normalizeSpeed)
        {
            float maxSpeed = Mathf.Max(0.1f, speedForMax);
            speedValue = Mathf.Clamp01(planarSpeed / maxSpeed);
        }

        UpdateAnimatorSpeed(speedValue);
    }

    private Vector3 GetGroundedPosition(Vector3 position, float fallbackY)
    {
        Vector3 rayStart = new Vector3(position.x, fallbackY + groundRaycastHeight, position.z);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRaycastHeight + groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            position.y = hit.point.y + groundOffset;
            return position;
        }

        position.y = fallbackY;
        return position;
    }

    private void SnapToGround()
    {
        transform.position = GetGroundedPosition(transform.position, transform.position.y);
    }
}
