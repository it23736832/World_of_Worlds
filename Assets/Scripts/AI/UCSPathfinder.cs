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

        // UCS open list — binary min-heap for O(n log n) vs the old O(n²) linear scan
        Dictionary<int, float> gCost = new Dictionary<int, float>();
        Dictionary<int, int> parent = new Dictionary<int, int>();
        HashSet<int> closedSet = new HashSet<int>();
        HashSet<int> frontierSet = new HashSet<int>();
        MinHeap openList = new MinHeap();

        gCost[startNode] = 0f;
        parent[startNode] = -1;
        openList.Push(startNode, 0f);
        frontierSet.Add(startNode);

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

            int currentNode = openList.Pop();
            frontierSet.Remove(currentNode);

            if (closedSet.Contains(currentNode)) continue;
            closedSet.Add(currentNode);
            if (LastExploredNodes.Count < maxDebugNodes) LastExploredNodes.Add(currentNode);

            if (currentNode == targetNode)
            {
                LastFailureReason = string.Empty;
                SnapshotFrontier(frontierSet);
                return RetracePath(parent, startNode, targetNode, graph.NodePositions);
            }

            if (!graph.Adjacency.ContainsKey(currentNode)) continue;

            float currentG = gCost[currentNode];
            foreach (GraphEdge edge in graph.Adjacency[currentNode])
            {
                if (closedSet.Contains(edge.toNodeId)) continue;

                float newG = currentG + edge.cost;

                if (!gCost.ContainsKey(edge.toNodeId) || newG < gCost[edge.toNodeId])
                {
                    gCost[edge.toNodeId] = newG;
                    parent[edge.toNodeId] = currentNode;
                    openList.Push(edge.toNodeId, newG);
                    frontierSet.Add(edge.toNodeId);
                }
            }
        }

        SnapshotFrontier(frontierSet);
        LastFailureReason = $"UCS exhausted all reachable nodes after {iterations} iterations. No path exists to target (disconnected graph?).";
        if (logSearchProblems && Time.time - _lastWarnTime >= WarnThrottle)
        {
            _lastWarnTime = Time.time;
            Debug.LogWarning($"[UCSPathfinder] {LastFailureReason}", this);
        }
        return new List<Vector3>();
    }

    private void SnapshotFrontier(HashSet<int> frontierSet)
    {
        LastFrontierNodes = new List<int>(frontierSet.Count);
        foreach (int nodeId in frontierSet)
        {
            LastFrontierNodes.Add(nodeId);
            if (LastFrontierNodes.Count >= maxDebugNodes) break;
        }
    }

    // Binary min-heap — O(log n) push/pop; used because PriorityQueue<T,P> requires .NET 6+
    private class MinHeap
    {
        private readonly List<(float g, int nodeId)> _data = new List<(float, int)>();
        public int Count => _data.Count;

        public void Push(int nodeId, float g)
        {
            _data.Add((g, nodeId));
            BubbleUp(_data.Count - 1);
        }

        public int Pop()
        {
            int result = _data[0].nodeId;
            int last = _data.Count - 1;
            _data[0] = _data[last];
            _data.RemoveAt(last);
            if (_data.Count > 0) SiftDown(0);
            return result;
        }

        private void BubbleUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (_data[i].g < _data[p].g)
                {
                    var tmp = _data[i]; _data[i] = _data[p]; _data[p] = tmp;
                    i = p;
                }
                else break;
            }
        }

        private void SiftDown(int i)
        {
            int n = _data.Count;
            while (true)
            {
                int smallest = i;
                int left = 2 * i + 1, right = 2 * i + 2;
                if (left  < n && _data[left].g  < _data[smallest].g) smallest = left;
                if (right < n && _data[right].g < _data[smallest].g) smallest = right;
                if (smallest == i) break;
                var tmp = _data[i]; _data[i] = _data[smallest]; _data[smallest] = tmp;
                i = smallest;
            }
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
