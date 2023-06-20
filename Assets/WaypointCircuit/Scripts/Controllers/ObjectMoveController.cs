using System;
using UnityEngine;
using WaypointCircuit.Views;

namespace WaypointCircuit.Controllers
{
    public class ObjectMoveController : MonoBehaviour
    {
        [SerializeField]
        [Range(1.0f, 100.0f)]
        private float speedMove = 10.0f;

        [SerializeField]
        [Range(1.0f, 100.0f)]
        private float speedRotate = 10.0f;

        [SerializeField]
        private Vector3 offset = new(0.0f, 0.5f, 0.0f);

        [SerializeField]
        private float rayCastDistance = 1.0f;

        private const string OBSTACLE_TAG = "Obstacle";
        private const string OBSTACLE_MOVE_TAG = "ObstacleMove";

        private bool stop;
        private Vector3 position;
        private Quaternion rotation;

        public event Action OnEndOfRoad;

        private Vector3 Position
        {
            get
            {
                position = transform.position - offset;
                return position;
            }

            set
            {
                position = value;
                transform.position = position + offset;
            }
        }

        private Quaternion Rotation
        {
            get
            {
                rotation = transform.rotation;
                return rotation;
            }

            set
            {
                rotation = value;
                transform.rotation = rotation;
            }
        }

        private void Update()
        {
            UpdateRayCast();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(OBSTACLE_TAG)) return;

            var obstacle = other.GetComponent<ObstacleView>();
            obstacle.PathIsBusy = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(OBSTACLE_TAG)) return;

            var obstacle = other.GetComponent<ObstacleView>();
            obstacle.PathIsBusy = true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            var tr = transform;
            Gizmos.DrawRay(tr.position, tr.forward * (rayCastDistance + tr.localScale.z));
        }

        public void Move(Vector3 currentPoint)
        {
            if (stop) return;

            var targetLookRotation = currentPoint - Position;

            if (targetLookRotation != Vector3.zero)
            {
                Rotation = Quaternion.Lerp(
                    transform.rotation,
                    Quaternion.LookRotation(currentPoint - Position, Vector3.up),
                    speedRotate * Time.deltaTime);
            }

            Position = Vector3.MoveTowards(
                Position,
                currentPoint,
                speedMove * Time.deltaTime);

            if (Vector3.Distance(Position, currentPoint) <= 0.1f)
            {
                OnEndOfRoad?.Invoke();
            }
        }

        private void UpdateRayCast()
        {
            var tr = transform;
            Physics.Raycast(tr.position, tr.forward, out var hit, rayCastDistance);

            if (hit.collider != null && hit.collider.CompareTag(OBSTACLE_MOVE_TAG))
            {
                stop = true;
            }
            else
            {
                stop = false;
            }
        }
    }
}