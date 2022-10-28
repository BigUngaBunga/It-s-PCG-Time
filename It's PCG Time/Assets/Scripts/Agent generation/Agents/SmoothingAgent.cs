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
}
