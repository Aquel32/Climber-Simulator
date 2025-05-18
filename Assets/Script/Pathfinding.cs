using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    private bool[,] walkableGrid;
    private int width, height;

    public Pathfinding(bool[,] walkableGrid)
    {
        this.walkableGrid = walkableGrid;
        width = walkableGrid.GetLength(0);
        height = walkableGrid.GetLength(1);
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        var openSet = new PriorityQueue<Node>();
        var allNodes = new Dictionary<Vector2Int, Node>();

        Node startNode = new Node(start, 0, GetHeuristic(start, target), null);
        openSet.Enqueue(startNode);
        allNodes[start] = startNode;

        while (openSet.Count > 0)
        {
            Node current = openSet.Dequeue();

            if (current.Position == target)
                return RetracePath(current);

            foreach (Vector2Int neighborPos in GetNeighbors(current.Position))
            {
                if (!IsWalkable(neighborPos))
                    continue;

                float tentativeG = current.G + 1; // Assuming uniform cost between neighbors

                if (allNodes.TryGetValue(neighborPos, out Node neighbor))
                {
                    if (tentativeG < neighbor.G)
                    {
                        neighbor.G = tentativeG;
                        neighbor.Parent = current;
                        openSet.UpdatePriority(neighbor);
                    }
                }
                else
                {
                    neighbor = new Node(neighborPos, tentativeG, GetHeuristic(neighborPos, target), current);
                    allNodes[neighborPos] = neighbor;
                    openSet.Enqueue(neighbor);
                }
            }
        }

        // No path found
        return null;
    }

    private List<Vector2Int> RetracePath(Node endNode)
    {
        var path = new List<Vector2Int>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }
        path.Reverse();
        return path;
    }

    private float GetHeuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
    }

    private bool IsWalkable(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
            return false;
        return walkableGrid[pos.x, pos.y];
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        var neighbors = new List<Vector2Int>();

        Vector2Int[] possibleMoves = {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1),
            // optionally diagonals
            new Vector2Int(1,1),
            new Vector2Int(1,-1),
            new Vector2Int(-1,1),
            new Vector2Int(-1,-1),
        };

        foreach (var move in possibleMoves)
        {
            Vector2Int neighborPos = pos + move;
            if (neighborPos.x >= 0 && neighborPos.x < width && neighborPos.y >= 0 && neighborPos.y < height)
            {
                neighbors.Add(neighborPos);
            }
        }

        return neighbors;
    }

    private class Node : System.IComparable<Node>
    {
        public Vector2Int Position;
        public float G; // cost from start
        public float H; // heuristic cost to target
        public Node Parent;

        public float F => G + H;

        public Node(Vector2Int pos, float g, float h, Node parent)
        {
            Position = pos;
            G = g;
            H = h;
            Parent = parent;
        }

        public int CompareTo(Node other)
        {
            return F.CompareTo(other.F);
        }
    }

    // Priority queue implementation for openSet - you can use your own or use a simple sorted list
    private class PriorityQueue<T> where T : System.IComparable<T>
    {
        private List<T> data = new List<T>();

        public int Count => data.Count;

        public void Enqueue(T item)
        {
            data.Add(item);
            data.Sort();
        }

        public T Dequeue()
        {
            if (data.Count == 0)
                throw new System.InvalidOperationException("Empty queue");
            T item = data[0];
            data.RemoveAt(0);
            return item;
        }

        public void UpdatePriority(T item)
        {
            data.Sort();
        }
    }
}