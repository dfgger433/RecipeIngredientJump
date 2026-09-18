using System;
using System.Text;
using HarmonyLib;

namespace CasualtiesUnknown.RecipeIngredientJump.Patches
{
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.IngredientTextForRecipe))]
    internal static class IngredientTextPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref string __result)
        {
            if (!RecipeIngredientJumpPlugin.Enabled.Value) return;
            if (string.IsNullOrEmpty(__result)) return;

            try
            {
                string[] lines = __result.Split('\n');
                var builder = new StringBuilder(__result.Length + lines.Length * 16);
                bool inLink = false;
                int ingredientIndex = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (IsIngredientMarkerLine(line))
                    {
                        if (inLink) builder.Append("</link>");
                        builder.Append("<link=\"").Append(ingredientIndex).Append("\">");
                        ingredientIndex++;
                        inLink = true;
                    }

                    builder.Append(line);
                    if (i < lines.Length - 1) builder.Append('\n');
                }

                if (inLink) builder.Append("</link>");

                __result = builder.ToString();
            }
            catch (Exception e)
            {
                if (RecipeIngredientJumpPlugin.Log != null)
                    RecipeIngredientJumpPlugin.Log.LogError($"材料文本添加链接失败: {e}");
            }
        }

        private static bool IsIngredientMarkerLine(string line)
        {
            return line.IndexOf("<sprite index=23>", StringComparison.Ordinal) >= 0
                || line.IndexOf("<sprite index=24>", StringComparison.Ordinal) >= 0
                || line.IndexOf("<sprite index=17", StringComparison.Ordinal) >= 0;
        }
    }
}
