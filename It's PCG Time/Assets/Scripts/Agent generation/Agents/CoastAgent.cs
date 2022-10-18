using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class CoastAgent : Agent
{
    public CoastAgent(Point position, Vector2 direction, int tokens) : base(position, direction, tokens)
    {
        colour = Color.blue;
    }

    public override void Act(float[,] heightMap)
    {
        base.Act(heightMap);
        Move();
    }

    protected override void EditMap(float[,] heightMap)
    {
        base.EditMap(heightMap);
        heightMap[Point.X, Point.Y]++;
    }
}
