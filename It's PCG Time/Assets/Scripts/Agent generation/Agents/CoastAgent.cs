using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class CoastAgent : Agent
{
    public static int minimumAdjacentWaterTiles;

    private Point attractor, repulsor;
    private int stepsLeftToTake;
    //private float skipMitosis = 0.05f;

    public CoastAgent(Point position, Vector2 direction, int tokens, int stepsToTake = 0) : base(position, direction, tokens)
    {
        colour = Color.blue;
        stepsLeftToTake = stepsToTake;
    }

    public override void Act()
    {
        base.Act();
        //TODO gör att agenterna går en stund innan de börjar agera
        if (tokensLeft >= 2 && manager.CanHaveMoreAgents())//&& !GetProbability(skipMitosis)
            Mitosis();
        else if (stepsLeftToTake <= 0 && !IsOnLand && GetNonLandAdjacentPoints(Point).Count > minimumAdjacentWaterTiles)
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

        float height = (generator.GetLayerHeight(AgentGenerator.LayerType.Grass) + generator.GetLayerHeight(AgentGenerator.LayerType.Mountain)) * Random.Range(0.4f,0.5f);
        generator.SetHeight(bestPoint, height);
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
        if (tokensLeft % 2 != 0)
            tokensLeft++;
        int halfOfTokens = tokensLeft / 2;
        int leftToSpawn = 2;
        List<Point> neighbors;
        if (manager.forceNonOverlap)
            neighbors = GetUnoccupiedNeighbors();
        else
            neighbors = GetAdjacentPoints(Point);


        while (leftToSpawn > 0)
        {
            if (neighbors.Count > 0)
            {
                manager.AddAgent(new CoastAgent(PopRandomNeighbour(), RandomDirection, halfOfTokens));
                leftToSpawn--;
            }
            else
            {
                do
                {
                    Move();
                } while (manager.IsOccupied(Point));
                manager.SetOccupation(Point, true);
                neighbors = GetUnoccupiedNeighbors();
            }
        }

        manager.RemoveAgent(this);

        Point PopRandomNeighbour()
        {
            int randomIndex = Random.Range(0, neighbors.Count - 1);
            var point = neighbors[randomIndex];
            neighbors.RemoveAt(randomIndex);
            return point;
        }
    }

    private List<Point> GetUnoccupiedNeighbors()
    {
        List<Point> neighbors = GetAdjacentPoints(Point);
        for (int i = neighbors.Count - 1; i >= 0; --i)
            if (manager.IsOccupied(neighbors[i]))
                neighbors.RemoveAt(i);
        return neighbors;
    }
}
