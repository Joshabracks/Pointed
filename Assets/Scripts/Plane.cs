using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FastNoise;

namespace Terrain
{

    public class Plane : MonoBehaviour
    {
        public GameObject player;
        public Material material;
        List<Chunk> chunks;
        public int chunkSize = 16;
        public int worldSize = 1920;
        public float density = 1;
        public int seed = 1337;
        private FastNoiseLite heightNoise;
        private FastNoiseLite biomeWarp;
        private FastNoiseLite slopeNoise;
        private FastNoiseLite sedimentNoise;
        private World world;

        void Start()
        {
            Application.targetFrameRate = 60;

            world = new World(seed, worldSize, density, .75f);

            chunks = new List<Chunk>();
        }

        private void AddChunk(int x, int z)
        {
            GameObject obj = new GameObject();
            chunks.Add(obj.AddComponent<Chunk>());
            chunks[chunks.Count - 1].Init(seed, chunkSize, .5f, density, new Vector2(x, z), material, world);
            chunks[chunks.Count - 1].AddVertices();
            chunks[chunks.Count - 1].Triangulate();
            chunks[chunks.Count - 1].Render(gameObject.transform);
        }

        private void checkChunks() {
            int _x = Mathf.FloorToInt((player.transform.position.x + (chunkSize / 2)) / chunkSize);
            int _z = Mathf.FloorToInt((player.transform.position.z + (chunkSize / 2)) / chunkSize);
            
            for (int x = _x - 1; x < _x + 1; x++) {
                for (int z = _z - 1; z < _z + 1; z++) {
                    string key = $"{x},{z}";
                    Transform child = gameObject.transform.Find(key);
                    if (child != null) {
                        child.gameObject.SetActive(true);
                    } else {
                        AddChunk(x, z);
                    }
                }
            }
        }


        // Update is called once per frame
        void Update()
        {
            checkChunks();
            // z++;
            // if (z >= 5)
            // {
            //     z = 0;
            //     x++;
            // }
            // if (x < 5)
            // {
            //     AddChunk();
            // }
        }
    }
}
