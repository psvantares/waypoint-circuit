using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaypointCircuit.Controllers
{
    public class ObjectController : MonoBehaviour
    {
        [SerializeField]
        private Core.WaypointCircuit waypointCircuit;

        [SerializeField]
        private ObjectMoveController objectMoveController;

        [SerializeField]
        private bool isDrive = true;

        [SerializeField]
        private bool isWait;

        [SerializeField]
        private bool isRepeat = true;

        private List<Vector3> points;
        private Vector3 currentPoint;
        private int pointIndex;
        private float wait;

        public event Action OnEndPath;
        public event Action OnRepeatPath;

        private void Awake()
        {
            wait = UnityEngine.Random.Range(1.0f, 5.0f);
        }

        private void Start()
        {
            points = waypointCircuit.CurvePoints;
            currentPoint = points[0];
        }

        private void Update()
        {
            if (!isDrive) return;

            if (isWait)
            {
                WaitStart();
            }
            else
            {
                objectMoveController.Move(currentPoint);
            }
        }

        private void OnEnable()
        {
            objectMoveController.OnEndOfRoad += GetNextPoint;
        }

        private void OnDisable()
        {
            objectMoveController.OnEndOfRoad -= GetNextPoint;
        }

        private void WaitStart()
        {
            wait -= Time.deltaTime;

            if (wait < 0)
            {
                isWait = false;
            }
        }

        private void GetNextPoint()
        {
            if (waypointCircuit.Circuit)
            {
                if (pointIndex >= points.Count - 1)
                {
                    if (pointIndex == points.Count - 1)
                    {
                        EndPath();
                    }
                    else
                    {
                        pointIndex++;
                        currentPoint = points[0];
                    }
                }
                else
                {
                    pointIndex++;
                    currentPoint = points[pointIndex];
                }
            }
            else
            {
                if (pointIndex >= points.Count - 1)
                {
                    EndPath();
                }
                else
                {
                    pointIndex++;
                    currentPoint = points[pointIndex];
                }
            }
        }

        private void EndPath()
        {
            if (isRepeat)
            {
                pointIndex = 0;

                OnRepeatPath?.Invoke();
            }
            else
            {
                isDrive = false;

                OnEndPath?.Invoke();
            }
        }
    }
}