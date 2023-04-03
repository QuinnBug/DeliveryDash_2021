using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Earclipping 
{
	public class EarClipper : MonoBehaviour
	{
		public bool drawTris;
        public float timePerPoly;
		private NodeMeshConstructor nmc;
		internal List<Triangle[]> triList = null;
		[Space]
		public List<Vertex> vertices;


		public void Start()
        {
			nmc = FindObjectOfType<NodeMeshConstructor>();
			triList = null;
		}

        public void Update()
        {
            if (nmc.meshCreated && triList == null)
            {
				StartCoroutine(ClipAllPolygons());
            }
        }

		public IEnumerator ClipAllPolygons() 
		{
			triList = new List<Triangle[]>();
			foreach (Polygon poly in nmc.polygons)
			{
				ClipPolygon(poly);
				yield return new WaitForSeconds(timePerPoly);
			}
			Debug.Log("Clipping done");
		}

        private void OnDrawGizmos()
        {
            if (triList != null && drawTris)
            {
                foreach (Triangle[] tris in triList)
                {
                    foreach (Triangle triangle in tris)
                    {
                        for (int i = 0; i < 3; i++)
                        {
							int j = Lists.ClampListIndex(i + 1, 3);

							Gizmos.color = Color.red;
							Gizmos.DrawLine(triangle.vertices[i], triangle.vertices[j]);
						}
						
                    }
                }
            }
        }

        void ClipPolygon(Polygon poly) 
	    {
			List<Triangle> triangles = new List<Triangle>();

			//If we just have three points, then we dont have to do all calculations
			if (poly.vertices.Length == 3)
			{
				triangles.Add(new Triangle(poly.vertices[0], poly.vertices[1], poly.vertices[2]));
				triList.Add(triangles.ToArray());
				return;
			}
	
			//Step 1. Store the vertices in a list and we also need to know the next and prev vertex
			vertices = new List<Vertex>();
	
			for (int i = 0; i < poly.vertices.Length; i++)
			{
				vertices.Add(new Vertex(poly.vertices[i]));
			}
	
			//Find the next and previous vertex
			for (int i = 0; i < vertices.Count; i++)
			{
				int nextPos = Lists.ClampListIndex(i + 1, vertices.Count);
	
				int prevPos = Lists.ClampListIndex(i - 1, vertices.Count);
	
				vertices[i].prev = vertices[prevPos];
	
				vertices[i].next = vertices[nextPos];
			}
	
			//Step 2. Find the reflex (concave) and convex vertices, and ear vertices
			for (int i = 0; i < vertices.Count; i++)
			{
				CheckIfReflexOrConvex(vertices[i]);
			}
	
			//Have to find the ears after we have found if the vertex is reflex or convex
			List<Vertex> earVertices = new List<Vertex>();
	
			for (int i = 0; i < vertices.Count; i++)
			{	
				IsVertexEar(vertices[i], vertices, earVertices);
			}
	
			//Step 3. Triangulate!
			while (true)
			{
				//This means we have just one triangle left
				if (vertices.Count == 3)
				{
					//The final triangle
					triangles.Add(new Triangle(vertices[0].point, vertices[0].prev.point, vertices[0].next.point));
					break;
				}
	
				//Make a triangle of the first ear
				Vertex earVertex = earVertices[0];
	
				Vertex earVertexPrev = earVertex.prev;
				Vertex earVertexNext = earVertex.next;
	
				Triangle newTriangle = new Triangle(earVertex.point, earVertexPrev.point, earVertexNext.point);

				//if (poly.TriInBounds(newTriangle))
				//{
				//for (int j = 0; j < 3; j++)
				//{
				//		int k = Lists.ClampListIndex(j + 1, 3);
				//		Debug.DrawLine(newTriangle.vertices[j], newTriangle.vertices[k], Color.blue, 120);
				//	}
				//}

				triangles.Add(newTriangle);

				//Remove the vertex from the lists
				earVertices.Remove(earVertex);
				vertices.Remove(earVertex);

				//Update the previous vertex and next vertex
				earVertexPrev.next = earVertexNext;
				earVertexNext.prev = earVertexPrev;

				//...see if we have found a new ear by investigating the two vertices that were part of the ear
				CheckIfReflexOrConvex(earVertexPrev);
				CheckIfReflexOrConvex(earVertexNext);
	
				earVertices.Remove(earVertexPrev);
				earVertices.Remove(earVertexNext);
	
				IsVertexEar(earVertexPrev, vertices, earVertices);
				IsVertexEar(earVertexNext, vertices, earVertices);
			}
	
			//Debug.Log(triangles.Count);
	
			triList.Add(triangles.ToArray());
	    }
	
		//Check if a vertex if reflex or convex, and add to appropriate list
		private void CheckIfReflexOrConvex(Vertex v)
		{
			v.isReflex = false;
	
			//This is a reflex vertex if its triangle is oriented clockwise
			Vector2 a = v.prev.point;
			Vector2 b = v.point;
			Vector2 c = v.next.point;
	
			v.isReflex = (new Triangle(a, b, c).IsTriangleOrientedClockwise());
		}
	
		//Check if a vertex is an ear
		private void IsVertexEar(Vertex v, List<Vertex> vertices, List<Vertex> earVertices)
		{
			//A reflex vertex cant be an ear!
			if (v.isReflex)
			{
				return;
			}
	
			bool hasPointInside = false;
			Triangle tri = new Triangle(v.prev.point, v.point, v.next.point);
			for (int i = 0; i < vertices.Count; i++)
			{
				//We only need to check if a reflex vertex is inside of the triangle
				if (vertices[i].isReflex)
				{
					//This means inside and not on the hull
					if (tri.IsPointInside(vertices[i].point))
					{
						hasPointInside = true;
						break;
					}
				}
			}
	
			if (!hasPointInside)
			{
				earVertices.Add(v);
			}
		}
	}
	
	[System.Serializable]
	public class Vertex
	{
		//position
		public Vector3 point;
		public Vertex next;
		public Vertex prev;
	
		public bool isReflex = false;
	
	    public Vertex(Vector3 p)
	    {
			point = p;
	    }
	}
	
	public class Triangle 
	{
	    public Vector3[] vertices = new Vector3[3];
	
		public Triangle(Vector3 a, Vector3 b, Vector3 c) 
		{
			vertices[0] = a;
			vertices[1] = b;
			vertices[2] = c;
		}
	
	    //adjusted for x,z orientation
	    public bool IsTriangleOrientedClockwise()
	    {
	        Vector3 p1, p2, p3;
	        p1 = vertices[0];
	        p2 = vertices[1];
	        p3 = vertices[2];
	
	        return p1.x * p2.z + p3.x * p1.z + p2.x * p3.z - p1.x * p3.z - p3.x * p2.z - p2.x * p1.z > 0.0f;
	    }
	
		//adjusted for x,z orientation
		public bool IsPointInside(Vector3 p)
		{
			Vector3 p1, p2, p3;
			p1 = vertices[0];
			p2 = vertices[1];
			p3 = vertices[2];
	
			//Based on Barycentric coordinates
			float denominator = ((p2.z - p3.z) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.z - p3.z));
	
			float a = ((p2.z - p3.z) * (p.x - p3.x) + (p3.x - p2.x) * (p.z - p3.z)) / denominator;
			float b = ((p3.z - p1.z) * (p.x - p3.x) + (p1.x - p3.x) * (p.z - p3.z)) / denominator;
			float c = 1 - a - b;
	
			//The point is within the triangle or on the border if 0 <= a <= 1 and 0 <= b <= 1 and 0 <= c <= 1
			//if (a >= 0f && a <= 1f && b >= 0f && b <= 1f && c >= 0f && c <= 1f)
			//{
			//    isWithinTriangle = true;
			//}
	
			//The point is within the triangle (including on the border)
			return (a >= 0f && a <= 1f && b >= 0f && b <= 1f && c >= 0f && c <= 1f);
		}
	}

	[System.Serializable]
	public class Polygon
	{
		public Vector3 center;
		public Vector3[] vertices;
		public Line[] lines;

		public Polygon(List<Line> nodeLines, Vector3 center)
		{
			lines = nodeLines.ToArray();

			List<Line> tempLines = new List<Line>(lines);
			List<Vector3> points = new List<Vector3>();

			points.Add(tempLines[0].a);
			points.Add(tempLines[0].b);
			tempLines.RemoveAt(0);

            while (tempLines.Count > 0)
            {
                for (int i = 0; i < tempLines.Count; i++)
                {
					if (tempLines[i].a == points[points.Count - 1])
					{
						points.Add(tempLines[i].b);
						tempLines.RemoveAt(i);
						break;
					}
					else if (tempLines[i].b == points[points.Count - 1])
					{
						points.Add(tempLines[i].a);
						tempLines.RemoveAt(i);
						break;
					}
				}
            }
            
			vertices = points.ToArray();
		}

        internal bool TriInBounds(Triangle newTriangle)
        {
			Line testLine = new Line(center, center + (Vector3.up + Vector3.right) * 99999999);

			//loop through the 3 midpoints of the lines
            for (int i = 0; i < 3; i++)
            {
				int intersectionCount = 0;
				int j = Lists.ClampListIndex(i + 1, 3);
				testLine.a = Vector3.Lerp(newTriangle.vertices[i], newTriangle.vertices[j], 0.5f);

                foreach (Line line in lines)
                {
					if (line.Contains(testLine.a) || line.DoesIntersect(testLine, out Vector3 iPoint))
                    {
						intersectionCount++;
                    }
                }

				if (intersectionCount % 2 == 0) return false;
            }

			return true;
        }
    }
}

namespace Utility 
{
	public static class Lists 
	{
		public static int ClampListIndex(int index, int listSize)
		{
			return ((index % listSize) + listSize) % listSize;
		}
	}
}

