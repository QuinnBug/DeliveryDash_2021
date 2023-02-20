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
        iPoints = new List<Vector3>();

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
                        Debug.Log("Both ^");
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
                        Debug.Log("Above ^");
                        Debug.DrawLine(above.a, current.a, Color.magenta, timePerStep);
                        events.Add(new Event(intersect, above, current));
                    }

                    if(below != null) Debug.DrawLine(below.a, below.b, Color.red, timePerStep);
                    if (below != null && DoesIntersect(current, below, out intersect))
                    {
                        Debug.Log("Below ^");
                        Debug.DrawLine(below.a, current.a, Color.magenta, timePerStep);
                        events.Add(new Event(intersect, current, below));
                    }

                    //testing to see if removing vertical lines from the list immediately will help
                    float angleA = Vector3.Angle(current.Direction(), Vector3.forward);
                    //if the line is vertical the angle from forward will be 0 or 180
                    if (angleA % 180 == 0 && SL.Contains(current)) SL.Remove(current);

                        break;

                case PointType.INTERSECTION:
                    // Sort the intersections
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

        //y = mx + b

        float[] equationA = GetLineEquation(lineA);
        float[] equationB = GetLineEquation(lineB);

        if (Mathf.Abs(equationA[0] - equationB[0]) <= 0.001f  || equationA[0] == 0 || equationB[0] == 0) 
        {
            //The lines are parellel because they slopes are the same (or close enough) or perhaps one of the lines is vertical
            // Either way we know they don't overlap (I think for vertical we need to do a diff calc)
            return false;
        }

        //m = A/B

        #region A B calculations
        // line 1
        float A1 = equationA[0];
        int B1 = 1;
        float C1 = equationA[1];
        C1 *= B1;

        //line 2
        float A2 = equationB[0];
        int B2 = 1;
        float C2 = equationB[1];
        C2 *= B2;
        #endregion

        // -(A/B)x + y - b = 0

        intersection.x = (B1 * C2 - B2 * C1) / (A1 * B2 - A2 * B1);
        intersection.z = -1 * ((C1 * A2 - C2 * A1) / (A1 * B2 - A2 * B1));

        //x = (b1 * c2 - b2 * c1) / (a1 * b2 - a2 * b1)
        //y = (c1 * a2 - c2 * a1) / (a1 * b2 - a2 * b1)


        //if the intersection is not within the range of either line then return false;
        if (!lineA.Contains(intersection) || !lineB.Contains(intersection))
        {
            return false;
        }

        Debug.Log("I @ " + intersection + " > " + equationA[0] + " " + equationA[1] + " :: " + equationB[0] + " " + equationB[1]);
        //we got through all the checks! that means we found an intersection in range
        return true;
    }

    private float[] GetLineEquation(Line line)
    {
        float[] result = new float[] { 0,0 };

        if (line.a.z == line.b.z || line.a.x == line.b.x) return result;
        //Debug.Log("AH -> " + line.a + " :: " + line.b);


        //m
        result[0] = (line.b.z - line.a.z) / (line.b.x - line.a.x);

        //b
        result[1] = line.a.z - (result[0] * line.a.x);


        return result;
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

    public bool Contains(Vector3 point) 
    {
        //Lines should always be created with a being leftmost and b being to the right
        //However the z could be either way around
        if ((point.x > a.x && point.x < b.x) && ((point.z > a.z && point.z < b.z) || (point.z < a.z && point.z > b.z)))
        {
            return true;
        }

        return false;
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
