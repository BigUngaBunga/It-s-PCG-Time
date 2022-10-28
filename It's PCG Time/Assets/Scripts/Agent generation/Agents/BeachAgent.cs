using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BeachAgent : LandAgent
{
    List<Point> visitedBefore; //TODO lägg till platser som agenten går till för att förhindra att den gå i cirklar

    public BeachAgent(Point position, Vector2 direction, int tokens, LandArea area) : base(position, direction, tokens, area)
    {
        colour = new UnityEngine.Color(1, 0.8f, 0.1f); //Yellow? Maybe
        PickRandomDirection();
        GoToRandomCoast();
        visitedBefore = new List<Point>();
    }

    public override void Act()
    {
        base.Act();
        //GoToRandomCoast();
        FollowCoast();
    }

    protected override void EditMap()
    {
        base.EditMap();
    }

    private void GoToRandomCoast() => position = ToVector2(area.GetRandomCoast());

    private void FollowCoast()
    {
        var adjacentCoast = area.GetAdjacentCoast(Point);
        float closestAngle = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < adjacentCoast.Count; i++)
        {
            if (bestIndex < 0)
            {
                bestIndex = i;
                closestAngle = GetAngle(adjacentCoast[i]);
            }
            else if (closestAngle > GetAngle(adjacentCoast[i]))
            {
                bestIndex = i;
                closestAngle = GetAngle(adjacentCoast[i]);
            }
        }
        Direction = GetDirection(adjacentCoast[bestIndex]);

        Move();

        Vector2 GetDirection(Point target) => new Vector2(target.X - Point.X, target.Y - Point.Y).normalized;
        float GetAngle(Point target) => Vector2.Angle(GetDirection(target), Direction);
    }

    private void MakeCoastalBeach()
    {

    }

    private void MakeInlandBeach()
    {

    }
}
