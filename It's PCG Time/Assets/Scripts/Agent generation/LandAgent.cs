using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class LandAgent : Agent
{
    protected LandArea area;

    public LandAgent(Point position, Vector2 direction, int tokens, LandArea area) : base(position, direction, tokens)
    {
        this.area = area;
    }

    protected List<Point> GetNeumannNeighborhood(Point point)
    {
        List<Point> neighbors = new List<Point> { new Point(point.X -1, point.Y), new Point(point.X + 1, point.Y),
                                                    new Point(point.X, point.Y -1 ), new Point(point.X, point.Y + 1)};

        for (int i = neighbors.Count; i >= 0; i--)
            if (!PointWithinBounds(neighbors[i]))
                neighbors.RemoveAt(i);
        return neighbors;
    }


}
