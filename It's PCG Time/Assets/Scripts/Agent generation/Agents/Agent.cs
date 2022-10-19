using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using Color = UnityEngine.Color;

public class Agent
{
    protected static AgentGenerator generator;

    public Color colour = Color.red;
    public bool IsActive => tokensLeft > 0;

    protected Point Point => new Point((int)position.x, (int)position.y);
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

    public Agent(Point position, Vector2 direction, int tokens)
    {
        this.position = new Vector2(position.X, position.Y);
        Direction = direction;
        tokensLeft = tokens;
    }

    public static void UpdateGenerator(AgentGenerator newGenerator) => generator = newGenerator;


    protected static Vector2 ToVector2(Point point) => new Vector2(point.X, point.Y);
    protected static float Distance(Point a, Point b) => (ToVector2(a) - ToVector2(b)).magnitude;

    public virtual void Act()
    {
        if (tokensLeft <= 0)
        {
            generator.RemoveAgent(this);
            return;
        }
            
    }

    protected virtual void EditMap()
    {
        tokensLeft--;
    }

    protected virtual void Move()
    {
        position += Direction;
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

    protected List<Point> GetAdjacentPoints(Point start)
    {
        List<Point> validPoints = new List<Point>();
        for (int x = -1; x < 2; x++)
            for (int y = -1; y < 2; y++)
            {
                Point point = new Point(x + start.X, y + start.Y);
                if (PointWithinBounds(point))
                    validPoints.Add(point);
            }
        return validPoints;
    }

    private void PickRandomDirection() => Direction = RandomDirection;

    public Vector3 GetPosition(float[,] heightMap, float addedHeight) => new Vector3(position.x, heightMap[Point.X, Point.Y] + addedHeight, position.y);
}
