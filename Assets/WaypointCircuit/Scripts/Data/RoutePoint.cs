using UnityEngine;

namespace WaypointCircuit.Data
{
    public struct RoutePoint
    {
        private Vector3 position;
        private Vector3 direction;

        public RoutePoint(Vector3 position, Vector3 direction)
        {
            this.position = position;
            this.direction = direction;
        }
    }
}