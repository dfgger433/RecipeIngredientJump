# 材料跳转配方 (RecipeIngredientJump)

> 为《Casualties: Unknown》制作页添加材料跳转、配方预览与用途查询的 BepInEx 插件。

English: [README-en.md](README-en.md)

## 简介

本插件为游戏制作界面提供更顺手的配方导航：

- 点击材料直接跳转到「产出该材料的配方」
- 同一材料有多个配方时，悬停图标显示缩小版配方页
- 右侧「用途」按钮：查看当前配方产物可以用于哪些配方

插件版本：`1.0.5`　插件 GUID：`com.casualtiesunknown.recipeingredientjump`

## 功能与使用

### 1. 点击材料跳转配方

在制作页右侧的「需求」列表中，点击任意材料（整块文字，包括下方灰色说明行），详情面板会切换到产出该材料的配方。

- 支持具体材料（如「绳子」）与性质类需求（如「任意切割工具」「任意液体」，使用游戏内置示例物品查找配方）
- 原材料（没有任何可见配方产出它）点击后不跳转
- 只切换右侧详情，不会重建或滚动左侧配方列表
- 目标配方被搜索/分类筛选隐藏时也能直接跳转（左侧列表保持原样）

### 2. 多配方图标与悬停预览

当同一材料有多个配方可产出时，材料行右侧会出现对应数量的小图标：

- 悬停图标：在图标左侧弹出缩小版配方页（配方名、结果图标、材料需求、制作信息）
- 点击图标：跳转到该配方
- 直接点击材料行：跳转到第一个候选配方

### 3. 用途查询按钮（EMI 风格）

制作页右侧新增「用途」按钮（安装了 EMI 时自动排在其标签下方）：

- 点击展开用途面板，列出所有把「当前配方产物」当作材料的配方
- 面板内容跟随左侧选择的配方实时刷新
- 未解锁（INT 不足）的配方置灰且不可点击
- 点击可见条目：选中该配方并关闭面板
- 没有用途时显示「没有用途配方」

## 安装

1. 安装 **BepInEx 5.4.x（Mono）**
2. 将 `RecipeIngredientJump.dll` 放入：

```
BepInEx/plugins/RecipeIngredientJump/RecipeIngredientJump.dll
```

3. 启动游戏，日志出现 `材料跳转配方 v1.0.5 已加载` 即安装成功

## 配置

配置文件：`BepInEx/config/com.casualtiesunknown.recipeingredientjump.cfg`

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `Enabled` | `true` | 启用点击材料跳转配方 |
| `ShowMultiRecipeIcons` | `true` | 同一材料有多个配方时，在材料行右侧显示对应的配方图标 |
| `ShowRecipePreview` | `true` | 悬停多配方图标时显示缩小的配方预览卡片 |
| `PreviewScale` | `0.5` | 配方预览卡片的缩放比例（0.25–1.0） |
| `ShowUsesButton` | `true` | 在制作页右侧显示「用途」按钮 |
| `UsesButtonYOffset` | `-1` | 用途按钮相对面板右上角的 Y 偏移；`-1` 表示自动（检测到 EMI 时排在 EMI 标签下方） |

## 公开 API

其他 BepInEx 插件可以引用 `RecipeIngredientJump.dll`，通过 `CasualtiesUnknown.RecipeIngredientJump.RecipeQuery` 查询配方关系：

```csharp
using System.Collections.Generic;
using CasualtiesUnknown.RecipeIngredientJump;

// 产出该物品的配方
List<Recipe> producers = RecipeQuery.FindRecipesProducing("rope");

// 产出该液体的配方
List<Recipe> liquidProducers = RecipeQuery.FindRecipesProducing("water", isLiquid: true);

// 使用该物品的配方（用途）
List<Recipe> consumers = RecipeQuery.FindRecipesUsing("rope");

// 使用某个实际物品实例的配方（沿用游戏自身的判定逻辑）
List<Recipe> consumers2 = RecipeQuery.FindRecipesUsing(item);

// 带匹配详情的用途查询
List<RecipeUsage> usages = RecipeQuery.FindUsages("rope");
foreach (RecipeUsage usage in usages)
{
    // usage.Recipe     命中的配方
    // usage.MatchCount 该配方中使用此材料的次数
    // usage.ByQuality  是否通过性质需求匹配
}
```

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `isLiquid` | `false` | 查询对象是否为液体 |
| `includeHidden` | `FindRecipes*` 为 `false`，`FindUsages` 为 `true` | 是否包含未解锁（`visible == false`）的配方 |
| `includeRepairs` | `FindRecipesProducing` 为 `false`，其余为 `true` | 是否包含修理配方 |

## 兼容性

- **EMI for CU**：用途按钮自动排在 EMI 的「配方 / 合成树 / 图鉴」标签下方，不会重叠
- **QoL Unknown / ItemCountDisplay**：本插件只追加 TMP link 与自己创建的图标，不修改其它 mod 的对象
- **KrokMP 等联机 mod**：所有功能均为本地 UI 操作，不发送任何网络数据
- 跳转只调用游戏的 `PlayerCamera.SelectRecipe`，不会重建或滚动左侧配方列表

## 已知限制

- 材料跳转只会寻找当前已解锁（INT 门槛）的配方；未解锁配方不参与跳转
- 用途列表会列出未解锁配方，但置灰且不可点击
- 预览卡片、材料图标的位置常量按游戏默认分辨率调试，极端分辨率下如出现偏移可反馈调整
- 游戏更新导致方法签名变化时，Harmony 补丁会失效（不会导致游戏崩溃）

## 构建

需要：

- .NET SDK（支持编译 `net472`）
- 游戏目录 `CasualtiesUnknown_Data\Managed` 下的 DLL：
  `UnityEngine.dll`、`UnityEngine.CoreModule.dll`、`UnityEngine.UI.dll`、`UnityEngine.UIModule.dll`、
  `UnityEngine.TextRenderingModule.dll`、`Unity.TextMeshPro.dll`、`Assembly-CSharp.dll`
- `BepInEx\core` 下的 DLL：`BepInEx.dll`、`0Harmony.dll`

`RecipeIngredientJump.csproj` 中的 `GameRoot` 指向游戏根目录，默认值 `..\..`（仓库位于游戏目录的 `mods/RecipeIngredientJump/` 时可用）。编译：

```powershell
dotnet build -c Release
```

编译成功后会自动把 DLL 复制到 `BepInEx/plugins/RecipeIngredientJump/`。

## 鸣谢

本项目使用 AI 大语言模型协助编写。

代码：DeepSeek（`deepseek-v4-flash-vision-exp`）

按钮与列表风格参考：EMI for CU（exmeow）

## 许可证

[MIT](LICENSE)
