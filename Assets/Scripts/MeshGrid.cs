using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                if (Mathf.PerlinNoise(x + seed * .517f, z + seed * .517f) > threshold)
                {
                    float offsetX = Mathf.PerlinNoise(x + seed * .1231f, z + seed * .1231f);
                    float offsetZ = Mathf.PerlinNoise(x + seed * .5134f, z + seed * .5134f);
                    
                    float xVal = offsetX + x;
                    float zVal = offsetZ + z;
                    vertices.Add(new Vector3(
                        xVal,
                        0,
                        zVal
                    ));
                }
            }
        }
        // Debug.Log("VERTICES: " + vertices.Count);
        triangulate();
        // Debug.Log("TRIANGLES: " + triangles.Count / 3);
    }

    private void triangulate()
    {
        triangles = new List<int>();
        List<int[]> possibleTris = new List<int[]>();
        for (int a = 0; a < vertices.Count; a++)
        {
            for (int b = 0; b < vertices.Count; b++)
            {
                if (b == a) continue;
                for (int c = 0; c < vertices.Count; c++)
                {
                    if (c == a || c == b) continue;
                    if (collinear(vertices[a].x, vertices[a].z, vertices[b].x, vertices[b].z, vertices[c].x, vertices[c].z)) continue;
                    possibleTris.Add(new int[3] { a, b, c });
                }
            }
        }
        List<int[]> pass1 = new List<int[]>();
        foreach (int[] tri in possibleTris)
        {
            Vector2 circumcenter = getCircumcenter(vertices[tri[0]], vertices[tri[1]], vertices[tri[2]]);
            float radius = Vector2.Distance(circumcenter, new Vector2(vertices[tri[0]].x, vertices[tri[0]].z));
            float radius2 = Vector2.Distance(circumcenter, new Vector2(vertices[tri[1]].x, vertices[tri[1]].z));
            float radius3 = Vector2.Distance(circumcenter, new Vector2(vertices[tri[2]].x, vertices[tri[2]].z));

            if (radius2 > radius) radius = radius2;
            if (radius3 > radius) radius = radius3;
            if (radius == float.NaN) continue;
            
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
                    break;
                }
            }
            if (!isBad) pass1.Add(tri);
        }

        List<int[]> pass2 = new List<int[]>();
        for (int i = 0; i < pass1.Count; i++)
        {
            bool add = true;
            for (int j = 0; j < pass2.Count; j++)
            {
                if (j == i) continue;
                if (
                    (pass2[j][0] == pass1[i][0] || pass2[j][0] == pass1[i][1] || pass2[j][0] == pass1[i][2]) &&
                    (pass2[j][1] == pass1[i][0] || pass2[j][1] == pass1[i][1] || pass2[j][1] == pass1[i][2]) &&
                    (pass2[j][2] == pass1[i][0] || pass2[j][2] == pass1[i][1] || pass2[j][2] == pass1[i][2])
                ) add = false;
            }
            if (add) pass2.Add(pass1[i]);
        }
        foreach (int[] tri in pass2) {
            addTriangle(tri[0], tri[1], tri[2]);
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

    private float sign(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
    }

    private bool trianglesIntersect(int[] a, int[] b)
    {
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

        foreach (Vector2[] sidea in triangle1)
        {
            foreach (Vector2[] sideb in triangle2)
            {
                if (linesIntersect(sidea[0].x, sidea[0].y, sidea[1].x, sidea[1].y, sideb[0].x, sideb[0].y, sideb[1].x, sideb[1].y)) return true;
            }
        }

        return false;
    }

    private bool linesIntersect(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
    {

        float det = (x2 - x1) * (y4 - y3) - (x4 - x3) * (y2 - y1);
        if (det == 0)
        {
            float alt = (x3 - x2) * (x1 - x4) + (y3 - y2) * (y1 - y4);
            if (alt == 0)
            {

            };
            return false;
        }
        else
        {
            float lambda = ((y4 - y3) * (x4 - x1) + (x3 - x4) * (y4 - y1)) / det;
            float gamma = ((y1 - y2) * (x4 - x1) + (x2 - x1) * (y4 - y1)) / det;
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

    private bool collinear(float x1, float y1, float x2, float y2, float x3, float y3)
    {

        float a = x1 * (y2 - y3) +
                x2 * (y3 - y1) +
                x3 * (y1 - y2);

        if (a == 0) return true;
        return false;

    }
}
