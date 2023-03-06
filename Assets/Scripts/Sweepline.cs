using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sweepline : Singleton<Sweepline>
{
    public bool untangle;
    public bool debugMode;
    public float timePerStep;
    public int eventsPerStep;
    [Space]
    public List<Vector3> polyPoints = null;
    public List<Line> polyLines = null;
    public List<Event> iEvents = null;
    [Space]
    public List<Line> SL;

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

        untangle = false;
    }

    private void Update()
    {
        if (polyPoints != null && iEvents == null)
        {
            polyLines = new List<Line>(LinesFromNodes());
            StartCoroutine(GetIntersections(polyLines.ToArray()));
            polyPoints = null;
        }

        if (untangle)
        {
            untangle = false;
            DebugUntangle();
        }
    }

    private Line[] LinesFromNodes()
    {
        List<Line> lineList = new List<Line>();

        for (int i = 0; i < polyPoints.Count - 1; i++)
        {
            Vector3 a = (polyPoints[i].x <= polyPoints[i + 1].x) ? polyPoints[i] : polyPoints[i + 1];
            Vector3 b = (polyPoints[i].x <= polyPoints[i + 1].x) ? polyPoints[i + 1] : polyPoints[i];

            lineList.Add(new Line(a, b, i));

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
                    Vector3 intersect = Vector3.zero;

                    foreach (Line otherLine in SL)
                    {
                        if (DoesIntersect(current, otherLine, out intersect))
                        {
                            events.Add(new Event(intersect, current, otherLine));
                        }
                    }

                    SL.Add(current);

                    #region old method
                    /*
                    Line above = null, below = null;
                    int lineIdx = FindLineIndexWithBinarySearch(SL.ToArray(), current, currentEvent.point.x);
                    Debug.Log(lineIdx);
                    SL.Insert(lineIdx, current);

                    //we can grab the lines above and below this by simply getting the i - and + this line
                    if (lineIdx - 1 >= 0) above = SL[lineIdx - 1];
                    if (lineIdx + 1 < SL.Count) below = SL[lineIdx + 1];


                    if(above != null) Debug.DrawLine(above.a, above.b, Color.blue, timePerStep);
                    if(below != null) Debug.DrawLine(below.a, below.b, Color.yellow, timePerStep);

                    //Tell the above and below lines that we have a line in between them now
				    if (above != null && below != null && DoesIntersect(above, below, out intersect))
                    {
                        //remove intersect from events list
                        foreach (Event testEvent in events)
                        {
                            //if it's not an intersection or if it isn't the above/below intersection
                            if (testEvent.type == PointType.INTERSECTION && testEvent.HasLine(above) && testEvent.HasLine(below)) 
                            {
                                events.Remove(testEvent);
                                break;
                            }
                        }
                    }
                    
                    Debug.DrawLine(current.a, current.b, Color.green, timePerStep);

                    if (above != null && DoesIntersect(current, above, out intersect))
                    {
                        events.Add(new Event(intersect, above, current));
                    }

                    if (below != null && DoesIntersect(current, below, out intersect))
                    {
                        events.Add(new Event(intersect, current, below));
                    }
                    */
                    #endregion

                    break;

                case PointType.INTERSECTION:
                    iEvents.Add(currentEvent);
                    break;

                case PointType.END:
                    #region old method
                    /*
                    int idx = SL.IndexOf(currentEvent.lines[0]);

                    Line top = null, bottom = null;

                    //we can grab the lines above and below this by simply getting the i - and + this line
                    if (idx - 1 >= 0) top = SL[idx - 1];
                    if (idx + 1 < SL.Count) bottom = SL[idx + 1];

                    //Check if the above and below lines intersect
                    if (top != null && bottom != null && DoesIntersect(top, bottom, out intersect))
                    {
                        events.Add(new Event(intersect, top, bottom));
                    }
                    */
                    #endregion

                    // Remove this line from the list of lines since it's over now.
                    SL.Remove(currentEvent.lines[0]);

                    break;
            }

            //Debug.Log("Event Count = " + events.Count + " :: SL Count = " + SL.Count + " :: " + currentEvent.type + " @ " + currentEvent.point);
            events.RemoveAt(0);

            SweepLineCompare slc = new SweepLineCompare();
            slc.x = currentEvent.point.x;
            SL.Sort(slc);

            //We sort the events each time to make sure that any added events are placed in the correct point
            if (events.Count > 1) events.Sort(new EventCompare());

            if (counter % eventsPerStep == 0)
            {
                //int i = 1;
                //foreach (Line line in SL)
                //{
                //    Debug.DrawLine(line.a, line.b, Color.gray, 120);
                //    float colorVal = (i / (float)SL.Count);
                //    Debug.DrawLine(line.a + Vector3.up, line.b + Vector3.up, new Color(colorVal, colorVal, colorVal), timePerStep);
                //    i++;
                //}

                yield return new WaitForSeconds(timePerStep);
            }
        }

        //Debug.Log(iPoints.Count);
    }

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
        //The vertical lines are still failing to be checked properly

        intersection = Vector3.zero;
        if (lineA == lineB)
        {
            //this is bad
            Debug.Log("Self Line Comparison");
            return false;
        }

        //y = mx + b

        float[] equationA = lineA.Equation();
        float[] equationB = lineB.Equation();

        if (Mathf.Abs(equationA[0] - equationB[0]) <= 0.001f) 
        {
            // The lines are parellel because their slopes are the same (or close enough))
            return false;
        }

        //if both are not vertical lines
        if (lineA.type != LineType.VERTICAL && lineB.type != LineType.VERTICAL)
        {
            //m = A/B

            #region A B calculations
            // line 1
            //float A1 = equationA[0];
            //int B1 = 1;
            //float C1 = equationA[1];
            //C1 *= B1;

            //line 2
            //float A2 = equationB[0];
            //int B2 = 1;
            //float C2 = equationB[1];
            //C2 *= B2;

            // -(A/B)x + y - b = 0

            //intersection.x = (B1 * C2 - B2 * C1) / (A1 * B2 - A2 * B1);
            //intersection.z = -1 * ((C1 * A2 - C2 * A1) / (A1 * B2 - A2 * B1));
            #endregion

            intersection.x = (equationB[1] - equationA[1]) / (equationA[0] - equationB[0]);
            intersection.z = -1 * ((equationA[1] * equationB[0] - equationB[1] * equationA[0]) / (equationA[0] - equationB[0]));

            //x = (b1 * c2 - b2 * c1) / (a1 * b2 - a2 * b1)
            //y = (c1 * a2 - c2 * a1) / (a1 * b2 - a2 * b1)
            //z = -1 * (c1 * a2 - c2 * a1) / (a1 * b2 - a2 * b1)
        }
        //if either are vertical lines
        else if (lineA.type == LineType.VERTICAL || lineB.type == LineType.VERTICAL)
        {
            //Debug.Log("One of the lines is vertical > " + lineA.type + " : " + lineB.type);

            Line verticalLine = lineA.type == LineType.VERTICAL ? lineA : lineB;
            Line otherLine = lineA.type == LineType.VERTICAL ? lineB : lineA;

            intersection = new Vector3(verticalLine.a.x, verticalLine.a.y, otherLine.GetYAtXOnLine(verticalLine.a.x));
        }

        //if either are horizontal lines
        if (lineA.type == LineType.HORIZONTAL || lineB.type == LineType.HORIZONTAL)
        {
            //Debug.Log("One of the lines is horizontal");
        }

        

        //if the intersection is not within the range of either line then return false;
        if (!lineA.Contains(intersection) || !lineB.Contains(intersection))
        {
            return false;
        }

        //we got through all the checks! that means we found an intersection in range
        return true;
    }

    public void DebugUntangle()
    {
        foreach (Event intersection in iEvents)
        {
            int i = 0, j = 0;

            while (i < polyLines.Count && polyLines[i].id != intersection.lines[0].id)
            {
                i++;
            }

            while (j < polyLines.Count && polyLines[j].id != intersection.lines[1].id)
            {
                j++;
            }

            if (j >= polyLines.Count || i >= polyLines.Count)
            {
                Debug.Log(i + " " + j + " - one of those lines may have been deleted");
                continue;
            }

            //How do I know which of the lines points is in the wrong place?
            //if i & j are 1/2 different from each other then they are one of the small overlaps and i+1 can be deleted and both i & j go to/from i.Point
            //

            int diff = Mathf.Abs(i - j);
            int x = i < j ? i : j;
            int y = i < j ? j : i;

            if (diff <= 2)
            {
                //there are some issues here, but it seems to work most of the time

                polyLines[x] = new Line(polyLines[x - 1].b, intersection.point, -1);
                polyLines[y] = new Line(intersection.point, polyLines[y + 1].a, -1);

                if(diff > 1) polyLines.RemoveAt(x + 1);
            }
            else
            {
                //this is a very odd result...
                //definitely cleaner than before but not perfectly

                //Vector3 tempV3 = polyLines[x].b;
                //polyLines[x].b = polyLines[y].a;
                //polyLines[y].a = tempV3;

                // -- new --

                //Find which direction we need to go
                //get the 4 points and identify which of them need to be paired to not cross.

                List<Vector3> points = new List<Vector3> { polyLines[x].a, polyLines[x].b, polyLines[y].a, polyLines[y].b };
                Vector3[] sortedPoints = new Vector3[4];

                Vector3 bottomLeft = Vector3.positiveInfinity, topRight = Vector3.negativeInfinity;

                foreach (Vector3 p in points)
                {
                    if (p.x < bottomLeft.x) bottomLeft.x = p.x;
                    if (p.x > topRight.x) topRight.x = p.x;

                    if (p.z < bottomLeft.z) bottomLeft.z = p.z;
                    if (p.z < topRight.z) topRight.z = p.z;
                }

                //bl,tl,tr,bl
                Vector3[] corners = new Vector3[] { bottomLeft, new Vector3(bottomLeft.x, 0, topRight.z), topRight, new Vector3(topRight.x, 0, bottomLeft.z) };

                for (int k = 0; k < 4; k++)
                {
                    int currentBestIdx = 0;

                    if (points.Count > 1)
                    {
                        for (int idx = 0; idx < points.Count; idx++)
                        {
                            if (currentBestIdx == idx) continue;

                            if (Vector3.Distance(points[currentBestIdx], corners[k]) > Vector3.Distance(points[idx], corners[k]))
                            {
                                currentBestIdx = idx;
                            }
                        }
                    }

                    sortedPoints[k] = points[currentBestIdx];
                    points.RemoveAt(currentBestIdx);
                }

                //bottom 2 & top 2 connect ????
                polyLines[x] = new Line(sortedPoints[0], sortedPoints[2], -1);
                polyLines[y] = new Line(sortedPoints[1], sortedPoints[3], -1);
            }
            
        }

        Debug.Log("Untangle complete");
        iEvents.Clear();
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

        //if (iEvents != null)
        //{
        //    foreach (Event intersection in iEvents)
        //    {
        //        Gizmos.color = Color.cyan;
        //        Gizmos.DrawCube(intersection.point, Vector3.one * 10);
        //    }
        //}
    }
}

[System.Serializable]
public class Line
{
    public int id;
    public Vector3 a;
    public Vector3 b;
    public LineType type = LineType.REGULAR;

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
        a = start;
        b = end;

        if (a.x == b.x) type = LineType.VERTICAL;
        else if (a.z == b.z) type = LineType.HORIZONTAL;
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

        if (zOne == zTwo) return first.a.z < second.a.z ? 1 : -1;
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
