using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SealBarricade : MonoBehaviour
{
    public static int ActiveCount { get; private set; }

    [SerializeField] private float   _duration       = 30f;
    [SerializeField] private float   _animationSpeed = 0.3f;
    [SerializeField] private Vector3 _wallSize       = new Vector3(3f, 3f, 0.5f);

    private NavMeshObstacle  _obstacle;
    private BoxCollider      _solidWall;
    private Animator         _animator;
    private Coroutine        _loopCoroutine;
    private NavMeshGraph     _graph;
    private UCSVillainChase  _villain;
    private AStarVillainChase _aStarVillain;
    private AStarGrid         _aStarGrid;

    private void OnEnable()  => ActiveCount++;
    private void OnDisable() => ActiveCount--;

    private void Start()
    {
        _obstacle = GetComponent<NavMeshObstacle>();
        _animator = GetComponent<Animator>();
        _graph        = FindObjectOfType<NavMeshGraph>();
        _villain      = FindObjectOfType<UCSVillainChase>();
        _aStarVillain = FindObjectOfType<AStarVillainChase>();
        _aStarGrid    = FindObjectOfType<AStarGrid>();

        if (_obstacle != null)
        {
            _obstacle.size                    = _wallSize;
            _obstacle.carvingTimeToStationary = 0f;
            _obstacle.carving                 = true;
            _obstacle.enabled                 = true;
        }

        // Add a solid (non-trigger) BoxCollider so villains' CharacterControllers are physically blocked.
        _solidWall        = gameObject.AddComponent<BoxCollider>();
        _solidWall.isTrigger = false;
        _solidWall.size   = _wallSize;

        // Let Rumi pass through — only Jinu should be physically blocked
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Collider[] playerColliders = player.GetComponentsInChildren<Collider>(true);
            Collider[] myColliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider pc in playerColliders)
                foreach (Collider mc in myColliders)
                    Physics.IgnoreCollision(pc, mc, true);
            foreach (Collider pc in playerColliders)
                Physics.IgnoreCollision(pc, _solidWall, true);
        }

        if (_animator != null)
        {
            _animator.speed = _animationSpeed;
            _loopCoroutine  = StartCoroutine(LoopStartAnimation());
        }

        StartCoroutine(RebuildAfterCarve());
        StartCoroutine(RebuildAStarAfterDelay());
        StartCoroutine(ExpireRoutine());
    }

    // Wait for Unity to apply the NavMesh carve, then rebuild the graph so UCS sees the blocked area
    private IEnumerator RebuildAfterCarve()
    {
        yield return new WaitForSeconds(0.2f); // Give NavMesh time to finish carving

        if (_graph == null)
        {
            Debug.LogWarning("[SealBarricade] No NavMeshGraph in scene — Jinu's path will not update.", this);
            yield break;
        }

        _graph.BuildGraph();
        _villain?.ForceRepath();
        Debug.Log("[SealBarricade] NavMesh graph rebuilt after carve. Jinu must find a new route.", this);
    }

    // Rebuild the A* grid after 10 s so the altar villain sees the seal as an obstacle and reroutes
    private IEnumerator RebuildAStarAfterDelay()
    {
        yield return new WaitForSeconds(5f);

        if (_aStarGrid == null)
        {
            Debug.LogWarning("[SealBarricade] No AStarGrid in scene — altar villain path will not update.", this);
            yield break;
        }

        _aStarGrid.BuildGrid();
        _aStarVillain?.ForceRepath();
        Debug.Log("[SealBarricade] A* grid rebuilt after 10 s. Altar villain must find a new route.", this);
    }

    private IEnumerator LoopStartAnimation()
    {
        while (true)
        {
            _animator.Play("WaterSpellStart", 0, 0f);
            yield return null;

            AnimatorStateInfo state;
            do
            {
                yield return null;
                state = _animator.GetCurrentAnimatorStateInfo(0);
            }
            while (state.IsName("WaterSpellStart") && state.normalizedTime < 1f);
        }
    }

    private IEnumerator ExpireRoutine()
    {
        yield return new WaitForSeconds(_duration);

        if (_loopCoroutine != null)
            StopCoroutine(_loopCoroutine);

        if (_animator != null)
        {
            _animator.speed = 1f;
            _animator.Play("WaterSpellFinish", 0, 0f);
        }

        if (_obstacle != null)   _obstacle.enabled = false;
        if (_solidWall != null)  _solidWall.enabled = false;

        // Wait for the NavMesh to fully restore the carved area before rebuilding
        yield return new WaitForSeconds(1.5f);

        if (_graph != null)
        {
            _graph.BuildGraph();
            _villain?.ForceRepath();
            Debug.Log("[SealBarricade] Barricade expired, graph rebuilt. Jinu can recalculate route.", this);
        }

        if (_aStarGrid != null)
        {
            _aStarGrid.BuildGrid();
            _aStarVillain?.ForceRepath();
            Debug.Log("[SealBarricade] A* grid rebuilt after expiry. Altar villain can recalculate route.", this);
        }

        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}
