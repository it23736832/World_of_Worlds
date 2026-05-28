using System.Collections.Generic;
using UnityEngine;

public class UCSPathfinder : MonoBehaviour
{
    [SerializeField] private NavMeshGraph graph;
    [SerializeField] private bool logSearchProblems = true;
    [SerializeField] private int maxIterations = 50000;
    [SerializeField] private int maxDebugNodes = 2000;

    public string LastFailureReason { get; private set; } = "No search has run yet.";
    public List<int> LastExploredNodes { get; private set; } = new List<int>();
    public List<int> LastFrontierNodes { get; private set; } = new List<int>();

    private float _lastWarnTime = -999f;
    private const float WarnThrottle = 5f;

    private void Awake()
    {
        if (graph == null) graph = GetComponent<NavMeshGraph>();
    }

    public List<Vector3> FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        LastExploredNodes = new List<int>();
        LastFrontierNodes = new List<int>();

        if (graph == null || graph.NodeCount == 0)
        {
            LastFailureReason = "NavMeshGraph missing or empty. Make sure it has built the graph.";
            if (logSearchProblems) Debug.LogWarning($"[UCSPathfinder] {LastFailureReason}", this);
            return new List<Vector3>();
        }

        int startNode = graph.GetNearestNodeId(startPosition);
        int targetNode = graph.GetNearestNodeId(targetPosition);

        if (startNode < 0 || targetNode < 0)
        {
            LastFailureReason = "Could not find a valid node near start or target position.";
            if (logSearchProblems) Debug.LogWarning($"[UCSPathfinder] {LastFailureReason}", this);
            return new List<Vector3>();
        }

        if (startNode == targetNode)
        {
            LastFailureReason = string.Empty;
            return new List<Vector3> { targetPosition };
        }

        // UCS open list: (cumulative cost g, nodeId)
        // We track the cheapest known cost and parent for each node
        Dictionary<int, float> gCost = new Dictionary<int, float>();
        Dictionary<int, int> parent = new Dictionary<int, int>();
        HashSet<int> closedSet = new HashSet<int>();

        // Open list entries: (g, nodeId) — we pick the minimum g each iteration
        List<(float g, int nodeId)> openList = new List<(float, int)>();

        gCost[startNode] = 0f;
        parent[startNode] = -1;
        openList.Add((0f, startNode));

        int iterations = 0;

        while (openList.Count > 0)
        {
            if (++iterations > maxIterations)
            {
                LastFailureReason = $"UCS hit iteration limit ({maxIterations}) after {iterations} iterations. Increase Max Iterations or reduce graph size.";
                if (logSearchProblems && Time.time - _lastWarnTime >= WarnThrottle)
                {
                    _lastWarnTime = Time.time;
                    Debug.LogWarning($"[UCSPathfinder] {LastFailureReason}", this);
                }
                return new List<Vector3>();
            }

            // Pop node with lowest g cost
            int bestIdx = 0;
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].g < openList[bestIdx].g) bestIdx = i;
            }
            var (currentG, currentNode) = openList[bestIdx];
            openList.RemoveAt(bestIdx);

            if (closedSet.Contains(currentNode)) continue;
            closedSet.Add(currentNode);
            if (LastExploredNodes.Count < maxDebugNodes) LastExploredNodes.Add(currentNode);

            if (currentNode == targetNode)
            {
                LastFailureReason = string.Empty;
                SnapshotFrontier(openList, closedSet);
                return RetracePath(parent, startNode, targetNode, graph.NodePositions);
            }

            if (!graph.Adjacency.ContainsKey(currentNode)) continue;

            foreach (GraphEdge edge in graph.Adjacency[currentNode])
            {
                if (closedSet.Contains(edge.toNodeId)) continue;

                float newG = currentG + edge.cost;

                if (!gCost.ContainsKey(edge.toNodeId) || newG < gCost[edge.toNodeId])
                {
                    gCost[edge.toNodeId] = newG;
                    parent[edge.toNodeId] = currentNode;
                    openList.Add((newG, edge.toNodeId));
                }
            }
        }

        SnapshotFrontier(openList, closedSet);
        LastFailureReason = $"UCS exhausted all reachable nodes after {iterations} iterations. No path exists to target (disconnected graph?).";
        if (logSearchProblems && Time.time - _lastWarnTime >= WarnThrottle)
        {
            _lastWarnTime = Time.time;
            Debug.LogWarning($"[UCSPathfinder] {LastFailureReason}", this);
        }
        return new List<Vector3>();
    }

    // Stores a snapshot of the current open set as the visible frontier for debug overlay
    private void SnapshotFrontier(List<(float g, int nodeId)> openList, HashSet<int> closedSet)
    {
        LastFrontierNodes = new List<int>();
        HashSet<int> seen = new HashSet<int>();
        foreach (var (_, nodeId) in openList)
        {
            if (closedSet.Contains(nodeId)) continue;
            if (!seen.Add(nodeId)) continue;
            LastFrontierNodes.Add(nodeId);
            if (LastFrontierNodes.Count >= maxDebugNodes) break;
        }
    }

    private static List<Vector3> RetracePath(Dictionary<int, int> parent, int startNode, int endNode, Vector3[] positions)
    {
        List<Vector3> path = new List<Vector3>();
        int current = endNode;

        while (current != startNode && current != -1)
        {
            path.Add(positions[current]);
            if (!parent.ContainsKey(current)) break;
            current = parent[current];
        }

        path.Reverse();
        return path;
    }
}
