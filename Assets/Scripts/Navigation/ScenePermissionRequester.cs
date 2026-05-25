using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
#if META_XR_MRUK
using Meta.XR.MRUtilityKit;
#endif

namespace IndoorNav
{
    /// <summary>
    /// On first launch the Quest pops a system dialog asking the user to allow
    /// the app to read scene / anchor data. If the user denies (or the dialog
    /// is missed), MRUK silently sees 0 rooms forever. This component requests
    /// the permission explicitly at startup and re-triggers an MRUK scene load
    /// after it is granted. Attach it to the MRUK GameObject (or anywhere in
    /// the scene). Logs every state transition so you can follow what is
    /// happening in logcat.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class ScenePermissionRequester : MonoBehaviour
    {
        private const string ScenePerm = "com.oculus.permission.USE_SCENE";
        private const string AnchorPerm = "com.oculus.permission.USE_ANCHOR_API";

        private IEnumerator Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            yield return RequestIfNeeded(ScenePerm);
            yield return RequestIfNeeded(AnchorPerm);
            Debug.Log($"[Perms] USE_SCENE granted = {Permission.HasUserAuthorizedPermission(ScenePerm)}");
            Debug.Log($"[Perms] USE_ANCHOR_API granted = {Permission.HasUserAuthorizedPermission(AnchorPerm)}");

            // After permissions are settled, force MRUK to (re)load from device.
#if META_XR_MRUK
            yield return new WaitForSeconds(0.5f);
            var mruk = MRUK.Instance;
            if (mruk != null)
            {
                Debug.Log("[Perms] Calling MRUK.LoadSceneFromDevice()");
                yield return mruk.LoadSceneFromDevice();
                Debug.Log($"[Perms] After LoadSceneFromDevice: rooms = {mruk.Rooms.Count}, initialized = {mruk.IsInitialized}");
            }
            else Debug.LogWarning("[Perms] MRUK.Instance still null after permissions.");
#endif
#else
            Debug.Log("[Perms] Editor / non-Android: skipping permission request.");
            yield break;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private IEnumerator RequestIfNeeded(string perm)
        {
            if (Permission.HasUserAuthorizedPermission(perm))
            {
                Debug.Log($"[Perms] Already granted: {perm}");
                yield break;
            }
            Debug.Log($"[Perms] Requesting: {perm}");
            bool done = false;
            var cbs = new PermissionCallbacks();
            cbs.PermissionGranted += s => { Debug.Log($"[Perms] Granted: {s}"); done = true; };
            cbs.PermissionDenied  += s => { Debug.Log($"[Perms] DENIED: {s}"); done = true; };
            cbs.PermissionDeniedAndDontAskAgain += s => { Debug.Log($"[Perms] DENIED PERMANENTLY: {s}"); done = true; };
            Permission.RequestUserPermission(perm, cbs);
            float t0 = Time.time;
            while (!done && Time.time - t0 < 15f) yield return null;
            if (!done) Debug.LogWarning($"[Perms] Timed out waiting for {perm}");
        }
#endif
    }
}
