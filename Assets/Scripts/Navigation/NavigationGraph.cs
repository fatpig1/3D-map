using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IndoorNav
{
    [Serializable]
    public class WaypointData
    {
        public string uuid;
        public string label;
        public string roomName;
        public List<int> neighbors = new List<int>();
    }

    [Serializable]
    public class NavigationGraph
    {
        public List<WaypointData> nodes = new List<WaypointData>();

        public int IndexOfUuid(string uuid)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].uuid == uuid) return i;
            return -1;
        }
    }

    public static class NavStore
    {
        private const string FileName = "nav-graph.json";

        private static string FullPath =>
            Path.Combine(Application.persistentDataPath, FileName);

        public static NavigationGraph Load()
        {
            try
            {
                if (!File.Exists(FullPath)) return new NavigationGraph();
                var json = File.ReadAllText(FullPath);
                var g = JsonUtility.FromJson<NavigationGraph>(json);
                return g ?? new NavigationGraph();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NavStore] Load failed: {e}");
                return new NavigationGraph();
            }
        }

        public static void Save(NavigationGraph graph)
        {
            try
            {
                var json = JsonUtility.ToJson(graph, true);
                File.WriteAllText(FullPath, json);
                Debug.Log($"[NavStore] Saved {graph.nodes.Count} nodes -> {FullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NavStore] Save failed: {e}");
            }
        }

        public static void Clear()
        {
            try { if (File.Exists(FullPath)) File.Delete(FullPath); }
            catch (Exception e) { Debug.LogError($"[NavStore] Clear failed: {e}"); }
        }
    }
}
