using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FastNoise;

namespace Terrain
{

    public struct LandMass
    {
        public float Width;
        public float Height;
        public float X;
        public float Z;
        public float RandomSeed;
        public Vector2 StartPoint()
        {
            return new Vector2(X, Z);
        }
        public Vector2 EndPoint()
        {
            return new Vector2(X + Width, Z + Height);
        }

        public Vector2 Center()
        {
            return (StartPoint() + EndPoint()) / 2;
        }
        public float Size()
        {
            return Width * Height;
        }
    }
    public class World
    {
        // public int seed;
        // public int size;
        // public float density;
        // public float waterRatio;
        public List<LandMass> landMasses;
        private FastNoiseLite heightNoise;
        private FastNoiseLite biomeWarp;
        private FastNoiseLite slopeNoise;
        private FastNoiseLite sedimentNoise;
        private int seed;
        private int size;
        private float waterRatio;
        private float density;
        public World(int seed, int size, float density, float waterRatio)
        {
            this.seed = seed;
            this.size = size;
            this.density = density;
            this.waterRatio = waterRatio;

            heightNoise = new FastNoiseLite();
            heightNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            heightNoise.SetSeed(seed);
            heightNoise.SetFrequency(density);
            heightNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            heightNoise.SetFractalOctaves(5);
            heightNoise.SetFractalGain(.5f);
            heightNoise.SetFractalLacunarity(2.5f);
            heightNoise.SetFractalPingPongStrength(3);
            heightNoise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.EuclideanSq);
            heightNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);
            heightNoise.SetCellularJitter(2.25f);

            sedimentNoise = new FastNoiseLite();
            sedimentNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            sedimentNoise.SetSeed(seed);
            sedimentNoise.SetFrequency(density);

            slopeNoise = new FastNoiseLite();
            slopeNoise.SetSeed(seed);
            slopeNoise.SetFrequency(density);
            slopeNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            slopeNoise.SetFractalOctaves(3);
            slopeNoise.SetFractalLacunarity(3.90f);
            slopeNoise.SetFractalGain(.3f);
            slopeNoise.SetFractalWeightedStrength(0);

            biomeWarp = new FastNoiseLite();
            biomeWarp.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
            biomeWarp.SetDomainWarpAmp(20);
            biomeWarp.SetFrequency(density);
            biomeWarp.SetFractalType(FastNoiseLite.FractalType.DomainWarpProgressive);
            biomeWarp.SetFractalLacunarity(0);
            biomeWarp.SetFractalGain(0);

            GenerateLandMasses();
        }

        public float GetHeight(float _x, float _z)
        {
            float x = _x % size;
            float z = _z % size;

            if (x < 0)
            {
                x = size + x;
            }
            if (x < 0)
            {
                z = size + z;
            }

            List<float> heights = new List<float>();
            foreach (LandMass landMass in landMasses)
            {
                float endpointX = landMass.EndPoint().x;
                float endpointZ = landMass.EndPoint().y;
                float X = 0;
                float Z = 0;
                if (x > landMass.X && x < endpointX && z > landMass.Z && z < endpointZ)
                {
                    X = x - landMass.X;
                    Z = z - landMass.Z;

                }
                else if (endpointX > size && endpointZ <= size && z > landMass.Z && z < endpointZ && x < endpointX % size)
                {
                    // x axis goes out of bounds z stays in
                    X = x + size - landMass.X;
                    Z = z - landMass.Z;
                }
                else if (endpointZ > size && endpointX <= size && x > landMass.X && x < endpointX && z < endpointZ % size)
                {
                    // z axis goes out of bounds x stays in
                    X = x - landMass.X;
                    Z = z + size - landMass.Z;

                }
                else if (endpointX > size && endpointZ > size)
                {
                    if (x < endpointX % size && z < endpointZ % size)
                    {
                        // x and z go out of bounds and point is in bottom left corner
                        X = x + size - landMass.X;
                        Z = z + size - landMass.Z;
                    }
                    else if (x < endpointX % size)
                    {
                        //  x and z axis go out of bounds and point is upper left corner 
                        X = x + size - landMass.X;
                        Z = z - landMass.Z;
                    }
                    else if (z < endpointZ % size)
                    {
                        // x and z axis go out of bounds and point is in lower right corner
                        X = x - landMass.X;
                        Z = z + size - landMass.Z;
                    }
                }
                else continue;
                float a = heightNoise.GetNoise(X + seed * landMass.RandomSeed, Z + seed * landMass.RandomSeed);
                float b = sedimentNoise.GetNoise(X + seed * landMass.RandomSeed, Z + seed * landMass.RandomSeed);
                float c = slopeNoise.GetNoise(X + seed * landMass.RandomSeed, Z + seed * landMass.RandomSeed);
                float d = biomeWarp.GetNoise(X + seed * landMass.RandomSeed, Z + seed * landMass.RandomSeed);
                float height = (a + b + c + d) / 4;
                Vector2 position = new Vector2(X, Z);
                float distance = Vector2.Distance(position, new Vector2());
                float comparison = Vector2.Distance(position, new Vector2(landMass.Width, landMass.Height));
                if (comparison < distance) distance = comparison;
                comparison = Vector2.Distance(position, new Vector2(0, landMass.Height));
                if (comparison < distance) distance = comparison;
                comparison = Vector2.Distance(position, new Vector2(landMass.Width, 0));
                if (comparison < distance) distance = comparison;

                distance = distance == 0 ? 0 : distance / ((landMass.Height + landMass.Width) / 2);
                heights.Add(height * distance);
            }
            if (heights.Count > 0)
            {
                float sum = 0;
                foreach (float height in heights) sum += height;
                sum /= heights.Count;
                return sum;
            }

            float e = heightNoise.GetNoise(x + seed * .97f, z + seed * .97f);
            float f = sedimentNoise.GetNoise(x + seed * .97f, z + seed * .97f);
            float g = slopeNoise.GetNoise(x + seed * .97f, z + seed * .97f);
            float h = biomeWarp.GetNoise(x + seed * .97f, z + seed * .97f);
            return (e + f + g + h) / 4;
        }

        public void GenerateLandMasses()
        {
            landMasses = new List<LandMass>();
            float volume = size * size;
            float targetWaterVolume = volume * (Mathf.Abs(waterRatio) % 1);
            float landMassVolume = 0;
            float massInhibitor = .5f;
            float x = 13;
            float y = 37;
            float randomSeed = .1234f;
            while (landMassVolume < targetWaterVolume)
            {
                x = Mathf.PerlinNoise(x + (seed * randomSeed) * massInhibitor * .89f, y + (seed * randomSeed) * massInhibitor * .89f);
                y = Mathf.PerlinNoise(x + (seed * randomSeed) * massInhibitor * .89f, y + (seed * randomSeed) * massInhibitor * .89f);
                randomSeed = Mathf.PerlinNoise(x, y);

                LandMass landMass = new LandMass();
                landMass.RandomSeed = randomSeed;
                landMass.Width = Mathf.CeilToInt(Mathf.Abs(size * massInhibitor * Mathf.PerlinNoise(x + seed * .79f, y + seed * .79f)));
                landMass.Height = Mathf.CeilToInt(Mathf.Abs(size * massInhibitor * Mathf.PerlinNoise(x + seed * .379f, y + seed * .379f)));

                landMass.X = Mathf.FloorToInt(Mathf.PerlinNoise(x + seed * .79f, y * seed * .79f)) * size;
                landMass.Z = Mathf.FloorToInt(Mathf.PerlinNoise(x + seed * .691f, y * seed * .691f)) * size;
                float v = landMass.Width * landMass.Height;

                // List<LandMass> landMassPortions = new List<LandMass>();
                foreach (LandMass lm in landMasses)
                {
                    if (v <= 0)
                    {
                        v = 0;
                        break;
                    }
                    if (doOverlap(landMass.StartPoint(), landMass.EndPoint(), lm.StartPoint(), lm.EndPoint()))
                    {
                        LandMass A = landMass;
                        LandMass B = lm;
                        List<int> points = pointsInside(landMass, lm);
                        if (points.Count == 0)
                        {
                            A = lm;
                            B = landMass;
                            points = pointsInside(lm, landMass);
                        }
                        if (points.Count == 0)
                        {
                            bool optA = A.X > B.X;
                            float x1 = optA ? A.X : B.X;
                            float z1 = optA ? B.Z : A.Z;
                            float x2 = optA ? A.EndPoint().x : B.EndPoint().x;
                            float z2 = optA ? B.EndPoint().y : A.EndPoint().y;

                            float Width = x2 - x1;
                            float Height = z2 - z1;

                            v -= Width * Height;
                        }
                        else if (points.Count == 1)
                        {

                            switch (points[0])
                            {
                                case 0:
                                    v -= (A.EndPoint().x - B.X) * (A.EndPoint().y - B.Z);
                                    break;
                                case 1:
                                    v -= (B.EndPoint().x - A.X) * (A.EndPoint().y - B.Z);
                                    break;
                                case 2:
                                    v -= (B.EndPoint().x - A.X) * (B.EndPoint().y - A.Z);
                                    break;
                                case 3:
                                    v -= (A.EndPoint().x - B.X) * (B.EndPoint().y - A.Z);
                                    break;
                            }
                        }
                        else if (points.Count == 2)
                        {
                            switch ($"{points[0]}{points[1]}")
                            {
                                case "01":
                                    v -= B.Width * (A.EndPoint().y - B.Z);
                                    break;
                                case "12":
                                    v -= B.Height * (B.EndPoint().x - A.X);
                                    break;
                                case "23":
                                    v -= B.Width * (B.EndPoint().y - A.Z);
                                    break;
                                case "03":
                                    v -= B.Height * (A.EndPoint().x - B.X);
                                    break;
                            }
                        }
                        else if (points.Count == 4)
                        {
                            v -= B.Width * B.Height;
                        }
                    }
                }
                landMassVolume += v > 0 ? v : 0;
                landMasses.Add(landMass);
            }

        }

        private List<int> pointsInside(LandMass lm1, LandMass lm2)
        {
            List<int> points = new List<int>();
            Vector2 a = lm1.StartPoint();
            Vector2 b = lm1.EndPoint();

            Vector2 c = lm1.StartPoint();
            Vector2 d = lm1.EndPoint();
            Vector2 e = new Vector2(d.x, c.y);
            Vector2 f = new Vector2(c.x, d.y);

            Vector2[] referencePoints = new Vector2[4] { c, d, e, f };
            for (int i = 0; i < referencePoints.Length; i++)
            {
                Vector2 point = referencePoints[i];
                if (point.x > a.x && point.x < b.x && point.x > a.y && point.y < b.y) points.Add(i);
            }

            return points;
        }

        private bool doOverlap(Vector2 l1, Vector2 r1, Vector2 l2, Vector2 r2)
        {
            // If one rectangle is on left side of other
            if (l1.x > r2.x || l2.x > r1.x)
            {
                return false;
            }

            // If one rectangle is above other
            if (r1.y > l2.y || r2.y > l1.y)
            {
                return false;
            }
            return true;
        }



    }
}
