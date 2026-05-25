using System;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.BuildingBlocks;

namespace IndoorNav
{
    /// <summary>
    /// Central waypoint store backed by Meta's SpatialAnchorCoreBuildingBlock
    /// for robust create / load / erase / relocalize. Holds the runtime graph,
    /// world positions, marker GameObjects, and adjacency. WaypointAuthor,
    /// WaypointEditor and NavigationManager all read/write through here.
    /// </summary>
    public class AnchorLoader : MonoBehaviour
    {
        [Header("Meta Spatial Anchor Core")]
        [Tooltip("The SpatialAnchorCoreBuildingBlock in the scene.")]
        public SpatialAnchorCoreBuildingBlock anchorCore;
        [Tooltip("Prefab instantiated at each anchor (the visible waypoint marker).")]
        public GameObject waypointPrefab;

        [Header("Graph")]
        public float autoConnectRadius = 4f;

        [Header("Events")]
        public Action OnReady;
        public Action OnChanged;

        private NavigationGraph _graph = new NavigationGraph();
        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<GameObject> _markers = new List<GameObject>();
        private readonly List<List<int>> _adjacency = new List<List<int>>();

        public IReadOnlyList<Vector3> Positions => _positions;
        public IReadOnlyList<GameObject> Markers => _markers;
        public IReadOnlyList<List<int>> Adjacency => _adjacency;
        public NavigationGraph Graph => _graph;
        public bool IsReady { get; private set; }

        // Metadata waiting to attach to the next created anchor (FIFO).
        private struct Pending { public string label; public string roomName; }
        private readonly Queue<Pending> _pending = new Queue<Pending>();

        private void Awake()
        {
            if (anchorCore != null)
            {
                anchorCore.OnAnchorCreateCompleted.AddListener(OnAnchorCreated);
                anchorCore.OnAnchorsLoadCompleted.AddListener(OnAnchorsLoaded);
            }
            else Debug.LogError("[AnchorLoader] No SpatialAnchorCoreBuildingBlock assigned!");
        }

        // ---------- Load ----------

        public void LoadFromDisk()
        {
            _graph = NavStore.Load();
            _positions.Clear(); _markers.Clear(); _adjacency.Clear();
            for (int i = 0; i < _graph.nodes.Count; i++)
            {
                _positions.Add(Vector3.zero);
                _markers.Add(null);
                _adjacency.Add(new List<int>(_graph.nodes[i].neighbors));
            }

            if (_graph.nodes.Count == 0 || anchorCore == null)
            {
                IsReady = true;
                OnReady?.Invoke();
                OnChanged?.Invoke();
                Debug.Log($"[AnchorLoader] Nothing to load ({_graph.nodes.Count} nodes).");
                return;
            }

            var uuids = new List<Guid>();
            foreach (var n in _graph.nodes)
                if (Guid.TryParse(n.uuid, out var g)) uuids.Add(g);

            Debug.Log($"[AnchorLoader] LoadAndInstantiateAnchors for {uuids.Count} uuids.");
            anchorCore.LoadAndInstantiateAnchors(waypointPrefab, uuids);
        }

        private void OnAnchorsLoaded(List<OVRSpatialAnchor> anchors)
        {
            int matched = 0;
            if (anchors != null)
            {
                foreach (var a in anchors)
                {
                    if (a == null) continue;
                    int idx = _graph.IndexOfUuid(a.Uuid.ToString());
                    if (idx >= 0)
                    {
                        _positions[idx] = a.transform.position;
                        _markers[idx] = a.gameObject;
                        matched++;
                    }
                }
            }
            IsReady = true;
            OnReady?.Invoke();
            OnChanged?.Invoke();
            Debug.Log($"[AnchorLoader] Loaded/localized {matched} of {_graph.nodes.Count} waypoints.");
        }

        // ---------- Add ----------

        public void AddWaypoint(Vector3 pos, Quaternion rot, string roomName)
        {
            if (anchorCore == null) { Debug.LogError("[AnchorLoader] No SpatialAnchorCore."); return; }
            _pending.Enqueue(new Pending { label = $"Waypoint {_graph.nodes.Count + 1}", roomName = roomName });
            anchorCore.InstantiateSpatialAnchor(waypointPrefab, pos, rot);
            Debug.Log("[AnchorLoader] InstantiateSpatialAnchor requested.");
        }

        private void OnAnchorCreated(OVRSpatialAnchor anchor, OVRSpatialAnchor.OperationResult result)
        {
            if (result != OVRSpatialAnchor.OperationResult.Success || anchor == null)
            {
                Debug.LogError($"[AnchorLoader] Anchor create failed: {result}");
                return;
            }
            var meta = _pending.Count > 0 ? _pending.Dequeue()
                : new Pending { label = $"Waypoint {_graph.nodes.Count + 1}", roomName = "?" };

            var data = new WaypointData { uuid = anchor.Uuid.ToString(), label = meta.label, roomName = meta.roomName };
            Vector3 worldPos = anchor.transform.position;
            int newIndex = _graph.nodes.Count;

            for (int i = 0; i < _positions.Count; i++)
            {
                if (_positions[i] == Vector3.zero) continue;
                if (Vector3.Distance(_positions[i], worldPos) <= autoConnectRadius)
                {
                    data.neighbors.Add(i);
                    if (i < _graph.nodes.Count) _graph.nodes[i].neighbors.Add(newIndex);
                    if (i < _adjacency.Count) _adjacency[i].Add(newIndex);
                }
            }

            _graph.nodes.Add(data);
            _positions.Add(worldPos);
            _markers.Add(anchor.gameObject);
            _adjacency.Add(new List<int>(data.neighbors));

            NavStore.Save(_graph);
            OnChanged?.Invoke();
            Debug.Log($"[AnchorLoader] Created waypoint idx={newIndex} uuid={data.uuid}, total={_graph.nodes.Count}");
        }

        // ---------- Remove ----------

        public bool RemoveWaypoint(int index)
        {
            if (index < 0 || index >= _graph.nodes.Count) return false;
            string uuidStr = _graph.nodes[index].uuid;

            var marker = index < _markers.Count ? _markers[index] : null;

            _graph.nodes.RemoveAt(index);
            if (index < _positions.Count) _positions.RemoveAt(index);
            if (index < _markers.Count) _markers.RemoveAt(index);
            if (index < _adjacency.Count) _adjacency.RemoveAt(index);

            for (int i = 0; i < _graph.nodes.Count; i++) FixNeighbors(_graph.nodes[i].neighbors, index);
            for (int i = 0; i < _adjacency.Count; i++) FixNeighbors(_adjacency[i], index);

            // Erase from device storage; also destroy the marker GameObject.
            if (anchorCore != null && Guid.TryParse(uuidStr, out var g))
                anchorCore.EraseAnchorByUuid(g);
            if (marker != null) Destroy(marker);

            NavStore.Save(_graph);
            OnChanged?.Invoke();
            Debug.Log($"[AnchorLoader] Removed waypoint idx={index}, remaining={_graph.nodes.Count}");
            return true;
        }

        /// <summary>Erase every anchor (device + disk) and clear the runtime store.</summary>
        public void ClearAll()
        {
            for (int i = 0; i < _markers.Count; i++)
                if (_markers[i] != null) Destroy(_markers[i]);

            if (anchorCore != null) anchorCore.EraseAllAnchors();

            _markers.Clear();
            _positions.Clear();
            _adjacency.Clear();
            _graph = new NavigationGraph();
            NavStore.Save(_graph);
            OnChanged?.Invoke();
            Debug.Log("[AnchorLoader] Cleared all waypoints.");
        }

        private static void FixNeighbors(List<int> ns, int removed)
        {
            for (int k = ns.Count - 1; k >= 0; k--)
            {
                if (ns[k] == removed) ns.RemoveAt(k);
                else if (ns[k] > removed) ns[k] = ns[k] - 1;
            }
        }

        public void PersistToDisk()
        {
            if (_graph != null) NavStore.Save(_graph);
        }

        /// <summary>Rename a waypoint's label (e.g. "Room 302") and persist.</summary>
        public void SetLabel(int index, string label)
        {
            if (_graph == null || index < 0 || index >= _graph.nodes.Count) return;
            _graph.nodes[index].label = label;
            NavStore.Save(_graph);
            OnChanged?.Invoke();
            Debug.Log($"[AnchorLoader] Renamed idx={index} -> '{label}'");
        }

        /// <summary>True if the waypoint at index has been localized (has a real position).</summary>
        public bool IsLocalized(int index)
        {
            return index >= 0 && index < _positions.Count && _positions[index] != Vector3.zero;
        }
    }
}
