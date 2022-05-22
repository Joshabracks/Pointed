using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Terrain
{

    public class CreepingPlane
    {
        public List<Vector3> vertices;
        public List<int> triangles;
        private List<List<int>> connections;
        private List<int> next;
        private int seed;
        private int size;
        private int root = 0;
        private int previous = 0;
        private int head = 0;
        private Vector2 offset;
        public bool running;
        public List<int> rendered;
        public CreepingPlane(int xOffset, int zOffset, int seed, int size)
        {
            this.seed = seed;
            this.size = size;
            vertices = new List<Vector3>();
            triangles = new List<int>();
            offset = new Vector2(xOffset * size, zOffset * size);
            // root = new node(offset, 0);

            vertices.Add(new Vector3(offset.x, 0, offset.y));
            // head = root;
            // previous = root;
            rendered = new List<int>();
            // rendered.Add(0);
            connections = new List<List<int>>();
            connections.Add(new List<int>());
            next = new List<int>();
            next.Add(0);
        }

        // private struct node
        // {
        //     public List<node> connections;
        //     public Vector2 location;
        //     public int index;
        //     // public bool rendered;
        //     public int next;
        //     public node(Vector2 location, int index)
        //     {
        //         this.location = location;
        //         this.index = index;
        //         connections = new List<node>();
        //         // rendered = false;
        //         next = 0;
        //     }
        // }

        private float getDistance(int x, int y, float i)
        {
            return Mathf.PerlinNoise(x + seed + i * .971f, y + seed + i * .971f) + 1;
        }

        private Vector2 getPoint(Vector2 origin, float distance, float angle)
        {
            float x = origin.x + Mathf.Cos(angle) * distance;
            float y = origin.y + Mathf.Sin(angle) * distance;
            return new Vector2(x, y);
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
            Vector2 midpoint = (vec3to2(vertices[a]) + vec3to2(vertices[b]) + vec3to2(vertices[c])) / 3;

            float aa = getAngle(midpoint, vec3to2(vertices[a]));
            float ab = getAngle(midpoint, vec3to2(vertices[b]));
            float ac = getAngle(midpoint, vec3to2(vertices[c]));

            Debug.Log($"<color=blue>ADD TRI:</color> {a}, {b}, {c}");
            if (aa > ab && aa > ac)
            {
                if (ab > ac) triangles.AddRange(new int[3] { a, b, c });
                else triangles.AddRange(new int[3] { a, c, b });
            }
            else if (ab > aa && ab > ac)
            {
                if (aa > ac) triangles.AddRange(new int[3] { b, a, c });
                else triangles.AddRange(new int[3] { b, c, a });
            }
            else
            {
                if (aa > ab) triangles.AddRange(new int[3] { c, a, b });
                else triangles.AddRange(new int[3] { c, b, a });
            }
        }

        private bool isPastMaxRotation(float rotation, float origin, float maxRotation)
        {
            if (origin > maxRotation)
            {
                if (rotation < 0)
                {
                    return false;
                }
                else if (rotation > maxRotation)
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (rotation > maxRotation)
                {
                    return true;
                }
                return false;
            }
        }

        private void updateHead(int n)
        {

            if (n == previous)
            {
                Debug.Log("<color=white>ZERO</color>");
                head = connections[n][next[n]];
                previous = n;
                next[previous]++;
            }
            else if (next[previous] < connections[previous].Count)
            {
                Debug.Log("<color=white>ONE</color>");
                head = connections[previous][next[previous]];
                next[previous]++;
            }
            else
            {
                while (rendered.Contains(connections[n][next[n]]))
                {
                    next[n]++;
                }
                Debug.Log("<color=white>TWO</color>");
                head = connections[n][next[n]];
                previous = n;
                next[previous]++;
            }
            Debug.Log($"<color=yellow>UPDATE HEAD</color>P: {previous} H: {head}");
            // if (head.rendered) {
            //     updateHead(head);
            // }
        }

        private (int[] nodes, float[] angles) getRange(int n)
        {
            if (connections[n].Count == 0)
            {
                return (new int[1] { n }, new float[2] { 0, (2 * Mathf.PI) });
            }


            // float previousAngle = getAngle(vec3to2(vertices[n]), vec3to2(vertices[previous])) + Mathf.PI * 2;
            // if (previousAngle < 0) previousAngle += Mathf.PI * 2;
            List<float> connectedAngles = new List<float>();
            List<int> connectedIndices = new List<int>();
            foreach (int index in connections[n])
            {
                if (index == previous) continue;
                if (rendered.Contains(index)) continue;
                connectedIndices.Add(index);
                float a = getAngle(vec3to2(vertices[n]), vec3to2(vertices[index])) + Mathf.PI * 2;
                // if (a < 0) a += Mathf.PI * 2;
                connectedAngles.Add(a);
            }

            int[] nodes = new int[2] { connectedIndices[0], connectedIndices[connectedIndices.Count - 1] };
            float[] angles = new float[2] { connectedAngles[0], connectedAngles[connectedIndices.Count - 1] };

            for (int i = 0; i < connectedIndices.Count; i++)
            {
                if (connectedAngles[i] < angles[0])
                {
                    angles[0] = connectedAngles[i];
                    nodes[0] = connectedIndices[i];
                }
                if (connectedAngles[i] > angles[1])
                {
                    angles[1] = connectedAngles[i];
                    nodes[1] = connectedIndices[i];
                }
            }

            Vector2 midpoint = (vec3to2(vertices[nodes[0]]) + vec3to2(vertices[nodes[1]]) + vec3to2(vertices[previous])) / 3;
            float aa = getAngle(midpoint, vec3to2(vertices[nodes[0]]));
            float ab = getAngle(midpoint, vec3to2(vertices[nodes[1]]));
            float ac = getAngle(midpoint, vec3to2(vertices[previous]));

            if (aa > ab && aa > ac)
            {
                Debug.Log("<color=cyan>A</color>");
                if (ab < ac) return (new int[2]{nodes[1], nodes[0]}, new float[2]{angles[1], angles[0]});
                else return (new int[2]{nodes[0], nodes[1]}, new float[2]{angles[0], angles[1]});
            }
            else if (ab > aa && ab > ac)
            {
                Debug.Log("<color=cyan>B</color>");
                if (aa < ac) return (new int[2]{nodes[1], nodes[0]}, new float[2]{angles[1], angles[0]});
                else return (new int[2]{nodes[0], nodes[1]}, new float[2]{angles[0], angles[1]});
            }
            else
            {
                Debug.Log("<color=cyan>C</color>");
                if (ab < aa) return (new int[2]{nodes[1], nodes[0]}, new float[2]{angles[1], angles[0]});
                else return (new int[2]{nodes[0], nodes[1]}, new float[2]{angles[0], angles[1]});
            }


            // Debug.Log("<color=pink>GET RANGE</color>: " + n.index);
            // if (connections[n].Count == 0)
            // {
            //     return (new int[1] { n }, new float[2] { 0, (2 * Mathf.PI) });
            // }

            // int threshhold = 1;
            // List<int> startAndFinish = new List<int>();
            // while (startAndFinish.Count < 2)
            // {

            //     foreach (int connection in connections[n])
            //     {
            //         // Debug.Log(connection.connections.Count);
            //         if (connections[connection].Count == threshhold)
            //         {
            //             startAndFinish.Add(connection);
            //         }
            //     }
            //     threshhold++;
            // }

            // startAndFinish.Sort(delegate (int x, int y)
            // {
            //     return x < y ? 1 : -1;
            // });

            // float a = getAngle(vec3to2(vertices[n]), vec3to2(vertices[startAndFinish[0]]));
            // float b = getAngle(vec3to2(vertices[n]), vec3to2(vertices[startAndFinish[1]]));
            // if (b < 0)
            // {
            //     b += Mathf.PI * 2;
            // }
            // a += (Mathf.PI * 10);
            // b += (Mathf.PI * 10);

            // return (startAndFinish.ToArray(), new float[2] { a, b });
        }

        private bool hasConnection(int origin, int connection)
        {
            return connections[origin].Contains(connection);
        }

        private IEnumerator setConnections(int n)
        {
            // setting range of rotation for adding new nodes.
            var ranges = getRange(n);
            var range = ranges.angles;
            // float origin = range[0];
            float rotation = range[0];
            float maxRotation = range[1];
            // float difference = (Mathf.PI * 2) - rotation;

            int start = ranges.nodes[0];
            int finish = 0;
            bool hasFinish = false;
            if (ranges.nodes.Length == 2)
            {
                finish = ranges.nodes[1];
                hasFinish = true;
            }
            Debug.Log($"<color=yellow>start:</color> {start}:{rotation}, <color=yellow>finish:</color> {finish}:{maxRotation}");
            while (start < finish && rotation < maxRotation)
            {
                if (connections[n].Count != 0)
                {
                    rotation += Mathf.PerlinNoise(vertices[n].x + seed + rotation * .531f, vertices[n].z + seed + rotation * .531f) + 1 * .66f;
                }
                Debug.Log($"{rotation} : {maxRotation}");
                if (rotation > maxRotation)
                {
                    Debug.Log("<color=red>GOING OVERBOARD: CONNECT</color> ---> ");
                    if (hasFinish)
                    {
                        addTriangle(n, connections[n][connections[n].Count - 1], finish);
                        if (!hasConnection(connections[n][connections[n].Count - 1], finish)) connections[connections[n][connections[n].Count - 1]].Add(finish);
                        if (!hasConnection(finish, connections[n][connections[n].Count - 1])) connections[finish].Add(connections[n][connections[n].Count - 1]);
                    }
                    else
                    {
                        int variableConnection = connections[n][0] != previous ? connections[n][0] : connections[n][1];
                        addTriangle(n, connections[n][connections[n].Count - 1], variableConnection);
                        if (!hasConnection(connections[n][connections[n].Count - 1], variableConnection)) connections[connections[n][connections[n].Count - 1]].Add(variableConnection);
                        if (!hasConnection(variableConnection, connections[n][connections[n].Count - 1])) connections[variableConnection].Add(connections[n][connections[n].Count - 1]);
                    }
                    if (!rendered.Contains(n))
                    {
                        rendered.Add(n);
                    }
                    updateHead(n);
                    yield return new WaitForSecondsRealtime(.5f);
                    yield break;
                }

                Vector2 location = getPoint(vec3to2(vertices[n]), getDistance((int)(vertices[n].x * 10), (int)(vertices[n].z * 10), rotation + n), rotation);
                int connectionIndex = vertices.Count;
                connections.Add(new List<int>());
                next.Add(0);
                vertices.Add(new Vector3(location.x, 0, location.y));
                if (connections[n].Count > 0)
                {
                    addTriangle(n, connections[n][connections[n].Count - 1], connectionIndex);
                    if (!hasConnection(connectionIndex, n)) connections[connectionIndex].Add(n);
                    if (!hasConnection(connectionIndex, connections[n][connections[n].Count - 1])) connections[connectionIndex].Add(connections[n][connections[n].Count - 1]);
                    if (!hasConnection(connections[n][connections[n].Count - 1], connectionIndex)) connections[connections[n][connections[n].Count - 1]].Add(connectionIndex);
                    // connection.connections.Add(n.connections[n.connections.Count - 1]);
                    if (!hasConnection(n, connectionIndex)) connections[n].Add(connectionIndex);
                }
                else
                {
                    connections[connectionIndex].Add(n);
                    connections[n].Add(connectionIndex);
                }
                yield return new WaitForSecondsRealtime(.5f);
                // yield return new WaitForEndOfFrame();
            }
            // updateHead(n);
            // n.rendered = true;
        }

        public IEnumerator Build()
        {
            // Debug.Log("building");
            yield return setConnections(head);
            running = false;
        }
    }
}
