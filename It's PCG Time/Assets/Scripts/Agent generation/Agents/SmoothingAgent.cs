using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class SmoothingAgent : LandAgent
{
    private Point startingPoint;
    private int coverage;

    public SmoothingAgent(Point position, Vector2 direction, int tokens, LandArea area) : base(position, direction, tokens, area)
    {
        colour = Color.yellow;
        startingPoint = area.GetRandomPoint();
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
        int smoothDistance = 1;
        var neighbors = GetAdjacentNeumann(Point, smoothDistance);

        int weight = 4 * smoothDistance;
        float height = generator.GetHeight(Point) * weight;
        foreach (var neighbor in neighbors)
        {
            ++weight;
            height += generator.GetHeight(neighbor);
        }
        height /= weight;
        generator.SetHeight(Point, height);
    }
}
