using UnityEngine;
using UnityEngine.AI;

public class VillainChase : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private float _updateRate = 0.1f;
    [SerializeField] private string _villainTag = "Villain";

    private NavMeshAgent _agent;
    private Animator _animator;
    private float _timer;

    private void Awake()
    {
        EnsureDoorTriggerPhysics();
    }

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();

        foreach (Animator anim in GetComponentsInChildren<Animator>())
        {
            if (anim.runtimeAnimatorController != null)
            {
                _animator = anim;
                break;
            }
        }
    }

    private void Update()
    {
        if (_player == null || _agent == null) return;

        _timer += Time.deltaTime;
        if (_timer >= _updateRate)
        {
            _timer = 0f;
            _agent.SetDestination(_player.position);
        }

        if (_animator != null)
        {
            bool isMoving = _agent.hasPath && !_agent.pathPending
                            && _agent.remainingDistance > _agent.stoppingDistance;
            float targetSpeed = isMoving ? 1f : 0f;
            _animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);
        }
    }

    private void EnsureDoorTriggerPhysics()
    {
        if (!string.IsNullOrEmpty(_villainTag) && !CompareTag(_villainTag))
        {
            try { gameObject.tag = _villainTag; } catch { }
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;

        if (HasNonTriggerCollider())
            return;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
        capsule.isTrigger = false;
        capsule.radius = agent != null ? agent.radius : 0.35f;
        capsule.height = agent != null ? agent.height : 1.8f;
        capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
    }

    private bool HasNonTriggerCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (!collider.isTrigger)
                return true;
        }

        return false;
    }
}
