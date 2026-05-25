using UnityEngine;
#if META_XR_MRUK
using Meta.XR.MRUtilityKit;
#endif

namespace IndoorNav
{
    /// <summary>
    /// Optional helper that snaps a list of world-space waypoints onto the
    /// MRUK floor plane (y = floor height) and optionally rejects points that
    /// would cross a wall. Falls through to a no-op if MRUK is unavailable.
    /// Define META_XR_MRUK in Player Settings -> Scripting Define Symbols to
    /// enable the integration once the MRUK package is installed.
    /// </summary>
    public static class MrukFloorSnap
    {
        public static Vector3 SnapToFloor(Vector3 worldPos, float fallbackY = 0f)
        {
#if META_XR_MRUK
            var mruk = MRUK.Instance;
            if (mruk != null && mruk.Rooms != null && mruk.Rooms.Count > 0)
            {
                // Prefer the room whose 2D footprint contains the point; else the
                // floor with the closest Y. Works across multiple scanned rooms.
                float bestY = float.NaN;
                float bestDistY = float.PositiveInfinity;
                bool foundContaining = false;

                for (int i = 0; i < mruk.Rooms.Count; i++)
                {
                    var room = mruk.Rooms[i];
                    if (room == null) continue;
                    var floor = room.FloorAnchor;
                    if (floor == null) continue;
                    float y = floor.transform.position.y;

                    if (room.IsPositionInRoom(worldPos, true))
                    {
                        bestY = y;
                        foundContaining = true;
                        break;
                    }
                    float d = Mathf.Abs(worldPos.y - y);
                    if (d < bestDistY) { bestDistY = d; bestY = y; }
                }

                if (foundContaining || !float.IsNaN(bestY))
                    return new Vector3(worldPos.x, bestY, worldPos.z);
            }
#endif
            return new Vector3(worldPos.x, fallbackY, worldPos.z);
        }

        public static bool SegmentCrossesWall(Vector3 a, Vector3 b)
        {
#if META_XR_MRUK
            var room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
            if (room == null) return false;
            Vector3 dir = b - a;
            float len = dir.magnitude;
            if (len < 1e-4f) return false;
            Ray r = new Ray(a, dir / len);
            return room.Raycast(r, len, LabelFilter.FromEnum(MRUKAnchor.SceneLabels.WALL_FACE), out _, out _);
#else
            return false;
#endif
        }
    }
}
