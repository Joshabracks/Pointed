using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Terrain
{

    public class Plane : MonoBehaviour
    {
        public Material material;
        List<Chunk> chunks;
        public int chunkSize = 16;
        public int seed = 1337;
        // private Chunk template;

        void Start()
        {
            // template = gameObject.AddComponent<Chunk>();
            chunks = new List<Chunk>();
            for (int x = 0; x < 3; x++) {
                for (int z = 0; z < 3; z++) {
                    GameObject obj = new GameObject();
                    chunks.Add(obj.AddComponent<Chunk>());
                    chunks[chunks.Count - 1].Init(seed, chunkSize, .5f, new Vector2(x * chunkSize, z * chunkSize), material);
                    chunks[chunks.Count - 1].AddVertices();
                    chunks[chunks.Count - 1].Triangulate();
                    chunks[chunks.Count - 1].Render();
                }
            }
        }

        // Update is called once per frame
        // void Update()
        // {
        // }
    }
}
