using System.Collections.Generic;
using UnityEngine;

public class AStarPathfind
{
    public AStarGrid grid;

    public AStarPathfind(AStarGrid grid)
    {
        this.grid = grid;
    }

    private float Heuristic(AStarNode a, AStarNode b, bool diagonal = false)
    {
        float dx = Mathf.Abs(a.xPos - b.xPos);
        float dy = Mathf.Abs(a.yPos - b.yPos);

        if (!diagonal)
        {
            return dx + dy;
        }

        return Mathf.Max(dx, dy);
    }

    public List<AStarNode> CreatePath(AStarNode start, AStarNode end, bool diagonal = false)
    {
        if (start == null || end == null)
        {
            return null;
        }

        grid.ResetNode();

        List<AStarNode> openSet = new List<AStarNode>();
        HashSet<AStarNode> openLookup = new HashSet<AStarNode>();
        HashSet<AStarNode> closedSet = new HashSet<AStarNode>();

        start.gCost = 0;
        start.hCost = Heuristic(start, end, diagonal);
        openSet.Add(start);
        openLookup.Add(start);

        while (openSet.Count > 0)
        {
            int shortest = 0;
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < openSet[shortest].fCost)
                {
                    shortest = i;
                }
            }

            AStarNode currentNode = openSet[shortest];
            openSet.RemoveAt(shortest);
            openLookup.Remove(currentNode);

            if (currentNode == end)
            {
                List<AStarNode> path = new List<AStarNode>();
                AStarNode tempNode = end;
                while (tempNode != null)
                {
                    path.Add(tempNode);
                    tempNode = tempNode.parent;
                }

                path.Reverse();
                return path;
            }

            closedSet.Add(currentNode);

            List<AStarNode> neighbors = grid.GetNeighborNodes(currentNode, diagonal);
            for (int i = 0; i < neighbors.Count; i++)
            {
                AStarNode neighbor = neighbors[i];
                if (closedSet.Contains(neighbor) || !neighbor.isWalkable)
                {
                    continue;
                }

                float gCost = currentNode.gCost + Heuristic(currentNode, neighbor, diagonal);
                if (gCost >= neighbor.gCost)
                {
                    continue;
                }

                neighbor.parent = currentNode;
                neighbor.gCost = gCost;
                neighbor.hCost = Heuristic(neighbor, end, diagonal);

                if (!openLookup.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                    openLookup.Add(neighbor);
                }
            }
        }

        return null;
    }

    public List<AStarNode> CreatePath(Vector3Int start, Vector3Int end, bool diagonal)
    {
        AStarNode startNode = grid.GetNodeFromWorld(start);
        AStarNode endNode = grid.GetNodeFromWorld(end);
        return CreatePath(startNode, endNode, diagonal);
    }
}