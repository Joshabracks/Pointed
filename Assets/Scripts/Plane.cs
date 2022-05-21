using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Terrain
{

    public class Plane : MonoBehaviour
    {
        private CreepingPlane creepingPlane;
        public Material material;
        Mesh mesh;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        void Start()
        {
            creepingPlane = new CreepingPlane(0, 0, 1337, 20);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            mesh = new Mesh();
            meshFilter.sharedMesh = mesh;
        }

        // Update is called once per frame
        void Update()
        {
            if (!creepingPlane.running) {
                creepingPlane.running = true;
                StartCoroutine(creepingPlane.Build());
            }
            mesh.vertices = creepingPlane.vertices.ToArray();
            mesh.triangles = creepingPlane.triangles.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }
    }
}
