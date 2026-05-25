using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IndoorNav
{
    /// <summary>
    /// Simple destination selector. Builds a vertical button list from the
    /// labels in a NavigationGraph at runtime and raises <see cref="OnDestinationSelected"/>
    /// when the user clicks one. Attach this to a world-space Canvas root.
    /// </summary>
    public class DestinationMenu : MonoBehaviour
    {
        [Tooltip("Parent under which destination buttons are spawned (a VerticalLayoutGroup is recommended).")]
        public RectTransform contentRoot;

        [Tooltip("Optional button prefab. If null, a default text button is built procedurally.")]
        public Button buttonPrefab;

        public Action<int> OnDestinationSelected;

        private readonly List<Button> _spawned = new List<Button>();

        public void Build(NavigationGraph graph, IReadOnlyList<Vector3> positions)
        {
            if (contentRoot == null) contentRoot = (RectTransform)transform;
            ClearChildren();
            if (graph == null) return;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (i >= positions.Count) break;
                if (positions[i] == Vector3.zero) continue; // Not localized.
                int index = i;
                var node = graph.nodes[i];
                string fallback = node.label ?? $"WP {i}";
                string display = string.IsNullOrEmpty(node.roomName) || node.roomName == "?"
                    ? fallback
                    : $"[{node.roomName}] {fallback}";
                var btn = SpawnButton(display);
                btn.onClick.AddListener(() => OnDestinationSelected?.Invoke(index));
                _spawned.Add(btn);
            }
        }

        private void ClearChildren()
        {
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            _spawned.Clear();
        }

        private Button SpawnButton(string label)
        {
            if (buttonPrefab != null)
            {
                var b = Instantiate(buttonPrefab, contentRoot);
                var t = b.GetComponentInChildren<Text>();
                if (t != null) t.text = label;
                return b;
            }

            var go = new GameObject($"DestBtn_{label}", typeof(RectTransform),
                typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(contentRoot, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(280f, 60f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 60f;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 5);
            trt.offsetMax = new Vector2(-10, -5);
            var txt = textGo.GetComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return go.GetComponent<Button>();
        }
    }
}
