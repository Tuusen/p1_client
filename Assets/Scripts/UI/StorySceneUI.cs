using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class StorySceneUI : MonoBehaviour
    {
        // Header
        private Text collectionNameText;
        private Text goldText;

        // Node display
        private Text nodeNameText;
        private Text nodeTypeText;
        private Image nodeIconImage;
        private RectTransform nodePanelRt;

        // Branch lines
        private readonly List<GameObject> branchLines = new List<GameObject>();

        // Bottom
        private Text effectsCountText;

        // Animation
        private CanvasGroup interactionBlocker;
        private CanvasGroup nodePanelGroup;
        private RectTransform centerRt;
        private GameObject nodePanel;
        private bool isAnimating;

        private const float FADE_OUT_DURATION = 0.3f;
        private const float SLIDE_DURATION = 0.5f;
        private const float FADE_IN_DURATION = 0.3f;

        private Font cachedFont;

        private void Start()
        {
            if (StoryManager.Instance == null || !StoryManager.Instance.IsInAdventure)
            {
                GameHelper.LoadScene("MainMenu");
                return;
            }

            cachedFont = GameHelper.LoadFont();
            BuildUI();

            StoryManager.Instance.OnNodeChanged += HandleNodeChanged;
            StoryManager.Instance.OnGoldChanged += HandleGoldChanged;
            StoryManager.Instance.OnEffectAcquired += HandleEffectAcquired;

            if (StoryManager.Instance.ShouldPlayTransition &&
                StoryManager.Instance.Runtime.previousNodeId > 0 &&
                StoryManager.Instance.Runtime.previousNodeId != StoryManager.Instance.Runtime.currentNodeId)
            {
                StoryManager.Instance.ClearTransitionFlag();
                StartCoroutine(PlayNodeTransition());
            }
            else
            {
                StoryManager.Instance.ClearTransitionFlag();
                RefreshNodeDisplay();
            }
        }

        private void OnDestroy()
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.OnNodeChanged -= HandleNodeChanged;
                StoryManager.Instance.OnGoldChanged -= HandleGoldChanged;
                StoryManager.Instance.OnEffectAcquired -= HandleEffectAcquired;
            }
        }

        // ===== Event Handlers =====

        private void HandleNodeChanged(int oldNodeId, int newNodeId)
        {
            RefreshNodeDisplay();
        }

        private void HandleGoldChanged(int gold)
        {
            if (goldText != null)
                goldText.text = $"Gold: {gold}";
        }

        private void HandleEffectAcquired(int effectId)
        {
            UpdateEffectsCount();
        }

        // ===== Core Display =====

        private void RefreshNodeDisplay()
        {
            if (StoryManager.Instance == null || !StoryManager.Instance.IsInAdventure)
                return;

            StoryNodeConfig node = StoryManager.Instance.CurrentNode;
            StoryCollectionConfig collection = StoryManager.Instance.CurrentCollection;
            if (node == null) return;

            // Header
            if (collectionNameText != null && collection != null)
                collectionNameText.text = collection.name ?? "";
            if (goldText != null)
                goldText.text = $"Gold: {StoryManager.Instance.GetGold()}";

            // Node info + branches
            DisplayNodeData(node);

            // Effects count
            UpdateEffectsCount();
        }

        private void DisplayNodeData(StoryNodeConfig node)
        {
            if (node == null) return;

            if (nodeNameText != null)
                nodeNameText.text = node.name ?? "";
            if (nodeTypeText != null)
                nodeTypeText.text = GetNodeTypeLabel(node.type);
            if (nodeIconImage != null)
            {
                if (!string.IsNullOrEmpty(node.icon))
                {
                    Sprite sprite = GameHelper.LoadSprite(node.icon);
                    if (sprite != null)
                    {
                        nodeIconImage.sprite = sprite;
                        nodeIconImage.color = Color.white;
                    }
                    else
                    {
                        nodeIconImage.color = GetNodeTypeColor(node.type);
                    }
                }
                else
                {
                    nodeIconImage.color = GetNodeTypeColor(node.type);
                }
            }

            RebuildBranchLines(node);
        }

        private void UpdateEffectsCount()
        {
            if (effectsCountText == null || StoryManager.Instance == null || StoryManager.Instance.Runtime == null)
                return;

            int count = StoryManager.Instance.Runtime.ownedEffectIds.Count;
            effectsCountText.text = count > 0 ? $"Collected: {count} items" : "";
        }

        private int GetBranchCount(StoryNodeConfig node)
        {
            if (node.branchLineCount > 0)
                return node.branchLineCount;
            if (node.nextNodes != null && node.nextNodes.Length > 0)
                return node.nextNodes.Length;
            if (node.defaultNextNodeId > 0)
                return 1;
            return 0;
        }

        // ===== Branch Lines =====

        private void RebuildBranchLines(StoryNodeConfig node)
        {
            for (int i = 0; i < branchLines.Count; i++)
            {
                if (branchLines[i] != null)
                    Destroy(branchLines[i]);
            }
            branchLines.Clear();

            int count = GetBranchCount(node);
            if (count <= 0 || nodePanelRt == null) return;

            float totalSpread = Mathf.Min(count * 30f, 120f);

            for (int i = 0; i < count; i++)
            {
                float yOffset;
                if (count == 1)
                    yOffset = 0f;
                else
                    yOffset = Mathf.Lerp(-totalSpread / 2f, totalSpread / 2f, (float)i / (count - 1));

                GameObject lineObj = new GameObject($"BranchLine_{i}");
                lineObj.transform.SetParent(nodePanelRt, false);
                RectTransform lineRt = lineObj.AddComponent<RectTransform>();

                // Anchor at panel's right edge center, extend rightward
                lineRt.anchorMin = new Vector2(1f, 0.5f);
                lineRt.anchorMax = new Vector2(1f, 0.5f);
                lineRt.pivot = new Vector2(0f, 0.5f);
                lineRt.anchoredPosition = new Vector2(5f, yOffset);
                lineRt.sizeDelta = new Vector2(800f, 3f);

                Image lineImg = lineObj.AddComponent<Image>();
                lineImg.color = GetBranchLineColor(i, count);

                branchLines.Add(lineObj);
            }
        }

        private Color GetBranchLineColor(int index, int total)
        {
            if (total <= 1)
                return new Color(0.6f, 0.8f, 1f, 0.7f);

            float hue = Mathf.Lerp(0.55f, 0.15f, (float)index / (total - 1));
            return Color.HSVToRGB(hue, 0.5f, 0.9f) * new Color(1f, 1f, 1f, 0.7f);
        }

        // ===== Button Handlers =====

        private void OnNodePanelClicked()
        {
            if (isAnimating) return;
            if (StoryManager.Instance == null || !StoryManager.Instance.IsInAdventure) return;

            StoryNodeConfig node = StoryManager.Instance.CurrentNode;
            if (node == null) return;

            if (node.type == StoryNodeType.Battle)
            {
                LevelSelectWin win = GameHelper.OpenWin<LevelSelectWin>();
                if (win != null)
                    win.ShowForStoryNode(node.levelId);
            }
            else
            {
                StoryManager.Instance.ExecuteCurrentNode();
            }
        }

        private void OnBackClicked()
        {
            if (StoryManager.Instance != null)
                StoryManager.Instance.BackToMainMenu();
        }

        // ===== Transition Animation =====

        private IEnumerator PlayNodeTransition()
        {
            isAnimating = true;
            interactionBlocker.interactable = false;
            interactionBlocker.blocksRaycasts = true;

            StoryRuntime runtime = StoryManager.Instance.Runtime;
            StoryNodeConfig oldNode = Cfg.StoryNode.Get(runtime.previousNodeId);
            StoryNodeConfig newNode = StoryManager.Instance.CurrentNode;

            if (oldNode == null || newNode == null)
            {
                RefreshNodeDisplay();
                isAnimating = false;
                interactionBlocker.interactable = true;
                yield break;
            }

            // Show old node state first
            DisplayNodeData(oldNode);

            // Update header with current data
            StoryCollectionConfig collection = StoryManager.Instance.CurrentCollection;
            if (collectionNameText != null && collection != null)
                collectionNameText.text = collection.name ?? "";
            if (goldText != null)
                goldText.text = $"Gold: {StoryManager.Instance.GetGold()}";
            UpdateEffectsCount();

            yield return new WaitForSeconds(0.2f);

            // Find which branch was taken
            int takenBranchIndex = FindTakenBranch(oldNode, runtime.currentNodeId);

            // Phase 1: Fade out node panel + non-taken branches
            float elapsed = 0f;
            while (elapsed < FADE_OUT_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FADE_OUT_DURATION);
                nodePanelGroup.alpha = 1f - t;

                for (int i = 0; i < branchLines.Count; i++)
                {
                    if (i == takenBranchIndex) continue;
                    Image lineImg = branchLines[i] != null ? branchLines[i].GetComponent<Image>() : null;
                    if (lineImg != null)
                    {
                        Color c = lineImg.color;
                        c.a = Mathf.Lerp(0.7f, 0f, t);
                        lineImg.color = c;
                    }
                }
                yield return null;
            }
            nodePanelGroup.alpha = 0f;

            // Phase 2: Slide along taken branch
            elapsed = 0f;
            Image takenLineImg = (takenBranchIndex >= 0 && takenBranchIndex < branchLines.Count && branchLines[takenBranchIndex] != null)
                ? branchLines[takenBranchIndex].GetComponent<Image>() : null;

            while (elapsed < SLIDE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / SLIDE_DURATION);
                float easedT = EaseInOutQuad(t);

                centerRt.anchoredPosition = new Vector2(Mathf.Lerp(0f, -400f, easedT), 0f);

                if (takenLineImg != null)
                {
                    Color c = takenLineImg.color;
                    c.a = Mathf.Lerp(0.7f, 0f, t);
                    takenLineImg.color = c;
                }
                yield return null;
            }

            // Reset position, switch to new node
            centerRt.anchoredPosition = Vector2.zero;
            DisplayNodeData(newNode);

            // Prepare fade-in: set everything invisible
            nodePanelGroup.alpha = 0f;
            for (int i = 0; i < branchLines.Count; i++)
            {
                Image lineImg = branchLines[i] != null ? branchLines[i].GetComponent<Image>() : null;
                if (lineImg != null)
                {
                    Color c = lineImg.color;
                    c.a = 0f;
                    lineImg.color = c;
                }
            }

            // Phase 3: Fade in new node + branches
            elapsed = 0f;
            // Cache target colors before animating
            Color[] targetColors = new Color[branchLines.Count];
            for (int i = 0; i < branchLines.Count; i++)
            {
                Image lineImg = branchLines[i] != null ? branchLines[i].GetComponent<Image>() : null;
                if (lineImg != null)
                    targetColors[i] = GetBranchLineColor(i, branchLines.Count);
            }

            while (elapsed < FADE_IN_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FADE_IN_DURATION);
                nodePanelGroup.alpha = t;

                for (int i = 0; i < branchLines.Count; i++)
                {
                    Image lineImg = branchLines[i] != null ? branchLines[i].GetComponent<Image>() : null;
                    if (lineImg != null)
                    {
                        Color c = targetColors[i];
                        c.a = Mathf.Lerp(0f, targetColors[i].a, t);
                        lineImg.color = c;
                    }
                }
                yield return null;
            }
            nodePanelGroup.alpha = 1f;

            // Restore interaction
            isAnimating = false;
            interactionBlocker.interactable = true;
        }

        private int FindTakenBranch(StoryNodeConfig prevNode, int currentNodeId)
        {
            if (prevNode.nextNodes != null)
            {
                for (int i = 0; i < prevNode.nextNodes.Length; i++)
                {
                    if (prevNode.nextNodes[i].nodeId == currentNodeId)
                        return i;
                }
            }
            return 0;
        }

        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        // ===== Utilities =====

        private string GetNodeTypeLabel(int type)
        {
            switch (type)
            {
                case StoryNodeType.Battle: return "Battle";
                case StoryNodeType.Event: return "Event";
                case StoryNodeType.Shop: return "Shop";
                case StoryNodeType.Ending: return "Ending";
                default: return "";
            }
        }

        private Color GetNodeTypeColor(int type)
        {
            switch (type)
            {
                case StoryNodeType.Battle: return new Color(0.8f, 0.3f, 0.3f);
                case StoryNodeType.Event: return new Color(0.3f, 0.7f, 0.9f);
                case StoryNodeType.Shop: return new Color(0.9f, 0.8f, 0.3f);
                case StoryNodeType.Ending: return new Color(0.7f, 0.5f, 0.9f);
                default: return Color.gray;
            }
        }

        // ===== Dynamic UI Build =====

        private void BuildUI()
        {
            // Root canvas setup
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;
            }
            if (GetComponent<CanvasScaler>() == null)
            {
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            interactionBlocker = gameObject.AddComponent<CanvasGroup>();

            RectTransform rootRt = GetComponent<RectTransform>();

            // Background
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f);

            // === Header bar ===
            GameObject headerObj = CreatePanel(rootRt, "Header",
                new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -60f), Vector2.zero,
                new Color(0f, 0f, 0f, 0.6f));
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();

            // Back button
            Button backBtn = CreateTextButton(headerRt, "BackButton",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(10f, 5f), new Vector2(100f, -5f),
                new Color(0.4f, 0.2f, 0.2f, 0.9f), 22, "< Back");
            backBtn.onClick.AddListener(OnBackClicked);

            // Collection name
            collectionNameText = CreateTextElement(headerRt, "CollectionName",
                new Vector2(0.2f, 0f), new Vector2(0.8f, 1f),
                Vector2.zero, Vector2.zero,
                28, Color.white, TextAnchor.MiddleCenter);

            // Gold display
            goldText = CreateTextElement(headerRt, "Gold",
                new Vector2(0.8f, 0f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(-15f, 0f),
                24, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleRight);

            // === Center area (node panel container) ===
            GameObject centerObj = CreatePanel(rootRt, "CenterArea",
                new Vector2(0f, 0.15f), new Vector2(1f, 0.85f),
                Vector2.zero, Vector2.zero,
                Color.clear);
            centerRt = centerObj.GetComponent<RectTransform>();
            // Remove Image to make transparent
            Destroy(centerObj.GetComponent<Image>());

            // Node panel (centered, fixed size)
            nodePanel = CreatePanel(centerRt, "NodePanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                new Color(0.1f, 0.1f, 0.2f, 0.9f));
            nodePanelRt = nodePanel.GetComponent<RectTransform>();
            nodePanelRt.sizeDelta = new Vector2(280f, 200f);
            nodePanelRt.anchoredPosition = new Vector2(-80f, 0f);
            nodePanelGroup = nodePanel.AddComponent<CanvasGroup>();
            nodePanel.AddComponent<Button>().onClick.AddListener(OnNodePanelClicked);

            // Node icon (top part of panel)
            GameObject iconObj = new GameObject("NodeIcon");
            iconObj.transform.SetParent(nodePanelRt, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0f, 30f);
            iconRt.sizeDelta = new Vector2(70f, 70f);
            nodeIconImage = iconObj.AddComponent<Image>();
            nodeIconImage.preserveAspect = true;
            nodeIconImage.color = Color.gray;

            // Node name
            nodeNameText = CreateTextElement(nodePanelRt, "NodeName",
                new Vector2(0f, 0f), new Vector2(1f, 0.35f),
                new Vector2(10f, 20f), new Vector2(-10f, 0f),
                24, Color.white, TextAnchor.MiddleCenter);

            // Node type tag
            nodeTypeText = CreateTextElement(nodePanelRt, "NodeType",
                new Vector2(0f, 0f), new Vector2(1f, 0.15f),
                new Vector2(10f, 5f), new Vector2(-10f, 0f),
                18, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter);

            // === Bottom area ===

            // Effects count
            effectsCountText = CreateTextElement(rootRt, "EffectsCount",
                new Vector2(0f, 0.08f), new Vector2(1f, 0.14f),
                new Vector2(20f, 0f), new Vector2(-20f, 0f),
                20, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter);
        }

        private GameObject CreatePanel(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private Text CreateTextElement(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            int fontSize, Color color, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            Text text = obj.AddComponent<Text>();
            text.font = cachedFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        private Button CreateTextButton(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color bgColor, int fontSize, string label)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            Image img = obj.AddComponent<Image>();
            img.color = bgColor;
            Button btn = obj.AddComponent<Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            Text text = textObj.AddComponent<Text>();
            text.font = cachedFont;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            text.color = Color.white;

            return btn;
        }
    }
}
