using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaypointCircuit.Core.Controllers
{
	public class ObjectController : MonoBehaviour
	{
		[SerializeField] private WaypointCircuit _waypointCircuit;
		[SerializeField] private ObjectMoveController _objectMoveController;
		[SerializeField] private bool _isDrive = true;
		[SerializeField] private bool _isWait;
		[SerializeField] private bool _isRepeat = true;

		private List<Vector3> _points;
		private Vector3 _currentPoint;
		private int _pointIndex;
		private float _wait;

		public event Action OnEndPath;
		public event Action OnRepeatPath;

		private void Awake()
		{
			_wait = UnityEngine.Random.Range(1.0f, 5.0f);
		}

		private void Start()
		{
			_points = _waypointCircuit._curvePoints;
			_currentPoint = _points[0];
		}

		private void Update()
		{
			if (!_isDrive) return;

			if (_isWait)
			{
				WaitStart();
			}
			else
			{
				_objectMoveController.Move(_currentPoint);
			}
		}

		private void OnEnable()
		{
			_objectMoveController.OnEndOfRoad += GetNextPoint;
		}

		private void OnDisable()
		{
			_objectMoveController.OnEndOfRoad -= GetNextPoint;
		}

		private void WaitStart()
		{
			_wait -= Time.deltaTime;

			if (_wait < 0)
			{
				_isWait = false;
			}
		}

		private void GetNextPoint()
		{
			if (_waypointCircuit.Circuit)
			{
				if (_pointIndex >= _points.Count - 1)
				{
					if (_pointIndex == _points.Count - 1)
					{
						EndPath();
					}
					else
					{
						_pointIndex++;
						_currentPoint = _points[0];
					}
				}
				else
				{
					_pointIndex++;
					_currentPoint = _points[_pointIndex];
				}
			}
			else
			{
				if (_pointIndex >= _points.Count - 1)
				{
					EndPath();
				}
				else
				{
					_pointIndex++;
					_currentPoint = _points[_pointIndex];
				}
			}
		}

		private void EndPath()
		{
			if (_isRepeat)
			{
				_pointIndex = 0;

				OnRepeatPath?.Invoke();
			}
			else
			{
				_isDrive = false;

				OnEndPath?.Invoke();
			}
		}
	}
}