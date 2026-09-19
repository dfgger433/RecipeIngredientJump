using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CasualtiesUnknown.RecipeIngredientJump
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class RecipeIngredientJumpPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.casualtiesunknown.recipeingredientjump";
        public const string PluginName = "材料跳转配方";
        public const string PluginVersion = "1.0.6";

        public static RecipeIngredientJumpPlugin Instance;
        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> ShowMultiRecipeIcons;
        internal static ConfigEntry<bool> ShowRecipePreview;
        internal static ConfigEntry<float> PreviewScale;
        internal static ConfigEntry<bool> ShowUsesButton;
        internal static ConfigEntry<float> UsesButtonYOffset;
        internal static ConfigEntry<bool> HideCandidatesWhenSatisfied;

        private Harmony harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "启用制作页点击材料跳转到产出该材料的配方");
            ShowMultiRecipeIcons = Config.Bind("General", "ShowMultiRecipeIcons", true,
                "同一材料有多个配方时，在材料行右侧显示对应的配方图标");
            ShowRecipePreview = Config.Bind("General", "ShowRecipePreview", true,
                "悬停多配方图标时显示缩小的配方预览卡片");
            PreviewScale = Config.Bind("General", "PreviewScale", 0.5f,
                new ConfigDescription("配方预览卡片的缩放比例", new AcceptableValueRange<float>(0.25f, 1f)));
            ShowUsesButton = Config.Bind("General", "ShowUsesButton", true,
                "在制作页右侧显示「用途」按钮（列出使用当前配方产物的配方）");
            UsesButtonYOffset = Config.Bind("General", "UsesButtonYOffset", -1f,
                "用途按钮相对面板右上角的 Y 偏移；-1 表示自动（检测到 EMI 时排在 EMI 标签下方）");
            HideCandidatesWhenSatisfied = Config.Bind("General", "HideCandidatesWhenSatisfied", true,
                "材料行已满足（背包中有可用材料）时隐藏右侧的多配方候选图标；关闭则始终显示");

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            Logger.LogInfo($"{PluginName} v{PluginVersion} 已加载");
        }

        private void OnDestroy()
        {
            try
            {
                harmony?.UnpatchSelf();
            }
            catch
            {
            }
            RecipeJumpService.DestroyIcons();
            RecipeJumpPreview.DestroyCard();
            RecipeUsesUi.Destroy();
        }
    }
}
