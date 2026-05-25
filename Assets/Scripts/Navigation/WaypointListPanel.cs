using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IndoorNav
{
    /// <summary>
    /// Anchor List panel driven by editor-authored UI. Assign the title text,
    /// the content container (a VerticalLayoutGroup), and a row prefab (with a
    /// WaypointRow component) in the Inspector. At runtime this instantiates one
    /// row per waypoint, fills the label, and wires the Rename / Delete buttons.
    /// All visuals (colors, fonts, sizes, layout) are editable in the editor.
    /// </summary>
    public class WaypointListPanel : MonoBehaviour
    {
        [Header("References")]
        public AnchorLoader loader;
        public Transform head;

        [Header("Editor-authored UI")]
        [Tooltip("Optional title Text; receives 'Anchor List (N)'.")]
        public Text titleText;
        [Tooltip("Container the rows are instantiated under (VerticalLayoutGroup recommended).")]
        public RectTransform content;
        [Tooltip("Row prefab with a WaypointRow component.")]
        public WaypointRow rowPrefab;

        [Header("Placement (follows head)")]
        public bool followHead = true;
        public float distance = 0.7f;
        public float horizontalOffset = -0.45f;
        public float verticalOffset = 0.1f;

        [Header("Preset room names (Rename cycles through these)")]
        public string[] presetNames =
        {
            "Room 301", "Room 302", "Bathroom", "Kitchen",
            "Elevator", "Nurse Station", "Dining Hall", "Exit"
        };

        private readonly List<GameObject> _rows = new List<GameObject>();

        private void Start()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (loader != null) loader.OnChanged += Rebuild;
            Rebuild();
        }

        private void OnDestroy()
        {
            if (loader != null) loader.OnChanged -= Rebuild;
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

        public void Rebuild()
        {
            if (content == null || rowPrefab == null || loader == null) return;

            for (int i = 0; i < _rows.Count; i++) if (_rows[i] != null) Destroy(_rows[i]);
            _rows.Clear();

            var graph = loader.Graph;
            if (graph == null) return;
            if (titleText != null) titleText.text = $"Anchor List ({graph.nodes.Count})";

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                int index = i;
                var node = graph.nodes[i];
                bool localized = loader.IsLocalized(i);
                string mark = localized ? "<color=#6cff6c>OK</color>" : "<color=#ff6c6c>..</color>";
                string label = string.IsNullOrEmpty(node.label) ? $"WP{i}" : node.label;

                var rowGo = Instantiate(rowPrefab.gameObject, content);
                rowGo.SetActive(true);
                var row = rowGo.GetComponent<WaypointRow>();
                if (row != null)
                {
                    if (row.labelText != null)
                    {
                        row.labelText.supportRichText = true;
                        row.labelText.text = $"{mark} {label}";
                    }
                    if (row.renameButton != null)
                    {
                        row.renameButton.onClick.RemoveAllListeners();
                        row.renameButton.onClick.AddListener(() =>
                            loader.SetLabel(index, NextPreset(loader.Graph.nodes[index].label)));
                    }
                    if (row.deleteButton != null)
                    {
                        row.deleteButton.onClick.RemoveAllListeners();
                        row.deleteButton.onClick.AddListener(() => loader.RemoveWaypoint(index));
                    }
                }
                _rows.Add(rowGo);
            }
        }

        private string NextPreset(string current)
        {
            if (presetNames == null || presetNames.Length == 0) return current;
            int idx = System.Array.IndexOf(presetNames, current);
            return presetNames[(idx + 1) % presetNames.Length];
        }
    }
}
