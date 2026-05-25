using UnityEngine;
#if META_XR_MRUK
using Meta.XR.MRUtilityKit;
#endif

namespace IndoorNav
{
    /// <summary>
    /// Forces MRUK to load *all* scanned rooms (not only the current one) so
    /// navigation/visualization survives crossing room boundaries. Attach to the
    /// MRUK GameObject. Runs before MRUK's own Start() because of script
    /// execution order (Awake fires across all components before any Start).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MrukSetup : MonoBehaviour
    {
        public bool loadAllRooms = true;
        public bool enableWorldLock = true;

        private void Awake()
        {
#if META_XR_MRUK
            var mruk = GetComponent<MRUK>();
            if (mruk == null) mruk = MRUK.Instance;
            if (mruk == null)
            {
                Debug.LogWarning("[MrukSetup] No MRUK component found on this GameObject.");
                return;
            }
            if (mruk.SceneSettings != null)
            {
                // MRUK v201 loads all scanned rooms by default; RoomFilter is a
                // query helper enum, not a settings field. Just make sure load-
                // on-startup is enabled.
                mruk.SceneSettings.LoadSceneOnStartup = true;
                Debug.Log($"[MrukSetup] LoadOnStart={mruk.SceneSettings.LoadSceneOnStartup}, " +
                          $"DataSource={mruk.SceneSettings.DataSource}");
            }
            mruk.EnableWorldLock = enableWorldLock;

            // Hook room lifecycle so logcat clearly shows when each room comes/goes.
            MRUK.Instance.RoomCreatedEvent.AddListener(r =>
                Debug.Log($"[MrukSetup] Room created: {(r != null ? r.gameObject.name : "null")}, total = {MRUK.Instance.Rooms.Count}"));
            MRUK.Instance.RoomRemovedEvent.AddListener(r =>
                Debug.Log($"[MrukSetup] Room removed: {(r != null ? r.gameObject.name : "null")}, remaining = {MRUK.Instance.Rooms.Count}"));
#else
            Debug.LogWarning("[MrukSetup] META_XR_MRUK is not defined; skipping.");
#endif
        }
    }
}
