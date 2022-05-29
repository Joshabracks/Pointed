using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public int seed;

    private int size;

    public bool running = false;

    private float threshold;

    public Material material;

    Mesh mesh;

    MeshRenderer meshRenderer;

    MeshFilter meshFilter;

    public List<Vector3> vertices;

    public List<int> NorthVertices = null;

    public List<int> WestVertices = null;

    public List<int> SouthVertices = null;

    public List<int> EastVertices = null;

    public Vector3[] NeighborVerticesNorth = null;

    public Vector3[] NeighborVerticesSouth = null;

    public Vector3[] NeighborVerticesEast = null;

    public Vector3[] NeighborVerticesWest = null;

    public List<int> triangles;

    private Vector2 offset;
    private int vertexNeighborCutoff;

    void OnDrawGizmos()
    {
        if (Selection.Contains(gameObject))
        {
            if (NeighborVerticesEast != null)
            {
                Gizmos.color = Color.green;
                foreach (Vector3 v in NeighborVerticesEast)
                {
                    Gizmos.DrawSphere(v, 0.1f);
                }
            }

            if (NeighborVerticesWest != null)
            {
                Gizmos.color = Color.magenta;
                foreach (Vector3 v in NeighborVerticesWest)
                {
                    Gizmos.DrawSphere(v, 0.1f);
                }
            }

            if (NeighborVerticesNorth != null)
            {
                Gizmos.color = Color.yellow;
                foreach (Vector3 v in NeighborVerticesNorth)
                {
                    Gizmos.DrawSphere(v, 0.1f);
                }
            }

            if (NeighborVerticesSouth != null)
            {
                Gizmos.color = Color.cyan;
                foreach (Vector3 v in NeighborVerticesSouth)
                {
                    Gizmos.DrawSphere(v, 0.1f);
                }
            }
        }
    }

    public void Init(
        int seed,
        int size,
        float threshold,
        Vector2 offset,
        Material material
    )
    {
        this.seed = seed;
        this.size = size;
        this.threshold = threshold;
        this.material = material;
        this.offset = offset;
        vertices = new List<Vector3>();
        triangles = new List<int>();

        // gameObject.transform.position =
        //     new Vector3(offset.x * size, 0, offset.y * size);
        gameObject.name = $"{offset.x},{offset.y}";
    }

    public void AddVertices()
    {
        vertices = new List<Vector3>();
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                if (
                    Mathf
                        .PerlinNoise(x + offset.x + seed * .517f,
                        z + offset.y + seed * .517f) >
                    threshold
                )
                {
                    float offsetX =
                        Mathf
                            .Abs(Mathf
                                .PerlinNoise(x + offset.x + seed * .1231f,
                                z + offset.y + seed * .1231f));
                    float offsetZ =
                        Mathf
                            .Abs(Mathf
                                .PerlinNoise(x + offset.x + seed * .5134f,
                                z + offset.y + seed * .5134f));

                    float xVal = offsetX + x + (offset.x * size);
                    float zVal = offsetZ + z + (offset.y * size);
                    Vector3 vertex = new Vector3(xVal, 0, zVal);
                    vertices.Add (vertex);
                }
            }
        }

        if (NeighborVerticesEast != null)
        {
            vertices.AddRange (NeighborVerticesEast);
        }
        if (NeighborVerticesWest != null)
        {
            vertices.AddRange (NeighborVerticesWest);
        }
        if (NeighborVerticesNorth != null)
        {
            vertices.AddRange (NeighborVerticesNorth);
        }
        if (NeighborVerticesSouth != null)
        {
            vertices.AddRange (NeighborVerticesSouth);
        }

        vertices
            .Add(new Vector3((size / 2) + (offset.x * size),
                0,
                (size * 1.5f) + (offset.y * size))); // North vertex
        vertices
            .Add(new Vector3(-(size / 2) + (offset.x * size),
                0,
                (size / 2) + (offset.y * size))); // West vertex
        vertices
            .Add(new Vector3((size * 1.5f) + (offset.x * size),
                0,
                (size / 2) + (offset.y * size))); // East vertex
        vertices
            .Add(new Vector3((size / 2) + (offset.x * size),
                0,
                -(size / 2) + (offset.y * size))); // South vertex
    }

    public void Triangulate() // implimentation of delaunay triangulation
    {
        // reserve vertex lists for neighboring chunk use
        NorthVertices = new List<int>();
        SouthVertices = new List<int>();
        WestVertices = new List<int>();
        EastVertices = new List<int>();
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
                        bool neighborAdded = false;
                        if (a >= vertices.Count - 4)
                        {
                            neighborAdded = true;
                            if (a == vertices.Count - 4)
                            {
                                // South vertex
                                if (
                                    !NorthVertices.Contains(b) &&
                                    b < vertices.Count - 4
                                ) NorthVertices.Add(b);
                                if (
                                    !NorthVertices.Contains(c) &&
                                    c < vertices.Count - 4
                                ) NorthVertices.Add(c);
                            }
                            else if (a == vertices.Count - 3)
                            {
                                // East vertex
                                if (
                                    !EastVertices.Contains(b) &&
                                    b < vertices.Count - 4
                                ) EastVertices.Add(b);
                                if (
                                    !EastVertices.Contains(c) &&
                                    c < vertices.Count - 4
                                ) EastVertices.Add(c);
                            }
                            else if (a == vertices.Count - 2)
                            {
                                // West vertex
                                if (
                                    !WestVertices.Contains(b) &&
                                    b < vertices.Count - 4
                                ) WestVertices.Add(b);
                                if (
                                    !WestVertices.Contains(c) &&
                                    c < vertices.Count - 4
                                ) WestVertices.Add(c);
                            }
                            else
                            {
                                // North vertex
                                if (
                                    !NorthVertices.Contains(b) &&
                                    b < vertices.Count - 4
                                ) NorthVertices.Add(b);
                                if (
                                    !NorthVertices.Contains(c) &&
                                    c < vertices.Count - 4
                                ) NorthVertices.Add(c);
                            }
                        }
                        if (b >= vertices.Count - 4)
                        {
                            neighborAdded = true;
                            if (b == vertices.Count - 4)
                            {
                                // South vertex
                                if (
                                    !NorthVertices.Contains(a) &&
                                    a < vertices.Count - 4
                                ) NorthVertices.Add(a);
                                if (
                                    !NorthVertices.Contains(c) &&
                                    c < vertices.Count - 4
                                ) NorthVertices.Add(c);
                            }
                            else if (b == vertices.Count - 3)
                            {
                                // East vertex
                                if (
                                    !EastVertices.Contains(a) &&
                                    a < vertices.Count - 4
                                ) EastVertices.Add(a);
                                if (
                                    !EastVertices.Contains(c) &&
                                    c < vertices.Count - 4
                                ) EastVertices.Add(c);
                            }
                            else if (b == vertices.Count - 2)
                            {
                                // West vertex
                                if (
                                    !WestVertices.Contains(a) &&
                                    a < vertices.Count - 4
                                ) WestVertices.Add(a);
                                if (
                                    !WestVertices.Contains(c) &&
                                    c < vertices.Count - 4
                                ) WestVertices.Add(c);
                            }
                            else
                            {
                                // North vertex
                                if (
                                    !NorthVertices.Contains(a) &&
                                    a < vertices.Count - 4
                                ) NorthVertices.Add(a);
                                if (
                                    !NorthVertices.Contains(c) &&
                                    c < vertices.Count - 4
                                ) NorthVertices.Add(c);
                            }
                        }
                        if (c >= vertices.Count - 4)
                        {
                            neighborAdded = true;
                            if (c == vertices.Count - 4)
                            {
                                // South vertex
                                if (
                                    !SouthVertices.Contains(b) &&
                                    b < vertices.Count - 4
                                ) SouthVertices.Add(b);
                                if (
                                    !SouthVertices.Contains(a) &&
                                    a < vertices.Count - 4
                                ) SouthVertices.Add(a);
                            }
                            else if (c == vertices.Count - 3)
                            {
                                // East vertex
                                if (
                                    !EastVertices.Contains(b) &&
                                    b < vertices.Count - 4
                                ) EastVertices.Add(b);
                                if (
                                    !EastVertices.Contains(a) &&
                                    a < vertices.Count - 4
                                ) EastVertices.Add(a);
                            }
                            else if (c == vertices.Count - 2)
                            {
                                // West vertex
                                if (
                                    !WestVertices.Contains(b) &&
                                    b < vertices.Count - 4
                                ) WestVertices.Add(b);
                                if (
                                    !WestVertices.Contains(a) &&
                                    a < vertices.Count - 4
                                ) WestVertices.Add(a);
                            }
                            else
                            {
                                // North vertex
                                if (
                                    !NorthVertices.Contains(b) &&
                                    b < vertices.Count - 4
                                ) NorthVertices.Add(b);
                                if (
                                    !NorthVertices.Contains(a) &&
                                    a < vertices.Count - 4
                                ) NorthVertices.Add(a);
                            }
                        }
                        if (!neighborAdded)
                        {
                            addTriangle (a, b, c);
                        }
                    }
                }
            }
        }

        running = false;
    }

    public void Render()
    {
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer.material = material;
        mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
    }

    public Vector3[] GetEastVertices()
    {
        Vector3[] result = new Vector3[EastVertices.Count];
        for (int i = 0; i < EastVertices.Count; i++)
        {
            result[i] = vertices[EastVertices[i]];
        }
        return result;
    }

    public Vector3[] GetWestVertices()
    {
        Vector3[] result = new Vector3[WestVertices.Count];
        for (int i = 0; i < WestVertices.Count; i++)
        {
            result[i] = vertices[WestVertices[i]];
        }
        return result;
    }

    public Vector3[] GetNorthVertices()
    {
        Vector3[] result = new Vector3[NorthVertices.Count];
        for (int i = 0; i < NorthVertices.Count; i++)
        {
            result[i] = vertices[NorthVertices[i]];
        }
        return result;
    }

    public Vector3[] GetSouthVertices()
    {
        Vector3[] result = new Vector3[SouthVertices.Count];
        for (int i = 0; i < SouthVertices.Count; i++)
        {
            result[i] = vertices[SouthVertices[i]];
        }
        return result;
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
