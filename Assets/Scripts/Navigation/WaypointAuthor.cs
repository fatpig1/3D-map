using UnityEngine;
#if META_XR_MRUK
using Meta.XR.MRUtilityKit;
#endif

namespace IndoorNav
{
    /// <summary>
    /// Authoring input handler. On the place button it asks the central
    /// AnchorLoader to create a spatial anchor (via Meta SpatialAnchorCore) at
    /// the controller position. The loader handles persistence + visualization.
    /// Deletion is handled by WaypointEditor.
    /// </summary>
    public class WaypointAuthor : MonoBehaviour
    {
        [Header("Mode")]
        public bool authoringEnabled = false;

        [Header("Central store")]
        public AnchorLoader loader;

        [Header("Input")]
        public OVRInput.Button placeButton = OVRInput.Button.One;   // A
        public OVRInput.Button saveButton  = OVRInput.Button.Two;   // B (manual re-save)
        public OVRInput.Controller controller = OVRInput.Controller.RTouch;
        [Tooltip("Optional: ray origin transform (RightHandAnchor). If null, uses OVRInput pose.")]
        public Transform placeOrigin;

        private void Update()
        {
            if (!authoringEnabled) return;

            if (OVRInput.GetDown(placeButton, controller))
            {
                Vector3 pos; Quaternion rot;
                if (placeOrigin != null)
                {
                    pos = placeOrigin.position;
                    rot = placeOrigin.rotation;
                }
                else
                {
                    pos = OVRInput.GetLocalControllerPosition(controller);
                    rot = OVRInput.GetLocalControllerRotation(controller);
                    var rig = FindAnyObjectByType<OVRCameraRig>();
                    if (rig != null)
                    {
                        pos = rig.trackingSpace.TransformPoint(pos);
                        rot = rig.trackingSpace.rotation * rot;
                    }
                }

                Debug.Log($"[WaypointAuthor] Place at {pos:F2}");
                if (loader != null) loader.AddWaypoint(pos, rot, TryGetRoomName(pos));
                else Debug.LogError("[WaypointAuthor] No AnchorLoader assigned.");
            }

            if (OVRInput.GetDown(saveButton, controller) && loader != null)
            {
                loader.PersistToDisk();
                Debug.Log("[WaypointAuthor] Manual save.");
            }
        }

        private static string TryGetRoomName(Vector3 worldPos)
        {
#if META_XR_MRUK
            var mruk = MRUK.Instance;
            if (mruk == null || mruk.Rooms == null) return "?";
            for (int i = 0; i < mruk.Rooms.Count; i++)
            {
                var room = mruk.Rooms[i];
                if (room != null && room.IsPositionInRoom(worldPos, true))
                    return $"Room {i + 1}";
            }
#endif
            return "?";
        }
    }
}
