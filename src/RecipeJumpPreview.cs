using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualtiesUnknown.RecipeIngredientJump
{
    internal static class RecipeJumpPreview
    {
        private const float CardWidth = 560f;
        private const float CardPadding = 14f;
        private const float CardSpacing = 8f;
        private const float IconGap = 10f;
        private const float MaxScreenHeightRatio = 0.6f;

        private static GameObject cardRoot;
        private static RectTransform cardRect;
        private static Canvas cardCanvas;
        private static TextMeshProUGUI nameText;
        private static Image iconImage;
        private static LayoutElement iconLayout;
        private static TextMeshProUGUI ingredientsText;
        private static TextMeshProUGUI infoText;

        internal static void Show(int recipeIndex, RectTransform anchor)
        {
            if (!RecipeIngredientJumpPlugin.ShowRecipePreview.Value) return;
            if (Recipes.recipes == null) return;
            if (recipeIndex < 0 || recipeIndex >= Recipes.recipes.Count) return;

            var recipe = Recipes.recipes[recipeIndex];
            if (recipe?.result == null) return;

            var cam = PlayerCamera.main;
            if (cam == null || cam.craftingPanel == null) return;

            try
            {
                EnsureCard(cam, anchor);
                if (cardRoot == null) return;

                cardRoot.SetActive(true);
                Populate(cam, recipe);
                PositionCard(anchor);
            }
            catch (Exception e)
            {
                if (RecipeIngredientJumpPlugin.Log != null)
                    RecipeIngredientJumpPlugin.Log.LogError($"配方预览显示失败: {e}");
                Hide();
            }
        }

        internal static void Hide()
        {
            if (cardRoot != null) cardRoot.SetActive(false);
        }

        internal static void DestroyCard()
        {
            if (cardRoot != null)
            {
                UnityEngine.Object.Destroy(cardRoot);
                cardRoot = null;
                cardRect = null;
                cardCanvas = null;
                nameText = null;
                iconImage = null;
                iconLayout = null;
                ingredientsText = null;
                infoText = null;
            }
        }

        private static void EnsureCard(PlayerCamera cam, RectTransform anchor)
        {
            if (cardRoot != null) return;

            var canvas = anchor != null ? anchor.GetComponentInParent<Canvas>() : null;
            if (canvas == null) canvas = cam.mainCanvas;
            if (canvas == null && GlobalDark.main != null) canvas = GlobalDark.main.canvas;
            if (canvas == null) return;

            cardCanvas = canvas;
            cardRoot = new GameObject("RecipeJumpPreview", typeof(RectTransform), typeof(Image));
            cardRoot.transform.SetParent(canvas.transform, false);
            cardRoot.transform.SetAsLastSibling();

            cardRect = (RectTransform)cardRoot.transform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(CardWidth, 120f);

            var background = cardRoot.GetComponent<Image>();
            background.color = new Color(0.04f, 0.04f, 0.04f, 0.96f);
            background.raycastTarget = false;

            var outline = cardRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;

            var layout = cardRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)CardPadding, (int)CardPadding, (int)CardPadding, (int)CardPadding);
            layout.spacing = CardSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = cardRoot.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            nameText = CreateText(cardRect, "Name", GetTemplate(cam, 5));
            nameText.alignment = TextAlignmentOptions.Center;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(cardRect, false);
            iconImage = iconGo.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconLayout = iconGo.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 110f;
            iconLayout.preferredHeight = 110f;

            ingredientsText = CreateText(cardRect, "Ingredients", GetTemplate(cam, 8));
            ingredientsText.alignment = TextAlignmentOptions.TopLeft;

            infoText = CreateText(cardRect, "Info", GetTemplate(cam, 10));
            infoText.alignment = TextAlignmentOptions.TopLeft;

            cardRoot.AddComponent<RecipeJumpPreviewBehaviour>();
            cardRoot.SetActive(false);
        }

        private static TextMeshProUGUI GetTemplate(PlayerCamera cam, int childIndex)
        {
            var panel = cam.craftingPanel.transform;
            if (panel.childCount <= childIndex) return null;
            return panel.GetChild(childIndex).GetComponent<TextMeshProUGUI>();
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string name, TextMeshProUGUI template)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (template != null)
            {
                text.font = template.font;
                text.fontSharedMaterial = template.fontSharedMaterial;
                text.fontSize = template.fontSize;
                text.enableAutoSizing = template.enableAutoSizing;
                text.fontSizeMin = template.fontSizeMin;
                text.fontSizeMax = template.fontSizeMax;
                text.color = template.color;
                text.fontStyle = template.fontStyle;
                text.margin = template.margin;
            }

            text.richText = true;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft;
            return text;
        }

        private static void Populate(PlayerCamera cam, Recipe recipe)
        {
            if (nameText != null) nameText.text = recipe.fullName;

            if (iconImage != null && iconLayout != null)
            {
                var sprite = recipe.resultSprite;
                iconImage.sprite = sprite.Item1;
                iconImage.color = sprite.Item2;

                if (sprite.Item1 != null)
                {
                    var size = PlayerCamera.ImageSizeDelta(sprite.Item1.texture, 12f, 150f);
                    iconLayout.preferredWidth = Mathf.Max(24f, size.x);
                    iconLayout.preferredHeight = Mathf.Max(24f, size.y);
                }
                else
                {
                    iconLayout.preferredWidth = 24f;
                    iconLayout.preferredHeight = 24f;
                }
            }

            if (ingredientsText != null)
            {
                var items = recipe.GetItemsForRecipeThorough();
                ingredientsText.text = cam.IngredientTextForRecipe(recipe, items);
            }

            if (infoText != null)
                infoText.text = BuildInfoText(cam, recipe);

            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
        }

        private static string BuildInfoText(PlayerCamera cam, Recipe recipe)
        {
            if (cam.body == null) return "";

            string info = "";
            if (recipe.result.isLiquid)
            {
                info += Locale.GetOther("craftinfovolume") +
                        recipe.result.resultCondition.ToString("0.#") + "mL\n";
            }
            else
            {
                info += Locale.GetOther("craftinfocondition") +
                        (recipe.result.resultCondition * 100f).ToString("0.#") + "%\n";
            }

            info += Locale.GetOther("craftinfoamount") + recipe.result.amount.ToString() + "\n";
            info += "<color=#" + ColorUtility.ToHtmlStringRGB(cam.RecipeINTToColor(recipe.INT)) + ">" +
                    Locale.GetOther("craftinfoint")
                        .Replace("<1>", recipe.INT.ToString())
                        .Replace("<2>", cam.body.skills.INT.ToString()) + "\n";

            int diff = cam.body.skills.INT - recipe.INT;
            if (diff == -1) info += Locale.GetOther("craftminorfail");
            else if (diff == -2) info += Locale.GetOther("craftmajorfail");
            else if (diff == -3) info += Locale.GetOther("craftcriticalfail");

            return info;
        }

        private static void PositionCard(RectTransform anchor)
        {
            if (anchor == null || cardRect == null) return;

            var canvas = cardCanvas;
            if (canvas == null) return;

            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null) return;

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 iconScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, anchor.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, iconScreen, uiCamera, out Vector2 local))
                return;

            float scale = RecipeIngredientJumpPlugin.PreviewScale.Value;
            if (scale < 0.05f) scale = 0.5f;

            Vector2 cardSize = cardRect.rect.size * scale;
            float maxHeight = canvasRect.rect.height * MaxScreenHeightRatio;
            if (cardSize.y > maxHeight && cardSize.y > 0f)
            {
                scale *= maxHeight / cardSize.y;
                cardSize = cardRect.rect.size * scale;
            }

            cardRect.localScale = Vector3.one * scale;

            float halfW = cardSize.x * 0.5f;
            float halfH = cardSize.y * 0.5f;
            float minX = canvasRect.rect.xMin + 4f;
            float maxX = canvasRect.rect.xMax - 4f;
            float minY = canvasRect.rect.yMin + 4f;
            float maxY = canvasRect.rect.yMax - 4f;

            float centerX = local.x - IconGap - halfW;
            if (centerX - halfW < minX)
                centerX = local.x + IconGap + halfW;

            float centerY = local.y;

            centerX = Mathf.Clamp(centerX, minX + halfW, maxX - halfW);
            centerY = Mathf.Clamp(centerY, minY + halfH, maxY - halfH);

            Vector2 anchorReference = canvasRect.rect.center;
            cardRect.anchoredPosition = new Vector2(centerX, centerY) - anchorReference;
        }
    }

    internal class RecipeJumpPreviewBehaviour : MonoBehaviour
    {
        private void Update()
        {
            var cam = PlayerCamera.main;
            if (cam == null || cam.craftingPanel == null || !cam.craftingPanel.activeSelf)
                RecipeJumpPreview.Hide();
        }
    }
}
