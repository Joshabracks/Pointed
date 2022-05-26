using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Terrain
{

    public class Plane : MonoBehaviour
    {
        // private CreepingPlane creepingPlane;
        public Material material;
        Mesh mesh;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshGrid grid;
        public int seed = 1337;
        private int previous;
        void Start()
        {
            // creepingPlane = new CreepingPlane(0, 0, 734, 20);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            mesh = new Mesh();
            grid = new MeshGrid(seed, 8, .5f);
            grid.running = true;
            // StartCoroutine(grid.Build());
            grid.Build();
            mesh.vertices = grid.vertices.ToArray();
            mesh.triangles = grid.triangles.ToArray();
            // mesh.colors = grid.colors;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
            previous = seed;
        }

        // Update is called once per frame
        // void Update()
        // {
            // if (previous != seed)
            // {
            //     Debug.Log(previous);
            //     if (!grid.running)
            //     {
            //         grid.running = true;
            //         grid.seed = seed;
            //         StartCoroutine(grid.Build());
            //     } else {
            //         previous = seed;
            //         mesh.vertices = grid.vertices.ToArray();
            //         mesh.triangles = grid.triangles.ToArray();
            //         // mesh.colors = grid.colors;
            //         mesh.RecalculateNormals();
            //         mesh.RecalculateTangents();
            //         mesh.RecalculateBounds();
            //         // meshFilter.sharedMesh = mesh;
            //     }
            // }
        // }
    }
}
