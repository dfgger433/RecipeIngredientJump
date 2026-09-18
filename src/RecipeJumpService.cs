using System;
using System.Collections.Generic;
using UnityEngine;

namespace CasualtiesUnknown.RecipeIngredientJump
{
    internal static class RecipeJumpService
    {
        private static readonly List<GameObject> jumpIcons = new List<GameObject>();

        internal static Recipe GetSelectedRecipe(PlayerCamera cam)
        {
            if (cam == null || Recipes.recipes == null) return null;

            int index = cam.selectedRecipe;
            if (index < 0 || index >= Recipes.recipes.Count) return null;

            return Recipes.recipes[index];
        }

        internal static List<Recipe> FindCandidates(RecipeItem requirement)
        {
            var candidates = new List<Recipe>();
            if (requirement == null || Recipes.recipes == null) return candidates;

            if (requirement.specific)
            {
                if (string.IsNullOrEmpty(requirement.specificId)) return candidates;

                foreach (var recipe in Recipes.recipes)
                {
                    if (!IsEligible(recipe)) continue;
                    if (recipe.result.id != requirement.specificId) continue;
                    if (recipe.result.isLiquid != requirement.isLiquid) continue;

                    candidates.Add(recipe);
                }

                return candidates;
            }

            if (requirement.quality == null) return candidates;

            string exampleId = FindExampleId(requirement.quality.id);
            if (!string.IsNullOrEmpty(exampleId))
            {
                foreach (var recipe in Recipes.recipes)
                {
                    if (!IsEligible(recipe)) continue;
                    if (recipe.result.id != exampleId) continue;
                    if (recipe.result.isLiquid != requirement.isLiquid) continue;

                    candidates.Add(recipe);
                }
            }

            var others = new List<Recipe>();
            foreach (var recipe in Recipes.recipes)
            {
                if (!IsEligible(recipe)) continue;
                if (candidates.Contains(recipe)) continue;
                if (!ResultMeetsQuality(recipe, requirement.quality, requirement.isLiquid)) continue;

                others.Add(recipe);
            }

            others.Sort((a, b) => a.INT.CompareTo(b.INT));
            candidates.AddRange(others);

            return candidates;
        }

        private static bool IsEligible(Recipe recipe)
        {
            if (recipe?.result == null) return false;
            if (recipe.isRepair) return false;
            if (!recipe.visible) return false;
            return true;
        }

        private static string FindExampleId(string qualityId)
        {
            if (Recipes.QualityExamples == null) return null;

            foreach (var pair in Recipes.QualityExamples)
            {
                if (pair.Key != null && pair.Key.id == qualityId) return pair.Value;
            }

            return null;
        }

        private static bool ResultMeetsQuality(Recipe recipe, CraftingQuality requirement, bool requirementIsLiquid)
        {
            if (recipe.result.isLiquid != requirementIsLiquid) return false;

            if (requirementIsLiquid)
            {
                if (Liquids.Registry == null) return false;
                if (!Liquids.Registry.TryGetValue(recipe.result.id, out LiquidType liquidType)) return false;
                if (liquidType?.qualities == null) return false;

                float volume = recipe.result.resultCondition;
                if (volume <= 0f) return false;

                for (int i = 0; i < liquidType.qualities.Count; i++)
                {
                    var quality = liquidType.qualities[i];
                    if (quality != null && quality.id == requirement.id &&
                        quality.amount * volume >= requirement.amount)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (Item.GlobalItems == null) return false;
            if (!Item.GlobalItems.TryGetValue(recipe.result.id, out ItemInfo info)) return false;
            if (info?.qualities == null) return false;

            for (int i = 0; i < info.qualities.Count; i++)
            {
                var quality = info.qualities[i];
                if (quality != null && quality.id == requirement.id &&
                    quality.amount >= requirement.amount)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void JumpToIngredient(PlayerCamera cam, int ingredientIndex)
        {
            var recipe = GetSelectedRecipe(cam);
            if (recipe?.items == null) return;
            if (ingredientIndex < 0 || ingredientIndex >= recipe.items.Count) return;

            var candidates = FindCandidates(recipe.items[ingredientIndex]);
            if (candidates.Count == 0) return;

            SelectRecipe(cam, candidates[0]);
        }

        internal static void SelectRecipe(PlayerCamera cam, Recipe target)
        {
            if (cam == null || target == null) return;

            try
            {
                cam.SelectRecipe(target.index);
            }
            catch (Exception e)
            {
                if (RecipeIngredientJumpPlugin.Log != null)
                    RecipeIngredientJumpPlugin.Log.LogError($"跳转配方失败: {e}");
            }
        }

        internal static void RegisterIcon(GameObject icon)
        {
            if (icon != null) jumpIcons.Add(icon);
        }

        internal static void DestroyIcons()
        {
            RecipeJumpPreview.Hide();

            for (int i = 0; i < jumpIcons.Count; i++)
            {
                if (jumpIcons[i] != null) UnityEngine.Object.Destroy(jumpIcons[i]);
            }
            jumpIcons.Clear();
        }
    }
}
