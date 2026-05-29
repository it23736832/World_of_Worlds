using System.Collections.Generic;
using UnityEngine;

public class AStarGrid : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Vector2 gridWorldSize = new Vector2(80f, 80f);
    [SerializeField] private float nodeRadius = 0.5f;
    [SerializeField] private bool allowDiagonalMovement = true;
    [SerializeField] private bool preventDiagonalCornerCutting = true;

    [Header("World Scan")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float scanHeight = 20f;
    [SerializeField] private float groundCheckDistance = 50f;
    [SerializeField] private float obstacleCheckHeight = 1.8f;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private float maxStepHeight = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool drawGrid;
    [SerializeField] private bool drawOnlyBlocked;
    [SerializeField] private bool logBakeSummary = true;

    private AStarNode[,] _grid;
    private float _nodeDiameter;
    private int _gridSizeX;
    private int _gridSizeY;
    private int _walkableNodeCount;

    public float NodeRadius => nodeRadius;
    public int WalkableNodeCount => _walkableNodeCount;
    public int GridSizeX => _gridSizeX;
    public int GridSizeY => _gridSizeY;

    private void Awake()
    {
        BuildGrid();
    }

    [ContextMenu("Bake A* Grid")]
    public void BuildGrid()
    {
        _nodeDiameter = nodeRadius * 2f;
        _gridSizeX = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.x / _nodeDiameter));
        _gridSizeY = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.y / _nodeDiameter));
        _grid = new AStarNode[_gridSizeX, _gridSizeY];
        _walkableNodeCount = 0;

        Vector3 worldBottomLeft = transform.position
            - Vector3.right * gridWorldSize.x * 0.5f
            - Vector3.forward * gridWorldSize.y * 0.5f;

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                Vector3 samplePosition = worldBottomLeft
                    + Vector3.right * (x * _nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * _nodeDiameter + nodeRadius);

                bool hasGround = TryGetGroundPoint(samplePosition, out Vector3 groundPoint);
                bool blocked = hasGround && Physics.CheckCapsule(
                    groundPoint + Vector3.up * nodeRadius,
                    groundPoint + Vector3.up * obstacleCheckHeight,
                    nodeRadius,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore);

                bool walkable = hasGround && !blocked;
                if (walkable)
                {
                    _walkableNodeCount++;
                }

                _grid[x, y] = new AStarNode(walkable, groundPoint, x, y);
            }
        }

        if (logBakeSummary)
        {
            Debug.Log($"[AStarGrid] Baked {_gridSizeX}x{_gridSizeY} grid. Walkable nodes: {_walkableNodeCount}. Ground Mask: {groundMask.value}, Obstacle Mask: {obstacleMask.value}", this);
        }
    }

    public AStarNode NodeFromWorldPoint(Vector3 worldPosition)
    {
        if (_grid == null)
        {
            BuildGrid();
        }

        Vector3 localPosition = worldPosition - transform.position;
        float percentX = (localPosition.x + gridWorldSize.x * 0.5f) / gridWorldSize.x;
        float percentY = (localPosition.z + gridWorldSize.y * 0.5f) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);
        return _grid[x, y];
    }

    public bool ContainsWorldPoint(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition - transform.position;
        return Mathf.Abs(localPosition.x) <= gridWorldSize.x * 0.5f &&
               Mathf.Abs(localPosition.z) <= gridWorldSize.y * 0.5f;
    }

    public AStarNode ClosestWalkableNodeFromWorldPoint(Vector3 worldPosition, int searchRadius = 6)
    {
        AStarNode centerNode = NodeFromWorldPoint(worldPosition);
        if (centerNode.walkable)
        {
            return centerNode;
        }

        for (int radius = 1; radius <= searchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                    {
                        continue;
                    }

                    int checkX = centerNode.gridX + x;
                    int checkY = centerNode.gridY + y;
                    if (checkX < 0 || checkX >= _gridSizeX || checkY < 0 || checkY >= _gridSizeY)
                    {
                        continue;
                    }

                    AStarNode node = _grid[checkX, checkY];
                    if (node.walkable)
                    {
                        return node;
                    }
                }
            }
        }

        return centerNode;
    }

    public bool TryGetClosestWalkablePoint(Vector3 worldPosition, out Vector3 walkablePoint, int searchRadius = 100)
    {
        AStarNode node = ClosestWalkableNodeFromWorldPoint(worldPosition, searchRadius);
        if (node != null && node.walkable)
        {
            walkablePoint = node.worldPosition;
            return true;
        }

        walkablePoint = worldPosition;
        return false;
    }

    public List<AStarNode> GetNeighbours(AStarNode node)
    {
        List<AStarNode> neighbours = new List<AStarNode>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                if (!allowDiagonalMovement && Mathf.Abs(x) + Mathf.Abs(y) > 1)
                {
                    continue;
                }

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < _gridSizeX && checkY >= 0 && checkY < _gridSizeY)
                {
                    if (preventDiagonalCornerCutting && x != 0 && y != 0)
                    {
                        AStarNode horizontal = _grid[node.gridX + x, node.gridY];
                        AStarNode vertical = _grid[node.gridX, node.gridY + y];
                        if (!horizontal.walkable || !vertical.walkable)
                        {
                            continue;
                        }
                    }

                    neighbours.Add(_grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    public void ResetPathData()
    {
        if (_grid == null)
        {
            return;
        }

        foreach (AStarNode node in _grid)
        {
            node.gCost = int.MaxValue;
            node.hCost = 0;
            node.parent = null;
        }
    }

    public bool TryGetGroundPoint(Vector3 position, out Vector3 groundPoint)
    {
        Vector3 origin = new Vector3(position.x, transform.position.y + scanHeight, position.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPoint = hit.point + Vector3.up * groundOffset;
            return true;
        }

        groundPoint = position;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

        if (!drawGrid || _grid == null)
        {
            return;
        }

        foreach (AStarNode node in _grid)
        {
            if (drawOnlyBlocked && node.walkable)
            {
                continue;
            }

            Gizmos.color = node.walkable ? new Color(1f, 1f, 1f, 0.2f) : new Color(1f, 0f, 0f, 0.8f);
            Gizmos.DrawCube(node.worldPosition, Vector3.one * (_nodeDiameter * 0.8f));
        }
    }
}
