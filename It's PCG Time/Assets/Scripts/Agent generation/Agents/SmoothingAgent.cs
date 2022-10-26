using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class SmoothingAgent : Agent
{
    private Point startingPoint;
    private LandArea area;
    private int coverage;

    public SmoothingAgent(Point position, Vector2 direction, int tokens, LandArea area) : base(position, direction, tokens)
    {
        colour = Color.yellow;
        startingPoint = area.GetRandomPoint();
        this.area = area;
        this.position = ToVector2(startingPoint);
        coverage = manager.smoothDistance;
    }

    public override void Act()
    {
        base.Act();
        var adjacent = area.GetAdjacentPoints(startingPoint, coverage);
        position = ToVector2(GetRandomPoint(adjacent));
        SmoothenPoint();
    }

    

    private void SmoothenPoint()
    {
        tokensLeft--;
        var neighbors = GetAdjacentPoints(Point);

        int weight = 3;
        float height = generator.GetHeight(Point) * 3;
        foreach (var neighbor in neighbors)
        {
            ++weight;
            height += generator.GetHeight(neighbor);
        }
        height /= weight;
        generator.SetHeight(Point, height);
    }

    private List<Point> GetNeumannNeighborhood(Point point)
    {
        List<Point> neighbors = new List<Point> { new Point(point.X -1, point.Y), new Point(point.X + 1, point.Y),
                                                    new Point(point.X, point.Y -1 ), new Point(point.X, point.Y + 1)};
        
        for (int i = neighbors.Count; i >= 0; i--)
            if (!PointWithinBounds(neighbors[i]))
                neighbors.RemoveAt(i);
        return neighbors;
    }
}
