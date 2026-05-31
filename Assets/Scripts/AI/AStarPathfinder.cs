using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    [SerializeField] private AStarGrid grid;
    [SerializeField] private int nearestWalkableSearchRadius = 200;
    [SerializeField] private bool logSearchProblems = true;

    public string LastFailureReason { get; private set; } = "No search has run yet.";

    private void Awake()
    {
        if (grid == null)
        {
            grid = GetComponent<AStarGrid>();
        }
    }

    public List<Vector3> FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        if (grid == null)
        {
            LastFailureReason = "AStarPathfinder has no AStarGrid assigned.";
            return new List<Vector3>();
        }

        if (grid.WalkableNodeCount <= 0)
        {
            LastFailureReason = "Grid has 0 walkable nodes. Ground Mask/layers/colliders are wrong, or Obstacle Mask blocks everything.";
            if (logSearchProblems)
            {
                Debug.LogWarning($"[AStarPathfinder] {LastFailureReason}", this);
            }

            return new List<Vector3>();
        }

        if (logSearchProblems)
        {
            if (!grid.ContainsWorldPoint(startPosition))
            {
                Debug.LogWarning($"[AStarPathfinder] Start position is outside AStarGrid: {startPosition}", this);
            }

            if (!grid.ContainsWorldPoint(targetPosition))
            {
                Debug.LogWarning($"[AStarPathfinder] Target position is outside AStarGrid: {targetPosition}", this);
            }
        }

        AStarNode startNode = grid.ClosestWalkableNodeFromWorldPoint(startPosition, nearestWalkableSearchRadius);
        AStarNode targetNode = grid.ClosestWalkableNodeFromWorldPoint(targetPosition, nearestWalkableSearchRadius);

        if (startNode == null || targetNode == null || !startNode.walkable || !targetNode.walkable)
        {
                string startWalkable = startNode != null ? startNode.walkable.ToString() : "null";
                string targetWalkable = targetNode != null ? targetNode.walkable.ToString() : "null";
                LastFailureReason = $"Start/target node not walkable. Start walkable: {startWalkable}, Target walkable: {targetWalkable}.";
            if (logSearchProblems)
            {
                Debug.LogWarning($"[AStarPathfinder] {LastFailureReason}", this);
            }

            return new List<Vector3>();
        }

        if (startNode == targetNode)
        {
            LastFailureReason = string.Empty;
            return new List<Vector3> { targetPosition };
        }

        grid.ResetPathData();

        // Min-heap open set: O(log n) push/pop vs the old O(n) linear scan
        MinHeap openHeap = new MinHeap();
        HashSet<AStarNode> closedSet = new HashSet<AStarNode>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        startNode.parent = null;
        openHeap.Push(startNode);

        while (openHeap.Count > 0)
        {
            AStarNode currentNode = openHeap.Pop();

            // Lazy deletion: skip stale heap entries for nodes already finalised
            if (closedSet.Contains(currentNode)) continue;
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                LastFailureReason = string.Empty;
                return RetracePath(startNode, targetNode);
            }

            foreach (AStarNode neighbour in grid.GetNeighbours(currentNode))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCost < neighbour.gCost)
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;
                    // Push updated entry; the old (higher-cost) entry becomes stale and is skipped on pop
                    openHeap.Push(neighbour);
                }
            }
        }

        LastFailureReason = $"Search exhausted. Start node ({startNode.gridX},{startNode.gridY}) and target node ({targetNode.gridX},{targetNode.gridY}) may be separated by obstacle nodes.";
        return new List<Vector3>();
    }

    private static List<Vector3> RetracePath(AStarNode startNode, AStarNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        AStarNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    private static int GetDistance(AStarNode a, AStarNode b)
    {
        int distanceX = Mathf.Abs(a.gridX - b.gridX);
        int distanceY = Mathf.Abs(a.gridY - b.gridY);

        if (distanceX > distanceY)
        {
            return 14 * distanceY + 10 * (distanceX - distanceY);
        }

        return 14 * distanceX + 10 * (distanceY - distanceX);
    }

    // Binary min-heap sorted by FCost (then hCost as tiebreaker) — O(log n) push/pop
    private class MinHeap
    {
        private readonly List<AStarNode> _data = new List<AStarNode>();
        public int Count => _data.Count;

        public void Push(AStarNode node)
        {
            _data.Add(node);
            BubbleUp(_data.Count - 1);
        }

        public AStarNode Pop()
        {
            AStarNode result = _data[0];
            int last = _data.Count - 1;
            _data[0] = _data[last];
            _data.RemoveAt(last);
            if (_data.Count > 0) SiftDown(0);
            return result;
        }

        private static bool HasPriority(AStarNode a, AStarNode b)
        {
            return a.FCost < b.FCost || (a.FCost == b.FCost && a.hCost < b.hCost);
        }

        private void BubbleUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (HasPriority(_data[i], _data[parent]))
                {
                    AStarNode tmp = _data[i]; _data[i] = _data[parent]; _data[parent] = tmp;
                    i = parent;
                }
                else break;
            }
        }

        private void SiftDown(int i)
        {
            int n = _data.Count;
            while (true)
            {
                int best = i;
                int left = 2 * i + 1, right = 2 * i + 2;
                if (left  < n && HasPriority(_data[left],  _data[best])) best = left;
                if (right < n && HasPriority(_data[right], _data[best])) best = right;
                if (best == i) break;
                AStarNode tmp = _data[i]; _data[i] = _data[best]; _data[best] = tmp;
                i = best;
            }
        }
    }
}
