using UnityEngine;
using UnityEngine.UI;

namespace IndoorNav
{
    /// <summary>
    /// Controller UI clicker that depends only on OVRInput (proven to work in
    /// this project) and analytic ray-vs-RectTransform math. No EventSystem,
    /// no GraphicRaycaster, no XRI, no colliders.
    ///
    /// Ray origin/direction come from <see cref="rayOrigin"/> (assign the OVR
    /// RightHandAnchor / RightControllerAnchor — it is already tracked by the
    /// Meta rig). A LineRenderer is created at runtime for the visible beam.
    /// On <see cref="clickButton"/> press, the hovered Button's onClick is
    /// invoked directly.
    /// </summary>
    public class OVRTriggerUIClicker : MonoBehaviour
    {
        [Header("Ray source")]
        [Tooltip("Transform whose forward axis is the ray (assign RightHandAnchor or RightControllerAnchor).")]
        public Transform rayOrigin;
        public OVRInput.Controller controller = OVRInput.Controller.RTouch;
        public OVRInput.Button clickButton = OVRInput.Button.PrimaryIndexTrigger;

        [Header("Targets")]
        [Tooltip("Leave empty to auto-find all World Space canvases at runtime.")]
        public Canvas[] canvases;
        [Tooltip("If true, ignore the array and scan the scene for all World Space canvases each frame.")]
        public bool autoFindCanvases = true;

        [Header("Ray visual")]
        public float maxDistance = 8f;
        public Color rayColor = new Color(0.3f, 0.8f, 1f, 1f);
        public Color rayHitColor = new Color(1f, 0.85f, 0.2f, 1f);
        public float rayWidth = 0.008f;

        [Header("Hover")]
        public Color hoverTint = new Color(0.5f, 0.8f, 1f, 1f);

        [Header("Diagnostics")]
        public bool periodicLogging = true;

        private LineRenderer _line;
        private Button _hovered;
        private Image _hoveredImg;
        private Color _hoveredOrig;
        private float _lastLog;
        private Canvas[] _worldCanvases = new Canvas[0];
        private float _lastCanvasScan;

        private Canvas[] ActiveCanvases
        {
            get
            {
                if (!autoFindCanvases && canvases != null && canvases.Length > 0)
                    return canvases;
                // Re-scan periodically (canvases may be enabled/disabled per mode).
                if (Time.time - _lastCanvasScan > 0.5f || _worldCanvases.Length == 0)
                {
                    _lastCanvasScan = Time.time;
                    var all = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    var list = new System.Collections.Generic.List<Canvas>();
                    foreach (var c in all)
                        if (c.renderMode == RenderMode.WorldSpace) list.Add(c);
                    _worldCanvases = list.ToArray();
                }
                return _worldCanvases;
            }
        }

        private void Start()
        {
            if (rayOrigin == null)
            {
                var rig = FindAnyObjectByType<OVRCameraRig>();
                if (rig != null) rayOrigin = rig.rightHandAnchor;
            }

            var go = new GameObject("ClickerRay");
            go.transform.SetParent(transform, false);
            _line = go.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.widthMultiplier = rayWidth;
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Hidden/Internal-Colored");
            _line.material = new Material(sh);
            _line.startColor = rayColor;
            _line.endColor = rayColor;
            _line.numCapVertices = 2;

            Debug.Log($"[Clicker] Start. rayOrigin={(rayOrigin != null ? rayOrigin.name : "NULL")}, canvases={(canvases?.Length ?? 0)}");
        }

        private void Update()
        {
            if (rayOrigin == null) return;
            Vector3 origin = rayOrigin.position;
            Vector3 dir = rayOrigin.forward;

            Button hit = FindButton(origin, dir, out Vector3 hitPoint);

            // Visual line.
            if (_line != null)
            {
                _line.SetPosition(0, origin);
                _line.SetPosition(1, hit != null ? hitPoint : origin + dir * maxDistance);
                Color c = hit != null ? rayHitColor : rayColor;
                _line.startColor = c;
                _line.endColor = c;
            }

            // Hover transitions.
            if (hit != _hovered)
            {
                if (_hoveredImg != null) _hoveredImg.color = _hoveredOrig;
                _hovered = hit;
                _hoveredImg = null;
                if (_hovered != null)
                {
                    _hoveredImg = _hovered.targetGraphic as Image;
                    if (_hoveredImg != null)
                    {
                        _hoveredOrig = _hoveredImg.color;
                        _hoveredImg.color = hoverTint;
                    }
                    Debug.Log($"[Clicker] HOVER {_hovered.gameObject.name}");
                }
            }

            // Click.
            if (_hovered != null && OVRInput.GetDown(clickButton, controller))
            {
                Debug.Log($"[Clicker] CLICK {_hovered.gameObject.name}");
                _hovered.onClick.Invoke();
            }

            if (periodicLogging && Time.time - _lastLog > 1f)
            {
                _lastLog = Time.time;
                Debug.Log($"[Clicker] alive origin={origin:F2} dir={dir:F2} hovered={(_hovered != null ? _hovered.name : "none")}");
            }
        }

        private Button FindButton(Vector3 origin, Vector3 dir, out Vector3 hitPoint)
        {
            hitPoint = origin + dir * maxDistance;
            var targets = ActiveCanvases;
            if (targets == null) return null;
            Button best = null;
            float bestT = float.PositiveInfinity;

            for (int i = 0; i < targets.Length; i++)
            {
                var canvas = targets[i];
                if (canvas == null || !canvas.isActiveAndEnabled) continue;
                var crt = canvas.transform as RectTransform;
                if (crt == null) continue;

                // Canvas plane (front face normal = -forward toward the viewer,
                // but plane orientation does not matter for intersection).
                var plane = new Plane(crt.forward, crt.position);
                var ray = new Ray(origin, dir);
                if (!plane.Raycast(ray, out float t))
                {
                    // Try the opposite-facing plane too.
                    plane = new Plane(-crt.forward, crt.position);
                    if (!plane.Raycast(ray, out t)) continue;
                }
                if (t < 0f || t > maxDistance || t >= bestT) continue;

                Vector3 worldHit = ray.GetPoint(t);

                var buttons = canvas.GetComponentsInChildren<Button>(false);
                for (int b = 0; b < buttons.Length; b++)
                {
                    var btn = buttons[b];
                    if (btn == null || !btn.IsInteractable() || !btn.isActiveAndEnabled) continue;
                    var brt = btn.transform as RectTransform;
                    if (brt == null) continue;
                    Vector3 local = brt.InverseTransformPoint(worldHit);
                    if (brt.rect.Contains(new Vector2(local.x, local.y)))
                    {
                        best = btn;
                        bestT = t;
                        hitPoint = worldHit;
                    }
                }
            }
            return best;
        }
    }
}
