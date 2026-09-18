using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CasualtiesUnknown.RecipeIngredientJump
{
    public class IngredientLinkClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                var text = GetComponent<TMP_Text>();
                if (text == null || text.textInfo == null) return;

                int linkIndex = TMP_TextUtilities.FindIntersectingLink(
                    text, eventData.position, eventData.pressEventCamera);
                if (linkIndex < 0) return;

                string linkId = text.textInfo.linkInfo[linkIndex].GetLinkID();
                if (!int.TryParse(linkId, out int ingredientIndex)) return;

                var cam = PlayerCamera.main;
                if (cam == null) return;

                RecipeJumpService.JumpToIngredient(cam, ingredientIndex);
            }
            catch (Exception e)
            {
                if (RecipeIngredientJumpPlugin.Log != null)
                    RecipeIngredientJumpPlugin.Log.LogError($"材料点击跳转失败: {e}");
            }
        }
    }
}
