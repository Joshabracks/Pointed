using System.Collections.Generic;
using UnityEngine;

public class MeshGrid
{
    public int seed;

    private int size;

    public bool running = false;

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
                if (
                    Mathf.PerlinNoise(x + seed * .517f, z + seed * .517f) >
                    threshold
                )
                {
                    float offsetX =
                        Mathf.PerlinNoise(x + seed * .1231f, z + seed * .1231f);
                    float offsetZ =
                        Mathf.PerlinNoise(x + seed * .5134f, z + seed * .5134f);

                    float xVal = offsetX + x;
                    float zVal = offsetZ + z;
                    Vector3 vertex = new Vector3(xVal, 0, zVal);
                    vertices.Add (vertex);
                }
            }
        }
        triangulate();
    }

    private void triangulate()
    {
        triangles = new List<int>();

        for (int a = 0; a < vertices.Count - 2; a++)
        {
            for (int b = a + 1; b < vertices.Count - 1; b++)
            {
                if (b == a) continue;
                for (int c = b + 1; c < vertices.Count; c++)
                {
                    if (c == a || c == b) continue;
                    Vector2 circumcenter =
                        getCircumcenter(vertices[a], vertices[b], vertices[c]);
                    float radius =
                        Vector2
                            .Distance(circumcenter,
                            new Vector2(vertices[a].x, vertices[a].z));
                    if (radius == float.NaN) continue;
                    bool isBad = false;
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        if (i == a || i == b || i == c) continue;
                        if (
                            Vector2
                                .Distance(circumcenter,
                                new Vector2(vertices[i].x, vertices[i].z)) <
                            radius
                        )
                        {
                            isBad = true;
                            break;
                        }
                    }
                    if (!isBad)
                    {
                        addTriangle (a, b, c);
                    }
                }
            }
        }
        running = false;
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
        Vector2 midpoint =
            (
            vec3to2(vertices[a]) + vec3to2(vertices[b]) + vec3to2(vertices[c])
            ) /
            3;

        float aa = getAngle(midpoint, vec3to2(vertices[a]));
        float ab = getAngle(midpoint, vec3to2(vertices[b]));
        float ac = getAngle(midpoint, vec3to2(vertices[c]));

        if (aa > ab && aa > ac)
        {
            if (ab > ac)
                triangles.AddRange(new int[3] { a, b, c });
            else
                triangles.AddRange(new int[3] { a, c, b });
        }
        else if (ab > aa && ab > ac)
        {
            if (aa > ac)
                triangles.AddRange(new int[3] { b, a, c });
            else
                triangles.AddRange(new int[3] { b, c, a });
        }
        else
        {
            if (aa > ab)
                triangles.AddRange(new int[3] { c, a, b });
            else
                triangles.AddRange(new int[3] { c, b, a });
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
        float ux =
            (
            (ax * ax + ay * ay) * (by - cy) +
            (bx * bx + by * by) * (cy - ay) +
            (cx * cx + cy * cy) * (ay - by)
            ) /
            d;
        float uy =
            (
            (ax * ax + ay * ay) * (cx - bx) +
            (bx * bx + by * by) * (ax - cx) +
            (cx * cx + cy * cy) * (bx - ax)
            ) /
            d;
        return new Vector2(ux, uy);
    }
}
