using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGrid
{

    private int seed;
    private int size;
    private float threshold;
    public List<Vector3> vertices;
    public List<int> triangles;
    public MeshGrid(int seed, int size, float threshold)
    {
        this.seed = seed;
        this.size = size;
        this.threshold = threshold;
        vertices = new List<Vector3>();
        triangles = new List<int>();
    }

    public void Build()
    {
        vertices = new List<Vector3>();
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                // if (Mathf.PerlinNoise(x + seed * .517f, z + seed * .517f) > threshold)
                {
                    vertices.Add(new Vector3(
                        x,
                        // x + Mathf.PerlinNoise(x + seed * .1231f, z + seed * .1231f),
                        0,
                        z
                        // z + Mathf.PerlinNoise(x + seed * .5134f, z + seed * .5134f)
                    ));
                    // int[] result = new int[2] { 0, 0 };
                    // float max = 0;
                    // for (int xx = 0; xx < 10; xx++)
                    // {
                    //     for (int zz = 0; zz < 10; zz++)
                    //     {
                    //         float val = Mathf.PerlinNoise(x + seed * ((xx + 1 ) / 10) * .517f, z + seed * ((zz + 1 ) / 10) * .517f) + 1;
                    //         if (val > max)
                    //         {
                    //             result = new int[2] { xx, zz };
                    //             max = val;
                    //         }
                    //     }
                    // }
                    // Debug.Log(result);
                    // vertices.Add(new Vector3(x + (1 / (result[0] + 1)), 0, z + (1 /(result[1] + 1))));
                }
            }
        }
        Debug.Log("VERTICES: " + vertices.Count);
        // triangulate2D();
        triangulate();
        Debug.Log("TRIANGLES: " + triangles.Count / 3);
    }

    private void triangulate()
    {
        triangles = new List<int>();
        for (int a = 0; a < vertices.Count; a++)
        {
            List<int[]> possibleTris = new List<int[]>();
            for (int b = 0; b < vertices.Count; b++)
            {
                if (b == a) continue;
                for (int c = 0; c < vertices.Count; c++)
                {
                    if (c == a || c == b) continue;
                    possibleTris.Add(new int[3] { a, b, c });
                }
            }
            List<int[]> pass1 = new List<int[]>();
            foreach (int[] tri in possibleTris)
            {
                Vector2 circumcenter = getCircumcenter(vertices[tri[0]], vertices[tri[1]], vertices[tri[2]]);
                // Debug.Log(circumcenter);
                float radius = Vector2.Distance(circumcenter, new Vector2(vertices[tri[0]].x, vertices[tri[0]].z));
                float radius2 = Vector2.Distance(circumcenter, new Vector2(vertices[tri[1]].x, vertices[tri[1]].z));
                float radius3 = Vector2.Distance(circumcenter, new Vector2(vertices[tri[2]].x, vertices[tri[2]].z));

                if (radius2 > radius) radius = radius2;
                if (radius3 > radius) radius = radius3;
                if (radius == float.NaN) continue;
                // Debug.Log($"{radius} == {radius2} == {radius3}");
                bool isBad = false;
                for (int i = 0; i < vertices.Count; i++)
                {
                    if (i == tri[0] || i == tri[1] || i == tri[2]) continue;
                    if (
                        Vector2.Distance(circumcenter, new Vector2(vertices[i].x, vertices[i].z)) < radius
                        || PointInTriangle(vertices[i], vertices[tri[0]], vertices[tri[1]], vertices[tri[2]])
                        )
                    {
                        isBad = true;
                        // Debug.Log("IS BAD: " + i);
                        break;
                    }
                }
                if (!isBad) pass1.Add(tri);
            }
            // Debug.Log("Pass1: " + pass1.Count);
            List<int[]> pass2 = new List<int[]>();
            for (int i = 0; i < pass1.Count; i++) {
                bool add = true;
                for (int j = 0; j < pass2.Count; j++) {
                    if (j == i) continue;
                    if (
                        (pass2[j][0] == pass1[i][0] || pass2[j][0] == pass1[i][1] || pass2[j][0] == pass1[i][2]) &&
                        (pass2[j][1] == pass1[i][0] || pass2[j][1] == pass1[i][1] || pass2[j][1] == pass1[i][2]) &&
                        (pass2[j][2] == pass1[i][0] || pass2[j][2] == pass1[i][1] || pass2[j][2] == pass1[i][2])
                    ) add = false;
                }
                if (add) pass2.Add(pass1[i]);
            }
            foreach (int[] tri in pass2) addTriangle(tri[0], tri[1], tri[2]);
        }
    }
    private void triangulate2D()
    {
        List<int[]> triangulation = new List<int[]>();
        vertices.AddRange(new Vector3[3] { new Vector3(), new Vector3(), new Vector3() });
        int topIndex = vertices.Count - 1;
        int rightIndex = vertices.Count - 2;
        int leftIndex = vertices.Count - 3;
        bool allInside = false;
        bool removed = false;
        triangulation.Add(new int[] { rightIndex, leftIndex, topIndex });

        // add super-triangle to triangulation
        while (!allInside)
        {
            allInside = true;
            for (int i = 0; i < vertices.Count - 3; i++)
            {
                vertices[topIndex] = new Vector3(vertices[topIndex].x, 0, vertices[topIndex].z + 1);
                vertices[rightIndex] = new Vector3(vertices[rightIndex].x + 1, 0, vertices[rightIndex].z - 1);
                vertices[leftIndex] = new Vector3(vertices[leftIndex].x - 1, 0, vertices[leftIndex].z);
                if (vertices[i].z > vertices[topIndex].z) vertices[topIndex] = new Vector3(vertices[topIndex].x, 0, vertices[i].z + 1);
                if (vertices[i].z < vertices[rightIndex].z)
                {
                    vertices[rightIndex] = new Vector3(vertices[rightIndex].x, 0, vertices[i].z - 1);
                    vertices[leftIndex] = new Vector3(vertices[leftIndex].x, 0, vertices[i].z - 1);
                }
                if (vertices[i].x < vertices[leftIndex].x) vertices[leftIndex] = new Vector3(vertices[i].x - 1, 0, vertices[leftIndex].z);
                if (vertices[i].x > vertices[rightIndex].x) vertices[rightIndex] = new Vector3(vertices[i].x + 1, 0, vertices[rightIndex].z);

                if (!PointInTriangle(vertices[i], vertices[topIndex], vertices[rightIndex], vertices[leftIndex]))
                {
                    allInside = false;
                    continue;
                }
            }
        }

        for (int p = 0; p < vertices.Count; p++)
        {
            Vector3 point = vertices[p];
            List<int[]> badTriangles = new List<int[]>();
            foreach (int[] triangle in triangulation)
            {
                if (PointInTriangle(point, vertices[triangle[0]], vertices[triangle[1]], vertices[triangle[2]]))
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
                    bool edgeInOthers = false;
                    for (int j = 0; j < badTriangles.Count; j++)
                    {
                        if (j == i) continue;
                        int[] b = badTriangles[j];
                        if (edgeInTriangle(edge, b)) edgeInOthers = true;
                        // int[][] edges2 = new int[][] {
                        //     new int[]{b[0], b[1]},
                        //     new int[]{b[1], b[2]},
                        //     new int[]{b[2], b[0]}
                        // };
                        // foreach(int[] edge2 in edges2) {
                        //     if (linesIntersect(
                        //             vertices[edge[0]].x, 
                        //             vertices[edge[0]].z,
                        //             vertices[edge[1]].x, 
                        //             vertices[edge[1]].z,
                        //             vertices[edge2[0]].x, 
                        //             vertices[edge2[0]].z,
                        //             vertices[edge2[1]].x, 
                        //             vertices[edge2[1]].z
                        //         )
                        //     ) {
                        //         edgeInOthers = true;
                        //         // break;
                        //     }
                        // }
                    }
                    if (!edgeInOthers) polygon.Add(edge);
                }
            }

            foreach (int[] badTriangle in badTriangles)
            {
                while (removed)
                {
                    removed = false;
                    foreach (int[] triangle in triangulation)
                    {
                        if (badTriangle[0] == triangle[0] && badTriangle[1] == badTriangle[1] && badTriangle[2] == badTriangle[2])
                        {
                            triangulation.Remove(triangle);
                            removed = true;
                            break;
                        }
                    }
                    // if (removed) continue;
                }
            }

            foreach (int[] edge in polygon)
            {
                triangulation.Add(new int[3] { edge[0], edge[1], p });
            }
        }

        removed = true;
        while (removed)
        {
            removed = false;
            foreach (int[] triangle in triangulation)
            {
                if (
                    triangle[0] == rightIndex || triangle[0] == leftIndex || triangle[0] == topIndex ||
                    triangle[1] == rightIndex || triangle[1] == leftIndex || triangle[1] == topIndex ||
                    triangle[2] == rightIndex || triangle[2] == leftIndex || triangle[2] == topIndex
                )
                {
                    triangulation.Remove(triangle);
                    removed = true;
                    break;
                }
            }
        }

        triangles = new List<int>();
        foreach (int[] triangle in triangulation)
        {
            addTriangle(triangle[0], triangle[1], triangle[2]);
        }
    }

    private float getAngle(Vector2 a, Vector2 b)
    {
        float angle = Mathf.Atan2(b.y - a.y, b.x - a.x);
        return angle;
    }

    private Vector2 vec3to2(Vector3 a)
    {
        return new Vector2(a.x, a.z);
    }

    private void addTriangle(int a, int b, int c)
    {
        Vector2 midpoint = (vec3to2(vertices[a]) + vec3to2(vertices[b]) + vec3to2(vertices[c])) / 3;

        float aa = getAngle(midpoint, vec3to2(vertices[a]));
        float ab = getAngle(midpoint, vec3to2(vertices[b]));
        float ac = getAngle(midpoint, vec3to2(vertices[c]));

        if (aa > ab && aa > ac)
        {
            if (ab > ac) triangles.AddRange(new int[3] { a, b, c });
            else triangles.AddRange(new int[3] { a, c, b });
        }
        else if (ab > aa && ab > ac)
        {
            if (aa > ac) triangles.AddRange(new int[3] { b, a, c });
            else triangles.AddRange(new int[3] { b, c, a });
        }
        else
        {
            if (aa > ab) triangles.AddRange(new int[3] { c, a, b });
            else triangles.AddRange(new int[3] { c, b, a });
        }
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

    private float sign(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
    }

    // private bool linesIntersect(int[] a, int[] b, int[] c, int[] d) {
    //     return linesIntersect(
    //         vertices[a[0]].x,
    //         vertices[a[0]].z,
    //         vertices[b[0]].x,
    //         vertices[b[0]].z,
    //         vertices[c[0]].x,
    //         vertices[c[0]].z,
    //         vertices[d[0]].x,
    //         vertices[d[0]].z
    //     );
    // }

    private bool trianglesIntersect(int[] a, int[]b) {
        Vector2[][] triangle1 = new Vector2[3][] {
            new Vector2[2] {new Vector2(vertices[a[0]].x, vertices[a[0]].z), new Vector2(vertices[a[1]].x, vertices[a[1]].z)},
            new Vector2[2] {new Vector2(vertices[a[1]].x, vertices[a[1]].z), new Vector2(vertices[a[2]].x, vertices[a[2]].z)},
            new Vector2[2] {new Vector2(vertices[a[2]].x, vertices[a[2]].z), new Vector2(vertices[a[0]].x, vertices[a[0]].z)},
        };

        Vector2[][] triangle2 = new Vector2[3][] {
            new Vector2[2] {new Vector2(vertices[b[0]].x, vertices[b[0]].z), new Vector2(vertices[b[1]].x, vertices[b[1]].z)},
            new Vector2[2] {new Vector2(vertices[b[1]].x, vertices[b[1]].z), new Vector2(vertices[b[2]].x, vertices[b[2]].z)},
            new Vector2[2] {new Vector2(vertices[b[2]].x, vertices[b[2]].z), new Vector2(vertices[b[0]].x, vertices[b[0]].z)},
        };

        foreach(Vector2[] sidea in triangle1) {
            foreach(Vector2[] sideb in triangle2) {
                if (linesIntersect(sidea[0].x, sidea[0].y, sidea[1].x, sidea[1].y, sideb[0].x, sideb[0].y, sideb[1].x, sideb[1].y)) return true;
            }
        }
        return false;
    }

    private bool linesIntersect(float a, float b, float c, float d, float p, float q, float r, float s)
    {
        // var det, gamma, lambda;
        float det;
        float gamma;
        float lambda;
        det = (c - a) * (s - q) - (r - p) * (d - b);
        if (det == 0)
        {
            return false;
        }
        else
        {
            lambda = ((s - q) * (r - a) + (p - r) * (s - b)) / det;
            gamma = ((b - d) * (r - a) + (c - a) * (s - b)) / det;
            return (0 < lambda && lambda < 1) && (0 < gamma && gamma < 1);
        }
    }

    private Vector2 getCircumcenter(Vector3 a, Vector3 b, Vector3 c)
    {
        float ax = a.x;
        float ay = a.z;
        float bx = b.x;
        float by = b.z;
        float cx = c.x;
        float cy = c.z;
        float d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        float ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
        float uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;
        return new Vector2(ux, uy);
        // float m1 = c1 + c3;
        // float m2 = c2 + c4;
        // float m3 = m1 / 2;
        // float m4 = m2 / 2;
        // float s1a = c3 - c1;
        // float s1b = c4 - c2;
        // float s1c = s1a / s1b;
        // float s2a = c5 - c3;
        // float s2b = c6 - c4;
        // float s2c = s2a / s2b;
        // float s1 = -1 / s1c;
        // float s2 = -1 / s2c;

        // Vector3 ab = (a + b) / 2;
        // float d1 = Mathf.Atan2(b.x - a.x, b.z - a.z);
        // Vector2 d = new Vector2(
        //     ab.x + Mathf.Cos(d1),
        //     ab.z + Mathf.Sin(d1)
        // );

        // Vector3 bc = (b + c) / 2;
        // float d2 = Mathf.Atan2(c.x - b.x, c.z - b.z);
        // Vector2 e = new Vector2(
        //     bc.x + Mathf.Cos(d2),
        //     bc.z + Mathf.Cos(d2)
        // );

        // float x1 = a.x;
        // float y1 = a.z;
        // float x2 = d.x;
        // float y2 = d.y;
        // float x3 = b.x;
        // float y3 = b.z;
        // float x4 = e.x;
        // float y4 = e.y;

        // float denom = (y4 - y3)*(x2 - x1) - (x4 - x3)*(y2 - y1);
        // float ua = ((x4 - x3)*(y1 - y3) - (y4 - y3)*(x1 - x3))/denom;
        // float ub = ((x2 - x1)*(y1 - y3) - (y2 - y1)*(x1 - x3))/denom;

        // return new Vector2(
        //     x1 + ua * (x2 - x1),
        //     y1 + ua * (y2 - y1)
        // );
    }

    private bool PointInTriangle(Vector3 pt, Vector3 v1, Vector3 v2, Vector3 v3)
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
