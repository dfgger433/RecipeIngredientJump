using System.Collections.Generic;

namespace CasualtiesUnknown.RecipeIngredientJump
{
    public sealed class RecipeUsage
    {
        public Recipe Recipe;
        public int MatchCount;
        public bool ByQuality;
    }

    public static class RecipeQuery
    {
        public static List<Recipe> FindRecipesUsing(string id, bool isLiquid = false,
            bool includeHidden = false, bool includeRepairs = true)
        {
            var usages = FindUsages(id, isLiquid, includeHidden, includeRepairs);
            var recipes = new List<Recipe>(usages.Count);
            for (int i = 0; i < usages.Count; i++)
            {
                recipes.Add(usages[i].Recipe);
            }
            return recipes;
        }

        public static List<Recipe> FindRecipesUsing(Item item,
            bool includeHidden = false, bool includeRepairs = true)
        {
            var recipes = new List<Recipe>();
            if (item == null || Recipes.recipes == null) return recipes;

            foreach (var recipe in Recipes.recipes)
            {
                if (recipe?.items == null || recipe.result == null) continue;
                if (!includeHidden && !recipe.visible) continue;
                if (!includeRepairs && recipe.isRepair) continue;

                for (int i = 0; i < recipe.items.Count; i++)
                {
                    var requirement = recipe.items[i];
                    if (requirement != null && requirement.DoesUseItemType(item))
                    {
                        recipes.Add(recipe);
                        break;
                    }
                }
            }

            return recipes;
        }

        public static List<Recipe> FindRecipesProducing(string id, bool isLiquid = false,
            bool includeHidden = false, bool includeRepairs = false)
        {
            var recipes = new List<Recipe>();
            if (string.IsNullOrEmpty(id) || Recipes.recipes == null) return recipes;

            foreach (var recipe in Recipes.recipes)
            {
                if (recipe?.result == null) continue;
                if (recipe.result.id != id) continue;
                if (recipe.result.isLiquid != isLiquid) continue;
                if (!includeHidden && !recipe.visible) continue;
                if (!includeRepairs && recipe.isRepair) continue;

                recipes.Add(recipe);
            }

            return recipes;
        }

        public static List<RecipeUsage> FindUsages(string id, bool isLiquid = false,
            bool includeHidden = true, bool includeRepairs = true)
        {
            var usages = new List<RecipeUsage>();
            if (string.IsNullOrEmpty(id) || Recipes.recipes == null) return usages;

            foreach (var recipe in Recipes.recipes)
            {
                if (recipe?.items == null || recipe.result == null) continue;
                if (!includeHidden && !recipe.visible) continue;
                if (!includeRepairs && recipe.isRepair) continue;

                int matchCount = 0;
                bool byQuality = false;

                for (int i = 0; i < recipe.items.Count; i++)
                {
                    var requirement = recipe.items[i];
                    if (requirement == null || requirement.isLiquid != isLiquid) continue;
                    if (!isLiquid && !string.IsNullOrEmpty(requirement.ignoredId) &&
                        requirement.ignoredId == id)
                    {
                        continue;
                    }

                    if (requirement.specific)
                    {
                        if (requirement.specificId == id) matchCount++;
                    }
                    else if (requirement.quality != null &&
                             HasQuality(id, isLiquid, requirement.quality.id))
                    {
                        matchCount++;
                        byQuality = true;
                    }
                }

                if (matchCount > 0)
                {
                    usages.Add(new RecipeUsage
                    {
                        Recipe = recipe,
                        MatchCount = matchCount,
                        ByQuality = byQuality
                    });
                }
            }

            return usages;
        }

        private static bool HasQuality(string id, bool isLiquid, string qualityId)
        {
            if (string.IsNullOrEmpty(qualityId)) return false;

            if (isLiquid)
            {
                if (Liquids.Registry == null) return false;
                if (!Liquids.Registry.TryGetValue(id, out LiquidType liquid)) return false;
                if (liquid?.qualities == null) return false;

                for (int i = 0; i < liquid.qualities.Count; i++)
                {
                    var quality = liquid.qualities[i];
                    if (quality != null && quality.id == qualityId) return true;
                }
                return false;
            }

            if (Item.GlobalItems == null) return false;
            if (!Item.GlobalItems.TryGetValue(id, out ItemInfo info)) return false;
            if (info?.qualities == null) return false;

            for (int i = 0; i < info.qualities.Count; i++)
            {
                var quality = info.qualities[i];
                if (quality != null && quality.id == qualityId) return true;
            }
            return false;
        }
    }
}
