using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class CoastAgent : Agent
{
    private Point attractor, repulsor;
    private int stepsLeftToTake;
    private float skipMitosis = 0.05f;

    public CoastAgent(Point position, Vector2 direction, int tokens, int stepsToTake) : base(position, direction, tokens)
    {
        colour = Color.blue;
        stepsLeftToTake = stepsToTake;
    }

    public override void Act()
    {
        base.Act();
        //TODO gör att agenterna går en stund innan de börjar agera
        if (tokensLeft >= 2 && generator.CanHaveMoreAgents() && !GetProbability(skipMitosis))
            Mitosis();
        else if (stepsLeftToTake <= 0 && !IsOnLand && GetNonLandAdjacentPoints(Point).Count >= 3)
            EditMap();
        else
        {
            --stepsLeftToTake;
            Move();
        }
    }

    protected override void EditMap()
    {
        base.EditMap();
        int maxDistance = 4;
        attractor = GetRandomNearbyPoint(maxDistance);
        repulsor = GetRandomNearbyPoint(maxDistance, attractor);
        EditBestPoint(GetNonLandAdjacentPoints(Point));
        PickRandomDirection();
    }
    
    private void EditBestPoint(List<Point> points)
    {
        float currentScore, bestScore = float.MinValue;
        Point bestPoint;
        for (int i = 0; i < points.Count; i++)
        {
            currentScore = EvaluateScore(points[i]);
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                bestPoint = points[i];
            }
        }
        generator.HeightMap[bestPoint.X, bestPoint.Y] = generator.GetLayerHeight(AgentGenerator.LayerType.Grass);
        --tokensLeft;
        float EvaluateScore(Point position) => Distance(position, attractor) - Distance(position, repulsor);
    }

    private List<Point> GetNonLandAdjacentPoints(Point point)
    {
        var adjecentPoints = GetAdjacentPoints(point);
        for (int i = adjecentPoints.Count - 1; i >= 0; --i)
            if (generator.IsLand(adjecentPoints[i]))
                adjecentPoints.RemoveAt(i);
        return adjecentPoints;
    }

    private Point GetRandomNearbyPoint(int maxDistance, params Point[] excludedPoints)
    {
        Point randomPoint;
        do//TODO hindra do-while loopen från att köra för evigt eller krascha
        {
            randomPoint = new Point(Random.Range(Point.X - maxDistance, Point.X + maxDistance), Random.Range(Point.Y - maxDistance, Point.Y + maxDistance));
        } while (!PointWithinBounds(randomPoint) && CollidesWithExcluded());
        return randomPoint;

        bool CollidesWithExcluded()
        {
            foreach (var point in excludedPoints)
                if (randomPoint == point)
                    return true;
            return false;
        }
    }

    private void Mitosis()
    {
        int halfOfTokens = tokensLeft / 2;
        tokensLeft -= halfOfTokens;

        CoastAgent child = new CoastAgent(generator.GetPointOnEdge() , RandomDirection, halfOfTokens, stepsLeftToTake);
        generator.AddAgent(child);
    }
}
