using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sweepline : Singleton<Sweepline>
{
    public List<Node> nodes;

    float closestPair(Node[] points, int n) 
    {
        nodes = new List<Node>(points);
        nodes.Sort(new NodeCompare());

        List<Node> box = new List<Node>() { nodes[0] };

        float best = Mathf.Infinity;
        int left = 0;
        for (int i = 1; i < n; i++)
        {
            while (left < i && nodes[i].point.x - nodes[left].point.x > best)
            {
                box.Remove(nodes[left++]);
            }

            Range xRange = new Range(nodes[i].point.x - best, nodes[i].point.x);
            Range yRange = new Range(nodes[i].point.y - best, nodes[i].point.y + best);

            foreach (Node item in box)
            {
                if (item.point.x > xRange.max) break;

                if (xRange.Contains(item.point.x) && yRange.Contains(item.point.y))
                {
                    float distance = Vector2.Distance(nodes[i].point, item.point);
                    if (distance < best) best = distance;
                }
            }

            box.Add(nodes[i]);
        }

        return best;
    }

    Vector3[] GetIntersections(Line[] lines) 
    {
        List<Event> events = new List<Event>();
        foreach (Line line in lines)
        {
            events.Add(new Event(line.a, PointType.START, line));
            events.Add(new Event(line.b, PointType.END, line));
        }
        events.Sort(new EventCompare());

        //Should be sorted from top to bottom by start point
        List<Line> SL = new List<Line>();

        List<Vector3> intersections = new List<Vector3>();

        Event currentEvent;
        while (events.Count > 0)
        {
            currentEvent = events[0];

            switch (currentEvent.type)
            {
                case PointType.START:
                    Line current = currentEvent.lines[0];
                    Line above = null, below = null;

                    //find where this line would fall in the y
                    int i = 0;
                    while (i < SL.Count && current.a.y > SL[i].a.y)
                    {
                        i++;
                    }

                    //Add this line to the correct point in the lines list
                    SL.Insert(i, current);
                    //we can grab the lines above and below this by simply getting the i - and + this line
                    if (i - 1 >= 0) above = SL[i - 1];
                    if (i + 1 < SL.Count) below = SL[i + 1];

                    Vector3 intersect = Vector3.zero;
                    if (above != null && below != null && DoesIntersect(above, below, out intersect))
                    {
                        //remove intersect from events list
                        foreach (Event item in events)
                        {
                            //if it's not an intersection or if it isn't the above/below intersection
                            if (item.type != PointType.INTERSECTION || !(item.HasLine(above) && item.HasLine(below))) continue;
                            
                            events.Remove(item);
                            break;
                        }
                    }

                    if (above != null && DoesIntersect(current, above, out intersect))
                    {
                        events.Add(new Event(intersect, PointType.INTERSECTION));
                    }

                    if (below != null && DoesIntersect(current, below, out intersect))
                    {
                        events.Add(new Event(intersect, PointType.INTERSECTION));
                    }
                    break;

                case PointType.INTERSECTION:
                    // Sort the intersections
                    intersections.Add(currentEvent.point);
                    break;

                case PointType.END:
                    // Remove this line from the list of lines since it's over now.
                    SL.Remove(currentEvent.lines[0]);
                    break;
            }

            events.RemoveAt(0);

            //sort the events again? To allow for those pesky intersections to be handled
            events.Sort(new EventCompare());
        }

        return intersections.ToArray();
    }

    public bool DoesIntersect(Line lineA, Line lineB, out Vector3 intersection)
    {
        intersection = lineA.a;
        if (IsParallel(lineA.Normal(), lineB.Normal())) return false;
        if (IsOrthogonal(lineA.a - lineB.a, lineA.Normal())) return false;

        float A, B, C, D;
        A = lineA.Normal().x;
        B = lineA.Normal().y;
        C = lineB.Normal().x;
        D = lineB.Normal().y;

        float k1 = (A * lineA.a.x) + (B + lineA.a.y);
        float k2 = (C * lineB.a.x) + (D + lineB.a.y);


        float x_intersect = (D * k1 - B * k2) / (A * D - B * C);
        float y_intersect = (-C * k1 + A * k2) / (A * D - B * C);
        intersection = new Vector2(x_intersect, y_intersect);

        if (!IsBetween(lineA.a, lineA.b, intersection) || !IsBetween(lineB.a, lineB.b, intersection)) return false;

        return true;
    }

    //Are 2 vectors parallel?
    bool IsParallel(Vector2 v1, Vector2 v2)
    {
        //2 vectors are parallel if the angle between the vectors are 0 or 180 degrees
        if (Vector2.Angle(v1, v2) == 0f || Vector2.Angle(v1, v2) == 180f)
        {
            return true;
        }

        return false;
    }

    //Are 2 vectors orthogonal?
    bool IsOrthogonal(Vector2 v1, Vector2 v2)
    {
        //2 vectors are orthogonal is the dot product is 0
        //We have to check if close to 0 because of floating numbers
        if (Mathf.Abs(Vector2.Dot(v1, v2)) < 0.000001f)
        {
            return true;
        }

        return false;
    }

    //Is a point c between 2 other points a and b?
    bool IsBetween(Vector2 a, Vector2 b, Vector2 c)
    {
        bool isBetween = false;

        //Entire line segment
        Vector2 ab = b - a;
        //The intersection and the first point
        Vector2 ac = c - a;

        //Need to check 2 things: 
        //1. If the vectors are pointing in the same direction = if the dot product is positive
        //2. If the length of the vector between the intersection and the first point is smaller than the entire line
        if (Vector2.Dot(ab, ac) > 0f && ab.sqrMagnitude >= ac.sqrMagnitude)
        {
            isBetween = true;
        }

        return isBetween;
    }
}

public class Line
{
    public Vector3 a;
    public Vector3 b;

    public Line(Vector3 start, Vector3 end) 
    {
        a = start;
        b = end;
    }

    public Vector2 Direction() 
    {
        return (b - a).normalized;
    }
    
    public Vector2 Normal() 
    {
        return new Vector2(-Direction().y, Direction().x); 
    }
}

public class Event 
{
    public Vector3 point;
    public PointType type;
    public Line[] lines;

    public Event(Vector3 p, PointType t, Line l = null) 
    {
        point = p;
        type = t;
        lines = new Line[1] { l };
    }

    public Event(Vector3 p, Line one, Line two)
    {
        point = p;
        type = PointType.INTERSECTION;
        lines = new Line[2] { one, two };
    }

    public bool HasLine(Line test) 
    {
        foreach (Line line in lines)
        {
            if (test == line) return true;
        }

        return false;
    }
}

public enum PointType 
{
    START,
    INTERSECTION,
    END
}

public class NodeCompare : IComparer<Node>
{
    //returns leftmost or topmost if they are the same x
    public int Compare(Node first, Node second)
    {
        if (first.point == second.point) return 0;

        if (first.point.x != second.point.x)
        {
            return first.point.x < second.point.x ? 1 : -1;
        }
        else
        {
            return first.point.y > second.point.y ? 1 : -1;
        }

    }
}

public class VectorCompare : IComparer<Vector3>
{
    //returns leftmost or topmost if they are the same x
    public int Compare(Vector3 first, Vector3 second)
    {
        if (first == second) return 0;

        if (first.x != second.x)
        {
            return first.x < second.x ? 1 : -1;
        }
        else
        {
            return first.y > second.y ? 1 : -1;
        }

    }
}

public class EventCompare : IComparer<Event>
{
    //returns which line starts most to the left
    public int Compare(Event first, Event second)
    {
        if (first.point == second.point) return 0;

        if (first.point.x != second.point.x)
        {
            return first.point.x < second.point.x ? 1 : -1;
        }
        else
        {
            return first.point.y > second.point.y ? 1 : -1;
        }

    }
}
