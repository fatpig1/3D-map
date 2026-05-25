using System.Collections.Generic;
using UnityEngine;

namespace IndoorNav
{
    /// <summary>
    /// Renders the planned path. Draws a LineRenderer between waypoints and
    /// instantiates chevron quads at fixed intervals pointing along the path.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PathRenderer : MonoBehaviour
    {
        [Header("Chevron arrows")]
        [Tooltip("Optional chevron prefab. If null, a procedural quad is used.")]
        public GameObject chevronPrefab;
        public float chevronSpacing = 0.5f;
        public float chevronHeightOffset = 0.02f;
        public Color chevronColor = new Color(0.7f, 0.3f, 1f, 1f);

        [Header("Line")]
        public Color lineColor = new Color(0.7f, 0.3f, 1f, 0.8f);
        public float lineWidth = 0.03f;

        private LineRenderer _line;
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private Material _chevronMat;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.widthMultiplier = lineWidth;
            _line.positionCount = 0;
            _line.useWorldSpace = true;
            var lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMat.color = lineColor;
            _line.material = lineMat;

            _chevronMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _chevronMat.color = chevronColor;
        }

        public void Clear()
        {
            _line.positionCount = 0;
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Destroy(_spawned[i]);
            _spawned.Clear();
        }

        public void SetPath(IList<Vector3> waypoints)
        {
            Clear();
            if (waypoints == null || waypoints.Count < 2) return;

            _line.positionCount = waypoints.Count;
            for (int i = 0; i < waypoints.Count; i++)
                _line.SetPosition(i, waypoints[i] + Vector3.up * chevronHeightOffset);

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector3 a = waypoints[i];
                Vector3 b = waypoints[i + 1];
                Vector3 dir = b - a;
                float segLen = dir.magnitude;
                if (segLen < 1e-4f) continue;
                Vector3 dirN = dir / segLen;

                int count = Mathf.Max(1, Mathf.FloorToInt(segLen / chevronSpacing));
                for (int k = 0; k < count; k++)
                {
                    float t = (k + 0.5f) / count;
                    Vector3 pos = Vector3.Lerp(a, b, t) + Vector3.up * chevronHeightOffset;
                    Quaternion rot = Quaternion.LookRotation(dirN, Vector3.up);
                    SpawnChevron(pos, rot);
                }
            }
        }

        /// <summary>
        /// Draw chevron arrows along every edge of the waypoint graph (the
        /// full "passway" network). Used in Navigate mode to show all
        /// connections between waypoints. Does NOT use the LineRenderer (that
        /// is reserved for the A* path); only spawns chevrons.
        /// </summary>
        public void SetEdges(IList<Vector3> positions, IList<List<int>> adjacency)
        {
            Clear();
            if (positions == null || adjacency == null) return;

            for (int i = 0; i < adjacency.Count && i < positions.Count; i++)
            {
                var a = positions[i];
                if (a == Vector3.zero) continue;
                var neighbors = adjacency[i];
                for (int n = 0; n < neighbors.Count; n++)
                {
                    int j = neighbors[n];
                    if (j <= i) continue; // draw each undirected edge once
                    if (j >= positions.Count) continue;
                    var b = positions[j];
                    if (b == Vector3.zero) continue;

                    Vector3 dir = b - a;
                    float segLen = dir.magnitude;
                    if (segLen < 1e-4f) continue;
                    Vector3 dirN = dir / segLen;
                    int count = Mathf.Max(1, Mathf.FloorToInt(segLen / chevronSpacing));
                    for (int k = 0; k < count; k++)
                    {
                        float t = (k + 0.5f) / count;
                        Vector3 pos = Vector3.Lerp(a, b, t) + Vector3.up * chevronHeightOffset;
                        Quaternion rot = Quaternion.LookRotation(dirN, Vector3.up);
                        SpawnChevron(pos, rot);
                    }
                }
            }
        }

        private void SpawnChevron(Vector3 pos, Quaternion rot)
        {
            GameObject go;
            if (chevronPrefab != null)
            {
                go = Instantiate(chevronPrefab, pos, rot, transform);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.transform.SetParent(transform, false);
                // Lay it on the floor pointing forward along path.
                go.transform.position = pos;
                go.transform.rotation = rot * Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale = new Vector3(0.18f, 0.25f, 1f);
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                var rend = go.GetComponent<Renderer>();
                if (rend != null) rend.material = _chevronMat;
            }
            _spawned.Add(go);
        }
    }
}
