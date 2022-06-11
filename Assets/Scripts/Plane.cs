using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FastNoise;

namespace Terrain
{

    public class Plane : MonoBehaviour
    {
        public Material material;
        List<Chunk> chunks;
        public int chunkSize = 16;
        public float density = 1;
        public int seed = 1337;
        // private Chunk template;
        public int x = 0;
        public int z = 0;
        private FastNoiseLite heightNoise;
        private FastNoiseLite biomeWarp;

        void Start()
        {

            heightNoise = new FastNoiseLite();
            heightNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            heightNoise.SetSeed(seed);
            heightNoise.SetFrequency(density);
            heightNoise.SetFractalType(FastNoiseLite.FractalType.PingPong);
            heightNoise.SetFractalOctaves(5);
            heightNoise.SetFractalGain(.5f);
            heightNoise.SetFractalLacunarity(2.5f);
            heightNoise.SetFractalPingPongStrength(3);
            heightNoise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.EuclideanSq);
            heightNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);
            heightNoise.SetCellularJitter(2.25f);


            biomeWarp = new FastNoiseLite();
            biomeWarp.SetDomainWarpType(FastNoiseLite.DomainWarpType.BasicGrid);
            biomeWarp.SetDomainWarpAmp(20);
            biomeWarp.SetFrequency(density);
            biomeWarp.SetFractalType(FastNoiseLite.FractalType.DomainWarpProgressive);
            biomeWarp.SetFractalLacunarity(0.50f);
            biomeWarp.SetFractalGain(0.75f);
            // template = gameObject.AddComponent<Chunk>();
            chunks = new List<Chunk>();
            // for (int x = 0; x < 3; x++) {
            // for (int z = 0; z < 3; z++) {
            AddChunk();
            // }
            // }
        }

        private void AddChunk()
        {
            GameObject obj = new GameObject();
            chunks.Add(obj.AddComponent<Chunk>());

            // GameObject north = GameObject.Find($"{x},{z + 1}");
            // GameObject south = GameObject.Find($"{x},{z - 1}");
            // GameObject east = GameObject.Find($"{x + 1},{z}");
            // GameObject west = GameObject.Find($"{x - 1},{z}");

            // if (north != null) chunks[chunks.Count - 1].NeighborVerticesNorth = north.GetComponent<Chunk>().GetNorthVertices();
            // if (south != null) chunks[chunks.Count - 1].NeighborVerticesSouth = south.GetComponent<Chunk>().GetSouthVertices();
            // if (east != null) chunks[chunks.Count - 1].NeighborVerticesEast = east.GetComponent<Chunk>().GetEastVertices();
            // if (west != null) chunks[chunks.Count - 1].NeighborVerticesWest = west.GetComponent<Chunk>().GetWestVertices();

            chunks[chunks.Count - 1].Init(seed, chunkSize, .5f, density, new Vector2(x, z), material, heightNoise, biomeWarp);
            chunks[chunks.Count - 1].AddVertices();
            chunks[chunks.Count - 1].Triangulate();
            chunks[chunks.Count - 1].Render();
        }

        // Update is called once per frame
        void Update()
        {
            z++;
            if (z >= 3)
            {
                z = 0;
                x++;
            }
            if (x < 3)
            {
                AddChunk();
            }
            // foreach(Chunk chunk in chunks)
            // {
            //     if (!chunk.finalCheck) {
            //         chunk.FinalCheck();
            //     }
            // }
        }
    }
}
