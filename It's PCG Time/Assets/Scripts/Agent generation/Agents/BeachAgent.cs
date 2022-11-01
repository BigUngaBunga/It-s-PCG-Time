using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BeachAgent : LandAgent
{
    private int searchDistance;
    private float targetHeight;
    private List<Point> visitedBefore;
    private Point previousPosition;

    public BeachAgent(Point position, Vector2 direction, int tokens, LandArea area, float targetHeightFactor, int searchDistance) : base(position, direction, tokens, area)
    {
        colour = new UnityEngine.Color(1, 0.8f, 0.1f); //Yellow? Maybe
        PickRandomDirection();
        GoToRandomCoast();
        visitedBefore = new List<Point>();
        this.searchDistance = searchDistance;
        targetHeight = generator.GetLayerHeight(AgentGenerator.LayerType.Beach) * targetHeightFactor;
        previousPosition = Point;
    }

    public override void Act()
    {
        base.Act();
        Move();
        EditMap();
    }

    protected override void EditMap()
    {
        var adjacent = area.GetAdjacentPoints(Point, searchDistance);
        float weight = 3;
        foreach (var point in adjacent)
        {
            base.EditMap();
            float height = generator.GetHeight(point) * weight;
            height += targetHeight;
            generator.SetHeight(point, height / (weight + 1));
        }
    }

    private void GoToRandomCoast() => position = ToVector2(area.GetRandomCoast());

    protected override void Move()
    {
        var adjacentCoast = area.GetAdjacentCoast(Point);
        for (int i = adjacentCoast.Count - 1; i >= 0; i--)
            if (adjacentCoast[i] == previousPosition)
                adjacentCoast.RemoveAt(i);

        int index = Random.Range(0, adjacentCoast.Count - 1);
        if (adjacentCoast.Count <= 0 || index < 0)
        {
            GoToRandomCoast();
            return;
        }
        MoveTo(adjacentCoast[index]);
        previousPosition = Point;

        if (visitedBefore.Contains(Point))
        {
            visitedBefore.Clear();
            GoToRandomCoast();
        }
        visitedBefore.Add(Point);
    }

    private float GetAverageHeight(List<Point> points)
    {
        float height = 0;
        foreach (var point in points)
            height += generator.GetHeight(point);
        return height /= points.Count;
    }
}
