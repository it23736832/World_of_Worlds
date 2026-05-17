---
name: pathfinding
description: Use when implementing A*, BFS, UCS, or any graph search algorithm.
Covers priority queues, heuristic design, graph structures, and complexity analysis.
---

# Pathfinding Implementation Guide

## Graph Structure
- Nodes: extracted from NavMesh vertices or sampled walkable positions
- Edges: weighted by Euclidean distance between connected nodes
- Adjacency list: Dictionary<int, List<(int neighbor, float weight)>>

## A* Algorithm
- Priority queue: use SortedSet or custom min-heap (NOT List.Sort())
- f(n) = g(n) + h(n)
- Heuristic must be admissible (never overestimate) and consistent
- For 3D NavMesh: Euclidean distance is admissible

## BFS (unweighted) / UCS (weighted, no heuristic)
- BFS: Queue<Node>, explores by depth
- UCS: PriorityQueue<Node> ordered by g(n) only
- UCS is A* with h(n) = 0

## Dynamic Recalculation
- When barricade placed: remove edges from adjacency list
- Trigger re-pathfind for all affected agents
- Guard against infinite loops: if no path exists, agent stops

## Complexity (for viva)
- A*: O(b^d) worst case, much better with good heuristic
- BFS: O(V + E) time, O(V) space
- UCS: O(V + E log V) with binary heap
