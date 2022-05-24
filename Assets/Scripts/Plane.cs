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
        void Start()
        {
            // creepingPlane = new CreepingPlane(0, 0, 734, 20);
            MeshGrid grid = new MeshGrid(1337, 5, .5f);
            grid.Build();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            mesh = new Mesh();
            mesh.vertices = grid.vertices.ToArray();
            mesh.triangles = grid.triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
        }

        // Update is called once per frame
        // void Update()
        // {
        //     if (!creepingPlane.running) {
        //         creepingPlane.running = true;
        //         StartCoroutine(creepingPlane.Build());
        //     }
        //     mesh.vertices = creepingPlane.vertices.ToArray();
        //     mesh.triangles = creepingPlane.triangles.ToArray();
        //     mesh.RecalculateBounds();
        //     mesh.RecalculateNormals();
        // }
    }
}
