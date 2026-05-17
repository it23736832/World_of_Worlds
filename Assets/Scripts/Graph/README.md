# Graph Scripts

World representation consumed by the search algorithms.

- **NavMesh-to-graph extraction** — samples Unity NavMesh into nodes and builds adjacency list
- **Adjacency list** — `Dictionary<int, List<Edge>>` storing weighted, directed edges
- **Edge severing** — removes or restores edges when barricades are placed or doors are closed
