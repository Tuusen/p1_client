using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class WinManager : MonoBehaviour
    {
        private static WinManager instance;
        public static WinManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("WinManager");
                    instance = go.AddComponent<WinManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private readonly Dictionary<Type, BaseWin> winCache = new Dictionary<Type, BaseWin>();
        private Transform winRoot;
        private int baseSortOrder = 100;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureWinRoot();
        }

        private void EnsureWinRoot()
        {
            if (winRoot != null) return;

            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("WinCanvas");
                canvasObj.transform.SetParent(transform);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = baseSortOrder;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            winRoot = canvas.transform;
        }

        public T OpenWin<T>() where T : BaseWin
        {
            return OpenWin<T>(ViewPathManager.GetPath(typeof(T).Name));
        }

        public T OpenWin<T>(string prefabPath) where T : BaseWin
        {
            Type type = typeof(T);

            if (winCache.TryGetValue(type, out BaseWin cached))
            {
                cached.Show();
                UpdateSortOrder(cached);
                return (T)cached;
            }

            GameObject prefab = GameHelper.LoadPrefab(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[WinManager] 找不到窗口预制体: {prefabPath}");
                return null;
            }

            EnsureWinRoot();
            string prefabName = type.Name;
            GameObject winObj = Instantiate(prefab, winRoot);
            winObj.name = prefabName;

            T win = winObj.GetComponent<T>();
            if (win == null)
            {
                Debug.LogError($"[WinManager] 预制体缺少组件: {prefabName} 上找不到 {type.Name}");
                Destroy(winObj);
                return null;
            }

            winCache[type] = win;
            win.Init();
            win.Show();
            UpdateSortOrder(win);
            return win;
        }

        public void CloseWin<T>() where T : BaseWin
        {
            Type type = typeof(T);
            if (winCache.TryGetValue(type, out BaseWin win))
            {
                win.OnClose();
            }
        }

        public void DestroyWin<T>() where T : BaseWin
        {
            Type type = typeof(T);
            if (winCache.TryGetValue(type, out BaseWin win))
            {
                winCache.Remove(type);
                if (win != null && win.gameObject != null)
                    Destroy(win.gameObject);
            }
        }

        public T GetWin<T>() where T : BaseWin
        {
            Type type = typeof(T);
            if (winCache.TryGetValue(type, out BaseWin win))
                return (T)win;
            return null;
        }

        public bool IsWinOpen<T>() where T : BaseWin
        {
            Type type = typeof(T);
            return winCache.TryGetValue(type, out BaseWin win) && win != null && win.IsVisible;
        }

        public void CloseAllWins()
        {
            foreach (var kvp in winCache)
            {
                if (kvp.Value != null && kvp.Value.IsVisible)
                    kvp.Value.OnClose();
            }
        }

        public void DestroyAllWins()
        {
            foreach (var kvp in winCache)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                    Destroy(kvp.Value.gameObject);
            }
            winCache.Clear();
        }

        private void UpdateSortOrder(BaseWin win)
        {
            Canvas winCanvas = win.GetComponent<Canvas>();
            if (winCanvas == null)
            {
                winCanvas = win.gameObject.AddComponent<Canvas>();
                winCanvas.overrideSorting = true;
                if (win.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    win.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            winCanvas.sortingOrder = baseSortOrder + win.SortOrder;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
