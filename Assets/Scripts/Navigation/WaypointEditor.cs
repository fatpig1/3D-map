using UnityEngine;

namespace IndoorNav
{
    /// <summary>
    /// Hover-and-delete editor for individual waypoints. Reads the waypoint set
    /// from the central <see cref="AnchorLoader"/>, highlights the marker the
    /// controller ray points at, and on the delete button erases that anchor
    /// (device + disk) via the loader. Only active in Author mode.
    /// </summary>
    public class WaypointEditor : MonoBehaviour
    {
        [Header("References")]
        public AnchorLoader loader;
        public NavigationManager manager;
        [Tooltip("Transform whose forward axis is the ray (assign RightHandAnchor).")]
        public Transform rayOrigin;

        [Header("Input")]
        public OVRInput.Controller controller = OVRInput.Controller.RTouch;
        public OVRInput.Button deleteButton = OVRInput.Button.PrimaryHandTrigger; // right grip

        [Header("Pick settings")]
        public float pickRadius = 0.15f;
        public float maxDistance = 8f;
        public bool authorModeOnly = true;

        [Header("Visual")]
        public Color hoverTint = new Color(1f, 0.4f, 0.2f, 1f);

        private int _hoveredIdx = -1;
        private Renderer _hoveredRend;
        private Color _origColor;

        private void Start()
        {
            if (rayOrigin == null)
            {
                var rig = FindAnyObjectByType<OVRCameraRig>();
                if (rig != null) rayOrigin = rig.rightHandAnchor;
            }
        }

        private void Update()
        {
            if (loader == null) { ClearHover(); return; }
            if (authorModeOnly && manager != null &&
                manager.CurrentMode != NavigationManager.Mode.Author) { ClearHover(); return; }
            if (rayOrigin == null) { ClearHover(); return; }

            Vector3 origin = rayOrigin.position;
            Vector3 dir = rayOrigin.forward;

            int hitIdx = FindHovered(origin, dir);

            if (hitIdx != _hoveredIdx)
            {
                ClearHover();
                _hoveredIdx = hitIdx;
                if (_hoveredIdx >= 0 && _hoveredIdx < loader.Markers.Count)
                {
                    var go = loader.Markers[_hoveredIdx];
                    if (go != null)
                    {
                        _hoveredRend = go.GetComponentInChildren<Renderer>();
                        if (_hoveredRend != null)
                        {
                            _origColor = _hoveredRend.material.color;
                            _hoveredRend.material.color = hoverTint;
                        }
                    }
                }
            }

            if (_hoveredIdx >= 0 && OVRInput.GetDown(deleteButton, controller))
            {
                int toDelete = _hoveredIdx;
                Debug.Log($"[WaypointEditor] Delete request idx={toDelete}");
                ClearHover();
                // Loader erases the anchor (via SpatialAnchorCore) and destroys the marker.
                loader.RemoveWaypoint(toDelete);
            }
        }

        private int FindHovered(Vector3 origin, Vector3 dir)
        {
            int best = -1;
            float bestT = float.PositiveInfinity;
            var positions = loader.Positions;
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 p = positions[i];
                if (p == Vector3.zero) continue;
                Vector3 v = p - origin;
                float t = Vector3.Dot(v, dir);
                if (t < 0f || t > maxDistance) continue;
                Vector3 closest = origin + dir * t;
                float perp = (p - closest).magnitude;
                if (perp <= pickRadius && t < bestT)
                {
                    bestT = t;
                    best = i;
                }
            }
            return best;
        }

        private void ClearHover()
        {
            if (_hoveredRend != null)
            {
                _hoveredRend.material.color = _origColor;
                _hoveredRend = null;
            }
            _hoveredIdx = -1;
        }

        public int HoveredIndex => _hoveredIdx;
    }
}
