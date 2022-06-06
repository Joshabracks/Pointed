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
    private float heightMax = 500;
    public float density = 5f;


    public void Init(
        int seed,
        int size,
        float threshold,
        float density,
        Vector2 offset,
        Material material
    )
    {
        this.seed = seed;
        this.size = size;
        this.threshold = threshold;
        this.density = density;
        this.material = material;
        this.offset = offset;
        vertices = new List<Vector3>();
        triangles = new List<int>();
        gameObject.name = $"{offset.x},{offset.y}";
    }

    private void OnDrawGizmos()
    {
        if (Selection.activeGameObject != gameObject) return;
        if (NeighborVerticesEast != null)
        {
            Gizmos.color = Color.green;
            foreach (Vector3 v in NeighborVerticesEast) Gizmos.DrawSphere(v, 0.1f);
        }
        if (NeighborVerticesSouth != null)
        {
            Gizmos.color = Color.red;
            foreach (Vector3 v in NeighborVerticesSouth) Gizmos.DrawSphere(v, 0.1f);
        }
        if (NeighborVerticesNorth != null)
        {
            Gizmos.color = Color.blue;
            foreach (Vector3 v in NeighborVerticesNorth) Gizmos.DrawSphere(v, 0.1f);
        }
        if (NeighborVerticesWest != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Vector3 v in NeighborVerticesWest) Gizmos.DrawSphere(v, 0.1f);
        }
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
                    float height = Mathf.PerlinNoise((xVal * density) + seed, (zVal * density) + seed);
                    Vector3 vertex = new Vector3(xVal, height * heightMax, zVal);
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
        chunkCenter.y = 0;

        // Iterate all viable triangle combinations
        // a and b ignore last 4 vertices
        // a/b/c never overlap
        for (int a = 0; a < vertices.Count - 5; a++)
        {
            for (int b = a + 1; b < vertices.Count - 4; b++)
            {
                for (int c = b + 1; c < vertices.Count; c++)
                {
                    Vector3 vA = new Vector3(vertices[a].x, 0, vertices[a].z);
                    Vector3 vB = new Vector3(vertices[b].x, 0, vertices[b].z);
                    Vector3 vC = new Vector3(vertices[c].x, 0, vertices[c].z);
                    
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
                        Vector3 vI = new Vector3(vertices[i].x, 0, vertices[i].z);
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
                                    float circumDist = Vector3.Distance(circumcenterV3, vA) + Vector3.Distance(circumcenterV3, vB) + Vector3.Distance(circumcenterV3, vC);
                                    float iDist = Vector3.Distance(vI, vA) + Vector3.Distance(vI, vB) + Vector3.Distance(vI, vC);
                                    if (circumDist < iDist)
                                    {
                                        Vector3 triCenter = (vA + vB + vC) / 3;
                                        if (Vector3.Distance(triCenter, chunkCenter) < Vector3.Distance(circumcenterV3, chunkCenter))
                                        {
                                            suspectTriangles.AddRange(new int[3] { a, b, c });
                                        }
                                    }
                                }

                            }
                            else if (stopPointScore == 3 && i > vertices.Count - 4)
                            {
                                Vector3 center = (vA + vB + vC) / 3;
                                center.y = heightMax + 1;
                                Ray ray = new Ray(center, Vector3.down);
                                RaycastHit hit;
                                if (!Physics.Raycast(ray, out hit))
                                {
                                    break;
                                }
                            }
                            isBad = true;
                            break;
                        }
                    }
                    if (isBad || isSuspect) continue;
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
                            Vector3 center = (vA + vB + vC) / 3;
                            center.y = heightMax + 1;
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
                    }
                }
            }
        }

        // Add edge cases
        List<int> suspectTriangles2 = new List<int>();
        for (int i = 0; i < suspectTriangles.Count; i += 3)
        {
            List<int> a = new List<int>();
            a.AddRange(new int[3] { suspectTriangles[i], suspectTriangles[i + 1], suspectTriangles[i + 2] });
            int sharedSides = 0;
            List<int> sharedTriangles = new List<int>();
            // Determine if suspect triangle "a" is surrounded by 3 non-suspect triangles
            for (int j = 0; j < triangles.Count; j += 3)
            {
                int[] b = new int[3] { triangles[j], triangles[j + 1], triangles[j + 2] };
                int sharedPoints = 0;
                if (a.Contains(b[0])) sharedPoints++;
                if (a.Contains(b[1])) sharedPoints++;
                if (a.Contains(b[2])) sharedPoints++;
                if (sharedPoints == 2)
                {
                    sharedTriangles.AddRange(b);
                    sharedSides++;
                }
            }
            if (sharedSides == 3) // "a" is surrounded by 3 triangles.  Add to triangles.
            {
                addTriangle(a[0], a[1], a[2]);
                continue;
            }

            // Determine if suspect triangle borders at least 1 other suspect triangle, with a total of 3 bordering triangles
            for (int j = 0; j < suspectTriangles.Count; j += 3)
            {
                if (j == i) continue;
                int[] b = new int[3] { suspectTriangles[j], suspectTriangles[j + 1], suspectTriangles[j + 2] };
                int sharedPoints = 0;
                if (a.Contains(b[0])) sharedPoints++;
                if (a.Contains(b[1])) sharedPoints++;
                if (a.Contains(b[2])) sharedPoints++;
                if (sharedPoints == 2)
                {
                    sharedTriangles.AddRange(b);
                    sharedSides++;
                }
            }
            if (sharedSides == 3) // triangle "a" borders at least 1 other suspect triangle for a total of 3 triangles bordered
            {
                suspectTriangles2.AddRange(a);
                continue;
            }
            if (sharedSides == 2)
            {
                List<int> openSideCorners = new List<int>();
                int[] b = new int[3] { sharedTriangles[0], sharedTriangles[1], sharedTriangles[2] };
                int[] c = new int[3] { sharedTriangles[3], sharedTriangles[4], sharedTriangles[5] };
                foreach (int corner in b)
                {
                    if ((corner != c[0] && corner != c[1] && corner != c[2]) && (corner == a[0] || corner == a[1] || corner == a[2]))
                    {
                        openSideCorners.Add(corner);
                        break;
                    }
                }
                if (openSideCorners.Count == 1)
                {
                    foreach (int corner in c)
                    {
                        if ((corner != b[0] && corner != b[1] && corner != b[2]) && (corner == a[0] || corner == a[1] || corner == a[2]))
                        {
                            openSideCorners.Add(corner);
                            break;
                        }
                    }
                }
                if (openSideCorners.Count == 2)
                {
                    int leftoverCorner = -1;
                    if (a[0] != openSideCorners[0] && a[0] != openSideCorners[1]) leftoverCorner = a[0];
                    else if (a[1] != openSideCorners[0] && a[1] != openSideCorners[1]) leftoverCorner = a[1];
                    else if ((a[2] != openSideCorners[0] && a[2] != openSideCorners[1])) leftoverCorner = a[2];
                    if (leftoverCorner != -1)
                    {
                        if (NorthVertices.Contains(leftoverCorner) || SouthVertices.Contains(leftoverCorner) || EastVertices.Contains(leftoverCorner) || SouthVertices.Contains(leftoverCorner))
                        {
                            int additionalCorner = -1;
                            for (int j = 0; j < triangles.Count; j += 3)
                            {
                                int[] d = new int[3] { triangles[j], triangles[j + 1], triangles[j + 2] };
                                int dCornerMatches = 0;
                                if (openSideCorners[0] == d[0]) dCornerMatches++;
                                if (openSideCorners[0] == d[1]) dCornerMatches++;
                                if (openSideCorners[0] == d[2]) dCornerMatches++;
                                if (dCornerMatches != 1) continue;
                                for (int k = 0; k < triangles.Count; k += 3)
                                {
                                    if (k == j) continue;
                                    int[] e = new int[3] { triangles[k], triangles[k + 1], triangles[k + 2] };
                                    int eCornerMatches = 0;
                                    if (openSideCorners[1] == e[0]) eCornerMatches++;
                                    if (openSideCorners[1] == e[1]) eCornerMatches++;
                                    if (openSideCorners[1] == e[2]) eCornerMatches++;
                                    if (eCornerMatches != 1) continue;
                                    foreach (int cornerE in e)
                                    {
                                        foreach (int cornerD in d)
                                        {
                                            if (cornerE == cornerD && cornerE != leftoverCorner)
                                            {
                                                additionalCorner = cornerD;
                                                break;
                                            };
                                        }
                                        if (additionalCorner != -1) break;
                                    }
                                }
                                if (additionalCorner != -1) break;
                            }
                            // if (leftoverCorner == additionalCorner) continue;
                            if (additionalCorner != -1 && additionalCorner != leftoverCorner)
                            {
                                addTriangle(additionalCorner, openSideCorners[0], openSideCorners[1]);
                                addTriangle(leftoverCorner, openSideCorners[0], openSideCorners[1]);
                                continue;
                            }
                        }
                    }
                }
            }
        }
        // Determine if two suspectTriangles from suspectTriangles2 border each other
        for (int i = 0; i < suspectTriangles2.Count; i += 3)
        {
            List<int> a = new List<int>();
            a.AddRange(new int[3] { suspectTriangles2[i], suspectTriangles2[i + 1], suspectTriangles2[i + 2] });
            for (int j = 0; j < suspectTriangles2.Count; j += 3)
            {
                if (j == i) continue;
                int[] b = new int[3] { suspectTriangles2[j], suspectTriangles2[j + 1], suspectTriangles2[j + 2] };
                int sharedPoints = 0;
                if (a.Contains(b[0])) sharedPoints++;
                if (a.Contains(b[1])) sharedPoints++;
                if (a.Contains(b[2])) sharedPoints++;
                if (sharedPoints == 2) addTriangle(a[0], a[1], a[2]); // triangle borders another triangle from suspectTriangles2
                break;
            }
        }
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
