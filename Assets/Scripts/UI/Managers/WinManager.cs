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
        private readonly Dictionary<Type, int> winOpenOrder = new Dictionary<Type, int>();
        private Transform winRoot;
        private int baseSortOrder = 1000;
        private int openSequenceCounter = 0;

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
                UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            winRoot = canvas.transform;
        }

        public T OpenWin<T>(string winName = null, object param = null) where T : BaseWin
        {
            Type type = typeof(T);
            string prefabPath = !string.IsNullOrEmpty(winName) 
                ? ViewPathManager.GetPath(winName) 
                : ViewPathManager.GetPath(type.Name);

            Debug.LogWarning($"[WinManager] 打开窗口: {prefabPath}");

            // 如果窗口已存在，则重置参数并提升到最上层
            if (winCache.TryGetValue(type, out BaseWin cached))
            {
                // 传递参数给ResetOpen方法
                cached.ResetOpen(param);
                
                // 更新打开顺序，确保在同优先级中处于最上层
                winOpenOrder[type] = ++openSequenceCounter;
                
                // 显示窗口并更新层级
                cached.Show();
                UpdateSortOrder(cached);
                return (T)cached;
            }

            // 创建新窗口
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
            winOpenOrder[type] = ++openSequenceCounter;
            win.Init(param);
            win.Show();
            UpdateSortOrder(win);
            return win;
        }

        /// <summary>
        /// 根据窗口名称关闭窗口
        /// </summary>
        /// <param name="winName">窗口名称（GameObject.name）</param>
        public void CloseWin(string winName)
        {
            if (string.IsNullOrEmpty(winName)) return;

            foreach (var kvp in winCache)
            {
                if (kvp.Value != null && kvp.Value.gameObject.name == winName)
                {
                    kvp.Value.gameObject.SetActive(false);
                    break;
                }
            }
        }

        public void DestroyWin<T>() where T : BaseWin
        {
            Type type = typeof(T);
            if (winCache.TryGetValue(type, out BaseWin win))
            {
                winCache.Remove(type);
                winOpenOrder.Remove(type);
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
            winOpenOrder.Clear();
            openSequenceCounter = 0;
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
            // 计算层级：baseSortOrder(1000) + priority权重(10/100 * 100000) + sortOrder + 打开顺序
            // 确保所有窗口层级(最小1000010)始终高于场景UI(通常为0-1000)
            int priorityWeight = (int)win.Priority * 100000;
            int openOrder = winOpenOrder.TryGetValue(win.GetType(), out int order) ? order : 0;
            winCanvas.sortingOrder = baseSortOrder + priorityWeight + win.SortOrder + openOrder;

            // Ensure full-screen RectTransform to block click-through
            RectTransform rt = win.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Ensure a background Image exists for raycast blocking
            if (win.GetComponent<UnityEngine.UI.Image>() == null)
            {
                UnityEngine.UI.Image blocker = win.gameObject.AddComponent<UnityEngine.UI.Image>();
                blocker.color = new Color(0f, 0f, 0f, 0f);
                blocker.raycastTarget = true;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
