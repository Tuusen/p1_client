using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class DragVisualManager : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;

        private GameObject dimOverlay;
        private GameObject heroGlow;
        private GameObject aimLineObj;
        private LineRenderer aimLine;
        private GameObject shieldCircle;
        private GameObject summonCircle;
        private GameObject[] edgeGlows;

        private SpriteRenderer heroSR;
        private Color heroOrigColor;
        private bool isDragging;
        private DragHintUI dragHint;

        private void Awake()
        {
            if (battleManager == null)
                battleManager = GetComponent<BattleManager>();
        }

        public void BeginDrag(SkillCategory category, string hintText = "")
        {
            if (isDragging) EndDrag();
            isDragging = true;

            Time.timeScale = 0.3f;
            CreateDimOverlay();

            // Show drag hint
            if (!string.IsNullOrEmpty(hintText))
            {
                Canvas canvas = FindScreenOverlayCanvas();
                if (canvas != null)
                {
                    dragHint = new DragHintUI();
                    dragHint.Show(hintText, canvas);
                }
            }

            switch (category)
            {
                case SkillCategory.Self:
                    CreateHeroGlow();
                    break;
                case SkillCategory.Projectile:
                    CreateAimLine();
                    break;
                case SkillCategory.Aoe:
                    CreateEdgeGlow();
                    break;
                case SkillCategory.Shield:
                    CreateRangeCircle(ref shieldCircle, 4f, new Color(0.3f, 0.7f, 1f, 0.5f));
                    break;
                case SkillCategory.Summon:
                    CreateRangeCircle(ref summonCircle, 3f, new Color(0.9f, 0.7f, 0.2f, 0.5f));
                    break;
            }
        }

        public void UpdateDrag(SkillCategory category)
        {
            if (!isDragging) return;

            if (category == SkillCategory.Projectile)
                UpdateAimLine();
            else if (category == SkillCategory.Aoe)
                UpdateEdgeGlow();

            // Keep circles following hero
            Transform heroT = battleManager != null ? battleManager.HeroTransform : null;
            if (heroT != null)
            {
                if (shieldCircle != null) shieldCircle.transform.position = heroT.position;
                if (summonCircle != null) summonCircle.transform.position = heroT.position;
                if (heroGlow != null) heroGlow.transform.position = heroT.position;
            }
        }

        public void EndDrag()
        {
            if (!isDragging) return;
            isDragging = false;

            Time.timeScale = 1f;

            // Restore hero color
            if (heroSR != null)
            {
                heroSR.color = heroOrigColor;
                heroSR = null;
            }

            DestroyObj(ref dimOverlay);
            DestroyObj(ref heroGlow);
            DestroyAimLine();
            DestroyObj(ref shieldCircle);
            DestroyObj(ref summonCircle);
            DestroyEdgeGlows();

            if (dragHint != null)
            {
                dragHint.Hide();
                dragHint = null;
            }
        }

        private void OnDisable()
        {
            if (isDragging) EndDrag();
        }

        // ===== Screen Dim =====
        private void CreateDimOverlay()
        {
            Canvas canvas = FindScreenOverlayCanvas();
            if (canvas == null) return;

            dimOverlay = new GameObject("DragDimOverlay");
            dimOverlay.transform.SetParent(canvas.transform, false);
            dimOverlay.transform.SetAsFirstSibling();

            Image img = dimOverlay.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.5f);
            img.raycastTarget = false;

            RectTransform rt = dimOverlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ===== Hero Glow (Self) =====
        private void CreateHeroGlow()
        {
            Transform heroT = battleManager != null ? battleManager.HeroTransform : null;
            if (heroT == null) return;

            heroSR = heroT.GetComponent<SpriteRenderer>();
            if (heroSR != null)
            {
                heroOrigColor = heroSR.color;
                heroSR.color = new Color(1f, 1f, 0.5f, 1f);
            }

            // Halo circle behind hero
            heroGlow = new GameObject("HeroGlow");
            heroGlow.transform.position = heroT.position;
            SpriteRenderer glowSR = heroGlow.AddComponent<SpriteRenderer>();
            glowSR.sprite = GameHelper.LoadSprite("Sprites/range_circle");
            glowSR.color = new Color(1f, 1f, 0.6f, 0.3f);
            glowSR.sortingOrder = 4;
            heroGlow.transform.localScale = Vector3.one * 3f;
        }

        // ===== Aim Line (Projectile) =====
        private void CreateAimLine()
        {
            Transform heroT = battleManager != null ? battleManager.HeroTransform : null;
            if (heroT == null) return;

            aimLineObj = new GameObject("AimLine");
            aimLine = aimLineObj.AddComponent<LineRenderer>();
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
            aimLine.startWidth = 0.12f;
            aimLine.endWidth = 0.04f;
            aimLine.startColor = new Color(1f, 0.8f, 0.2f, 0.9f);
            aimLine.endColor = new Color(1f, 1f, 1f, 0.3f);
            aimLine.sortingOrder = 9;
            aimLine.positionCount = 2;

            UpdateAimLine();
        }

        private void UpdateAimLine()
        {
            if (aimLine == null || battleManager == null) return;
            Transform heroT = battleManager.HeroTransform;
            if (heroT == null) return;

            Vector3 heroPos = heroT.position;
            Transform target = battleManager.GetNearestEnemy(heroPos, 50f);

            Vector3 endPos;
            if (target != null)
            {
                endPos = target.position;
            }
            else
            {
                endPos = heroPos + Vector3.right * 5f;
            }

            aimLine.SetPosition(0, heroPos);
            aimLine.SetPosition(1, endPos);
        }

        private void DestroyAimLine()
        {
            if (aimLineObj != null)
            {
                Destroy(aimLineObj);
                aimLineObj = null;
                aimLine = null;
            }
        }

        // ===== Range Circle (Shield / Summon) =====
        private void CreateRangeCircle(ref GameObject circleObj, float diameter, Color color)
        {
            Transform heroT = battleManager != null ? battleManager.HeroTransform : null;
            if (heroT == null) return;

            circleObj = new GameObject("RangeCircle");
            circleObj.transform.position = heroT.position;
            SpriteRenderer sr = circleObj.AddComponent<SpriteRenderer>();
            sr.sprite = GameHelper.LoadSprite("Sprites/range_circle");
            sr.color = color;
            sr.sortingOrder = 6;

            // range_circle sprite is 128x128 at 100 PPU = 1.28 world units
            // Scale to desired diameter
            float spriteWorldSize = 1.28f;
            float scale = diameter / spriteWorldSize;
            circleObj.transform.localScale = Vector3.one * scale;
        }

        // ===== Edge Glow (Aoe) =====
        private void CreateEdgeGlow()
        {
            Canvas canvas = FindScreenOverlayCanvas();
            if (canvas == null) return;

            edgeGlows = new GameObject[4];
            // 0=top, 1=bottom, 2=left, 3=right
            for (int i = 0; i < 4; i++)
            {
                GameObject go = new GameObject($"EdgeGlow_{i}");
                go.transform.SetParent(canvas.transform, false);
                go.transform.SetAsLastSibling();

                Image img = go.AddComponent<Image>();
                img.color = new Color(1f, 0.5f, 0.2f, 0.4f);
                img.raycastTarget = false;

                RectTransform rt = go.GetComponent<RectTransform>();
                switch (i)
                {
                    case 0: // top
                        rt.anchorMin = new Vector2(0, 1);
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = new Vector2(0, -60);
                        rt.offsetMax = Vector2.zero;
                        break;
                    case 1: // bottom
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = new Vector2(1, 0);
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = new Vector2(0, 60);
                        break;
                    case 2: // left
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = new Vector2(0, 1);
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = new Vector2(60, 0);
                        break;
                    case 3: // right
                        rt.anchorMin = new Vector2(1, 0);
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = new Vector2(-60, 0);
                        rt.offsetMax = Vector2.zero;
                        break;
                }

                edgeGlows[i] = go;
            }
        }

        private void UpdateEdgeGlow()
        {
            if (edgeGlows == null) return;
            float alpha = 0.25f + Mathf.PingPong(Time.unscaledTime * 2f, 0.4f);
            Color c = new Color(1f, 0.5f, 0.2f, alpha);
            for (int i = 0; i < edgeGlows.Length; i++)
            {
                if (edgeGlows[i] == null) continue;
                Image img = edgeGlows[i].GetComponent<Image>();
                if (img != null) img.color = c;
            }
        }

        private void DestroyEdgeGlows()
        {
            if (edgeGlows == null) return;
            for (int i = 0; i < edgeGlows.Length; i++)
            {
                if (edgeGlows[i] != null)
                    Destroy(edgeGlows[i]);
            }
            edgeGlows = null;
        }

        // ===== Helpers =====
        private Canvas FindScreenOverlayCanvas()
        {
            Canvas[] allCanvas = FindObjectsOfType<Canvas>();
            foreach (var c in allCanvas)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    return c;
            }
            return null;
        }

        private void DestroyObj(ref GameObject obj)
        {
            if (obj != null)
            {
                Destroy(obj);
                obj = null;
            }
        }
    }
}
