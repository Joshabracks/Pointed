using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FastNoise;
using DelaunatorSharp;

namespace Terrain
{

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

        // public List<int> NorthVertices = null;

        // public List<int> WestVertices = null;

        // public List<int> SouthVertices = null;

        // public List<int> EastVertices = null;

        // public Vector3[] NeighborVerticesNorth = null;

        // public Vector3[] NeighborVerticesSouth = null;

        // public Vector3[] NeighborVerticesEast = null;

        // public Vector3[] NeighborVerticesWest = null;
        public List<int> triangles;
        // private int startNeighbors;
        private Vector2 offset;
        // private int vertexNeighborCutoff;
        // public bool finalCheck = false;
        private float heightMax = 200;
        public float density = 5f;
        List<int> sideIndices;
        World world;
        // public FastNoiseLite biomeWarp;
        // public FastNoiseLite heightNoise;
        // public FastNoiseLite slopeNoise;
        // public FastNoiseLite sedimentNoise;
        class Point : IPoint
        {
            public double X { get; set; }
            public double Y { get; set; }
            public Point(Vector3 vertex)
            {
                X = (double)vertex.x;
                Y = (double)vertex.z;
            }
        }

        public void Init(
            int seed,
            int size,
            float threshold,
            float density,
            Vector2 offset,
            Material material,
            World world
        // FastNoiseLite heightNoise,
        // FastNoiseLite biomeWarp,
        // FastNoiseLite slopeNoise,
        // FastNoiseLite sedimentNoise
    )
    {
        this.seed = seed;
        this.size = size;
        this.threshold = threshold;
        this.density = density;
        this.material = material;
        this.offset = offset;
        this.world = world;
        // this.heightNoise = heightNoise;
        // this.biomeWarp = biomeWarp;
        // this.slopeNoise = slopeNoise;
        // this.sedimentNoise = sedimentNoise;
        vertices = new List<Vector3>();
        triangles = new List<int>();
        gameObject.name = $"{offset.x},{offset.y}";
    }



    public void AddVertices()
    {
        vertices = new List<Vector3>();
        sideIndices = new List<int>();

        for (int x = 0; x < size + 1; x++)
        {
            for (int z = 0; z < size + 1; z++)
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
                    float height = getHeight(xVal, zVal);

                    Vector3 vertex = new Vector3(xVal, height, zVal);
                    if (x == 0 || z == 0 || x == size || z == size)
                    {
                        sideIndices.Add(vertices.Count);
                    }
                    vertices.Add(vertex);
                }
            }
        }
    }

    private float getHeight(float x, float z)
    {
        return world.GetHeight(x, z) * heightMax;
        // float warp = biomeWarp.GetNoise(x, z);
        // float slope = slopeNoise.GetNoise(x, z) + 1;
        // float sediment = sedimentNoise.GetNoise(warp * x, warp * z);
        // float height = heightNoise.GetNoise(warp * x, warp * z);
        // return height > sediment ? height * heightMax * slope : sediment * heightMax * slope;
        // return height * heightMax * slope;
    }


    public void Triangulate()
    {
        Point[] points = new Point[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            points[i] = new Point(vertices[i]);
        }
        Delaunator delaunator = new Delaunator(points);
        IEnumerable<ITriangle> tris = delaunator.GetTriangles();

        int bottomRight = 0;
        int bottomLeft = 0;
        int topRight = 0;
        int topLeft = 0;

        float bottomRightDistance = float.MaxValue;
        float bottomLeftDistance = float.MaxValue;
        float topRightDistance = float.MaxValue;
        float topLeftDistance = float.MaxValue;

        Vector2 bottomRightCoord = new Vector2((size * offset.x) + size, size * offset.y);
        Vector2 bottomLeftCoord = new Vector2(size * offset.x, size * offset.y);
        Vector2 topRightCoord = new Vector2((size * offset.x) + size, (size * offset.y) + size);
        Vector2 topLeftCoord = new Vector2(size * offset.x, (size * offset.y) + size);
        foreach (ITriangle tri in tris)
        {
            IEnumerable<int> indices = delaunator.PointsOfTriangle(tri.Index);

            int edgeVertexCount = 0;

            foreach (int index in indices)
            {
                if (sideIndices.Contains(index)) edgeVertexCount++;
            }

            if (edgeVertexCount != 3)
            {
                foreach (int index in indices)
                {
                    Vector2 vert = new Vector2(vertices[index].x, vertices[index].z);
                    float br = Vector2.Distance(vert, bottomRightCoord);
                    float bl = Vector2.Distance(vert, bottomLeftCoord);
                    float tr = Vector2.Distance(vert, topRightCoord);
                    float tl = Vector2.Distance(vert, topLeftCoord);
                    if (br < bottomRightDistance)
                    {
                        bottomRight = index;
                        bottomRightDistance = br;
                    }
                    if (bl < bottomLeftDistance)
                    {
                        bottomLeft = index;
                        bottomLeftDistance = bl;
                    }
                    if (tr < topRightDistance)
                    {
                        topRight = index;
                        topRightDistance = tr;
                    }
                    if (tl < topLeftDistance)
                    {
                        topLeft = index;
                        topLeftDistance = tl;
                    }
                }
                triangles.AddRange(indices);
            }
        }

        vertices[bottomRight] = new Vector3(bottomRightCoord.x, getHeight(bottomRightCoord.x, bottomRightCoord.y), bottomRightCoord.y);
        vertices[bottomLeft] = new Vector3(bottomLeftCoord.x, getHeight(bottomLeftCoord.x, bottomLeftCoord.y), bottomLeftCoord.y);
        vertices[topRight] = new Vector3(topRightCoord.x, getHeight(topRightCoord.x, topRightCoord.y), topRightCoord.y);
        vertices[topLeft] = new Vector3(topLeftCoord.x, getHeight(topLeftCoord.x, topLeftCoord.y), topLeftCoord.y);

    }

    public void Render(Transform parent)
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
        gameObject.transform.SetParent(parent);

        running = false;
    }

    // void OnBecameVisible()
    // {
    //     enabled = true;
    // }

    // void OnBecameInvisible() {
    //     Debug.Log("vanish");
    //     enabled = false;
    // }
}

}