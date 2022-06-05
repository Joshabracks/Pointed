using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public int seed;

    private int size;

    public bool running = true;

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
    public bool finalCheck = false;


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
        float borderOffset = size / 2;
        vertices.Add(new Vector3((size / 2) + (offset.x * size), 0, (size + borderOffset) + (offset.y * size))); // North vertex
        vertices.Add(new Vector3((-borderOffset) + (offset.x * size), 0, (size / 2) + (offset.y * size))); // West vertex
        vertices.Add(new Vector3((size + borderOffset) + (offset.x * size), 0, (size / 2) + (offset.y * size))); // East vertex
        vertices.Add(new Vector3((size / 2) + (offset.x * size), 0, (-borderOffset) + (offset.y * size))); // South vertex
    }

    public void Triangulate() // implimentation of delaunay triangulation
    {
        // reserve vertex lists for neighboring chunk use
        NorthVertices = new List<int>();
        SouthVertices = new List<int>();
        WestVertices = new List<int>();
        EastVertices = new List<int>();
        triangles = new List<int>();
        List<int> suspectTriangles = new List<int>();
        List<int> neighborTriangles = new List<int>();

        Vector3 chunkCenter = new Vector3((offset.x * size) + (size / 2), 0, (offset.y * size) + (size / 2));

        // Iterate all viable triangle combinations
        // a and b ignore last 4 vertices
        // a/b/c never overlap
        for (int a = 0; a < vertices.Count - 5; a++)
        {
            for (int b = a + 1; b < vertices.Count - 4; b++)
            {
                for (int c = b + 1; c < vertices.Count; c++)
                {
                    // Get circumcenter and circumcircle radius
                    Vector2 circumcenter = getCircumcenter(vertices[a], vertices[b], vertices[c]);
                    float radius = Vector2.Distance(circumcenter, new Vector2(vertices[a].x, vertices[a].z));

                    // Determine if triangle is build with "outer" vertices
                    int stopPointScore = 0;
                    if (a >= startNeighbors && a < vertices.Count - 4) stopPointScore++;
                    if (b >= startNeighbors && b < vertices.Count - 4) stopPointScore++;
                    if (c >= startNeighbors && c < vertices.Count - 4) stopPointScore++;
                    int stopPoint = stopPointScore == 2 ? vertices.Count - 4 : vertices.Count;

                    bool isBad = false;
                    bool isSuspect = false;
                    for (int i = 0; i < stopPoint; i++)
                    {
                        if (i == a || i == b || i == c) continue;
                        if (Vector2.Distance(circumcenter, new Vector2(vertices[i].x, vertices[i].z)) < radius)
                        {
                            if (stopPointScore == 1 && c < vertices.Count - 4 && i > vertices.Count - 4)
                            {
                                Vector2 A = new Vector2(vertices[a].x, vertices[a].z);
                                Vector2 B = new Vector2(vertices[b].x, vertices[b].z);
                                Vector2 C = new Vector2(vertices[c].x, vertices[c].z);

                                float[] angles = findAngles(A, B, C);
                                float maxDegrees = 90;
                                float maxAngle = angles[0];
                                if (angles[1] > maxAngle) maxAngle = angles[1];
                                if (angles[2] > maxAngle) maxAngle = angles[2];

                                if (maxAngle > maxDegrees)
                                {
                                    Vector3 circumcenterV3 = new Vector3(circumcenter.x, 0, circumcenter.y);
                                    float circumDist = Vector3.Distance(circumcenterV3, vertices[a]) + Vector3.Distance(circumcenterV3, vertices[b]) + Vector3.Distance(circumcenterV3, vertices[c]);
                                    float iDist = Vector3.Distance(vertices[i], vertices[a]) + Vector3.Distance(vertices[i], vertices[b]) + Vector3.Distance(vertices[i], vertices[c]);
                                    if (circumDist < iDist)
                                    {
                                        Vector3 triCenter = (vertices[a] + vertices[b] + vertices[c]) / 3;
                                        if (Vector3.Distance(triCenter, chunkCenter) < Vector3.Distance(circumcenterV3, chunkCenter))
                                        {
                                            suspectTriangles.AddRange(new int[3] { a, b, c });
                                            Debug.DrawLine(vertices[a], vertices[b], Color.green, 5000);
                                            Debug.DrawLine(vertices[a], vertices[c], Color.green, 5000);
                                            Debug.DrawLine(vertices[c], vertices[b], Color.green, 5000);
                                            // Debug.DrawLine(circumcenterV3, chunkCenter, Color.blue, 5000);
                                            // Debug.DrawLine(triCenter, chunkCenter, Color.red, 5000);

                                            // Debug.DrawLine(circumcenterV3, vertices[a], Color.cyan, 5000);
                                            // Debug.DrawLine(circumcenterV3, vertices[b], Color.cyan, 5000);
                                            // Debug.DrawLine(circumcenterV3, vertices[c], Color.cyan, 5000);

                                            // Debug.DrawLine(vertices[i], vertices[a], Color.magenta, 5000);
                                            // Debug.DrawLine(vertices[i], vertices[b], Color.magenta, 5000);
                                            // Debug.DrawLine(vertices[i], vertices[c], Color.magenta, 5000);
                                            // isSuspect = true;
                                            // break;
                                        }
                                    }
                                }

                            }
                            isBad = true;
                            break;
                        }
                    }
                    if (isBad || isSuspect) continue;
                    // if (!isBad && !isSuspect)
                    // {
                    bool neighborAdded = false;
                    if (a >= vertices.Count - 4)
                    {
                        neighborAdded = true;
                        neighborTriangles.Add(b);
                        neighborTriangles.Add(c);
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
                        neighborTriangles.Add(c);
                        neighborTriangles.Add(a);
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
                        neighborTriangles.Add(b);
                        neighborTriangles.Add(a);
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
                    // if (neighborAdded)
                    // {
                    //     neighborTriangles.AddRange(new int[] { a, b, c });
                    //     // Debug.DrawLine(vertices[a], vertices[b], Color.magenta, 5000);
                    //     // Debug.DrawLine(vertices[a], vertices[c], Color.magenta, 5000);
                    //     // Debug.DrawLine(vertices[c], vertices[b], Color.magenta, 5000);
                    // }
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

                        if (!na || !nb || !nc)
                        {

                            addTriangle(a, b, c);
                        }
                        // }
                    }
                }
            }
        }
        // if (suspectTriangles.Count > 0)
        // {
        //     for (int i = 0; i < neighborTriangles.Count; i += 3)
        //     {
        //         Debug.DrawLine(vertices[neighborTriangles[i]], vertices[neighborTriangles[i + 1]], Color.magenta, 5000);
        //         Debug.DrawLine(vertices[neighborTriangles[i]], vertices[neighborTriangles[i + 2]], Color.magenta, 5000);
        //         Debug.DrawLine(vertices[neighborTriangles[i + 2]], vertices[neighborTriangles[i + 1]], Color.magenta, 5000);

        //     }
        // }
        // if (suspectTriangles.Count > 0)
        // {

        //     for (int i = 0; i < neighborTriangles.Count; i += 2)
        //     {
        //         Debug.DrawLine(vertices[neighborTriangles[i]], vertices[neighborTriangles[i + 1]], Color.magenta, 5000);
        //     }
        // }
        for (int i = 0; i < suspectTriangles.Count; i += 3)
        {
            List<int> a = new List<int>(); 
            a.AddRange(new int[3] { suspectTriangles[i], suspectTriangles[i + 1], suspectTriangles[i + 2] });
            int sharedSides = 0;
            for (int j = 0; j < triangles.Count; j += 3) {
                int[] b = new int[3]{triangles[j], triangles[j + 1], triangles[j + 2] };
                int sharedPoints = 0;
                if (a.Contains(b[0])) sharedPoints++;
                if (a.Contains(b[1])) sharedPoints++;
                if (a.Contains(b[2])) sharedPoints++;
                if (sharedPoints == 2) sharedSides++;
            }
            // for (int j = 0; j < suspectTriangles.Count; j += 3) {
            //     if (j == i) continue;
            //     int[] b = new int[3]{suspectTriangles[j], suspectTriangles[j + 1], suspectTriangles[j + 2] };
            //     int sharedPoints = 0;
            //     if (a.Contains(b[0])) sharedPoints++;
            //     if (a.Contains(b[1])) sharedPoints++;
            //     if (a.Contains(b[2])) sharedPoints++;
            //     if (sharedPoints == 2) sharedSides++;
            // }
            if (sharedSides == 3) {

            Debug.DrawLine(vertices[a[0]], vertices[a[1]], Color.green, 5000);
            Debug.DrawLine(vertices[a[0]], vertices[a[2]], Color.green, 5000);
            Debug.DrawLine(vertices[a[2]], vertices[a[1]], Color.green, 5000);
            addTriangle(a[0], a[1], a[2]);
            }
            // List<int[]> sharedTriangles = new List<int[]>();
            // int sharedPoints = 0;
            // for (int j = 0; j < neighborTriangles.Count; j += 3) {
            //     sharedPoints = 0;
            //     int[] b = new int[3] {neighborTriangles[j], neighborTriangles[j + 1], neighborTriangles[j + 2]};
            //     if (b[0] == a[0] || b[0] == a[1] || b[0] == a[2]) sharedPoints++;
            //     if (b[1] == a[0] || b[1] == a[1] || b[1] == a[2]) sharedPoints++;
            //     if (b[2] == a[0] || b[2] == a[1] || b[2] == a[2]) sharedPoints++;
            //     if (sharedPoints == 2) {
            //         sharedTriangles.Add(b);
            //         Debug.DrawLine(vertices[b[0]], vertices[b[1]], Color.magenta, 5000);
            //         Debug.DrawLine(vertices[b[0]], vertices[b[2]], Color.magenta, 5000);
            //         Debug.DrawLine(vertices[b[2]], vertices[b[1]], Color.magenta, 5000);
            //     }
            // }
            // if (sharedTriangles.Count != 2) continue;
            // for (int j = 0; j < neighborTriangles.Count; j++) {
            //     int inside  = 0;
            //     int[] b = new int[3] {neighborTriangles[j], neighborTriangles[j + 1], neighborTriangles[j + 2]};

            //     Vector3 centerB = (vertices[b[0]] + vertices[b[1]] + vertices[b[2]]) / 3;
            //     float radius = Vector2.Distance(new Vector2(vertices[b[0]].x, vertices[b[0]].z), centerB);

            //     foreach (int[] c in sharedTriangles) {
            //         Vector3 centerC = ((vertices[c[0]]) + (vertices[c[1]]) + (vertices[c[2]])) / 3;
            //         if (Vector3.Distance(centerB, centerC) < radius) {
            //             inside++;
            //         }
            //     }
            //     if (inside == 2) {
            //         Debug.DrawLine(centerB, vertices[a[0]], Color.blue, 5000);
            //         Debug.DrawLine(centerB, vertices[a[1]], Color.blue, 5000);
            //         Debug.DrawLine(centerB, vertices[a[2]], Color.blue, 5000);

            //         Debug.DrawLine(centerB, vertices[b[0]], Color.red, 5000);
            //         Debug.DrawLine(centerB, vertices[b[1]], Color.red, 5000);
            //         Debug.DrawLine(centerB, vertices[b[2]], Color.red, 5000);

            //         addTriangle(a[0], a[1], a[2]);
            //         break;
            //     }
            // }
        }

        // foreach (int[] a in suspectTriangles)
        //     int isNeighbor = 0;
        //     if (NorthVertices.Contains(a[0]) || SouthVertices.Contains(a[0]) || EastVertices.Contains(a[0]) || WestVertices.Contains(a[0])) isNeighbor++;
        //     if (NorthVertices.Contains(a[1]) || SouthVertices.Contains(a[1]) || EastVertices.Contains(a[1]) || WestVertices.Contains(a[1])) isNeighbor++;
        //     if (NorthVertices.Contains(a[2]) || SouthVertices.Contains(a[2]) || EastVertices.Contains(a[2]) || WestVertices.Contains(a[2])) isNeighbor++;
        //     Debug.Log(isNeighbor);
        //     if (isNeighbor == 1)
        //     {
        //         addTriangle(a[0], a[1], a[2]);
        //     }
        // bool isBad = false;
        // Vector3[][] aSides = new Vector3[3][]{
        //     new Vector3[2]{vertices[a[0]], vertices[a[1]]},
        //     new Vector3[2]{vertices[a[0]], vertices[a[2]]},
        //     new Vector3[2]{vertices[a[2]], vertices[a[1]]}
        // };

        // for (int i = 0; i < triangles.Count; i += 3)
        // {
        //     int[] b = new int[3] { triangles[i], triangles[i + 1], triangles[i + 2] };
        //     Vector3[][] bSides = new Vector3[3][]{
        //         new Vector3[2]{vertices[b[0]], vertices[b[1]]},
        //         new Vector3[2]{vertices[b[0]], vertices[b[2]]},
        //         new Vector3[2]{vertices[b[2]], vertices[b[1]]}
        //     };

        //     foreach (Vector3[] sideA in aSides)
        //     {
        //         if (isBad) break;
        //         foreach (Vector3[] sideB in bSides)
        //         {
        //             // if (LineLineIntersection(sideA[0], sideA[1], sideB[0], sideB[1]))
        //             if (
        //                 intersects(sideA[0].x, sideA[0].z, sideA[1].x, sideA[1].z, sideB[0].x, sideB[0].z, sideB[1].x, sideB[1].z)
        //                 )
        //             {
        //                 isBad = true;
        //                 break;
        //             }
        //         }
        //     }
        //     if (isBad) break;
        // }

        // if (!isBad)
        // {
        //     Debug.DrawLine(vertices[a[0]], vertices[a[1]], Color.green, 5000);
        //     Debug.DrawLine(vertices[a[0]], vertices[a[2]], Color.green, 5000);
        //     Debug.DrawLine(vertices[a[2]], vertices[a[1]], Color.green, 5000);
        //     addTriangle(a[0], a[1], a[2]);
        // }
        // else
        // {
        //     Debug.DrawLine(vertices[a[0]], vertices[a[1]], Color.red, 5000);
        //     Debug.DrawLine(vertices[a[0]], vertices[a[2]], Color.red, 5000);
        //     Debug.DrawLine(vertices[a[2]], vertices[a[1]], Color.red, 5000);
        // }
        // }
    }

    private bool intersects(float a, float b, float c, float d, float p, float q, float r, float s)
    {
        float det, gamma, lambda;
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

    public static bool LineLineIntersection(Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        // if (
        //     Vector3.Equals(linePoint1, linePoint2) ||
        //     Vector3.Equals(linePoint1, lineVec2) ||
        //     Vector3.Equals(lineVec1, linePoint2) ||
        //     Vector3.Equals(lineVec1, lineVec2)
        // ) return false;

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parallel
        if (Mathf.Abs(planarFactor) < 0.0001f
                && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            // float s = Vector3.Dot(crossVec3and2, crossVec1and2)
            //         / crossVec1and2.sqrMagnitude;
            return true;
        }
        else
        {
            return false;
        }
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

        running = false;
    }

    // void Update()
    // {
    //     if (!running && !finalCheck) FinalCheck();
    // }

    public void FinalCheck()
    {
        if (offset.x == 0 || offset.y == 0)
        {
            finalCheck = true;
            return;
        }
        for (int x = 0; x < size * 4; x++)
        {
            for (int z = 0; z < size * 4; z++)
            {
                Ray ray = new Ray(new Vector3(((size * offset.x) + x) / 4, 1, ((size * offset.y) + z) / 4), Vector3.down);
                RaycastHit hit;
                if (!Physics.Raycast(ray, out hit))
                {
                    Vector3[] n = new Vector3[3];
                    Vector3[] s = new Vector3[3];
                    Vector3[] e = new Vector3[3];
                    Vector3[] w = new Vector3[3];
                    Vector3 origin = ray.origin;
                    int maxTries = 40;
                    int tries = 0;
                    while (!Physics.Raycast(ray, out hit) || tries < maxTries)
                    { //probe north
                        ray = new Ray(new Vector3(ray.origin.x, 1, ray.origin.y + .1f), Vector3.down);
                        tries++;
                    }
                    if (tries == maxTries) continue;
                    Mesh mesh = hit.collider.gameObject.GetComponent<MeshFilter>().mesh;
                    n[0] = mesh.vertices[hit.triangleIndex];
                    n[1] = mesh.vertices[hit.triangleIndex + 1];
                    n[2] = mesh.vertices[hit.triangleIndex + 2];

                    tries = 0;
                    ray = new Ray(origin, Vector3.down);

                    while (!Physics.Raycast(ray, out hit) || tries < maxTries)
                    { //probe south
                        ray = new Ray(new Vector3(ray.origin.x, 1, ray.origin.y - .1f), Vector3.down);
                        tries++;
                    }
                    if (tries == maxTries) continue;
                    mesh = hit.collider.gameObject.GetComponent<MeshFilter>().mesh;
                    s[0] = mesh.vertices[hit.triangleIndex];
                    s[1] = mesh.vertices[hit.triangleIndex + 1];
                    s[2] = mesh.vertices[hit.triangleIndex + 2];

                    tries = 0;
                    ray = new Ray(origin, Vector3.down);

                    while (!Physics.Raycast(ray, out hit) || tries < maxTries)
                    { //probe east
                        ray = new Ray(new Vector3(ray.origin.x + .1f, 1, ray.origin.y), Vector3.down);
                        tries++;
                    }
                    if (tries == maxTries) continue;
                    mesh = hit.collider.gameObject.GetComponent<MeshFilter>().mesh;
                    e[0] = mesh.vertices[hit.triangleIndex];
                    e[1] = mesh.vertices[hit.triangleIndex + 1];
                    e[2] = mesh.vertices[hit.triangleIndex + 2];

                    tries = 0;
                    ray = new Ray(origin, Vector3.down);

                    while (!Physics.Raycast(ray, out hit) || tries < maxTries)
                    { //probe west
                        ray = new Ray(new Vector3(ray.origin.x, -.1f, ray.origin.y), Vector3.down);
                        tries++;
                    }
                    if (tries == maxTries) continue;
                    mesh = hit.collider.gameObject.GetComponent<MeshFilter>().mesh;
                    w[0] = mesh.vertices[hit.triangleIndex];
                    w[1] = mesh.vertices[hit.triangleIndex + 1];
                    w[2] = mesh.vertices[hit.triangleIndex + 2];

                    Vector3[] a = n;
                    Vector3[] b = s;
                    Vector3[] c = e;
                    if (Vector3.Equals(a[0], b[0]) && Vector3.Equals(a[1], b[1]) && Vector3.Equals(a[2], b[2]))
                    {
                        b = w;
                    }
                    else if (Vector3.Equals(a[0], c[0]) && Vector3.Equals(a[1], c[1]) && Vector3.Equals(a[2], c[2]))
                    {
                        c = w;
                    }
                    else if (Vector3.Equals(b[0], c[0]) && Vector3.Equals(b[1], c[1]) && Vector3.Equals(b[2], c[2]))
                    {
                        c = w;
                    }

                    List<Vector3> newTri = new List<Vector3>();
                    foreach (Vector3 point1 in a)
                    {
                        foreach (Vector3 point2 in c)
                        {
                            if (Vector3.Equals(point1, point2))
                            {
                                newTri.Add(point1);

                                break;
                            }
                        }
                        foreach (Vector3 point2 in b)
                        {
                            if (Vector3.Equals(point1, point2))
                            {
                                newTri.Add(point1);
                                break;
                            }
                        }
                    }
                    foreach (Vector3 point1 in b)
                    {
                        foreach (Vector3 point2 in c)
                        {
                            if (Vector3.Equals(point1, point2))
                            {
                                newTri.Add(point1);
                                break;
                            }
                        }
                    }

                    if (newTri.Count == 3)
                    {
                        vertices.AddRange(newTri);
                        addTriangle(vertices.Count - 1, vertices.Count - 2, vertices.Count - 3);
                    }
                }
            }
        }
        finalCheck = true;
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

    // private float findAngle(Vector2 A, Vector2 B, Vector2 C)
    // {
    //     var AB = Mathf.Sqrt(Mathf.Pow(B.x - A.x, 2) + Mathf.Pow(B.y - A.y, 2));
    //     var BC = Mathf.Sqrt(Mathf.Pow(B.x - C.x, 2) + Mathf.Pow(B.y - C.y, 2));
    //     var AC = Mathf.Sqrt(Mathf.Pow(C.x - A.x, 2) + Mathf.Pow(C.y - A.y, 2));
    //     return Mathf.Acos((BC * BC + AB * AB - AC * AC) / (2 * BC * AB));
    // }

    private float[] findAngles(Vector2 A, Vector2 B, Vector2 C)
    {
        // Square of lengths be a2, b2, c2
        float a2 = lengthSquare(B, C);
        float b2 = lengthSquare(A, C);
        float c2 = lengthSquare(A, B);

        // length of sides be a, b, c
        float a = (float)Mathf.Sqrt(a2);
        float b = (float)Mathf.Sqrt(b2);
        float c = (float)Mathf.Sqrt(c2);

        // From Cosine law
        float alpha = (float)Mathf.Acos((b2 + c2 - a2) /
                                           (2 * b * c));
        float betta = (float)Mathf.Acos((a2 + c2 - b2) /
                                           (2 * a * c));
        float gamma = (float)Mathf.Acos((a2 + b2 - c2) /
                                           (2 * a * b));

        // Converting to degree
        alpha = (float)(alpha * 180 / Mathf.PI);
        betta = (float)(betta * 180 / Mathf.PI);
        gamma = (float)(gamma * 180 / Mathf.PI);

        // printing all the angles
        return new float[3] { alpha, betta, gamma };
    }

    static float lengthSquare(Vector2 p1, Vector2 p2)
    {
        float xDiff = p1.x - p2.x;
        float yDiff = p1.y - p2.y;
        return xDiff * xDiff + yDiff * yDiff;
    }

}
