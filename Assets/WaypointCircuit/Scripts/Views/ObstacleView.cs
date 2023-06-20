using UnityEngine;

namespace WaypointCircuit.Views
{
    public class ObstacleView : MonoBehaviour
    {
        [SerializeField]
        private bool pathIsBusy;

        public bool PathIsBusy
        {
            get => pathIsBusy;
            set => pathIsBusy = value;
        }
    }
}