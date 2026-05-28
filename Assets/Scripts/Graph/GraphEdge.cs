public class GraphEdge
{
    public int toNodeId;
    public float cost;

    public GraphEdge(int toNodeId, float cost)
    {
        this.toNodeId = toNodeId;
        this.cost = cost;
    }
}
