using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarClipper : MonoBehaviour
{
    
}

public class Polygon 
{
    Vector3[] vertices;
    Line[] lines;

    public Polygon(List<Line> nodeLines)
    {
        lines = nodeLines.ToArray();
        vertices = new Vector3[lines.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = lines[i].a;
        }
    }
}
