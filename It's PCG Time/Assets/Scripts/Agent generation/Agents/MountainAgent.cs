using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Color = UnityEngine.Color;

public class MountainAgent : LandAgent
{
    private float mountainWidth;
    private Stack<Point> mountainPeaks;
    private int numberOfPeaks;

    public MountainAgent(Point position, Vector2 direction, int tokens, LandArea area, float mountainWidth, float mountainProbability) : base(position, direction, tokens, area)
    {
        colour = Color.gray;
        this.mountainWidth = mountainWidth + Random.Range(0, 4f);
        while (GetProbability(mountainProbability - (numberOfPeaks * 100f / area.Size)))
            numberOfPeaks++;
        MoveTo(area.GetRandomPoint());
        GetMountainPeaks();
    }

    public override void Act()
    {
        if (numberOfPeaks <= 0 || mountainPeaks.Count <= 0)
        {
            tokensLeft = 0;
            base.Act();
        }
            
        Move();
        EditMap();
    }

    protected override void Move()
    {
        if (mountainPeaks.Count <= 0)
            return;
        Direction = ToVector2(mountainPeaks.Peek()) - ToVector2(Point);
        base.Move();
    }

    protected override void EditMap()
    {
        if (mountainPeaks.Count <= 0 || mountainPeaks.Peek() != Point)
            return;
        numberOfPeaks--;
        mountainPeaks.Pop();
        float targetHeight = 2f;
        targetHeight *= generator.GetLayerHeight(AgentGenerator.LayerType.Mountain);
        var targets = GetAdjacentPoints(Point, Mathf.RoundToInt(mountainWidth));
        foreach (var point in targets)
        {
            generator.SetHeight(point, GetNewHeight(point, targetHeight));
        }
    }

    private float GetNewHeight(Point point, float targetHeight)
    {
        float height = generator.GetHeight(point);
        float distance = Distance(Point, point) / mountainWidth;
        distance = Mathf.Min(1f, distance);
        float heightModifier = 2 * 2 * 2 * distance - 3 * 3 * distance + 1;//2x^3 - 3x^2 +1
        return height + targetHeight * Mathf.Min(heightModifier, 2f);
    }

    private void GetMountainPeaks()
    {
        var acceptablePeaks = new List<Point>();

        int attempts = (int)(Mathf.Log(area.Size) * 5);
        var possiblePeaks = area.GetRandomPoints(attempts);
        int bestPeakIndex = -1;
        float bestDistanceFromWater = 0;

        for (int i = 0; i < possiblePeaks.Count; i++)
        {
            Point currentPoint = possiblePeaks[i];
            float distance = Distance(currentPoint, area.GetClosestWaterTile(currentPoint));
            if (distance > bestDistanceFromWater)
            {
                bestPeakIndex = i;
                bestDistanceFromWater = distance;
            }
            if (distance > mountainWidth)
                acceptablePeaks.Add(currentPoint);
        }

        

        mountainPeaks = GetNonCollidingMountains(acceptablePeaks);
        if (mountainPeaks.Count <= 0)
            mountainPeaks.Push(possiblePeaks[bestPeakIndex]);
    }

    private Stack<Point> GetNonCollidingMountains(List<Point> peaks)
    {
        var result = new Stack<Point>();
        List<Point> currentPeaks = new List<Point>();
        foreach (var peak in peaks)
        {
            bool noCollisions = true;
            for (int i = currentPeaks.Count - 1; i >= 0; i--)
            {
                if (Distance(peak, currentPeaks[i]) <= mountainWidth * 0.8f)
                {
                    noCollisions = false;
                    break;
                }
            }
            if (noCollisions)
                currentPeaks.Add(peak);
        }

        System.Random random = new System.Random(generator.SeedValue);
        currentPeaks = currentPeaks.OrderBy(_ => random.Next()).ToList();
        foreach (var peak in currentPeaks)
            result.Push(peak);

        return result;
    }

}
