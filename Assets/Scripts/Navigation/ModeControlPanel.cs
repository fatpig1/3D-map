using UnityEngine;
using UnityEngine.UI;
#if META_XR_MRUK
using Meta.XR.MRUtilityKit;
#endif

namespace IndoorNav
{
    /// <summary>
    /// Logic-only controller for a runtime control UI. You author the Canvas
    /// (buttons, layout, fonts, colors) in the Unity editor and wire each
    /// Button.OnClick to one of the public methods below:
    ///   • EnterAuthor()   — switch to authoring mode
    ///   • EnterNavigate() — switch to navigation mode
    ///   • RetryMruk()     — force MRUK to reload scene data
    /// Optionally assign a Text or TMP_Text to <see cref="statusText"/> so
    /// the script can display a one-line status message.
    /// This script does NOT generate any UI. Parent the GameObject under
    /// CenterEyeAnchor (or any transform you want) for placement.
    /// </summary>
    public class ModeControlPanel : MonoBehaviour
    {
        [Header("References")]
        public NavigationManager manager;
        public WaypointAuthor author;
        public AnchorLoader loader;
        [Tooltip("Head transform used for follow-head placement. Auto-filled from Camera.main if empty.")]
        public Transform head;

        [Header("Optional UI")]
        [Tooltip("Optional Text that will receive status messages. Leave empty to log to console only.")]
        public Text statusText;

        [Header("Placement (follows head)")]
        [Tooltip("When enabled, the panel continuously follows the head. When disabled, the Transform is used as-is.")]
        public bool followHead = false;
        public float distance = 0.7f;
        [Tooltip("Sideways offset from head-forward. Positive = right, negative = left.")]
        public float horizontalOffset = 0f;
        [Tooltip("Up/down offset. Positive = up, negative = down.")]
        public float verticalOffset = 0f;

        private void Start()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (!followHead || head == null) return;
            Vector3 fwd = head.forward; fwd.y = 0; fwd.Normalize();
            Vector3 right = head.right; right.y = 0; right.Normalize();
            transform.position = head.position + fwd * distance
                + right * horizontalOffset + Vector3.up * verticalOffset;
            transform.rotation = Quaternion.LookRotation(transform.position - head.position, Vector3.up);
        }

        // ----- Public methods callable from Inspector OnClick -----

        public void EnterAuthor()
        {
            if (manager != null) manager.EnterAuthorMode();
            SetStatus("AUTHOR mode");
        }

        public void EnterNavigate()
        {
            if (manager != null) manager.EnterNavigateMode();
            SetStatus("NAVIGATE mode");
        }

        /// <summary>Erase every waypoint (device + disk). Wire to a "Delete All" button.</summary>
        public void DeleteAll()
        {
            if (loader != null)
            {
                int n = loader.Graph != null ? loader.Graph.nodes.Count : 0;
                loader.ClearAll();
                SetStatus($"Deleted all ({n}) waypoints.");
            }
            else SetStatus("No loader.");
        }

        public async void RetryMruk()
        {
#if META_XR_MRUK
            var mruk = MRUK.Instance;
            if (mruk == null) { SetStatus("MRUK null"); return; }
            SetStatus("MRUK loading...");
            await mruk.LoadSceneFromDevice();
            SetStatus($"MRUK rooms = {mruk.Rooms.Count}");
            Debug.Log($"[ModeControlPanel] Manual MRUK reload: rooms={mruk.Rooms.Count}, init={mruk.IsInitialized}");
#else
            SetStatus("META_XR_MRUK not defined");
#endif
        }

        /// <summary>
        /// Full re-alignment after the Quest tracking origin may have shifted
        /// (typical on app re-launch). Clears MRUK rooms, waits for the head
        /// pose to stabilize, then re-loads scene data and anchors so everything
        /// is positioned against the current tracking origin.
        /// </summary>
        public async void Relocalize()
        {
            SetStatus("Relocalizing...");
            Debug.Log("[ModeControlPanel] Relocalize begin");

#if META_XR_MRUK
            var mruk = MRUK.Instance;
            if (mruk != null)
            {
                mruk.ClearScene();
                // Let the tracker settle before fetching scene data again.
                await System.Threading.Tasks.Task.Delay(1500);
                await mruk.LoadSceneFromDevice();
                Debug.Log($"[ModeControlPanel] MRUK reloaded: rooms={mruk.Rooms.Count}");
            }
#endif
            // Re-localize spatial anchors so waypoint markers move with Quest's
            // updated coordinate system.
            if (loader != null)
            {
                Debug.Log("[ModeControlPanel] Reloading anchors");
                loader.LoadFromDisk();
            }

            SetStatus("Relocalized.");
        }

        private void SetStatus(string s)
        {
            if (statusText != null) statusText.text = s;
            Debug.Log($"[ModeControlPanel] {s}");
        }
    }
}
