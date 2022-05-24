using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BW : MonoBehaviour
{
    public List<int[]> triangulate2D(List<Vector2> points)
    {
        List<int[]> triangulation = new List<int[]>();
        points.AddRange(new Vector2[3] { new Vector2(), new Vector2(), new Vector2() });
        int topIndex = points.Count - 1;
        int rightIndex = points.Count - 2;
        int leftIndex = points.Count - 3;
        bool allInside = false;
        triangulation.Add(new int[] { rightIndex, leftIndex, topIndex });

        while (!allInside)
        {
            allInside = true;
            for (int i = 0; i < points.Count - 3; i++)
            {
                points[topIndex] = new Vector2(points[topIndex].x, points[topIndex].y + 1);
                points[rightIndex] = new Vector2(points[rightIndex].x + 1, points[rightIndex].y - 1);
                points[leftIndex] = new Vector2(points[leftIndex].x - 1, points[leftIndex].y);
                if (points[i].y > points[topIndex].y) points[topIndex] = new Vector2(points[topIndex].x, points[i].y + 1);
                if (points[i].y < points[rightIndex].y)
                {
                    points[rightIndex] = new Vector2(points[rightIndex].x, points[i].y - 1);
                    points[leftIndex] = new Vector2(points[leftIndex].x, points[i].y - 1);
                }
                if (points[i].x < points[leftIndex].x) points[leftIndex] = new Vector2(points[i].x - 1, points[leftIndex].y);
                if (points[i].x > points[rightIndex].x) points[rightIndex] = new Vector2(points[i].x + 1, points[rightIndex].y);

                if (!PointInTriangle(points[i], points[topIndex], points[rightIndex], points[leftIndex]))
                {
                    allInside = false;
                    continue;
                }
            }
        }

        // foreach (Vector2 point in points)
        for (int p = 0; p < points.Count; p++)
        {
            Vector2 point = points[p];
            List<int[]> badTriangles = new List<int[]>();
            foreach (int[] triangle in triangulation)
            {
                if (PointInTriangle(point, points[triangle[0]], points[triangle[1]], points[triangle[2]]))
                {
                    badTriangles.Add(triangle);
                }
            }

            List<int[]> polygon = new List<int[]>();
            
            for (int i = 0; i < badTriangles.Count; i++)
            {
                int[] a = badTriangles[i];
                int[][] edges = new int[][] {
                    new int[]{a[0], a[1]},
                    new int[]{a[1], a[2]},
                    new int[]{a[2], a[0]}
                };
                foreach (int[] edge in edges)
                {
                    for (int j = 0; j < badTriangles.Count; j++)
                    {
                        if (j == i) continue;
                        int[] b = badTriangles[j];
                        if (!edgeInTriangle(edge, b)) polygon.Add(edge);
                    }
                }
            }

            foreach (int[] badTriangle in badTriangles) {
                foreach (int[] triangle in triangulation) {
                    if (badTriangle[0] == triangle[0] && badTriangle[1] == badTriangle[1] && badTriangle[2] == badTriangle[2]) {
                        triangulation.Remove(triangle);
                    }
                }
            }

            foreach (int[] edge in polygon) {
                triangulation.Add(new int[3]{edge[0], edge[1], p});
            }
        }
        foreach (int[] triangle in triangulation)
        {
            if (
                triangle[0] == rightIndex || triangle[0] == leftIndex || triangle[0] == topIndex ||
                triangle[1] == rightIndex || triangle[1] == leftIndex || triangle[1] == topIndex ||
                triangle[2] == rightIndex || triangle[2] == leftIndex || triangle[2] == topIndex
            ) {
                triangulation.Remove(triangle);
            }
        }
        return triangulation;
    }

    private bool edgeInTriangle(int[] edge, int[] triangle)
    {
        int score = 0;
        foreach (int point in triangle)
        {
            if (point == edge[0] || point == edge[1]) score++;
        }
        if (score == 2) return true;
        return false;
    }

    private bool trianglesShareEdges(int[] a, int[] b)
    {
        int[][] ea = new int[3][] {
            new int[2]{a[0], a[1]},
            new int[2]{a[1], a[2]},
            new int[2]{a[2], a[0]}
        };
        int[][] eb = new int[3][] {
            new int[2]{b[0], b[1]},
            new int[2]{b[1], b[2]},
            new int[2]{b[2], b[0]}
        };
        foreach (int[] ta in ea)
        {
            foreach (int[] tb in eb)
            {
                if ((ta[0] == tb[0] && ta[1] == tb[1]) || (ta[0] == tb[1] && ta[1] == tb[0])) return true;
            }
        }
        return false;
    }

    private float sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float d1, d2, d3;
        bool has_neg, has_pos;

        d1 = sign(pt, v1, v2);
        d2 = sign(pt, v2, v3);
        d3 = sign(pt, v3, v1);

        has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(has_neg && has_pos);
    }
}
