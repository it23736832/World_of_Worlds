using UnityEngine;

public class AStarNode
{
    public readonly bool walkable;
    public readonly Vector3 worldPosition;
    public readonly int gridX;
    public readonly int gridY;

    public int gCost;
    public int hCost;
    public AStarNode parent;

    public int FCost => gCost + hCost;

    public AStarNode(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
    }
}
