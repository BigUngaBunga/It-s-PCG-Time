using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using Color = UnityEngine.Color;

public class Agent
{
    public static Point boundaries;

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

    private int tokensLeft;

    public Agent(Point position, Vector2 direction, int tokens)
    {
        this.position = new Vector2(position.X, position.Y);
        Direction = direction;
        tokensLeft = tokens;
    }

    public virtual void Act(float[,] heightMap)
    {
        
    }

    protected virtual void EditMap(float[,] heightMap)
    {
        tokensLeft--;
    }

    protected virtual void Move()
    {
        position += Direction;
        if (!IsWithinBounds())
            PickRandomDirection();
    }

    private bool IsWithinBounds()
    {
        bool withinBounds = true;
        if (position.x < 0)
        {
            withinBounds = false;
            position.x = 0;
        }
            
        else if (position.x > boundaries.X)
        {
            withinBounds = false;
            position.x = boundaries.X;
        }
            

        if (position.y < 0)
        {
            withinBounds = false;
            position.y = 0;
        }
            
        else if (position.y > boundaries.Y)
        {
            withinBounds = false;
            position.y = boundaries.Y;
        }
            
        
        return withinBounds;
    }

    private void PickRandomDirection() => Direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));

    public Vector3 GetPosition(float[,] heightMap, float addedHeight) => new Vector3(position.x, heightMap[Point.X, Point.Y] + addedHeight, position.y);
}
