using System.Collections.Generic;
using UnityEngine;

namespace IndoorNav
{
    /// <summary>
    /// Top-level orchestrator. Loads all saved waypoints ONCE at startup (via
    /// the central AnchorLoader), then lets the user switch freely between
    /// Author mode (add/delete waypoints) and Navigate mode (show connection
    /// arrows + A* path to a selected destination). Markers stay visible across
    /// modes because the loader is the single source of truth.
    /// </summary>
    public class NavigationManager : MonoBehaviour
    {
        public enum Mode { Author, Navigate }

        [Header("Mode")]
        public Mode startMode = Mode.Author;

        [Header("References")]
        public WaypointAuthor author;
        public AnchorLoader loader;
        public DestinationMenu menu;
        public PathRenderer pathRenderer;
        public Transform userHead;

        [Header("Snap path to floor (requires MRUK)")]
        public bool snapToFloor = true;
        public float fallbackFloorY = 0f;

        private List<Vector3> _positions = new List<Vector3>();
        private List<List<int>> _adjacency = new List<List<int>>();
        private int _selectedGoal = -1;
        private bool _loaded;

        public Mode CurrentMode { get; private set; }

        private void Start()
        {
            if (userHead == null && Camera.main != null) userHead = Camera.main.transform;

            if (author != null) author.loader = loader;

            if (loader == null)
            {
                Debug.LogError("[NavigationManager] No AnchorLoader assigned.");
                return;
            }

            // Load existing waypoints ONCE, regardless of starting mode.
            loader.OnReady = OnLoaderReady;
            loader.OnChanged = OnStoreChanged;
            loader.LoadFromDisk();
        }

        private void OnLoaderReady()
        {
            _loaded = true;
            CacheFromLoader();
            // Apply the chosen starting mode now that data is available.
            if (startMode == Mode.Author) EnterAuthorMode();
            else EnterNavigateMode();
        }

        private void CacheFromLoader()
        {
            _positions = new List<Vector3>(loader.Positions);
            _adjacency = new List<List<int>>(loader.Adjacency.Count);
            for (int i = 0; i < loader.Adjacency.Count; i++)
                _adjacency.Add(new List<int>(loader.Adjacency[i]));
        }

        /// <summary>Called whenever the loader's waypoint set changes (add/remove).</summary>
        private void OnStoreChanged()
        {
            CacheFromLoader();
            if (CurrentMode == Mode.Navigate && menu != null)
                menu.Build(loader.Graph, _positions);

            // Refresh the edge network in BOTH modes so deleting/adding a
            // waypoint immediately updates the arrows (unless a goal path is active).
            if (_selectedGoal < 0) ShowAllEdges();
        }

        // ---------- Mode switching (safe to call any time after load) ----------

        public void EnterAuthorMode()
        {
            if (author != null) author.authoringEnabled = true;
            if (menu != null) menu.gameObject.SetActive(false);
            _selectedGoal = -1;
            if (pathRenderer != null) ShowAllEdges();   // keep connections visible while editing
            CurrentMode = Mode.Author;
            Debug.Log("[NavigationManager] Author mode.");
        }

        public void EnterNavigateMode()
        {
            if (author != null) author.authoringEnabled = false;
            if (!_loaded) { Debug.Log("[NavigationManager] Navigate requested before load."); }

            if (menu != null)
            {
                menu.gameObject.SetActive(true);
                menu.OnDestinationSelected = OnGoalSelected;
                menu.Build(loader.Graph, _positions);
            }
            _selectedGoal = -1;
            ShowAllEdges();
            CurrentMode = Mode.Navigate;
            Debug.Log("[NavigationManager] Navigate mode.");
        }

        /// <summary>Draw chevron arrows along every graph edge (all connections).</summary>
        public void ShowAllEdges()
        {
            if (pathRenderer == null) return;
            if (_positions == null || _positions.Count == 0) { pathRenderer.Clear(); return; }
            var floored = new List<Vector3>(_positions.Count);
            for (int i = 0; i < _positions.Count; i++)
            {
                var p = _positions[i];
                floored.Add(snapToFloor && p != Vector3.zero
                    ? MrukFloorSnap.SnapToFloor(p, fallbackFloorY) : p);
            }
            pathRenderer.SetEdges(floored, _adjacency);
            Debug.Log($"[NavigationManager] Edge network: {_positions.Count} waypoints.");
        }

        private void OnGoalSelected(int idx)
        {
            _selectedGoal = idx;
            Replan();
        }

        private void Update()
        {
            if (CurrentMode == Mode.Navigate && _selectedGoal >= 0) Replan();
        }

        private float _lastPlanTime;
        private const float ReplanInterval = 0.5f;

        private void Replan()
        {
            if (Time.time - _lastPlanTime < ReplanInterval) return;
            _lastPlanTime = Time.time;
            if (_positions == null || _positions.Count == 0) return;
            if (_selectedGoal < 0 || _selectedGoal >= _positions.Count) return;
            if (userHead == null) return;

            int start = AStarPlanner.NearestNode(_positions, userHead.position);
            if (start < 0) return;

            var idxPath = AStarPlanner.Plan(_positions, _adjacency, start, _selectedGoal);
            if (idxPath == null || idxPath.Count < 1)
            {
                ShowAllEdges();
                return;
            }

            var pts = new List<Vector3>(idxPath.Count + 1);
            pts.Add(snapToFloor
                ? MrukFloorSnap.SnapToFloor(userHead.position, fallbackFloorY)
                : new Vector3(userHead.position.x, fallbackFloorY, userHead.position.z));
            for (int i = 0; i < idxPath.Count; i++)
            {
                var p = _positions[idxPath[i]];
                pts.Add(snapToFloor ? MrukFloorSnap.SnapToFloor(p, fallbackFloorY) : p);
            }

            if (pathRenderer != null) pathRenderer.SetPath(pts);
        }
    }
}
