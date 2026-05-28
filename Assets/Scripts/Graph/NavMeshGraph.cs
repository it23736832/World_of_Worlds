using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshGraph : MonoBehaviour
{
    [Header("Graph Settings")]
    [SerializeField] private float mergeVertexRadius = 0.05f;
    [SerializeField] private bool logBuildSummary = true;

    private Dictionary<int, List<GraphEdge>> _adjacency = new Dictionary<int, List<GraphEdge>>();
    private Vector3[] _nodePositions;

    public int NodeCount => _nodePositions != null ? _nodePositions.Length : 0;
    public Vector3[] NodePositions => _nodePositions;
    public Dictionary<int, List<GraphEdge>> Adjacency => _adjacency;

    private void Awake()
    {
        BuildGraph();
    }

    [ContextMenu("Build Graph")]
    public void BuildGraph()
    {
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();

        int[] remapTable = BuildRemapTable(tri.vertices, mergeVertexRadius);
        _nodePositions = BuildUniqueVertexArray(tri.vertices, remapTable);

        _adjacency.Clear();
        for (int i = 0; i < _nodePositions.Length; i++)
            _adjacency[i] = new List<GraphEdge>();

        int edgeCount = 0;
        int triCount = tri.indices.Length / 3;

        for (int t = 0; t < triCount; t++)
        {
            int a = remapTable[tri.indices[t * 3]];
            int b = remapTable[tri.indices[t * 3 + 1]];
            int c = remapTable[tri.indices[t * 3 + 2]];

            TryAddEdge(a, b, ref edgeCount);
            TryAddEdge(b, c, ref edgeCount);
            TryAddEdge(a, c, ref edgeCount);
        }

        if (logBuildSummary)
            Debug.Log($"[NavMeshGraph] {_nodePositions.Length} nodes, {edgeCount} edges from {triCount} NavMesh triangles.", this);
    }

    private void TryAddEdge(int a, int b, ref int edgeCount)
    {
        if (a == b) return;

        bool hasAB = false, hasBA = false;
        foreach (GraphEdge e in _adjacency[a]) if (e.toNodeId == b) { hasAB = true; break; }
        foreach (GraphEdge e in _adjacency[b]) if (e.toNodeId == a) { hasBA = true; break; }

        float cost = Vector3.Distance(_nodePositions[a], _nodePositions[b]);
        if (!hasAB) { _adjacency[a].Add(new GraphEdge(b, cost)); edgeCount++; }
        if (!hasBA) { _adjacency[b].Add(new GraphEdge(a, cost)); edgeCount++; }
    }

    // O(n) vertex merge using a dictionary keyed by rounded position
    private int[] BuildRemapTable(Vector3[] vertices, float radius)
    {
        int[] remap = new int[vertices.Length];
        Dictionary<long, int> lookup = new Dictionary<long, int>();
        List<Vector3> unique = new List<Vector3>();

        float invRadius = 1f / Mathf.Max(radius, 0.0001f);

        for (int i = 0; i < vertices.Length; i++)
        {
            long key = PositionKey(vertices[i], invRadius);
            if (lookup.TryGetValue(key, out int existingId))
            {
                remap[i] = existingId;
            }
            else
            {
                int newId = unique.Count;
                unique.Add(vertices[i]);
                lookup[key] = newId;
                remap[i] = newId;
            }
        }

        return remap;
    }

    private static long PositionKey(Vector3 v, float invRadius)
    {
        int x = Mathf.RoundToInt(v.x * invRadius);
        int y = Mathf.RoundToInt(v.y * invRadius);
        int z = Mathf.RoundToInt(v.z * invRadius);
        // Pack three ints into a long (clamped to 20-bit range for safety)
        return ((long)(x & 0xFFFFF) << 40) | ((long)(y & 0xFFFFF) << 20) | (long)(z & 0xFFFFF);
    }

    private static Vector3[] BuildUniqueVertexArray(Vector3[] vertices, int[] remapTable)
    {
        int maxId = 0;
        foreach (int id in remapTable) if (id > maxId) maxId = id;

        Vector3[] result = new Vector3[maxId + 1];
        for (int i = 0; i < vertices.Length; i++)
            result[remapTable[i]] = vertices[i];

        return result;
    }

    public int GetNearestNodeId(Vector3 worldPosition)
    {
        if (_nodePositions == null || _nodePositions.Length == 0) return -1;

        int nearest = 0;
        float bestSqr = (_nodePositions[0] - worldPosition).sqrMagnitude;

        for (int i = 1; i < _nodePositions.Length; i++)
        {
            float sqr = (_nodePositions[i] - worldPosition).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; nearest = i; }
        }

        return nearest;
    }

    public void SeverEdge(int a, int b)
    {
        if (_adjacency.ContainsKey(a)) _adjacency[a].RemoveAll(e => e.toNodeId == b);
        if (_adjacency.ContainsKey(b)) _adjacency[b].RemoveAll(e => e.toNodeId == a);
    }

    public void RestoreEdge(int a, int b)
    {
        if (!_adjacency.ContainsKey(a) || !_adjacency.ContainsKey(b)) return;
        float cost = Vector3.Distance(_nodePositions[a], _nodePositions[b]);
        if (!_adjacency[a].Exists(e => e.toNodeId == b)) _adjacency[a].Add(new GraphEdge(b, cost));
        if (!_adjacency[b].Exists(e => e.toNodeId == a)) _adjacency[b].Add(new GraphEdge(a, cost));
    }
}
