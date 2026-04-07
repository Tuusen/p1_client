using System;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class DialogueWin : BaseWin
    {
        [SerializeField] private Text speakerText;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button autoButton;
        [SerializeField] private Button clickArea;
        [SerializeField] private GameObject nextIndicator;

        private DialogueConfig currentConfig;
        private int currentLineIndex;
        private Action onComplete;

        // Typewriter
        private string fullText = "";
        private int charIndex;
        private float typeTimer;
        private bool isTyping;
        private const float TypeSpeed = 0.03f;

        // Auto mode
        private bool isAutoMode;
        private const float AutoDelay = 1.5f;
        private float autoTimer;

        private float savedTimeScale;
        private bool hasPausedTime;

        public override void Init()
        {
            base.Init();

            if (speakerText == null)
                BuildUI();

            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipClicked);
            if (autoButton != null)
                autoButton.onClick.AddListener(OnAutoClicked);
            if (clickArea != null)
                clickArea.onClick.AddListener(OnClickAreaPressed);
        }

        public override void Show()
        {
            base.Show();
        }

        public override void OnClose()
        {
            base.OnClose();

            // If dialogue is still active when closed externally, treat as skip
            if (currentConfig != null)
            {
                RestoreTimeScale();
                currentConfig = null;

                Action callback = onComplete;
                onComplete = null;
                callback?.Invoke();
            }
        }

        /// <summary>
        /// 显示对话序列。对话完成或跳过时调用 onComplete。
        /// </summary>
        public void ShowDialogue(DialogueConfig config, Action onComplete)
        {
            if (config == null || config.lines == null || config.lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            currentConfig = config;
            this.onComplete = onComplete;
            currentLineIndex = 0;
            isAutoMode = false;
            autoTimer = 0f;

            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            hasPausedTime = true;

            if (leftPortrait != null)
                leftPortrait.gameObject.SetActive(false);
            if (rightPortrait != null)
                rightPortrait.gameObject.SetActive(false);

            UpdateAutoButtonText();
            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (currentConfig == null || currentLineIndex >= currentConfig.lines.Length)
            {
                CompleteDialogue();
                return;
            }

            DialogueConfig.LinesItem line = currentConfig.lines[currentLineIndex];

            if (speakerText != null)
                speakerText.text = line.speaker ?? "";

            UpdatePortrait(line);

            fullText = line.text ?? "";
            charIndex = 0;
            typeTimer = 0f;
            isTyping = true;
            autoTimer = 0f;

            if (dialogueText != null)
                dialogueText.text = "";
            if (nextIndicator != null)
                nextIndicator.SetActive(false);
        }

        private void UpdatePortrait(DialogueConfig.LinesItem line)
        {
            Sprite portrait = null;
            if (line.roleId > 0)
            {
                RoleConfig role = Cfg.Role.Get(line.roleId);
                if (role != null && !string.IsNullOrEmpty(role.portraitPath))
                    portrait = GameHelper.LoadSprite(role.portraitPath);
            }

            bool isLeft = line.portraitSide == 0;

            // Set portrait on the active side
            if (portrait != null)
            {
                if (isLeft && leftPortrait != null)
                {
                    leftPortrait.sprite = portrait;
                    leftPortrait.gameObject.SetActive(true);
                }
                else if (!isLeft && rightPortrait != null)
                {
                    rightPortrait.sprite = portrait;
                    rightPortrait.gameObject.SetActive(true);
                }
            }

            // Highlight active side, dim inactive
            if (leftPortrait != null && leftPortrait.gameObject.activeSelf)
                leftPortrait.color = isLeft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            if (rightPortrait != null && rightPortrait.gameObject.activeSelf)
                rightPortrait.color = !isLeft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        private void Update()
        {
            if (currentConfig == null) return;

            if (isTyping)
            {
                typeTimer += Time.unscaledDeltaTime;
                while (typeTimer >= TypeSpeed && charIndex < fullText.Length)
                {
                    charIndex++;
                    typeTimer -= TypeSpeed;
                }

                if (dialogueText != null)
                    dialogueText.text = fullText.Substring(0, charIndex);

                if (charIndex >= fullText.Length)
                {
                    isTyping = false;
                    if (nextIndicator != null)
                        nextIndicator.SetActive(true);
                }
            }
            else if (isAutoMode)
            {
                autoTimer += Time.unscaledDeltaTime;
                if (autoTimer >= AutoDelay)
                    AdvanceLine();
            }
        }

        private void OnClickAreaPressed()
        {
            if (currentConfig == null) return;

            if (isTyping)
                CompleteTyping();
            else
                AdvanceLine();
        }

        private void CompleteTyping()
        {
            charIndex = fullText.Length;
            isTyping = false;

            if (dialogueText != null)
                dialogueText.text = fullText;
            if (nextIndicator != null)
                nextIndicator.SetActive(true);
        }

        private void AdvanceLine()
        {
            currentLineIndex++;
            autoTimer = 0f;
            ShowCurrentLine();
        }

        private void OnSkipClicked()
        {
            CompleteDialogue();
        }

        private void OnAutoClicked()
        {
            isAutoMode = !isAutoMode;
            autoTimer = 0f;
            UpdateAutoButtonText();
        }

        private void UpdateAutoButtonText()
        {
            if (autoButton == null) return;
            Text btnText = autoButton.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = isAutoMode ? "停止" : "自动";
        }

        private void CompleteDialogue()
        {
            RestoreTimeScale();
            currentConfig = null;

            Action callback = onComplete;
            onComplete = null;

            WinManager.Instance.CloseWin<DialogueWin>();
            callback?.Invoke();
        }

        private void RestoreTimeScale()
        {
            if (hasPausedTime)
            {
                Time.timeScale = savedTimeScale;
                hasPausedTime = false;
            }
        }

        // ===== Dynamic UI Build (fallback when prefab has no bindings) =====

        private void BuildUI()
        {
            Font font = GameHelper.LoadFont();
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // Full-screen click area (also serves as dim background)
            clickArea = CreateButton(root, "ClickArea",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.4f));

            RectTransform clickRt = clickArea.GetComponent<RectTransform>();

            // Left portrait
            leftPortrait = CreateImage(clickRt, "LeftPortrait",
                new Vector2(0f, 0.15f), new Vector2(0f, 0.15f),
                new Vector2(20f, 0f), new Vector2(200f, 280f),
                Color.white);
            leftPortrait.preserveAspect = true;
            leftPortrait.gameObject.SetActive(false);

            // Right portrait
            rightPortrait = CreateImage(clickRt, "RightPortrait",
                new Vector2(1f, 0.15f), new Vector2(1f, 0.15f),
                new Vector2(-200f, 0f), new Vector2(-20f, 280f),
                Color.white);
            rightPortrait.preserveAspect = true;
            rightPortrait.gameObject.SetActive(false);

            // Dialogue box panel (bottom 20% of screen)
            Image dialogueBox = CreateImage(clickRt, "DialogueBox",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(10f, 10f), new Vector2(-10f, 230f),
                new Color(0f, 0f, 0f, 0.85f));
            RectTransform boxRt = dialogueBox.GetComponent<RectTransform>();

            // Speaker name
            speakerText = CreateText(boxRt, "SpeakerName",
                new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(20f, -40f), new Vector2(300f, -8f),
                font, 26, Color.yellow, TextAnchor.MiddleLeft);

            // Dialogue text
            dialogueText = CreateText(boxRt, "DialogueText",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(20f, 10f), new Vector2(-20f, -45f),
                font, 22, Color.white, TextAnchor.UpperLeft);

            // Next indicator
            nextIndicator = new GameObject("NextIndicator");
            nextIndicator.transform.SetParent(boxRt, false);
            RectTransform indicatorRt = nextIndicator.AddComponent<RectTransform>();
            indicatorRt.anchorMin = new Vector2(1f, 0f);
            indicatorRt.anchorMax = new Vector2(1f, 0f);
            indicatorRt.anchoredPosition = new Vector2(-25f, 20f);
            indicatorRt.sizeDelta = new Vector2(20f, 20f);
            Text indicatorText = nextIndicator.AddComponent<Text>();
            indicatorText.font = font;
            indicatorText.fontSize = 22;
            indicatorText.alignment = TextAnchor.MiddleCenter;
            indicatorText.text = "\u25bc";
            indicatorText.color = Color.white;
            nextIndicator.SetActive(false);

            // Skip button (top-right)
            skipButton = CreateTextButton(clickRt, "SkipButton",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-130f, -50f), new Vector2(50f, 30f),
                new Color(0.3f, 0.3f, 0.3f, 0.8f), font, 20, "跳过");

            // Auto button
            autoButton = CreateTextButton(clickRt, "AutoButton",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-130f, -90f), new Vector2(50f, 30f),
                new Color(0.3f, 0.3f, 0.3f, 0.8f), font, 20, "自动");
        }

        private Image CreateImage(RectTransform parent, string name,
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
            return img;
        }

        private Text CreateText(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Font font, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            Text text = obj.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        private Button CreateButton(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color bgColor)
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
            return obj.AddComponent<Button>();
        }

        private Button CreateTextButton(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta,
            Color bgColor, Font font, int fontSize, string label)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
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
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            text.color = Color.white;

            return btn;
        }
    }
}
