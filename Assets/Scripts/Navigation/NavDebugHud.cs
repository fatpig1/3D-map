using UnityEngine;
using UnityEngine.UI;

namespace IndoorNav
{
    /// <summary>
    /// On-headset diagnostic overlay. Writes state into an editor-authored UI
    /// Text (assign <see cref="statusText"/>). The GameObject follows the head;
    /// put the authored Canvas as a child of this GameObject.
    /// </summary>
    public class NavDebugHud : MonoBehaviour
    {
        [Header("References")]
        public WaypointAuthor author;
        public AnchorLoader loader;
        public NavigationManager manager;
        public Transform head;

        [Header("Editor-authored UI")]
        [Tooltip("Assign a UI Text that receives the multi-line status string.")]
        public Text statusText;

        [Header("Placement (follows head)")]
        public bool followHead = true;
        public float distance = 1.0f;
        public float verticalOffset = -0.2f;
        [Tooltip("Sideways offset from head-forward. Positive = right, negative = left.")]
        public float horizontalOffset = 0f;

        private float _lastAt, _lastBt, _lastStickT;

        private void Start()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.Touch)) _lastAt = Time.time;
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.Touch)) _lastBt = Time.time;
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.Touch)) _lastStickT = Time.time;

            if (followHead && head != null)
            {
                Vector3 fwd = head.forward; fwd.y = 0; fwd.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, fwd); // horizontal-plane right vector
                transform.position = head.position
                    + fwd * distance
                    + right * horizontalOffset
                    + Vector3.up * verticalOffset;
                transform.rotation = Quaternion.LookRotation(transform.position - head.position, Vector3.up);
            }

            if (statusText == null) return;
            string mode = manager != null ? manager.CurrentMode.ToString() : "?";
            string authoring = author != null ? author.authoringEnabled.ToString() : "?";
            int graphCount = loader != null && loader.Graph != null ? loader.Graph.nodes.Count : 0;
            int loaderReady = loader != null && loader.IsReady ? 1 : 0;
            int mrukRooms = -1;
            string mrukState = "?";
#if META_XR_MRUK
            var mruk = Meta.XR.MRUtilityKit.MRUK.Instance;
            if (mruk != null)
            {
                mrukRooms = mruk.Rooms != null ? mruk.Rooms.Count : -1;
                mrukState = mruk.IsInitialized ? "init" : "loading";
            }
            else mrukState = "null";
#endif
            string scenePerm = "?", anchorPerm = "?";
#if UNITY_ANDROID && !UNITY_EDITOR
            scenePerm = UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                "com.oculus.permission.USE_SCENE") ? "OK" : "NO";
            anchorPerm = UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                "com.oculus.permission.USE_ANCHOR_API") ? "OK" : "NO";
#endif
            statusText.text =
                $"Mode: {mode}\n" +
                $"Authoring: {authoring}\n" +
                $"Waypoints: {graphCount}\n" +
                $"Loader ready: {loaderReady}\n" +
                $"MRUK rooms: {mrukRooms} ({mrukState})\n" +
                $"Perms: scene={scenePerm} anchor={anchorPerm}\n" +
                $"--- input ---\n" +
                $"A pressed: {(Time.time - _lastAt):F1}s ago\n" +
                $"B pressed: {(Time.time - _lastBt):F1}s ago\n" +
                $"Stick:     {(Time.time - _lastStickT):F1}s ago\n";
        }
    }
}
