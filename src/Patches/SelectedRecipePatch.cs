using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualtiesUnknown.RecipeIngredientJump.Patches
{
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.RefreshCurrentlySelectedRecipe))]
    internal static class SelectedRecipePatch
    {
        private const float IconSize = 20f;
        private const float IconStartOffset = 14f;
        private const float IconSpacing = 24f;
        private const float IconYOffset = 20f;

        [HarmonyPostfix]
        private static void Postfix(PlayerCamera __instance)
        {
            RecipeUsesUi.OnSelectedRecipeRefreshed(__instance);
            RecipeJumpService.DestroyIcons();
            if (!RecipeIngredientJumpPlugin.Enabled.Value) return;

            try
            {
                if (__instance == null || __instance.craftingPanel == null) return;

                var text = GetIngredientText(__instance);
                if (text == null) return;

                if (text.GetComponent<IngredientLinkClickHandler>() == null)
                    text.gameObject.AddComponent<IngredientLinkClickHandler>();
                text.raycastTarget = true;

                if (!RecipeIngredientJumpPlugin.ShowMultiRecipeIcons.Value) return;

                var recipe = RecipeJumpService.GetSelectedRecipe(__instance);
                if (recipe?.items == null || recipe.items.Count == 0) return;

                BuildMultiRecipeIcons(text, recipe);
            }
            catch (Exception e)
            {
                if (RecipeIngredientJumpPlugin.Log != null)
                    RecipeIngredientJumpPlugin.Log.LogError($"构建材料跳转图标失败: {e}");
            }
        }

        private static TextMeshProUGUI GetIngredientText(PlayerCamera cam)
        {
            Transform panel = cam.craftingPanel.transform;
            if (panel.childCount <= 8) return null;
            return panel.GetChild(8).GetComponent<TextMeshProUGUI>();
        }

        private static void BuildMultiRecipeIcons(TextMeshProUGUI text, Recipe recipe)
        {
            var info = text.textInfo;
            if (info == null || info.characterInfo == null) return;

            var items = recipe.GetItemsForRecipeThorough();
            bool hideSatisfied = RecipeIngredientJumpPlugin.HideCandidatesWhenSatisfied.Value;

            int ingredientIndex = 0;
            for (int c = 0; c < info.characterCount; c++)
            {
                if (ingredientIndex >= recipe.items.Count) break;

                var character = info.characterInfo[c];
                if (!character.isVisible) continue;
                if (character.elementType != TMP_TextElementType.Sprite) continue;
                if (!IsIngredientMarker(character.spriteIndex)) continue;

                int index = ingredientIndex;
                var requirement = recipe.items[index];
                ingredientIndex++;

                if (requirement == null) continue;

                if (hideSatisfied && items != null && index < items.Count && items[index] != null)
                    continue;

                var candidates = RecipeJumpService.FindCandidates(requirement);
                if (candidates.Count < 2) continue;

                Vector2 lineEnd = FindLineEnd(info, character.lineNumber);
                for (int k = 0; k < candidates.Count; k++)
                {
                    CreateIcon(text, candidates[k], lineEnd, k);
                }
            }
        }

        private static bool IsIngredientMarker(int spriteIndex)
        {
            return spriteIndex == 17 || spriteIndex == 23 || spriteIndex == 24;
        }

        private static Vector2 FindLineEnd(TMP_TextInfo info, int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= info.lineCount) return Vector2.zero;

            var line = info.lineInfo[lineNumber];
            int first = Mathf.Max(0, line.firstCharacterIndex);
            int last = Mathf.Min(line.lastCharacterIndex, info.characterCount - 1);

            float maxX = float.MinValue;
            float topY = 0f;
            bool found = false;

            for (int c = first; c <= last; c++)
            {
                var character = info.characterInfo[c];
                if (!character.isVisible) continue;

                if (!found)
                {
                    topY = character.topLeft.y;
                    found = true;
                }
                if (character.topRight.x > maxX) maxX = character.topRight.x;
            }

            return found ? new Vector2(maxX, topY) : Vector2.zero;
        }

        private static void CreateIcon(TextMeshProUGUI text, Recipe recipe, Vector2 lineEnd, int order)
        {
            var icon = Utils.Create("Special/RecipeItemPreview", text.transform);
            if (icon == null) return;

            var rect = icon.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(
                    lineEnd.x + IconStartOffset + order * IconSpacing,
                    lineEnd.y - IconYOffset);
            }

            if (icon.transform.childCount == 0) return;

            var image = icon.transform.GetChild(0).GetComponent<Image>();
            if (image == null) return;

            var sprite = recipe.resultSprite;
            if (sprite.Item1 == null) return;

            image.sprite = sprite.Item1;
            image.color = sprite.Item2;
            image.preserveAspect = true;
            image.raycastTarget = true;
            image.rectTransform.sizeDelta = PlayerCamera.ImageSizeDelta(sprite.Item1.texture, 4f, IconSize);

            var click = image.gameObject.AddComponent<RecipeJumpIconHandler>();
            click.recipeIndex = recipe.index;

            RecipeJumpService.RegisterIcon(icon);
        }
    }
}
