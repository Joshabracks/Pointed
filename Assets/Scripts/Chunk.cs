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
    private int startNeighbors;
    private Vector2 offset;
    private int vertexNeighborCutoff;


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
                        .PerlinNoise(x + (offset.x * size) + seed * .517f,
                        z + (offset.y * size) + seed * .517f) >
                    threshold
                )
                {
                    float offsetX =
                        Mathf
                            .Abs(Mathf
                                .PerlinNoise(x + (offset.x * size) + seed * .1231f,
                                z + (offset.y * size) + seed * .1231f));

                    float offsetZ =
                        Mathf
                            .Abs(Mathf
                                .PerlinNoise(x + (offset.x * size) + seed * .5134f,
                                z + (offset.y * size) + seed * .5134f));

                    float xVal = offsetX + x + (offset.x * size);
                    float zVal = offsetZ + z + (offset.y * size);
                    Vector3 vertex = new Vector3(xVal, 0, zVal);
                    vertices.Add(vertex);
                }
            }
        }

        startNeighbors = vertices.Count;

        if (NeighborVerticesEast != null) foreach (Vector3 vertex in NeighborVerticesEast)
            {
                if (!vertices.Contains(vertex)) vertices.Add(vertex);
            }
        if (NeighborVerticesWest != null) foreach (Vector3 vertex in NeighborVerticesWest)
            {
                if (!vertices.Contains(vertex)) vertices.Add(vertex);
            }
        if (NeighborVerticesNorth != null) foreach (Vector3 vertex in NeighborVerticesNorth)
            {
                if (!vertices.Contains(vertex)) vertices.Add(vertex);
            }
        if (NeighborVerticesSouth != null) foreach (Vector3 vertex in NeighborVerticesSouth)
            {
                if (!vertices.Contains(vertex)) vertices.Add(vertex);
            }

        vertices.Add(new Vector3((size / 2) + (offset.x * size), 0, (size * 1.5f) + (offset.y * size))); // North vertex
        vertices.Add(new Vector3(-(size / 2) + (offset.x * size), 0, (size / 2) + (offset.y * size))); // West vertex
        vertices.Add(new Vector3((size * 1.5f) + (offset.x * size), 0, (size / 2) + (offset.y * size))); // East vertex
        vertices.Add(new Vector3((size / 2) + (offset.x * size), 0, -(size / 2) + (offset.y * size))); // South vertex
    }

    public void Triangulate() // implimentation of delaunay triangulation
    {
        // reserve vertex lists for neighboring chunk use
        NorthVertices = new List<int>();
        SouthVertices = new List<int>();
        WestVertices = new List<int>();
        EastVertices = new List<int>();
        triangles = new List<int>();
        List<int[]> suspectTriangles = new List<int[]>();

        for (int a = 0; a < vertices.Count - 5; a++)
        {
            for (int b = a + 1; b < vertices.Count - 4; b++)
            {
                if (b == a) continue;
                for (int c = b + 1; c < vertices.Count; c++)
                {
                    if (c == a || c == b) continue;
                    Vector2 circumcenter = getCircumcenter(vertices[a], vertices[b], vertices[c]);
                    // Debug.Log(circumcenter);
                    float radius = Vector2.Distance(circumcenter, new Vector2(vertices[a].x, vertices[a].z));
                    // if (radius == float.NaN) continue;

                    bool isBad = false;
                    int stopPointScore = 0;
                    if (a >= startNeighbors && a < vertices.Count - 4) stopPointScore++;
                    if (b >= startNeighbors && b < vertices.Count - 4) stopPointScore++;
                    if (c >= startNeighbors && c < vertices.Count - 4) stopPointScore++;

                    int stopPoint = stopPointScore == 2 ? vertices.Count - 4 : vertices.Count;


                    for (int i = 0; i < stopPoint; i++)
                    {
                        if (i == a || i == b || i == c) continue;
                        if (Vector2.Distance(circumcenter, new Vector2(vertices[i].x, vertices[i].z)) < radius)
                        {
                            if (stopPointScore == 1 && c < vertices.Count - 4 && i > vertices.Count - 4)
                            {
                                Debug.DrawLine(vertices[a], vertices[b], Color.magenta, 5000);
                                Debug.DrawLine(vertices[a], vertices[c], Color.magenta, 5000);
                                Debug.DrawLine(vertices[c], vertices[b], Color.magenta, 5000);
                                suspectTriangles.Add(new int[3] { a, b, c });
                            }
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
                                if (!SouthVertices.Contains(b) && b < vertices.Count - 4) SouthVertices.Add(b);
                                if (!SouthVertices.Contains(c) && c < vertices.Count - 4) SouthVertices.Add(c);
                            }
                            else if (a == vertices.Count - 3)
                            {
                                // East vertex
                                if (!EastVertices.Contains(b) && b < vertices.Count - 4) EastVertices.Add(b);
                                if (!EastVertices.Contains(c) && c < vertices.Count - 4) EastVertices.Add(c);
                            }
                            else if (a == vertices.Count - 2)
                            {
                                // West vertex
                                if (!WestVertices.Contains(b) && b < vertices.Count - 4) WestVertices.Add(b);
                                if (!WestVertices.Contains(c) && c < vertices.Count - 4) WestVertices.Add(c);
                            }
                            else
                            {
                                // North vertex
                                if (!NorthVertices.Contains(b) && b < vertices.Count - 4) NorthVertices.Add(b);
                                if (!NorthVertices.Contains(c) && c < vertices.Count - 4) NorthVertices.Add(c);
                            }
                        }
                        if (b >= vertices.Count - 4)
                        {
                            neighborAdded = true;
                            if (b == vertices.Count - 4)
                            {
                                // South vertex
                                if (!SouthVertices.Contains(a) && a < vertices.Count - 4) SouthVertices.Add(a);
                                if (!SouthVertices.Contains(c) && c < vertices.Count - 4) SouthVertices.Add(c);
                            }
                            else if (b == vertices.Count - 3)
                            {
                                // East vertex
                                if (!EastVertices.Contains(a) && a < vertices.Count - 4) EastVertices.Add(a);
                                if (!EastVertices.Contains(c) && c < vertices.Count - 4) EastVertices.Add(c);
                            }
                            else if (b == vertices.Count - 2)
                            {
                                // West vertex
                                if (!WestVertices.Contains(a) && a < vertices.Count - 4) WestVertices.Add(a);
                                if (!WestVertices.Contains(c) && c < vertices.Count - 4) WestVertices.Add(c);
                            }
                            else
                            {
                                // North vertex
                                if (!NorthVertices.Contains(a) && a < vertices.Count - 4) NorthVertices.Add(a);
                                if (!NorthVertices.Contains(c) && c < vertices.Count - 4) NorthVertices.Add(c);
                            }
                        }
                        if (c >= vertices.Count - 4)
                        {
                            neighborAdded = true;
                            if (c == vertices.Count - 4)
                            {
                                // South vertex
                                if (!SouthVertices.Contains(b) && b < vertices.Count - 4) SouthVertices.Add(b);
                                if (!SouthVertices.Contains(a) && a < vertices.Count - 4) SouthVertices.Add(a);
                            }
                            else if (c == vertices.Count - 3)
                            {
                                // East vertex
                                if (!EastVertices.Contains(b) && b < vertices.Count - 4) EastVertices.Add(b);
                                if (!EastVertices.Contains(a) && a < vertices.Count - 4) EastVertices.Add(a);
                            }
                            else if (c == vertices.Count - 2)
                            {
                                // West vertex
                                if (!WestVertices.Contains(b) && b < vertices.Count - 4) WestVertices.Add(b);
                                if (!WestVertices.Contains(a) && a < vertices.Count - 4) WestVertices.Add(a);
                            }
                            else
                            {
                                // North vertex
                                if (!NorthVertices.Contains(b) && b < vertices.Count - 4) NorthVertices.Add(b);
                                if (!NorthVertices.Contains(a) && a < vertices.Count - 4) NorthVertices.Add(a);
                            }
                        }
                        if (!neighborAdded)
                        {
                            bool na = false;
                            bool nb = false;
                            bool nc = false;
                            if (NeighborVerticesEast != null)
                            {
                                foreach (Vector3 vertex in NeighborVerticesEast)
                                {
                                    if (!na && Vector3.Equals(vertices[a], vertex))
                                    {
                                        na = true;
                                    }
                                    if (!nb && Vector3.Equals(vertices[b], vertex))
                                    {
                                        nb = true;
                                    }
                                    if (!nc && Vector3.Equals(vertices[c], vertex))
                                    {
                                        nc = true;
                                    }
                                    if (na && nb && nc) break;
                                }
                            }
                            if (NeighborVerticesWest != null && (!na || !nb || !nc))
                            {
                                foreach (Vector3 vertex in NeighborVerticesWest)
                                {
                                    if (!na && Vector3.Equals(vertices[a], vertex))
                                    {
                                        na = true;
                                    }
                                    if (!nb && Vector3.Equals(vertices[b], vertex))
                                    {
                                        nb = true;
                                    }
                                    if (!nc && Vector3.Equals(vertices[c], vertex))
                                    {
                                        nc = true;
                                    }
                                    if (na && nb && nc) break;
                                }
                            }
                            if (NeighborVerticesNorth != null && (!na || !nb || !nc))
                            {
                                foreach (Vector3 vertex in NeighborVerticesNorth)
                                {
                                    if (!na && Vector3.Equals(vertices[a], vertex))
                                    {
                                        na = true;
                                    }
                                    if (!nb && Vector3.Equals(vertices[b], vertex))
                                    {
                                        nb = true;
                                    }
                                    if (!nc && Vector3.Equals(vertices[c], vertex))
                                    {
                                        nc = true;
                                    }
                                    if (na && nb && nc) break;
                                }
                            }
                            if (NeighborVerticesSouth != null && (!na || !nb || !nc))
                            {
                                foreach (Vector3 vertex in NeighborVerticesSouth)
                                {
                                    if (!na && Vector3.Equals(vertices[a], vertex))
                                    {
                                        na = true;
                                    }
                                    if (!nb && Vector3.Equals(vertices[b], vertex))
                                    {
                                        nb = true;
                                    }
                                    if (!nc && Vector3.Equals(vertices[c], vertex))
                                    {
                                        nc = true;
                                    }
                                    if (na && nb && nc) break;
                                }
                            }

                            if (na && nb && nc)
                            {

                                Vector3 center = (vertices[a] + vertices[b] + vertices[c]) / 3;
                                center.y = 1;
                                Ray ray = new Ray(center, Vector3.down);
                                RaycastHit hit;
                                if (!Physics.Raycast(ray, out hit))
                                {
                                    na = false;
                                    nb = false;
                                    nc = false;
                                }
                            }

                            if (!na || !nb || !nc) {
                                
                                    addTriangle(a, b, c);
                                }
                        }
                    }
                }
            }
        }
        List<int[]> addSuspects = new List<int[]>();
        foreach (int[] tri in suspectTriangles)
        {
            List<List<int>> sharedSides = new List<List<int>>();
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                List<int> sharedCorners = new List<int>();
                if (a == tri[0] || a == tri[1] || a == tri[2]) sharedCorners.Add(a);
                if (b == tri[0] || b == tri[1] || b == tri[2]) sharedCorners.Add(b);
                if (c == tri[0] || c == tri[1] || c == tri[2]) sharedCorners.Add(c);
                if (sharedCorners.Count == 2) sharedSides.Add(sharedCorners);
                if (sharedSides.Count == 3)
                {
                    break;
                }
            }
            if (sharedSides.Count == 3) {
                if (
                    (sharedSides[0][0] == sharedSides[1][0] || sharedSides[0][0] == sharedSides[1][1] || sharedSides[0][1] == sharedSides[1][0] || sharedSides[0][1] == sharedSides[1][1]) &&
                    (sharedSides[0][0] == sharedSides[2][0] || sharedSides[0][0] == sharedSides[2][1] || sharedSides[0][1] == sharedSides[2][0] || sharedSides[0][1] == sharedSides[2][1]) &&
                    (sharedSides[1][0] == sharedSides[2][0] || sharedSides[1][0] == sharedSides[2][1] || sharedSides[1][1] == sharedSides[2][0] || sharedSides[1][1] == sharedSides[2][1])
                ) addSuspects.Add(tri);
            }
        }
        foreach (int[] tri in addSuspects) {
            addTriangle(tri[0], tri[1], tri[2]);
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
        meshFilter.mesh = mesh;
        gameObject.AddComponent<MeshCollider>();

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
        // return getCircumcenter(new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z));

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

    private void lineFromPoints(Vector2 P, Vector2 Q, out float a, out float b, out float c)
    {
        a = Q.y - P.y;
        b = P.x - Q.x;
        c = (a * P.x) + (b * P.y);
    }

    private float[] perpendicularBisectorFromLine(Vector2 P, Vector2 Q, float a, float b, float c)
    {
        Vector2 midPoint = new Vector2((P.x + Q.x) / 2, (P.y + Q.y) / 2);
        c = (-b * midPoint.x) + (a * midPoint.y);
        float temp = a;
        a = -b;
        b = temp;
        return new float[3] { a, b, c };
    }

    private Vector2 lineLineIntersection(float a1, float b1, float c1, float a2, float b2, float c2)
    {
        float determinant = (a1 * b2) - (a2 * b1);
        if (determinant == 0)
        {
            return new Vector2(float.MaxValue, float.MaxValue);
        }
        else
        {
            float x = ((b2 * c1) - (b1 * c2) / determinant);
            float y = ((a1 * c2) - (a2 * c1) / determinant);
            return new Vector2(x, y);
        }
    }

    private Vector2 getCircumcenter(Vector2 P, Vector2 Q, Vector2 R)
    {
        float a;
        float b;
        float c;
        lineFromPoints(P, Q, out a, out b, out c);
        float e;
        float f;
        float g;
        lineFromPoints(P, Q, out e, out f, out g);
        float[] abc = perpendicularBisectorFromLine(P, Q, a, b, c);
        a = abc[0];
        b = abc[1];
        c = abc[2];
        float[] efg = perpendicularBisectorFromLine(P, Q, e, f, g);
        e = efg[0];
        f = efg[1];
        g = efg[2];

        Vector2 circumcenter = lineLineIntersection(a, b, c, e, f, g);
        return circumcenter;


    }
}
