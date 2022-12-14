using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

public class Agent
{
    protected static AgentGenerator generator;
    protected static AgentManager manager;

    public Color colour = Color.red;
    public bool IsActive => tokensLeft > 0;

    public Point Point => new Point((int)position.x, (int)position.y);
    protected Vector2 position;
    private Vector2 direction;
    protected Vector2 Direction
    {
        get { return direction; }
        set { direction = value.normalized; }
    }
    protected Vector2 RandomDirection => new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
    protected Point bounds = new Point(generator.HeightMap.GetLength(0) - 1, generator.HeightMap.GetLength(1) - 1);
    protected int tokensLeft;

    protected bool IsOnLand => generator.IsLand(Point);

    public Agent(Point position, Vector2 direction, int tokens)
    {
        this.position = new Vector2(position.X, position.Y);
        Direction = direction;
        tokensLeft = tokens;
    }

    public static void UpdateGenerator(AgentGenerator newGenerator) => generator = newGenerator;
    public static void UpdateManager(AgentManager newManager) => manager = newManager;

    protected static Point ToPoint(Vector2 position) => new Point((int)position.x, (int)position.y);
    protected static Vector2 ToVector2(Point point) => new Vector2(point.X, point.Y);
    public static float Distance(Point a, Point b) => (ToVector2(a) - ToVector2(b)).magnitude;
    protected void PickRandomDirection() => Direction = RandomDirection;
    protected bool GetProbability(float probability) => Random.value <= probability;
    public Vector3 GetPosition(float[,] heightMap, float addedHeight) => new Vector3(position.x, heightMap[Point.X, Point.Y] + addedHeight, position.y);

    public virtual void Act()
    {
        if (tokensLeft <= 0)
        {
            manager.RemoveAgent(this);
            return;
        }
            
    }

    protected virtual void EditMap() => tokensLeft--;

    protected virtual void Move()
    {
        position += Direction;
        if (!AgentIsWithinBounds())
            PickRandomDirection();
    }

    protected virtual void MoveTo(Point point)
    {
        position = ToVector2(point);
        if (!AgentIsWithinBounds())
            PickRandomDirection();
    }

    private bool AgentIsWithinBounds()
    {
        bool withinBounds = PointWithinBounds(position);
        if (position.x < 0)
            position.x = 0;
        else if (position.x > bounds.X)
            position.x = bounds.X;

        if (position.y < 0)
            position.y = 0;
        else if (position.y > bounds.Y)
            position.y = bounds.Y;

        return withinBounds;
    }

    protected bool PointWithinBounds(Vector2 point) => !(point.x < 0 || point.x > bounds.X || point.y < 0 || point.y > bounds.Y);
    protected bool PointWithinBounds(Point point) => PointWithinBounds(new Vector2(point.X, point.Y));

    protected List<Point> GetAdjacentPoints(Point start, int distance = 1, bool round = false)
    {
        List<Point> validPoints = new List<Point>();
        for (int x = -distance; x < (1 + distance); x++)
            for (int y = -distance; y < (1 + distance); y++)
            {
                Point point = new Point(x + start.X, y + start.Y);
                if (PointWithinBounds(point))
                {
                    if (round && Distance(start, point) > distance)
                        continue;
                    validPoints.Add(point);
                }
            }
        return validPoints;
    }

    protected List<Point> GetAdjacentNeumann(Point start, int distance = 1)
    {
        List<Point> adjacent = new List<Point>();

        for (int i = -distance; i < (1 + distance); i++)
        {
            if (i == 0)
                continue;
            Point pointX = new Point(start.X + distance, start.Y);
            Point pointY = new Point(start.X, start.Y + distance);
            adjacent.Add(pointX);
            adjacent.Add(pointY);
        }

        for (int i = adjacent.Count -1 ; i >= 0; --i)
            if (!PointWithinBounds(adjacent[i]))
                adjacent.RemoveAt(i);

        return adjacent;
    }

    protected Point GetRandomPoint(List<Point> points)
    {
        if (points.Count >= 1)
            return points[Random.Range(0, points.Count - 1)];
        return Point;

    }
    protected Point GetRandomAdjacentPoint(Point start) => GetRandomPoint(GetAdjacentPoints(start));
}
