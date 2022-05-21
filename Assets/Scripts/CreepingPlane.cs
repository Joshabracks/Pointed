using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Terrain
{

    public class CreepingPlane
    {
        public List<Vector3> vertices;
        public List<int> triangles;
        private int seed;
        private int size;
        private node root;
        private node previous;
        private node head;
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
            root = new node(offset, 0);
            vertices.Add(new Vector3(root.location.x, 0, root.location.y));
            head = root;
            previous = root;
            rendered = new List<int>();
        }

        private struct node
        {
            public List<node> connections;
            public Vector2 location;
            public int index;
            // public bool rendered;
            public int next;
            public node(Vector2 location, int index)
            {
                this.location = location;
                this.index = index;
                connections = new List<node>();
                // rendered = false;
                next = 0;
            }
        }

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
            return angle + (Mathf.PI * 10);
        }

        private void addTriangle(node a, node b, node c)
        {
            Vector2 midpoint = (a.location + b.location + c.location) / 3;

            float aa = getAngle(midpoint, a.location);
            float ab = getAngle(midpoint, b.location);
            float ac = getAngle(midpoint, c.location);

            Debug.Log($"<color=blue>ADD TRI:</color> {a.index}, {b.index}, {c.index}");
            if (aa > ab && aa > ac)
            {
                if (ab > ac) triangles.AddRange(new int[3] { a.index, b.index, c.index });
                else triangles.AddRange(new int[3] { a.index, c.index, b.index });
            }
            else if (ab > aa && ab > ac)
            {
                if (aa > ac) triangles.AddRange(new int[3] { b.index, a.index, c.index });
                else triangles.AddRange(new int[3] { b.index, c.index, a.index });
            }
            else
            {
                if (aa > ab) triangles.AddRange(new int[3] { c.index, a.index, b.index });
                else triangles.AddRange(new int[3] { c.index, b.index, a.index });
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

        private void updateHead(node n)
        {

            if (n.index == previous.index)
            {
                Debug.Log("<color=white>ZERO</color>");
                head = n.connections[n.next];
                previous = n;
                previous.next++;
            }
            else if (previous.next < previous.connections.Count)
            {
                Debug.Log("<color=white>ONE</color>");
                head = previous.connections[previous.next];
                previous.next++;
            }
            else
            {
                while (rendered.Contains(n.connections[n.next].index)) {
                    n.next++;
                }
                Debug.Log("<color=white>TWO</color>");
                head = n.connections[n.next];
                previous = n;
                previous.next++;
            }
            Debug.Log($"<color=yellow>UPDATE HEAD</color>P: {previous.index} H: {head.index}");
            // if (head.rendered) {
            //     updateHead(head);
            // }
        }

        private (node[] nodes, float[] angles) getRange(node n)
        {
            // Debug.Log("<color=pink>GET RANGE</color>: " + n.index);
            if (n.connections.Count == 0)
            {
                return (new node[1] { n }, new float[2] { 0, (2 * Mathf.PI) });
            }

            int threshhold = 1;
            List<node> startAndFinish = new List<node>();
            while (startAndFinish.Count < 2)
            {

                foreach (node connection in n.connections)
                {
                    // Debug.Log(connection.connections.Count);
                    if (connection.connections.Count == threshhold)
                    {
                        startAndFinish.Add(connection);
                    }
                }
                threshhold ++;
            }

            startAndFinish.Sort(delegate (node x, node y)
            {
                return x.index < y.index ? 1 : -1;
            });
            // Debug.Log($"<color=cyan>{startAndFinish[0].location} : {startAndFinish[1].location} : {n.location}</color>");

            float a = getAngle(n.location, startAndFinish[0].location);
            float b = getAngle(n.location, startAndFinish[1].location);
            if (b < a)
            {
                b += Mathf.PI * 2;
            }

            // if (a < 0) a = a + (Mathf.PI * 2);
            // if (b < 0) b = b + (Mathf.PI * 2);

            // if () {
            return (startAndFinish.ToArray(), new float[2] { a, b });
            // }
            // return (new node[2]{startAndFinish[1], startAndFinish[0]}, new float[2]{b, a});
            // Debug.Log($"<color=cyan>{a}:{b}</color>");
            // if (a > b)
            // {
            //     if (a - b > Mathf.PI)
            //     {
            //         Debug.Log("<color=cyan>ONE</color>");
            //         return (startAndFinish.ToArray(), new float[2] { a - Mathf.PI * 2, b });
            //     }
            //     Debug.Log("<color=cyan>TWO</color>");
            //     return (startAndFinish.ToArray(), new float[2] { a, b });
            // }
            // if (b - a > Mathf.PI)
            // {
            //     Debug.Log("<color=cyan>THREE</color>");
            //     return (new node[2] { startAndFinish[1], startAndFinish[0] }, new float[2] { b - Mathf.PI * 2, a });
            // }
            // Debug.Log("<color=cyan>FOUR</color>");
            // return (new node[2] { startAndFinish[1], startAndFinish[0] }, new float[2] { b, a });

            // float a = getAngle(n.location, n.connections[n.connections.Count - 1].location);

            // if (n.connections.Count == 1) {
            //     float angle = getAngle(n.location, n.connections[0].location);
            //     return new float[2]{a, a + (2 * Mathf.PI)};
            // }

            // node variableConnection = n.index != previous.connections[0].index ? n.connections[2] : n.connections[1];
            // float b = getAngle(n.location, variableConnection.location);
            // if (a < b && b - a > Mathf.PI) {
            //     return (startAndFinish.ToArray(), new float[2]{a, b});
            // } else if (a < b) { 
            //     return (startAndFinish.ToArray(), new float[2]{a, b + (2 * Mathf.PI)});
            // } else if (a - b > Mathf.PI) {
            //     return (new node[2]{startAndFinish[1], startAndFinish[0]}, new float[2]{b, a});
            // }
            // return (new node[2]{startAndFinish[1], startAndFinish[0]}, new float[2]{b, a + (2 * Mathf.PI)});
            // if (startAndFinish[0].index < startAndFinish[1].index) {
            //     if (a < b) {
            //         Debug.Log("<color=green>--></color> 1");
            //         return (startAndFinish.ToArray(), new float[2]{a, b});
            //     }
            //     Debug.Log("<color=green>--></color> 2");
            //     return (startAndFinish.ToArray(), new float[2]{a, b + (2 * Mathf.PI)});
            // } else {
            //     if (b < a) {
            //         Debug.Log("<color=green>--></color> 3");
            //         return (new node[2]{startAndFinish[1], startAndFinish[0]}, new float[2]{b, a});
            //     }
            //     Debug.Log("<color=green>--></color> 4");
            //     return (new node[2]{startAndFinish[1], startAndFinish[0]}, new float[2]{b, a + (2 * Mathf.PI)});
            // }
        }

        private bool hasConnection(node origin, node connection)
        {
            foreach (node n in origin.connections)
            {
                if (n.index == connection.index)
                {
                    return true;
                }
            }
            return false;
        }

        private IEnumerator setConnections(node n)
        {
            // setting range of rotation for adding new nodes.
            var ranges = getRange(n);
            var range = ranges.angles;
            // float origin = range[0];
            float rotation = range[0];
            float maxRotation = range[1];
            // float difference = (Mathf.PI * 2) - rotation;

            node start = ranges.nodes[0];
            node finish = new node();
            bool hasFinish = false;
            if (ranges.nodes.Length == 2)
            {
                finish = ranges.nodes[1];
                hasFinish = true;
            }
            Debug.Log($"<color=yellow>start:</color> {start.index}:{rotation}, <color=yellow>finish:</color> {finish.index}:{maxRotation}");
            while (rotation < maxRotation)
            {
                if (n.connections.Count != 0)
                {
                    rotation += Mathf.PerlinNoise(n.location.x + seed + rotation * .531f, n.location.y + seed + rotation * .531f) + 1 * .66f;
                }
                Debug.Log($"{rotation} : {maxRotation}");
                if (rotation > maxRotation)
                {
                    Debug.Log("<color=red>GOING OVERBOARD: CONNECT</color> ---> ");
                    if (hasFinish)
                    {
                        addTriangle(n, n.connections[n.connections.Count - 1], finish);
                        if (!hasConnection(n.connections[n.connections.Count - 1], finish)) n.connections[n.connections.Count - 1].connections.Add(finish);
                        if (!hasConnection(finish, n.connections[n.connections.Count - 1])) finish.connections.Add(n.connections[n.connections.Count - 1]);
                    }
                    else
                    {
                        node variableConnection = n.connections[0].index != previous.index ? n.connections[0] : n.connections[1];
                        addTriangle(n, n.connections[n.connections.Count - 1], variableConnection);
                        if (!hasConnection(n.connections[n.connections.Count - 1], variableConnection)) n.connections[n.connections.Count - 1].connections.Add(variableConnection);
                        if (!hasConnection(variableConnection, n.connections[n.connections.Count - 1])) variableConnection.connections.Add(n.connections[n.connections.Count - 1]);
                    }
                    rendered.Add(n.index);
                    updateHead(n);
                    yield return new WaitForSecondsRealtime(.5f);
                    yield break;
                }

                Vector2 location = getPoint(n.location, getDistance((int)(n.location.x * 10), (int)(n.location.y * 10), rotation + n.index), rotation);
                node connection = new node(location, vertices.Count);
                vertices.Add(new Vector3(connection.location.x, 0, connection.location.y));
                if (n.connections.Count > 0)
                {
                    addTriangle(n, n.connections[n.connections.Count - 1], connection);
                    if (!hasConnection(connection, n)) connection.connections.Add(n);
                    if (!hasConnection(connection, n.connections[n.connections.Count - 1])) connection.connections.Add(n.connections[n.connections.Count - 1]);
                    if (!hasConnection(n.connections[n.connections.Count - 1], connection)) n.connections[n.connections.Count - 1].connections.Add(connection);
                    // connection.connections.Add(n.connections[n.connections.Count - 1]);
                    if (!hasConnection(n, connection)) n.connections.Add(connection);
                }
                else
                {
                    connection.connections.Add(n);
                    n.connections.Add(connection);
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
