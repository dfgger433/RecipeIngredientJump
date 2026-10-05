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

                AddProducers(candidates, requirement.specificId, requirement.isLiquid);
                SortCandidates(candidates);

                return candidates;
            }

            if (requirement.quality == null) return candidates;

            string exampleId = FindExampleId(requirement.quality.id);
            if (!string.IsNullOrEmpty(exampleId))
            {
                AddProducers(candidates, exampleId, requirement.isLiquid);
            }

            foreach (var recipe in Recipes.recipes)
            {
                if (!IsEligible(recipe)) continue;
                if (candidates.Contains(recipe)) continue;
                if (!ResultMeetsQuality(recipe, requirement.quality, requirement.isLiquid)) continue;

                candidates.Add(recipe);
            }

            SortCandidates(candidates);

            return candidates;
        }

        // 候选排序：1) 现在就能制作 2) 缺材料但已拥有其中一部分 3) 一种材料都没有。
        // 同一档内普通配方优先，再按 INT 从低到高。
        private static void SortCandidates(List<Recipe> candidates)
        {
            if (candidates == null || candidates.Count <= 1) return;

            var entries = new List<CandidateEntry>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                entries.Add(new CandidateEntry
                {
                    Recipe = candidates[i],
                    Tier = GetCraftTier(candidates[i])
                });
            }

            entries.Sort(CompareCandidates);

            candidates.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                candidates.Add(entries[i].Recipe);
            }
        }

        private static int CompareCandidates(CandidateEntry left, CandidateEntry right)
        {
            if (left.Tier != right.Tier) return left.Tier.CompareTo(right.Tier);
            if (left.Recipe.isRepair != right.Recipe.isRepair) return left.Recipe.isRepair ? 1 : -1;
            return left.Recipe.INT.CompareTo(right.Recipe.INT);
        }

        // 0 = 可制作；1 = 缺材料但已有部分；2 = 没有任何材料
        private static int GetCraftTier(Recipe recipe)
        {
            if (recipe?.items == null) return 2;
            if (recipe.items.Count == 0) return 0;

            var matched = recipe.GetItemsForRecipeThorough();
            if (matched == null) return 2;

            int have = 0;
            for (int i = 0; i < matched.Count; i++)
            {
                if (matched[i] != null) have++;
            }

            if (have == 0) return 2;
            if (have >= recipe.items.Count) return 0;
            return 1;
        }

        private sealed class CandidateEntry
        {
            public Recipe Recipe;
            public int Tier;
        }

        // 普通配方优先；修理配方（如「电路板修复」）排在后面，
        // 这样只存在修理产出（例如电路板）时点击材料仍能跳转。
        private static void AddProducers(List<Recipe> target, string id, bool isLiquid)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                bool repairPass = pass == 1;

                foreach (var recipe in Recipes.recipes)
                {
                    if (!IsEligible(recipe)) continue;
                    if (recipe.isRepair != repairPass) continue;
                    if (recipe.result.id != id) continue;
                    if (recipe.result.isLiquid != isLiquid) continue;
                    if (target.Contains(recipe)) continue;

                    target.Add(recipe);
                }
            }
        }

        private static bool IsEligible(Recipe recipe)
        {
            if (recipe?.result == null) return false;
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
