using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MoveablePlatform : MonoBehaviour
{
    public enum PlatformType
    {
        Ascending, PingPong, StopOnEnd
    }

    [SerializeField] private PlatformType platformType; // Type of platform.
    public List<Vector2> points = new List<Vector2>(); // Path for platform to move around.
    [SerializeField] protected float moveSpeed = 3f; // Platform's speed.
    [SerializeField] private AnimationCurve platformCurve; // Platform movement speed curve.

    private bool reachPoint = true; // Determine if Platform has reach the next point.
    private bool moving = false; // Determine if Platform is moving.
    private bool pingPongGoingDown; // Determine if Platform Ping-Pong reverse or not.
    private int currentPoint = 0; // Determine current Platform's point.
    private int nextPoint; // Determine next Platform's point.

    [Header("Debugging")]
    public bool showPoints = true; // Debugging to show Points or not.
    public float pointSize = 0.5f; // Debugging to change point's size.
    public float lineThinkness = 3f; // Debugging to change line's thickness.
    public bool platformAtFirstPoint = false; // Debugging to move platform to first point.

    private void Start()
    {
        transform.position = points[currentPoint];
    }

    private void FixedUpdate()
    {
        FindThePoint();
        MoveToPosition();
    }

    // Function to find next point.
    protected virtual void FindThePoint()
    {
        for (int i = currentPoint; i < points.Count; i++)
        {
            // Check if platform reaches next point, find new one.
            if (reachPoint)
            {
                reachPoint = false;

                // Check if not Ping-Pong yet.
                if (!pingPongGoingDown)
                {
                    currentPoint = i;
                }
                else
                {
                    currentPoint = i - 2;
                }

                // Ascending - Keep moving toward next one.
                if (platformType == PlatformType.Ascending)
                {
                    nextPoint = i + 1;
                    
                    // When reaches the end, set next point as beginning point.
                    if (nextPoint == points.Count)
                    {
                        nextPoint = 0;
                    }
                }

                // Ping-Pong - Go forward and backward when reaches end of each side.
                if (platformType == PlatformType.PingPong)
                {
                    // If go normal, get next point.
                    if (!pingPongGoingDown)
                    {
                        nextPoint = i + 1;

                        // If reaches end, go reverse.
                        if (nextPoint == points.Count)
                        {
                            nextPoint = i - 2;
                            currentPoint--;
                            pingPongGoingDown = true;
                        }
                    }

                    // If go reverse, get reverse point.
                    if (pingPongGoingDown)
                    {
                        nextPoint = i - 1;
                        
                        // If reaches beginning, go normal.
                        if (nextPoint == 0)
                        {
                            nextPoint = 0;
                            currentPoint = -1;
                            pingPongGoingDown = false;
                        }
                    }
                }

                // Stop On End - Go only one-time and stop when reaches the end.
                if (platformType == PlatformType.StopOnEnd)
                {
                    nextPoint = i + 1;

                    // If reaches the end, stop.
                    if (nextPoint == points.Count)
                    {
                        return;
                    }
                }

                moving = true;
            }
        }
    }

    // Function to move Platform to certain position.
    protected virtual void MoveToPosition()
    {
        // If it's currently moving, move toward nextPoint.
        if (moving)
        {
            // If reaches that point, make it stop.
            if ((Vector2) transform.position == points[nextPoint])
            {
                moving = false;
                reachPoint = true;
                currentPoint++;
            }

            // If reaches that point and it's at the end, reset to beginning point.
            if ((Vector2) transform.position == points[nextPoint] && currentPoint == points.Count)
            {
                currentPoint = 0;
            }

            float speed = moveSpeed;
            bool useCurve = false;

            switch (platformType)
            {
                case PlatformType.PingPong:
                // Check if it's about to reach the end point.
                if ((!pingPongGoingDown && nextPoint == points.Count) || (pingPongGoingDown && nextPoint == 0))
                {
                    useCurve = true;
                }
                break;
            }

            if (useCurve)
            {
                float distance = Vector2.Distance(points[currentPoint], points[nextPoint]);
                speed = moveSpeed * platformCurve.Evaluate(distance);
            }

            transform.position = Vector3.MoveTowards(transform.position, points[nextPoint], speed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if player touches and is above the Platform.
        if (collision.gameObject.Equals(Player.instance.gameObject) && collision.gameObject.GetComponent<Collider2D>().bounds.min.y > GetComponent<Collider2D>().bounds.center.y)
        {
            collision.transform.parent = transform;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Check if player jump or walk away.
        if (collision.gameObject.Equals(Player.instance.gameObject))
        {
            collision.transform.parent = null;
        }
    }

    // Function to get Platform type.
    public PlatformType GetPlatformType()
    {
        return platformType;
    }

    protected virtual void OnDrawGizmos()
    {
        // If platformAtFirstPoint, move platform to start position.
        if(platformAtFirstPoint && !Application.isPlaying)
        {
            transform.position = points[0];
        }
    }

    public void SetPosition(Vector2 down, Vector2 up) {
        points.Clear();
        points.Add(down);
        points.Add(up); 
    }

}