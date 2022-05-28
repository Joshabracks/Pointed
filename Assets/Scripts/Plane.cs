using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Terrain
{

    public class Plane : MonoBehaviour
    {
        public Material material;
        Mesh mesh;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshGrid grid;
        public int chunkSize = 16;

        public int seed = 1337;
        private int previous;
        void Start()
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            mesh = new Mesh();
            grid = new MeshGrid(seed, chunkSize, .5f);
            grid.running = true;
            grid.Build();
            mesh.vertices = grid.vertices.ToArray();
            mesh.triangles = grid.triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
            previous = seed;
            
        }

        // Update is called once per frame
        // void Update()
        // {
        // }
    }
}
