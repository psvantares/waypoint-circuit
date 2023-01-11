using UnityEngine;

namespace WaypointCircuit.Core.Views
{
	public class ObstacleView : MonoBehaviour
	{
		[SerializeField] private bool _pathIsBusy;

		public bool PathIsBusy
		{
			get => _pathIsBusy;
			set => _pathIsBusy = value;
		}
	}
}