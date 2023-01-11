using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WaypointCircuit.Core.Data;

namespace WaypointCircuit.Core
{
	public class WaypointCircuit : MonoBehaviour
	{
		[Serializable]
		public class WaypointList
		{
			[SerializeField] public WaypointCircuit _circuit;
			[SerializeField] public Transform[] _items = new Transform[0];
		}

		[SerializeField] private RouteType _routeType;
		[SerializeField] private bool _circuit = true;
		[SerializeField] [Range(0.0f, 100.0f)] private float _entryDistance = 2f;
		[SerializeField] [Range(0.0f, 100.0f)] private float _exitDistance = 2f;
		[SerializeField] [Range(4, 50)] private int _curveSmoothing = 50;
		[SerializeField] [Range(4, 50)] private int _curveSmoothingInternal = 10;
		[SerializeField] private bool _isAlwaysDraw;

		private float _length;
		private int _numPoints;
		private Vector3[] _points;
		private float[] _distances;
		private Vector3 _curveStartPoint;
		private Vector3 _curveEndPoint;

		private int _p0N;
		private int _p1N;
		private int _p2N;
		private int _p3N;

		private float _i;
		private Vector3 _p0;
		private Vector3 _p1;
		private Vector3 _p2;
		private Vector3 _p3;

		public WaypointList _waypointList = new();
		public List<Vector3> _curvePoints = new();

		private Transform[] WaypointData => _waypointList._items;

		public bool Circuit
		{
			get => _circuit;
			set => _circuit = value;
		}

		private void Awake()
		{
			if (WaypointData.Length > 1)
			{
				CachePositionsAndDistances();
			}

			_numPoints = WaypointData.Length;

			CreateData();
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			DrawGizmos(_isAlwaysDraw);

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
			dist = Mathf.Repeat(dist, _length);

			while (_distances[point] < dist)
			{
				++point;
			}

			_p1N = ((point - 1) + _numPoints) % _numPoints;
			_p2N = point;

			_i = Mathf.InverseLerp(_distances[_p1N], _distances[_p2N], dist);

			if (_routeType == RouteType.External)
			{
				_p0N = ((point - 2) + _numPoints) % _numPoints;
				_p3N = (point + 1) % _numPoints;

				_p2N = _p2N % _numPoints;

				_p0 = _points[_p0N];
				_p1 = _points[_p1N];
				_p2 = _points[_p2N];
				_p3 = _points[_p3N];

				return CatMullRom(_p0, _p1, _p2, _p3, _i);
			}
			else
			{
				_p1N = ((point - 1) + _numPoints) % _numPoints;
				_p2N = point;

				return Vector3.Lerp(_points[_p1N], _points[_p2N], _i);
			}
		}

		private static Vector3 CatMullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
		{
			return 0.5f *
			       ((2 * p1) + (-p0 + p2) * i + (2 * p0 - 5 * p1 + 4 * p2 - p3) * i * i +
			        (-p0 + 3 * p1 - 3 * p2 + p3) * i * i * i);
		}

		private void CachePositionsAndDistances()
		{
			_points = new Vector3[WaypointData.Length + 1];
			_distances = new float[WaypointData.Length + 1];

			float accumulateDistance = 0;

			for (var i = 0; i < _points.Length; i++)
			{
				var t1 = WaypointData[(i) % WaypointData.Length];
				var t2 = WaypointData[(i + 1) % WaypointData.Length];

				if (t1 != null && t2 != null)
				{
					var p1 = t1.position;
					var p2 = t2.position;
					_points[i] = WaypointData[i % WaypointData.Length].position;
					_distances[i] = accumulateDistance;
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

			_waypointList._circuit = this;

			foreach (var child in WaypointData)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawCube(child.transform.position, new Vector3(0.5f, 0.5f, 0.5f));
			}

			if (WaypointData.Length > 1)
			{
				_numPoints = WaypointData.Length;

				CachePositionsAndDistances();
				_length = Circuit ? _distances[_distances.Length - 1] : _distances[_distances.Length - 2];

				switch (_routeType)
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
			for (float dist = 0; dist < _length; dist += _length / _curveSmoothing)
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

				_curveStartPoint = Vector3.MoveTowards(currentPoint, WaypointData[nextIndex].position, _entryDistance);
				_curveEndPoint = Vector3.MoveTowards(currentPoint, WaypointData[prevIndex].position, _exitDistance);

				var prev = _curveStartPoint;

				for (var i = 0; i < _curveSmoothingInternal; i++)
				{
					var t = i / (_curveSmoothingInternal - 1.0f);
					var position = (1.0f - t) * (1.0f - t) * _curveStartPoint + 2.0f * (1.0f - t) * t * currentPoint +
					               t * t * _curveEndPoint;

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
				_numPoints = WaypointData.Length;
				CachePositionsAndDistances();
				_length = Circuit ? _distances[_distances.Length - 1] : _distances[_distances.Length - 2];

				_curvePoints = _routeType switch
				{
					RouteType.External => GetPointExternal(),
					RouteType.Internal => GetPointInternal(),
					RouteType.Plain => GetPointPlain(),
					_ => _curvePoints
				};
			}
		}

		private List<Vector3> GetPointExternal()
		{
			var list = new List<Vector3>();

			for (float dist = 0; dist < _length; dist += _length / _curveSmoothing)
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

				_curveStartPoint = Vector3.MoveTowards(currentPoint, WaypointData[nextIndex].position, _entryDistance);
				_curveEndPoint = Vector3.MoveTowards(currentPoint, WaypointData[prevIndex].position, _exitDistance);

				var buffer = new List<Vector3>();

				for (var i = 0; i < _curveSmoothingInternal; i++)
				{
					var t = i / (_curveSmoothingInternal - 1.0f);
					var position = (1.0f - t) * (1.0f - t) * _curveStartPoint + 2.0f * (1.0f - t) * t * currentPoint +
					               t * t * _curveEndPoint;

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