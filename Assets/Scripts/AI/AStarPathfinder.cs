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

        List<AStarNode> openSet = new List<AStarNode> { startNode };
        HashSet<AStarNode> closedSet = new HashSet<AStarNode>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        startNode.parent = null;

        while (openSet.Count > 0)
        {
            AStarNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                AStarNode candidate = openSet[i];
                if (candidate.FCost < currentNode.FCost ||
                    candidate.FCost == currentNode.FCost && candidate.hCost < currentNode.hCost)
                {
                    currentNode = candidate;
                }
            }

            openSet.Remove(currentNode);
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
                if (newCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
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
}
