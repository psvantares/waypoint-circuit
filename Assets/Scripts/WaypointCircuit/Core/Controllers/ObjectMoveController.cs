using System;
using UnityEngine;
using WaypointCircuit.Core.Views;

namespace WaypointCircuit.Core.Controllers
{
	public class ObjectMoveController : MonoBehaviour
	{
		[SerializeField] [Range(1.0f, 100.0f)] private float _speedMove = 10.0f;
		[SerializeField] [Range(1.0f, 100.0f)] private float _speedRotate = 50.0f;
		[SerializeField] private Vector3 _offset = new Vector3(0.0f, 0.5f, 0.0f);
		[SerializeField] private float _rayCastDistance = 1.0f;

		private const string ObstacleTag = "Obstacle";
		private const string ObstacleMoveTag = "ObstacleMove";

		private bool _stop;
		private Vector3 _position;
		private Quaternion _rotation;

		public event Action OnEndOfRoad;

		private Vector3 Position
		{
			get
			{
				_position = transform.position - _offset;
				return _position;
			}

			set
			{
				_position = value;
				transform.position = _position + _offset;
			}
		}

		private Quaternion Rotation
		{
			get
			{
				_rotation = transform.rotation;
				return _rotation;
			}

			set
			{
				_rotation = value;
				transform.rotation = _rotation;
			}
		}

		private void Update()
		{
			UpdateRayCast();
		}

		private void OnTriggerExit(Collider other)
		{
			if (!other.CompareTag(ObstacleTag)) return;

			var obstacle = other.GetComponent<ObstacleView>();
			obstacle.PathIsBusy = false;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!other.CompareTag(ObstacleTag)) return;

			var obstacle = other.GetComponent<ObstacleView>();
			obstacle.PathIsBusy = true;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.magenta;
			var tr = transform;
			Gizmos.DrawRay(tr.position, tr.forward * (_rayCastDistance + tr.localScale.z));
		}

		public void Move(Vector3 currentPoint)
		{
			if (_stop) return;

			var targetLookRotation = currentPoint - Position;

			if (targetLookRotation != Vector3.zero)
			{
				Rotation = Quaternion.Lerp(
					transform.rotation,
					Quaternion.LookRotation(currentPoint - Position, Vector3.up),
					_speedRotate * Time.deltaTime);
			}

			Position = Vector3.MoveTowards(
				Position,
				currentPoint,
				_speedMove * Time.deltaTime);

			if (Vector3.Distance(Position, currentPoint) <= 0.1f)
			{
				OnEndOfRoad?.Invoke();
			}
		}

		private void UpdateRayCast()
		{
			var tr = transform;
			Physics.Raycast(tr.position, tr.forward, out var hit, _rayCastDistance);

			if (hit.collider != null && hit.collider.CompareTag(ObstacleMoveTag))
			{
				_stop = true;
			}
			else
			{
				_stop = false;
			}
		}
	}
}