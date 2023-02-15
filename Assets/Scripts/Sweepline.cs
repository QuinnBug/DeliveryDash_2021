using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sweepline : Singleton<Sweepline>
{
    public bool debugMode;
    public float timePerStep;
    public int eventsPerStep;
    [Space]
    public List<Vector3> polyPoints = null;
    public Line[] polyLines = null;
    public List<Vector3> iPoints = null;

    //float closestPair(Node[] points, int n) 
    //{
    //    polyPoints = new List<Vector3>(points);
    //    polyPoints.Sort(new NodeCompare());

    //    List<Node> box = new List<Node>() { polyPoints[0] };

    //    float best = Mathf.Infinity;
    //    int left = 0;
    //    for (int i = 1; i < n; i++)
    //    {
    //        while (left < i && polyPoints[i].point.x - polyPoints[left].point.x > best)
    //        {
    //            box.Remove(polyPoints[left++]);
    //        }

    //        Range xRange = new Range(polyPoints[i].point.x - best, polyPoints[i].point.x);
    //        Range yRange = new Range(polyPoints[i].point.y - best, polyPoints[i].point.y + best);

    //        foreach (Node item in box)
    //        {
    //            if (item.point.x > xRange.max) break;

    //            if (xRange.Contains(item.point.x) && yRange.Contains(item.point.y))
    //            {
    //                float distance = Vector2.Distance(polyPoints[i].point, item.point);
    //                if (distance < best) best = distance;
    //            }
    //        }

    //        box.Add(polyPoints[i]);
    //    }

    //    return best;
    //}

    private void Start()
    {
        polyPoints = null;
        polyLines = null;
        iPoints = null;
    }

    private void Update()
    {
        if (polyPoints != null && iPoints == null)
        {
            polyLines = LinesFromNodes();
            StartCoroutine(GetIntersections(polyLines));
            polyPoints = null;
        }
    }

    private Line[] LinesFromNodes()
    {
        List<Line> lineList = new List<Line>();

        for (int i = 0; i < polyPoints.Count - 1; i++)
        {
            Vector3 a = (polyPoints[i].x <= polyPoints[i + 1].x) ? polyPoints[i] : polyPoints[i + 1];
            Vector3 b = (polyPoints[i].x <= polyPoints[i + 1].x) ? polyPoints[i + 1] : polyPoints[i];

            lineList.Add(new Line(a, b));

            //Debug.DrawLine(a, b, Color.red, 5);
        }

        return lineList.ToArray();
    }

    IEnumerator GetIntersections(Line[] lines) 
    {
        int counter = 0;
        List<Event> events = new List<Event>();
        foreach (Line line in lines)
        {
            events.Add(new Event(line.a, PointType.START, line));
            events.Add(new Event(line.b, PointType.END, line));
        }
        events.Sort(new EventCompare());
        Debug.Log(events.Count);


        //Should be sorted from top to bottom by start point
        List<Line> SL = new List<Line>();

        Event currentEvent;
        while (events.Count > 0)
        {
            counter++;
            currentEvent = events[0];

            if (currentEvent == null) 
            {
                Debug.Log("NULL EVENT");
                events.RemoveAt(0);
                continue;
            }

            switch (currentEvent.type)
            {
                case PointType.START:
                    Line current = currentEvent.lines[0];
                    Line above = null, below = null;

                    //find where this line would fall in the y
                    int i = 0;
                    while (i < SL.Count && current.a.z > SL[i].a.z)
                    {
                        i++;
                    }

                    
                    SL.Insert(i, current);

                    //Add this line to the correct point in the lines list
                    

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

                    Debug.DrawLine(current.a, current.b, Color.green, timePerStep);

                    if(above != null) Debug.DrawLine(above.a, above.b, Color.blue, timePerStep);
                    if (above != null && DoesIntersect(current, above, out intersect))
                    {
                        events.Add(new Event(intersect, above, current ));
                    }

                    if(below != null) Debug.DrawLine(below.a, below.b, Color.red, timePerStep);
                    if (below != null && DoesIntersect(current, below, out intersect))
                    {
                        events.Add(new Event(intersect, current, below));
                    }

                    //testing to see if removing vertical lines from the list immediately will help
                    float angleA = Vector3.Angle(current.Direction(), Vector3.forward);
                    //if the line is vertical the angle from forward will be 0 or 180
                    if (angleA % 180 == 0 && SL.Contains(current)) SL.Remove(current);

                        break;

                case PointType.INTERSECTION:
                    // Sort the intersections
                    Debug.DrawLine(currentEvent.lines[0].a, currentEvent.lines[0].b, Color.blue, 10);
                    Debug.DrawLine(currentEvent.lines[1].a, currentEvent.lines[1].b, Color.green, 10);
                    iPoints.Add(currentEvent.point);
                    break;

                case PointType.END:
                    // Remove this line from the list of lines since it's over now.
                    Debug.DrawLine(currentEvent.lines[0].a, currentEvent.lines[0].b, Color.gray, 60);
                    SL.Remove(currentEvent.lines[0]);
                    break;
            }

            //Debug.Log("Event Count = " + events.Count + " :: SL Count = " + SL.Count + " :: " + currentEvent.type + " @ " + currentEvent.point);
            events.RemoveAt(0);

            if(events.Count > 1) events.Sort(new EventCompare());

            if(counter % eventsPerStep == 0) yield return new WaitForSeconds(timePerStep);
        }

        //Debug.Log(iPoints.Count);
    }

    public bool DoesIntersect(Line lineA, Line lineB, out Vector3 intersection)
    {
        intersection = Vector3.zero;
        if (lineA == lineB)
        {
            //this is bad
            Debug.Log("Self Line Comparison");
            return false;
        }

        #region old check
        //int result = 0;

        //if (IsParallel(lineA.Direction(), lineB.Direction())) result += 1;
        ////if (IsOrthogonal(lineA.a - lineB.a, lineA.Normal())) result += 10;

        //float A, B, C, D;
        //A = lineA.Normal().x;
        //B = lineA.Normal().z;
        //C = lineB.Normal().x;
        //D = lineB.Normal().z;

        //float k1 = (A * lineA.a.x) + (B + lineA.a.z);
        //float k2 = (C * lineB.a.x) + (D + lineB.a.z);

        //float x_intersect = (D * k1 - B * k2) / (A * D - B * C);
        //float z_intersect = (-C * k1 + A * k2) / (A * D - B * C);
        //intersection = new Vector3(x_intersect, 0, z_intersect);

        //result += (!IsBetween(lineA.a, lineA.b, intersection) || !IsBetween(lineB.a, lineB.b, intersection)) ? 0 : 100;

        //Debug.Log("Intersect -> " + result);

        //return result == 0;
        #endregion


        bool result = true;

        float angleA = Vector3.Angle(lineA.Direction(), Vector3.forward);
        float angleB = Vector3.Angle(lineB.Direction(), Vector3.forward);
        //if the line is vertical the angle from forward will be 0 or 180
        if (angleA % 180 == 0 || angleB % 180 == 0)
        {
            //Debug.DrawLine(lineA.a, lineA.b, Color.yellow, 5);
            result = false;
        }

        if(!result) Debug.Log("Angles = " + angleA + " & " + angleB);

        int[] o = new int[]
        {
            GetOrientation(lineA.a, lineB.a, lineA.b),
            GetOrientation(lineA.a, lineB.a, lineB.b),
            GetOrientation(lineA.b, lineB.b, lineA.a),
            GetOrientation(lineA.b, lineB.b, lineB.a)
        };

        if (!(o[0] != o[1] && o[2] != o[3])) result = false;

        if (!((o[0] == 0 && OnSegment(lineA.a, lineA.b, lineB.a)) ||
            (o[1] == 0 && OnSegment(lineA.a, lineB.b, lineB.a)) ||
            (o[2] == 0 && OnSegment(lineA.b, lineA.a, lineB.b)) ||
            (o[3] == 0 && OnSegment(lineA.b, lineB.a, lineB.b))
            )) result = false;

        if (true)
        {
            //all of this maths is wrong

            Debug.Log("Could Overlap");
            //Here we calculate the intersection
            Vector3 p = lineA.a;
            Vector3 r = lineA.b - lineA.a;
            Vector3 q = lineB.a;
            Vector3 s = lineB.b - lineB.a;

            float rXs = Cross(r, s);
            float qpXs = Cross(q - p, s);
            float qpXr = Cross(q - p, r);

            if (rXs == 0) { result = false; }
            else
            {
                float t = qpXs / rXs;
                float u = qpXr / rXs;

                intersection = p + (t * r);

                if (t < 0 || t > 1) result = false;
                else
                {
                    Debug.Log("rxs = " + rXs + " : qpxs = " + qpXs + " : t = " + t + " : point = " + intersection);
                }
            }
        }

        return result;
    }

    float Cross(Vector3 a, Vector3 b) 
    {
        return (a.x * b.z) - (a.z * b.x);
    }

    bool OnSegment(Vector3 p1, Vector3 q, Vector3 p2) 
    {
        float maxX = p1.x >= p2.x ? p1.x : p2.x;
        float maxZ = p1.z >= p2.z ? p1.z : p2.z;
        float minX = p1.x >= p2.x ? p1.x : p2.x;
        float minZ = p1.z >= p2.z ? p1.z : p2.z;

        return (q.x <= maxX && q.x >= minX && q.z <= maxZ && q.z >= minZ);
    }

    //returns 0 for collinnear, 1 for clockwise, -1 for ccw
    int GetOrientation(Vector3 p1, Vector3 q, Vector3 p2) 
    {
        float val = (q.z - p1.z) * (p2.x - q.x) - (q.x - p1.x) * (p2.z - q.z);

        return val == 0 ? 0 : val > 0 ? 1 : -1;
    }

    //Are 2 vectors parallel?
    bool IsParallel(Vector3 v1, Vector3 v2)
    {
        //2 vectors are parallel if the angle between the vectors are 0 or 180 degrees
        if (Vector3.Angle(v1, v2) == 0f || Vector3.Angle(v1, v2) == 180f)
        {
            Debug.Log(v1 + " - " + v2 + " :: " + Vector3.Angle(v1, v2));
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
    bool IsBetween(Vector3 a, Vector3 b, Vector3 c)
    {
        bool isBetween = false;

        //Entire line segment
        Vector3 ab = b - a;
        //The intersection and the first point
        Vector3 ac = c - a;

        //Need to check 2 things: 
        //1. If the vectors are pointing in the same direction = if the dot product is positive
        //2. If the length of the vector between the intersection and the first point is smaller than the entire line
        if (Vector3.Dot(ab, ac) > 0f && ab.sqrMagnitude >= ac.sqrMagnitude)
        {
            isBetween = true;
        }

        return isBetween;
    }

    private void OnDrawGizmos()
    {
        if (debugMode && polyLines != null)
        {
            foreach (Line line in polyLines)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(line.a, line.b);
            }
        }

        if (iPoints != null)
        {
            foreach (Vector3 point in iPoints)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(point, Vector3.one * 10);
            }
        }
    }
}

[System.Serializable]
public class Line
{
    public Vector3 a;
    public Vector3 b;

    public Line(Vector3 start, Vector3 end) 
    {
        a = start;
        b = end;
    }

    public Vector3 Direction() 
    {
        return (b - a).normalized;
    }
    
    public Vector3 Normal() 
    {
        return Quaternion.Euler(0, 90, 0) * Direction();

        //return new Vector3(-Direction().y, Direction().x); 
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
            return first.point.z < second.point.z ? 1 : -1;
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
            return first.z < second.z ? 1 : -1;
        }

    }
}

public class EventCompare : IComparer<Event>
{
    //returns which line starts most to the left
    public int Compare(Event first, Event second)
    {
        if (first == null) Debug.LogError("NULL EVENT 1");
        if (second == null) Debug.LogError("NULL EVENT 2");

        if (first.point == second.point)
        {
            if (first.type == second.type) return 0;
            
            return first.type < second.type ? 1 : -1;
        }

        if (first.point.x != second.point.x) return first.point.x > second.point.x ? 1 : -1;
        else if (first.point.z != second.point.z) return first.point.z < second.point.z ? 1 : -1;

        return 0;
    }
}
