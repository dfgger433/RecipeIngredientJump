using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CasualtiesUnknown.RecipeIngredientJump
{
    public class RecipeJumpIconHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public int recipeIndex = -1;

        public void OnPointerEnter(PointerEventData eventData)
        {
            try
            {
                RecipeJumpPreview.Show(recipeIndex, (RectTransform)transform);
            }
            catch (Exception e)
            {
                if (RecipeIngredientJumpPlugin.Log != null)
                    RecipeIngredientJumpPlugin.Log.LogError($"配方预览悬停失败: {e}");
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RecipeJumpPreview.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                RecipeJumpPreview.Hide();

                if (Recipes.recipes == null) return;
                if (recipeIndex < 0 || recipeIndex >= Recipes.recipes.Count) return;

                var cam = PlayerCamera.main;
                if (cam == null) return;

                RecipeJumpService.SelectRecipe(cam, Recipes.recipes[recipeIndex]);
            }
            catch (Exception e)
            {
                if (RecipeIngredientJumpPlugin.Log != null)
                    RecipeIngredientJumpPlugin.Log.LogError($"配方图标点击失败: {e}");
            }
        }
    }
}
