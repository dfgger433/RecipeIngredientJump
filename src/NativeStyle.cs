using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualtiesUnknown.RecipeIngredientJump
{
    /// <summary>
    /// 游戏原生 UI 风格的尺寸 / 颜色 / 贴图解析（自实现，不依赖 NativeUILib）。
    /// 数值规范参考 NativeUILib 的 NativeTheme：以 1080p 为基准，统一乘 Scale。
    /// </summary>
    internal static class NativeStyle
    {
        // 尺寸基准（1080p；与游戏 GlobalDark.uiScale = Screen.height / 1080f 一致）
        public const float TitleHeight = 30f;
        public const float Padding = 10f;
        public const float Spacing = 6f;
        public const float CardWidth = 460f;

        private static int uiLayer = int.MinValue;
        private static Dictionary<string, Sprite> spriteIndex;
        private static Sprite panelSprite;
        private static Sprite nanoSprite;

        public static float Scale => Mathf.Max(0.5f, Screen.height / 1080f);

        public static float Scaled(float value) => value * Scale;

        public static int FontSize(float mult = 1f) => Mathf.Max(6, Mathf.RoundToInt(20f * Scale * mult));

        public static Color TextColor => Color.white;
        public static Color TextDim => new Color(0.9f, 0.9f, 0.9f, 1f);
        public static Color Accent => new Color(1f, 0.765f, 0f, 1f);
        public static Color PanelColor => new Color(1f, 1f, 1f, 0.92f);
        public static Color TitleBar => new Color(1f, 1f, 1f, 0.85f);

        /// <summary>小面板九宫格（uiBlockSmall，回退 uiBlock / uiBlockNano / PlayerCamera.uiNano）。</summary>
        public static Sprite PanelSprite
        {
            get
            {
                if (panelSprite != null) return panelSprite;
                panelSprite = FirstOf("uiBlockSmall", "uiBlock", "uiBlockNano") ?? UiNano();
                return panelSprite;
            }
        }

        /// <summary>最小面板九宫格（uiBlockNano，回退 uiBlockSmall / uiBlock / PlayerCamera.uiNano）。</summary>
        public static Sprite NanoSprite
        {
            get
            {
                if (nanoSprite != null) return nanoSprite;
                nanoSprite = FirstOf("uiBlockNano", "uiBlockSmall", "uiBlock") ?? UiNano();
                return nanoSprite;
            }
        }

        /// <summary>按名字找游戏原生 Sprite（精确优先，其次包含匹配）。找不到返回 null。</summary>
        public static Sprite Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var sprite = Lookup(EnsureIndex(), name);
            if (sprite != null) return sprite;

            // 场景切换后旧贴图会被销毁：重建一次索引再试
            spriteIndex = BuildIndex();
            return Lookup(spriteIndex, name);
        }

        private static Sprite FirstOf(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var sprite = Find(names[i]);
                if (sprite != null) return sprite;
            }
            return null;
        }

        private static Sprite UiNano()
        {
            var cam = PlayerCamera.main;
            return cam != null ? cam.uiNano : null;
        }

        private static Sprite Lookup(Dictionary<string, Sprite> index, string name)
        {
            if (index == null || index.Count == 0) return null;

            if (index.TryGetValue(name, out Sprite exact) && exact != null) return exact;

            foreach (var pair in index)
            {
                if (pair.Value == null) continue;
                if (pair.Key.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return pair.Value;
            }

            return null;
        }

        private static Dictionary<string, Sprite> EnsureIndex()
        {
            if (spriteIndex != null && spriteIndex.Count > 0) return spriteIndex;
            spriteIndex = BuildIndex();
            return spriteIndex;
        }

        private static Dictionary<string, Sprite> BuildIndex()
        {
            var index = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            try
            {
                var all = Resources.FindObjectsOfTypeAll<Sprite>();
                for (int i = 0; i < all.Length; i++)
                {
                    var sprite = all[i];
                    if (sprite == null) continue;

                    string key = sprite.name;
                    if (string.IsNullOrEmpty(key)) continue;
                    if (!index.ContainsKey(key)) index[key] = sprite;
                }
            }
            catch (Exception e)
            {
                if (RecipeIngredientJumpPlugin.Log != null)
                    RecipeIngredientJumpPlugin.Log.LogWarning($"原生贴图索引构建失败: {e.Message}");
            }

            return index;
        }

        public static void SetUiLayer(GameObject target)
        {
            if (target == null) return;
            if (uiLayer == int.MinValue) uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) target.layer = uiLayer;
        }

        public static Image Panel(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            SetUiLayer(go);
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, int fontSize,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SetUiLayer(go);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = TextColor;
            text.richText = true;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            // 像素字体很可能没有 '…' 字形，Ellipsis 会整段不显示；这里统一用 Truncate/Overflow
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }
    }
}
