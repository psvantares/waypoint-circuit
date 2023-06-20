using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WaypointCircuit.Data;

namespace WaypointCircuit.Core
{
    public class WaypointCircuit : MonoBehaviour
    {
        [Serializable]
        public class WaypointList
        {
            [SerializeField]
            public WaypointCircuit Circuit;

            [SerializeField]
            public Transform[] Items;
        }

        [SerializeField]
        private RouteType routeType;

        [SerializeField]
        private bool circuit = true;

        [SerializeField]
        [Range(0.0f, 100.0f)]
        private float entryDistance = 2f;

        [SerializeField]
        [Range(0.0f, 100.0f)]
        private float exitDistance = 2f;

        [SerializeField]
        [Range(4, 50)]
        private int curveSmoothing = 50;

        [SerializeField]
        [Range(4, 50)]
        private int curveSmoothingInternal = 10;

        [SerializeField]
        private bool isAlwaysDraw;

        private float length;
        private int numPoints;
        private Vector3[] points;
        private float[] distances;
        private Vector3 curveStartPoint;
        private Vector3 curveEndPoint;

        private int p0N;
        private int p1N;
        private int p2N;
        private int p3N;

        private float i;
        private Vector3 p0;
        private Vector3 p1;
        private Vector3 p2;
        private Vector3 p3;

        public WaypointList WaypointListData = new();
        public List<Vector3> CurvePoints = new();

        private Transform[] WaypointData => WaypointListData.Items;

        public bool Circuit
        {
            get => circuit;
            set => circuit = value;
        }

        private void Awake()
        {
            if (WaypointData.Length > 1)
            {
                CachePositionsAndDistances();
            }

            numPoints = WaypointData.Length;

            CreateData();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawGizmos(isAlwaysDraw);

            foreach (var way in WaypointData)
            {
                if (Selection.Contains(way.gameObject))
                {
                    DrawGizmos(true);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }

#endif

        public RoutePoint GetRoutePoint(float dist)
        {
            var p1 = GetRoutePosition(dist);
            var p2 = GetRoutePosition(dist + 0.1f);
            var delta = p2 - p1;
            return new RoutePoint(p1, delta.normalized);
        }

        private Vector3 GetRoutePosition(float dist)
        {
            var point = 0;
            dist = Mathf.Repeat(dist, length);

            while (distances[point] < dist)
            {
                ++point;
            }

            p1N = ((point - 1) + numPoints) % numPoints;
            p2N = point;

            i = Mathf.InverseLerp(distances[p1N], distances[p2N], dist);

            if (routeType == RouteType.External)
            {
                p0N = ((point - 2) + numPoints) % numPoints;
                p3N = (point + 1) % numPoints;

                p2N = p2N % numPoints;

                p0 = points[p0N];
                p1 = points[p1N];
                p2 = points[p2N];
                p3 = points[p3N];

                return CatMullRom(p0, p1, p2, p3, i);
            }
            else
            {
                p1N = ((point - 1) + numPoints) % numPoints;
                p2N = point;

                return Vector3.Lerp(points[p1N], points[p2N], i);
            }
        }

        private static Vector3 CatMullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
        {
            return 0.5f * ((2 * p1) + (-p0 + p2) * i + (2 * p0 - 5 * p1 + 4 * p2 - p3) * i * i + (-p0 + 3 * p1 - 3 * p2 + p3) * i * i * i);
        }

        private void CachePositionsAndDistances()
        {
            points = new Vector3[WaypointData.Length + 1];
            distances = new float[WaypointData.Length + 1];

            float accumulateDistance = 0;

            for (var i = 0; i < points.Length; i++)
            {
                var t1 = WaypointData[(i) % WaypointData.Length];
                var t2 = WaypointData[(i + 1) % WaypointData.Length];

                if (t1 != null && t2 != null)
                {
                    var p1 = t1.position;
                    var p2 = t2.position;
                    points[i] = WaypointData[i % WaypointData.Length].position;
                    distances[i] = accumulateDistance;
                    accumulateDistance += (p1 - p2).magnitude;
                }
            }
        }

        private void DrawGizmos(bool selected)
        {
            if (!selected)
            {
                return;
            }

            WaypointListData.Circuit = this;

            foreach (var child in WaypointData)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(child.transform.position, new Vector3(0.5f, 0.5f, 0.5f));
            }

            if (WaypointData.Length > 1)
            {
                numPoints = WaypointData.Length;

                CachePositionsAndDistances();
                length = Circuit ? distances[^1] : distances[^2];

                switch (routeType)
                {
                    case RouteType.External:
                        CreateExternal();
                        break;
                    case RouteType.Internal:
                        CreateInternal();
                        break;
                    case RouteType.Plain:
                        CreatePlainLine();
                        break;
                }
            }
        }

        private void CreateExternal()
        {
            Gizmos.color = Color.yellow;

            var prev = WaypointData[0].position;
            for (float dist = 0; dist < length; dist += length / curveSmoothing)
            {
                var next = GetRoutePosition(dist);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            CreatePlainLine();
        }

        private void CreateInternal()
        {
            for (var n = 0; n < WaypointData.Length; n++)
            {
                if (!Circuit && n == 0)
                {
                    continue;
                }

                var nextIndex = n + 1;
                var prevIndex = n - 1;

                if ((n - 1) < 0)
                {
                    prevIndex = WaypointData.Length - 1;
                }

                if (n + 1 >= WaypointData.Length)
                {
                    nextIndex = Circuit ? 0 : WaypointData.Length - 1;
                }

                var currentPoint = WaypointData[n].position;

                curveStartPoint = Vector3.MoveTowards(currentPoint, WaypointData[nextIndex].position, entryDistance);
                curveEndPoint = Vector3.MoveTowards(currentPoint, WaypointData[prevIndex].position, exitDistance);

                var prev = curveStartPoint;

                for (var a = 0; a < curveSmoothingInternal; a++)
                {
                    var t = a / (curveSmoothingInternal - 1.0f);
                    var position = (1.0f - t) * (1.0f - t) * curveStartPoint + 2.0f * (1.0f - t) * t * currentPoint + t * t * curveEndPoint;

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(prev, position);
                    prev = position;
                }
            }

            CreatePlainLine();
        }

        private void CreatePlainLine()
        {
            Gizmos.color = Color.blue;

            for (var n = 0; n < WaypointData.Length; n++)
            {
                var nextIndex = n + 1;

                if (nextIndex >= WaypointData.Length)
                {
                    nextIndex = Circuit ? 0 : WaypointData.Length - 1;
                }

                var prev = WaypointData[n].position;
                var next = WaypointData[nextIndex].position;

                Gizmos.DrawLine(prev, next);
            }
        }

        private void CreateData()
        {
            if (WaypointData.Length > 1)
            {
                numPoints = WaypointData.Length;
                CachePositionsAndDistances();
                length = Circuit ? distances[^1] : distances[^2];

                CurvePoints = routeType switch
                {
                    RouteType.External => GetPointExternal(),
                    RouteType.Internal => GetPointInternal(),
                    RouteType.Plain => GetPointPlain(),
                    _ => CurvePoints
                };
            }
        }

        private List<Vector3> GetPointExternal()
        {
            var list = new List<Vector3>();

            for (float dist = 0; dist < length; dist += length / curveSmoothing)
            {
                var point = GetRoutePosition(dist);
                list.Add(point);
            }

            return list;
        }

        private List<Vector3> GetPointInternal()
        {
            var list = new List<Vector3>();
            for (var n = 0; n < WaypointData.Length; n++)
            {
                if (!Circuit && n == 0)
                {
                    continue;
                }

                var nextIndex = n + 1;
                var prevIndex = n - 1;

                if ((n - 1) < 0)
                {
                    prevIndex = WaypointData.Length - 1;
                }

                if (n + 1 >= WaypointData.Length)
                {
                    nextIndex = Circuit ? 0 : WaypointData.Length - 1;
                }

                var currentPoint = WaypointData[n].position;

                curveStartPoint = Vector3.MoveTowards(currentPoint, WaypointData[nextIndex].position, entryDistance);
                curveEndPoint = Vector3.MoveTowards(currentPoint, WaypointData[prevIndex].position, exitDistance);

                var buffer = new List<Vector3>();

                for (var a = 0; a < curveSmoothingInternal; a++)
                {
                    var t = a / (curveSmoothingInternal - 1.0f);
                    var position = (1.0f - t) * (1.0f - t) * curveStartPoint + 2.0f * (1.0f - t) * t * currentPoint + t * t * curveEndPoint;

                    buffer.Add(position);
                }

                buffer.Reverse();
                list.AddRange(buffer);
            }

            return list;
        }

        private List<Vector3> GetPointPlain()
        {
            var list = new List<Vector3>();
            list.AddRange(WaypointData.Select(t => t.position));

            return list;
        }
    }
}