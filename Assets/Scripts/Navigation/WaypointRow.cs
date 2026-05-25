using UnityEngine;
using UnityEngine.UI;

namespace IndoorNav
{
    /// <summary>
    /// Reference holder for one row of the Anchor List. Put this on the row
    /// prefab and wire the three child UI elements in the editor. WaypointListPanel
    /// instantiates this prefab per waypoint and fills it in at runtime.
    /// </summary>
    public class WaypointRow : MonoBehaviour
    {
        [Tooltip("Label text shown for the waypoint (e.g. 'OK Room 302').")]
        public Text labelText;
        [Tooltip("Button that cycles the preset room name.")]
        public Button renameButton;
        [Tooltip("Button that deletes this waypoint.")]
        public Button deleteButton;
    }
}
