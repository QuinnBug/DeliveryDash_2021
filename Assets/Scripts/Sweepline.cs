using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sweepline : Singleton<Sweepline>
{
    public bool debugMode;
    public float timePerStep;
    public int eventsPerStep;
    public float timePerUntangle;
    [Space]
    public float minPointDistance;
    [Space]
    public List<Vector3> polyPoints = null;
    public List<Line> polyLines = null;
    public List<Event> iEvents = null;
    [Space]
    public List<Line> SL;

    bool allLinesChecked = false;
    bool doIntersection = false;
    int nextId;

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
        iEvents = null;

        doIntersection = false;
        allLinesChecked = false;
    }

    private void Update()
    {
        if (polyPoints != null && iEvents == null)
        {
            polyLines = new List<Line>(LinesFromNodes());
            polyPoints = null;
            doIntersection = true;
        }

        if (!allLinesChecked && doIntersection)
        {
            StartCoroutine(GetIntersections(polyLines.ToArray()));
        }
    }

    private Line[] LinesFromNodes()
    {
        List<Line> lineList = new List<Line>();

        for (int i = 0; i < polyPoints.Count - 1; i++)
        {
            lineList.Add(new Line(polyPoints[i], polyPoints[i+1], nextId));
            nextId = i;
        }

        return lineList.ToArray();
    }

    IEnumerator GetIntersections(Line[] lines)
    {
        doIntersection = allLinesChecked = false;

        int counter = 0;
        List<Event> events = new List<Event>();
        foreach (Line line in lines)
        {
            events.Add(new Event(line.a, PointType.START, line));
            events.Add(new Event(line.b, PointType.END, line));
        }
        events.Sort(new EventCompare());

        SL = new List<Line>();
        iEvents = new List<Event>();

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

            //Debug.Log(currentEvent.type);
            Debug.DrawRay(new Vector3(currentEvent.point.x, 1, 10000), Vector3.back * 20000, Color.blue, timePerStep);

            switch (currentEvent.type)
            {
                case PointType.START:
                    Line current = currentEvent.lines[0];
                    Vector3 intersect;

                    foreach (Line otherLine in SL)
                    {
                        if (DoesIntersect(current, otherLine, out intersect))
                        {
                            events.Add(new Event(intersect, current, otherLine));
                        }
                    }

                    SL.Add(current);
                    break;

                case PointType.INTERSECTION:
                    iEvents.Add(currentEvent);
                    break;

                case PointType.END:
                    // Remove this line from the list of lines since it's over now.
                    SL.Remove(currentEvent.lines[0]);
                    break;
            }

            //Debug.Log("Event Count = " + events.Count + " :: SL Count = " + SL.Count + " :: " + currentEvent.type + " @ " + currentEvent.point);
            events.RemoveAt(0);

            SweepLineCompare slc = new SweepLineCompare();
            slc.x = currentEvent.point.x;

            if(SL.Count > 1) SL.Sort(slc);

            //We sort the events each time to make sure that any added events are placed in the correct point
            if (events.Count > 1) events.Sort(new EventCompare());

            if (counter % eventsPerStep == 0)
            {
                yield return new WaitForSeconds(timePerStep);
            }
        }

        DebugUntangle();
    }

    //for use with the SL list not the regular list
    public int FindLineIndexWithBinarySearch(Line[] lines, Line line, float x) 
    {
        if (lines.Length == 0) return 0;

        float zOne = line.GetYAtXOnLine(x);
        float zTwo = lines[0].GetYAtXOnLine(x);

        if (lines.Length == 1) return zTwo > zOne ? 1 : 0;

        int low = 0, high = lines.Length, index = (low + high) / 2;

        while (high - low > 1)
        {
            zTwo = lines[index].GetYAtXOnLine(x);

            if (zOne == zTwo)
            {
                //we have reached a point where both lines have the same y value at x;
                break;
            }

            if (zOne < zTwo) low = index + 1;
            if (zOne > zTwo) high = index - 1;
            
            index = Mathf.FloorToInt((low + high) / 2);
        }

        return index;
    }

    public bool DoesIntersect(Line lineA, Line lineB, out Vector3 intersection)
    {
        intersection = lineA.a;
        if (lineA == lineB)
        {
            //this is bad
            Debug.Log("Self Line Comparison");
            return false;
        }

        //y = mx + b

        float[] equationA = lineA.Equation();
        float[] equationB = lineB.Equation();

        float slopeDiff = equationA[0] - equationB[0];
        if (Mathf.Abs(slopeDiff) <= 0.001f) 
        {
            // The lines are parallel because their slopes are the same (or close enough))
            return false;
        }

        if (lineA.type == LineType.VERTICAL || lineB.type == LineType.VERTICAL)
        {
            //Debug.Log("One of the lines is vertical > " + lineA.type + " : " + lineB.type);

            Line verticalLine = lineA.type == LineType.VERTICAL ? lineA : lineB;
            Line otherLine = lineA.type == LineType.VERTICAL ? lineB : lineA;

            intersection = new Vector3(verticalLine.a.x, intersection.y, otherLine.GetYAtXOnLine(verticalLine.a.x));
        }
        else
        {
            intersection.x = (equationB[1] - equationA[1]) / (equationA[0] - equationB[0]);
            intersection.z = -1 * ((equationA[1] * equationB[0] - equationB[1] * equationA[0]) / (equationA[0] - equationB[0]));
        }

        if (lineA.SharesPoints(lineB)) return false;

        //if the intersection is not within the range of either line then return false;
        if (!lineA.Contains(intersection) || !lineB.Contains(intersection))
        {
            return false;
        }

        //Debug.Log(slopeDiff + " > " + lineA.type + " " + lineB.type);
        //Debug.DrawLine(lineA.a, lineA.b, Color.blue, 1.5f);
        //Debug.DrawLine(lineB.a, lineB.b, Color.green, 1.5f);

        //we got through all the checks! that means we found an intersection in range
        return true;
    }

    public void DebugUntangle()
    {
        Debug.Log("Untangle Time");

        foreach (Event intersection in iEvents)
        {
            int i = polyLines.IndexOf(intersection.lines[0]);
            int j = polyLines.IndexOf(intersection.lines[1]);

            if (i >= polyLines.Count || j >= polyLines.Count || j < 0 || i < 0) continue;

            if (i > j)
            {
                int temp = i;
                i = j;
                j = temp;
            }

            int diff = j - i;

            //if these 2 lines share endpoints then they can't overlap
            if (polyLines[i].SharesPoints(polyLines[j])) continue;

            if (DoesIntersect(polyLines[i], polyLines[j], out Vector3 iPoint))
            {
                //we're swapping a bunch of the lines but how can i make sure that the lines connect to the proper before/after lines

                polyLines[j] = polyLines[i].SwitchPoints(polyLines[j], false);

                //Line tempILine = polyLines[i];
                //Line tempJLine = polyLines[j];

                //switch (tempILine.type)
                //{
                //    case LineType.REGULAR:
                //    case LineType.HORIZONTAL:
                //        polyLines[i].SwitchPoints(tempJLine, false);
                //        break;

                //    case LineType.VERTICAL:
                //        break;
                //    default:
                //        break;
                //}

                //switch (tempJLine.type)
                //{
                //    case LineType.REGULAR:
                //    case LineType.HORIZONTAL:
                //        polyLines[j].SwitchPoints(tempILine, false);
                //        break;

                //    case LineType.VERTICAL:
                //        break;
                //    default:
                //        break;
                //}

                if (DoesIntersect(polyLines[i], polyLines[j], out iPoint))
                {
                    Debug.Log(i + " " + j + " lines -- " + polyLines[i].type + " " + polyLines[j].type);
                }
            }
            else
            {
                Debug.Log("Lines no longer intersect");
            }


        }

        allLinesChecked = false;
        doIntersection = true;

        //iEvents.Clear();

        // After we switch all of the lines we need to run a check to destroy any lines that would be crossed by the inner path
    }

    private void OnDrawGizmos()
    {
        if (debugMode && polyLines != null)
        {
            for (int i = 0; i < polyLines.Count; i++)
            {
                Gizmos.color = i%2 == 0 ? Color.magenta : Color.red;
                Gizmos.DrawLine(polyLines[i].a, polyLines[i].b);
            }
        }

        if (iEvents != null)
        {
            foreach (Event intersection in iEvents)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(intersection.point, Vector3.one * 7);
            }
        }
    }
}

[System.Serializable]
public class Line
{
    public int id;

    private Vector3 A;
    private Vector3 B;

    public Vector3 a {
        get { return A; } 
        set{ A = value; UpdateType(); }
    }

    public Vector3 b
    {
        get { return B; }
        set 
        { 
            B = value;
            UpdateType();
        }
    }

    public LineType type = LineType.REGULAR;

    //this updates the type and makes sure that a is leftmost or topmost based on type
    public void UpdateType() 
    {
        //we always want a to be left and b to be right
        

        if (a.x == b.x)
        {
            type = LineType.VERTICAL;
            
        }
        else if (a.z == b.z) type = LineType.HORIZONTAL;
        else type = LineType.REGULAR;
    }

    public bool CloserToA(Vector3 point) 
    {
        return Vector3.Distance(a, point) < Vector3.Distance(b, point);
    }

    public void AssignClosestPoint(Vector3 point) 
    {
        if (CloserToA(point))
        {
            a = point;
        }
        else
        {
            b = point;
        }
    }

    public Line(Vector3 start, Vector3 end)
    {
        //we use the capital A to avoid calling UpdateType more than needed

        A = (start.x <= end.x) ? start : end;
        b = (start.x <= end.x) ? end : start;

        if (type == LineType.VERTICAL)
        {
            A = (start.z >= end.z) ? start : end;
            b = (start.z >= end.z) ? end : start;
        }
    }

    public Line(Vector3 start, Vector3 end, int i) : this(start, end)
    {
        id = i;
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

        switch (type)
        {
            case LineType.REGULAR:
                return (point.x > a.x && point.x < b.x) && ((point.z > a.z && point.z < b.z) || (point.z < a.z && point.z > b.z));
                
            case LineType.HORIZONTAL:
                //if the lines are vertical then the z should be the same as either points z
                return (point.x > a.x && point.x < b.x) && point.z == a.z;

            case LineType.VERTICAL:
                //if the lines are vertical then the x should be the same as either points x
                return point.x == a.x && ((point.z >= a.z && point.z <= b.z) || (point.z <= a.z && point.z >= b.z));

            default:
                return false;
        }
    }

    public float GetYAtXOnLine(float x)
    {
        float[] equation = Equation();
        //y = mx + b;
        float y = (equation[0] * x) + equation[1];

        return y;
    }

    public float[] Equation() 
    {
        //return {m, b, flag for horiz, vert, regular}
        float[] result = new float[] { 0, 0 };

        //vertical lines can't use this calulation
        if(type != LineType.VERTICAL)
        {
            //m
            result[0] = (b.z - a.z) / (b.x - a.x);

            //b
            result[1] = a.z - (result[0] * a.x);
        }

        return result;
    }

    public void Flip()
    {
        Vector3 temp = a;
        a = b;
        b = temp;
    }

    internal float Length()
    {
        return Vector3.Distance(a,b);
    }

    internal bool SharesPoints(Line line)
    {
        return a == line.a || a == line.b || b == line.a || b == line.b;
    }

    internal Line SwitchPoints(Line line, bool doA) 
    {
        Vector3 temp = doA ? a:b;

        if (doA)
        {
            a = line.a;
            line.a = temp;
        }
        else
        {
            b = line.b;
            line.b = temp;
        }

        return line;
    }

    internal Line CrossPoints(Line line) 
    {
        Vector3 temp = b;

        b = line.a;
        line.a = temp;

        return line;
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
    START = 0,
    INTERSECTION = 1,
    END = 2
}

public enum LineType 
{
    REGULAR,
    HORIZONTAL,
    VERTICAL
}

public class LineCompare : IComparer<Line>
{
    //returns leftmost or topmost if they are the same x
    public int Compare(Line first, Line second)
    {
        if (first.a == second.a) return 0;

        if (first.a.z != second.a.z)
        {
            return first.a.z < second.a.z ? 1 : -1;
        }
        else if (first.b.z != second.b.z)
        {
            return first.b.z < second.b.z ? 1 : -1;
        }

        return 0;
    }
}

public class SweepLineCompare : IComparer<Line>
{
    public float x = 0;

    public int Compare(Line first, Line second)
    {
        float zOne = first.GetYAtXOnLine(x);
        float zTwo = second.GetYAtXOnLine(x);

        if (zOne == zTwo) return first.a.z <= second.a.z ? 1 : -1;
        else return zTwo >= zOne ? 1 : -1;
    }
}

public class EventCompare : IComparer<Event>
{
    //returns which line starts most to the left
    public int Compare(Event first, Event second)
    {
        if (first == null) Debug.LogError("NULL EVENT 1");
        if (second == null) Debug.LogError("NULL EVENT 2");

        if (first.point.x == second.point.x)
        {
            if (first.type == second.type) return 0;
            
            return first.type > second.type ? 1 : -1;
        }

        if (first.point.x != second.point.x) return first.point.x > second.point.x ? 1 : -1;
        else if (first.point.z != second.point.z) return first.point.z < second.point.z ? 1 : -1;

        return 0;
    }
}