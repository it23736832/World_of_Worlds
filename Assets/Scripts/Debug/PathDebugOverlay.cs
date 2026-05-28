using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PathDebugOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshGraph graph;
    [SerializeField] private UCSPathfinder pathfinder;
    [SerializeField] private UCSVillainChase villain;

    private void Start()
    {
        if (graph == null)      graph      = GetComponent<NavMeshGraph>();
        if (pathfinder == null) pathfinder = GetComponent<UCSPathfinder>();
        if (villain == null)    villain    = FindObjectOfType<UCSVillainChase>();
    }

    [Header("Colors")]
    [SerializeField] private Color graphEdgeColor = new Color(0.6f, 0.6f, 0.6f, 0.25f);
    [SerializeField] private Color exploredColor = new Color(0.2f, 0.4f, 1f, 0.8f);
    [SerializeField] private Color frontierColor = new Color(1f, 0.9f, 0f, 0.8f);
    [SerializeField] private Color pathColor = Color.green;

    [Header("Sizes")]
    [SerializeField] private float nodeSphereRadius = 0.25f;
    [SerializeField] private bool drawGraphEdges = true;

    private bool _visible;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[Key.F1].wasPressedThisFrame)
        {
            _visible = !_visible;
            Debug.Log($"[PathDebugOverlay] Debug overlay {(_visible ? "ON" : "OFF")} (F1 to toggle)");
        }
    }

    private void OnDrawGizmos()
    {
        if (!_visible || !Application.isPlaying) return;

        DrawGraphEdges();
        DrawExploredNodes();
        DrawFrontierNodes();
        DrawPath();
    }

    private void DrawGraphEdges()
    {
        if (!drawGraphEdges || graph == null || graph.Adjacency == null || graph.NodePositions == null) return;

        Gizmos.color = graphEdgeColor;
        foreach (var kvp in graph.Adjacency)
        {
            Vector3 from = graph.NodePositions[kvp.Key];
            foreach (GraphEdge edge in kvp.Value)
                Gizmos.DrawLine(from, graph.NodePositions[edge.toNodeId]);
        }
    }

    private void DrawExploredNodes()
    {
        if (pathfinder == null || graph == null || graph.NodePositions == null) return;
        List<int> explored = pathfinder.LastExploredNodes;
        if (explored == null) return;

        Gizmos.color = exploredColor;
        foreach (int id in explored)
        {
            if (id >= 0 && id < graph.NodePositions.Length)
                Gizmos.DrawSphere(graph.NodePositions[id] + Vector3.up * 0.5f, nodeSphereRadius);
        }
    }

    private void DrawFrontierNodes()
    {
        if (pathfinder == null || graph == null || graph.NodePositions == null) return;
        List<int> frontier = pathfinder.LastFrontierNodes;
        if (frontier == null) return;

        Gizmos.color = frontierColor;
        foreach (int id in frontier)
        {
            if (id >= 0 && id < graph.NodePositions.Length)
                Gizmos.DrawSphere(graph.NodePositions[id] + Vector3.up * 0.5f, nodeSphereRadius);
        }
    }

    private void DrawPath()
    {
        if (villain == null) return;
        List<Vector3> path = villain.CurrentPath;
        if (path == null || path.Count < 1) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
            Gizmos.DrawSphere(path[i], nodeSphereRadius * 0.5f);
        }
        Gizmos.DrawSphere(path[path.Count - 1], nodeSphereRadius * 0.5f);
    }
}
