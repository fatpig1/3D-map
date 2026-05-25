using UnityEngine;

namespace IndoorNav
{
    /// <summary>
    /// Minimal helper that configures a LineRenderer on the same GameObject as
    /// a forward-pointing local-space ray. Designed to be parented under a
    /// controller anchor (RightHandAnchor / RightControllerAnchor) so it moves
    /// and rotates with the controller without any per-frame math.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RayLine : MonoBehaviour
    {
        public float length = 5f;
        public Color color = new Color(0.4f, 0.7f, 1f, 0.9f);
        public float width = 0.004f;

        private void Awake()
        {
            var lr = GetComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, Vector3.forward * length);
            lr.widthMultiplier = width;
            // URP-compatible unlit material so it renders on Quest.
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = color;
            lr.material = mat;
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, color.a * 0.3f);
        }
    }
}
