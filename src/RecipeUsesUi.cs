using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualtiesUnknown.RecipeIngredientJump
{
    internal static class RecipeUsesUi
    {
        private static readonly Color Black = new Color(0.015f, 0.018f, 0.018f, 0.98f);
        private static readonly Color RaisedBlack = new Color(0.055f, 0.065f, 0.065f, 0.98f);
        private static readonly Color White = new Color(0.94f, 0.95f, 0.93f, 1f);
        private static readonly Color Muted = new Color(0.45f, 0.48f, 0.46f, 1f);
        private static readonly Color Active = new Color(0.3f, 0.8f, 0.36f, 1f);
        private static readonly Color Green = new Color(0.28f, 1f, 0.22f, 1f);

        private const float ButtonWidth = 112f;
        private const float ButtonHeight = 40f;
        private const float ButtonX = 8f;
        private const float EmiFirstButtonY = -18f;
        private const float EmiButtonStep = 46f;
        private const int EmiButtonCount = 3;
        private const float RowHeight = 64f;
        private const float RowIconSize = 44f;

        private static int uiLayer = int.MinValue;
        private static GameObject host;
        private static RectTransform panel;
        private static TextMeshProUGUI titleText;
        private static RectTransform listContent;
        private static Image buttonImage;
        private static TextMeshProUGUI buttonText;
        private static PlayerCamera cam;
        private static TMP_FontAsset font;
        private static readonly List<GameObject> rows = new List<GameObject>();

        internal static void OnSelectedRecipeRefreshed(PlayerCamera camera)
        {
            if (!RecipeIngredientJumpPlugin.ShowUsesButton.Value)
            {
                Destroy();
                return;
            }

            if (camera == null || camera.craftingPanel == null) return;

            try
            {
                cam = camera;
                if (host == null) Build(camera);
                if (host == null) return;

                if (panel != null && panel.gameObject.activeSelf)
                    RefreshList(camera);
            }
            catch (Exception e)
            {
                if (RecipeIngredientJumpPlugin.Log != null)
                    RecipeIngredientJumpPlugin.Log.LogError($"用途面板刷新失败: {e}");
            }
        }

        internal static void Destroy()
        {
            rows.Clear();
            if (host != null) UnityEngine.Object.Destroy(host);
            host = null;
            panel = null;
            titleText = null;
            listContent = null;
            buttonImage = null;
            buttonText = null;
            font = null;
        }

        internal static void ClosePanel()
        {
            if (panel == null) return;
            panel.gameObject.SetActive(false);
            UpdateButtonVisual(false);
        }

        private static void Build(PlayerCamera camera)
        {
            var parent = camera.craftingPanel.transform;
            font = camera.pinRecipeText != null
                ? camera.pinRecipeText.font
                : parent.GetComponentInChildren<TextMeshProUGUI>(true)?.font;

            host = new GameObject("RecipeUsesUI", typeof(RectTransform));
            SetUiLayer(host);
            host.transform.SetParent(parent, false);
            Stretch((RectTransform)host.transform);
            host.AddComponent<RecipeUsesWatcher>();

            var buttonGo = new GameObject("UsesButton", typeof(RectTransform), typeof(Image));
            SetUiLayer(buttonGo);
            buttonGo.transform.SetParent(host.transform, false);

            buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = RaisedBlack;

            var outline = buttonGo.AddComponent<Outline>();
            outline.effectColor = White;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            Anchor(
                (RectTransform)buttonGo.transform,
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(ButtonX, GetButtonY()),
                new Vector2(ButtonWidth, ButtonHeight));

            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.68f, 0.76f, 0.7f, 1f);
            colors.pressedColor = Active;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.65f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(Toggle);

            buttonText = CreateText("Label", buttonGo.transform, 18f, TextAlignmentOptions.Center);
            Stretch(buttonText.rectTransform, 5f, 5f, 3f, 3f);
            buttonText.text = UsesText.Button;
            buttonText.enableWordWrapping = false;
            buttonText.overflowMode = TextOverflowModes.Ellipsis;

            ApplyTabVisual(false);

            AddTooltip(buttonGo, UsesText.Button, UsesText.ButtonDescription);

            var panelImage = CreatePanel("UsesPanel", host.transform, Black, true);
            panel = panelImage.rectTransform;
            panel.anchorMin = new Vector2(0.505f, 0.018f);
            panel.anchorMax = new Vector2(0.992f, 0.988f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            AddTooltip(panelImage.gameObject, string.Empty, string.Empty);

            var header = CreatePanel("Header", panel, RaisedBlack);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 50f);

            titleText = CreateText("Title", header.transform, 21f, TextAlignmentOptions.Left);
            Stretch(titleText.rectTransform, 12f, 48f, 3f, 3f);

            var closeImage = CreatePanel("Close", header.transform, RaisedBlack, true);
            Anchor(
                closeImage.rectTransform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-7f, 0f),
                new Vector2(36f, 34f));
            var closeButton = closeImage.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(ClosePanel);
            var closeText = CreateText("Label", closeImage.transform, 18f, TextAlignmentOptions.Center);
            Stretch(closeText.rectTransform, 2f, 2f, 2f, 2f);
            closeText.text = "X";
            AddTooltip(closeImage.gameObject, UsesText.Close, UsesText.CloseDescription);

            CreateScrollView("List", panel, out listContent);
            var scrollRect = listContent.parent.parent as RectTransform;
            if (scrollRect != null)
            {
                scrollRect.anchorMin = Vector2.zero;
                scrollRect.anchorMax = Vector2.one;
                scrollRect.offsetMin = new Vector2(5f, 5f);
                scrollRect.offsetMax = new Vector2(-5f, -56f);
            }

            panel.gameObject.SetActive(false);
        }

        private static void Toggle()
        {
            if (panel == null) return;

            bool open = !panel.gameObject.activeSelf;
            panel.gameObject.SetActive(open);
            UpdateButtonVisual(open);

            if (open) RefreshList(cam);
        }

        private static void UpdateButtonVisual(bool open)
        {
            ApplyTabVisual(open);
        }

        private static void ApplyTabVisual(bool active)
        {
            if (buttonImage != null && cam != null)
            {
                var sprite = active ? cam.darkenedUiNano : cam.uiNano;
                buttonImage.sprite = sprite;
                buttonImage.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                buttonImage.color = sprite != null ? Color.white : (active ? RaisedBlack : Black);
            }

            if (buttonText != null) buttonText.color = active ? Green : White;
        }

        private static void RefreshList(PlayerCamera camera)
        {
            if (camera == null || listContent == null) return;

            ClearRows();

            var recipe = RecipeJumpService.GetSelectedRecipe(camera);
            if (recipe?.result == null)
            {
                if (titleText != null) titleText.text = UsesText.Title(string.Empty);
                AddEmpty(UsesText.NoSelection);
                return;
            }

            string id = recipe.result.id;
            bool isLiquid = recipe.result.isLiquid;
            string itemName = isLiquid ? Locale.GetOther(id) : Locale.GetItem(id);
            if (string.IsNullOrEmpty(itemName)) itemName = id;

            if (titleText != null) titleText.text = UsesText.Title(itemName);

            var usages = RecipeQuery.FindUsages(id, isLiquid, includeHidden: true, includeRepairs: true);
            if (usages.Count == 0)
            {
                AddEmpty(UsesText.NoUsages);
                return;
            }

            usages.Sort((left, right) =>
            {
                bool leftVisible = left.Recipe.visible;
                bool rightVisible = right.Recipe.visible;
                if (leftVisible != rightVisible) return leftVisible ? -1 : 1;
                return left.Recipe.INT.CompareTo(right.Recipe.INT);
            });

            for (int i = 0; i < usages.Count; i++)
            {
                AddRow(usages[i]);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        }

        private static void AddRow(RecipeUsage usage)
        {
            var recipe = usage.Recipe;
            bool visible = recipe.visible;

            var rowImage = CreatePanel("UsageRow", listContent, RaisedBlack, true);
            var rowRect = rowImage.rectTransform;
            rowImage.raycastTarget = visible;

            var layout = rowImage.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            layout.flexibleHeight = 0f;

            if (visible)
            {
                var button = rowImage.gameObject.AddComponent<Button>();
                button.targetGraphic = rowImage;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.7f, 0.8f, 0.72f, 1f);
                colors.pressedColor = Active;
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
                button.onClick.AddListener(() =>
                {
                    ClosePanel();
                    RecipeJumpService.SelectRecipe(cam, recipe);
                });
            }

            var iconImage = CreatePanel("Icon", rowRect, Color.white, true);
            iconImage.raycastTarget = false;
            Anchor(
                iconImage.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(9f, 0f),
                new Vector2(RowIconSize, RowIconSize));

            var sprite = recipe.resultSprite;
            iconImage.sprite = sprite.Item1;
            iconImage.color = visible ? sprite.Item2 : Muted;
            iconImage.preserveAspect = true;

            var text = CreateText("Text", rowRect, 18f, TextAlignmentOptions.Left);
            Stretch(text.rectTransform, 60f, 8f, 4f, 4f);
            text.color = visible ? White : Muted;
            text.text = recipe.simpleName + "\n<size=13><color=#8D948F>" +
                        FormatDetail(recipe, usage) + "</color></size>";

            AddTooltip(rowImage.gameObject, recipe.simpleName, recipe.description);
            rows.Add(rowImage.gameObject);
        }

        private static string FormatDetail(Recipe recipe, RecipeUsage usage)
        {
            string output = recipe.result.isLiquid
                ? recipe.result.resultCondition.ToString("0.#") + "mL"
                : "x" + recipe.result.amount;

            string detail = UsesText.IntLevel(recipe.INT) + " | " + output +
                            " | " + UsesText.RequiredUses(usage.MatchCount) +
                            (usage.ByQuality ? UsesText.QualityTag : string.Empty);

            if (!recipe.visible) detail += " | " + UsesText.Locked;
            return detail;
        }

        private static void AddEmpty(string message)
        {
            var text = CreateText("Empty", listContent, 20f, TextAlignmentOptions.Center);
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            text.color = Muted;
            text.text = message;
            rows.Add(text.gameObject);
        }

        private static void ClearRows()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null) UnityEngine.Object.Destroy(rows[i]);
            }
            rows.Clear();
        }

        private static float GetButtonY()
        {
            float configured = RecipeIngredientJumpPlugin.UsesButtonYOffset.Value;
            if (configured >= 0f) return configured;

            bool emiLoaded = false;
            try
            {
                emiLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos
                    .ContainsKey("exmeow.casualtiesunknown.emi");
            }
            catch
            {
            }

            return emiLoaded
                ? EmiFirstButtonY - EmiButtonCount * EmiButtonStep
                : EmiFirstButtonY;
        }

        private static void SetUiLayer(GameObject target)
        {
            if (uiLayer == int.MinValue)
                uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) target.layer = uiLayer;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SetUiLayer(go);
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image CreatePanel(string name, Transform parent, Color color, bool outline = false)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;

            if (outline)
            {
                var effect = rect.gameObject.AddComponent<Outline>();
                effect.effectColor = White;
                effect.effectDistance = new Vector2(1f, -1f);
                effect.useGraphicAlpha = true;
            }

            return image;
        }

        private static TextMeshProUGUI CreateText(
            string name, Transform parent, float fontSize, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = White;
            text.raycastTarget = false;
            text.richText = true;
            text.enableWordWrapping = true;
            return text;
        }

        private static void CreateScrollView(string name, Transform parent, out RectTransform content)
        {
            var background = CreatePanel(name, parent, Black);
            var scroll = background.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;

            var viewport = CreatePanel("Viewport", background.transform, Color.clear);
            Stretch(viewport.rectTransform, 2f, 2f, 2f, 2f);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = CreateRect("Content", viewport.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
        }

        private static void AddTooltip(GameObject target, string title, string description)
        {
            var tooltip = target.GetComponent<UITooltip>();
            if (tooltip == null) tooltip = target.AddComponent<UITooltip>();
            tooltip.skipLocale = true;
            tooltip.tipName = title;
            tooltip.tipDesc = description;
        }

        private static void Stretch(
            RectTransform rect,
            float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Anchor(
            RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }

    internal class RecipeUsesWatcher : MonoBehaviour
    {
        private void OnDisable()
        {
            RecipeUsesUi.ClosePanel();
        }
    }

    internal static class UsesText
    {
        private static bool? isChinese;

        private static bool IsChinese
        {
            get
            {
                if (isChinese.HasValue) return isChinese.Value;

                bool result = false;
                try
                {
                    if (!string.IsNullOrEmpty(Locale.currentLangName) &&
                        Locale.currentLangName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    {
                        result = true;
                    }
                    else
                    {
                        string probe = Locale.GetOther("craftanyitem");
                        if (!string.IsNullOrEmpty(probe))
                        {
                            foreach (char character in probe)
                            {
                                if (character >= 0x3400 && character <= 0x9fff)
                                {
                                    result = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch
                {
                }

                isChinese = result;
                return result;
            }
        }

        internal static string Button => IsChinese ? "用途" : "USES";
        internal static string ButtonDescription => IsChinese
            ? "查看所选配方产物可以用于哪些配方"
            : "Show recipes that use the selected recipe result";
        internal static string Close => IsChinese ? "关闭" : "CLOSE";
        internal static string CloseDescription => IsChinese ? "关闭用途面板" : "Close the uses panel";
        internal static string NoSelection => IsChinese ? "没有选中配方" : "NO SELECTED RECIPE";
        internal static string NoUsages => IsChinese ? "没有用途配方" : "NO USAGE RECIPES";
        internal static string QualityTag => IsChinese ? "（性质）" : " (quality)";
        internal static string Locked => IsChinese ? "未解锁" : "LOCKED";

        internal static string Title(string itemName)
        {
            return (IsChinese ? "用途：" : "USES: ") + itemName;
        }

        internal static string IntLevel(int intelligence)
        {
            return (IsChinese ? "智力 " : "INT ") + intelligence;
        }

        internal static string RequiredUses(int count)
        {
            return IsChinese ? "需要 " + count + " 个" : "uses x" + count;
        }
    }
}
