using System;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    /// <summary>
    /// DialogueWin 的参数类
    /// </summary>
    public class DialogueWinParam
    {
        public int dialogueId;
        public Action onComplete;
    }

    public class DialogueWin : BaseWin
    {
        private DialogueWinParam data => Data as DialogueWinParam;
        private Text txt_speakerName;
        private Text txt_dialogueText;
        private Image sp_leftPortrait;
        private Image sp_rightPortrait;
        private Button btn_skip;
        private Button btn_auto;
        private Button btn_clickArea;
        private GameObject txt_nextIndicator;

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

        public override void start()
        {
            if (data.dialogueId <= 0)
            {
                CompleteDialogue();
                return;
            }

            DialogueConfig config = Cfg.Dialogue.Get(data.dialogueId);
            if (config == null || config.lines == null || config.lines.Length == 0)
            {
                CompleteDialogue();
                return;
            }

            ShowDialogue(config,data.onComplete);
        }

        public override void closeWin()
        {
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
        private void ShowDialogue(DialogueConfig config, Action onComplete)
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

            GameManager.Instance.PauseGame();

            if (sp_leftPortrait != null)
                sp_leftPortrait.gameObject.SetActive(false);
            if (sp_rightPortrait != null)
                sp_rightPortrait.gameObject.SetActive(false);

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

            if (txt_speakerName != null)
                txt_speakerName.text = line.speaker ?? "";

            UpdatePortrait(line);

            fullText = line.text ?? "";
            charIndex = 0;
            typeTimer = 0f;
            isTyping = true;
            autoTimer = 0f;

            if (txt_dialogueText != null)
                txt_dialogueText.text = "";
            if (txt_nextIndicator != null)
                txt_nextIndicator.SetActive(false);
        }

        private void UpdatePortrait(DialogueConfig.LinesItem line)
        {
            Sprite portrait = null;
            if (line.roleId > 0)
            {
                RoleConfig role = Cfg.Role.Get(line.roleId);
                if (role != null && !string.IsNullOrEmpty(role.head))
                    portrait = GameHelper.LoadSprite(role.head);
            }

            bool isLeft = line.portraitSide == 0;

            // Set portrait on the active side
            if (portrait != null)
            {
                if (isLeft && sp_leftPortrait != null)
                {
                    sp_leftPortrait.sprite = portrait;
                    sp_leftPortrait.gameObject.SetActive(true);
                }
                else if (!isLeft && sp_rightPortrait != null)
                {
                    sp_rightPortrait.sprite = portrait;
                    sp_rightPortrait.gameObject.SetActive(true);
                }
            }

            // Highlight active side, dim inactive
            if (sp_leftPortrait != null && sp_leftPortrait.gameObject.activeSelf)
                sp_leftPortrait.color = isLeft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            if (sp_rightPortrait != null && sp_rightPortrait.gameObject.activeSelf)
                sp_rightPortrait.color = !isLeft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
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

                if (txt_dialogueText != null)
                    txt_dialogueText.text = fullText.Substring(0, charIndex);

                if (charIndex >= fullText.Length)
                {
                    isTyping = false;
                    if (txt_nextIndicator != null)
                        txt_nextIndicator.SetActive(true);
                }
            }
            else if (isAutoMode)
            {
                autoTimer += Time.unscaledDeltaTime;
                if (autoTimer >= AutoDelay)
                    AdvanceLine();
            }
        }

        public override void onBtnClick(Button btn, object param)
        {
            string name = btn.name;
            switch (name)
            {
                case "btn_skip":
                    CompleteDialogue();
                    break;
                case "btn_auto":
                    isAutoMode = !isAutoMode;
                    autoTimer = 0f;
                    UpdateAutoButtonText();
                    break;
                case "btn_clickArea":
                    if (currentConfig == null) return;

                    if (isTyping)
                        CompleteTyping();
                    else
                        AdvanceLine();
                    break;
                default:
                    break;
            }
        }

        private void CompleteTyping()
        {
            charIndex = fullText.Length;
            isTyping = false;

            if (txt_dialogueText != null)
                txt_dialogueText.text = fullText;
            if (txt_nextIndicator != null)
                txt_nextIndicator.SetActive(true);
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
            if (btn_auto == null) return;
            Text btnText = btn_auto.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = isAutoMode ? "停止" : "自动";
        }

        private void CompleteDialogue()
        {
            RestoreTimeScale();
            currentConfig = null;

            Action callback = onComplete;
            onComplete = null;

            callback?.Invoke();
            OnClose();
        }

        private void RestoreTimeScale()
        {
            GameManager.Instance.ResetTimeScale();
        }
    }
}