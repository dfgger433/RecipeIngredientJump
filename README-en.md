# RecipeIngredientJump

> A BepInEx plugin for *Casualties: Unknown* that adds ingredient jumps, recipe previews and usage lookup to the crafting screen.

Chinese: [README.md](README.md)

## Overview

This plugin makes navigating recipes on the crafting screen much easier:

- Click an ingredient to jump to the recipe that produces it
- When an ingredient has multiple producer recipes, hover an icon to see a scaled-down recipe page
- A "USES" button on the right: see which recipes consume the result of the currently selected recipe

Version: `1.0.5`　Plugin GUID: `com.casualtiesunknown.recipeingredientjump`

## Features

### 1. Click an ingredient to jump to its recipe

In the requirements list on the crafting screen, click any ingredient (the whole text block, including the grey detail lines) and the detail panel switches to the recipe that produces it.

- Works for specific ingredients (e.g. `rope`) and quality requirements (e.g. "any cutting tool", "any liquid"; the game's built-in example item is used to find a recipe)
- Raw materials (no visible producer recipe) do nothing when clicked
- Only the detail panel is switched; the left recipe list is never rebuilt or scrolled
- Works even when the target recipe is hidden by the search/category filter (the left list stays as-is)

### 2. Multi-recipe icons and hover preview

When several recipes can produce the same ingredient, small icons appear to the right of the ingredient line:

- Hover: a scaled-down recipe page pops up on the left of the icon (name, result icon, requirements, crafting info)
- Click: jump to that recipe
- Clicking the ingredient line itself jumps to the first candidate recipe

### 3. Uses button (EMI style)

A "USES" button is added on the right side of the crafting panel (automatically placed below EMI's tabs when EMI is installed):

- Click to open the uses panel listing every recipe that consumes the currently selected recipe's result
- The panel follows the recipe selected on the left in real time
- Locked recipes (INT too low) are greyed out and not clickable
- Clicking a visible row selects that recipe and closes the panel
- Shows "NO USAGE RECIPES" when there is nothing to list

## Installation

1. Install **BepInEx 5.4.x (Mono)**
2. Place `RecipeIngredientJump.dll` at:

```
BepInEx/plugins/RecipeIngredientJump/RecipeIngredientJump.dll
```

3. Launch the game; the log line `材料跳转配方 v1.0.5 已加载` means it loaded successfully

## Configuration

Config file: `BepInEx/config/com.casualtiesunknown.recipeingredientjump.cfg`

| Option | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` | Enable clicking ingredients to jump to their recipe |
| `ShowMultiRecipeIcons` | `true` | Show recipe icons next to an ingredient when it has multiple producer recipes |
| `ShowRecipePreview` | `true` | Show the scaled-down recipe preview card when hovering a multi-recipe icon |
| `PreviewScale` | `0.5` | Scale of the preview card (0.25–1.0) |
| `ShowUsesButton` | `true` | Show the "USES" button on the crafting screen |
| `UsesButtonYOffset` | `-1` | Y offset of the uses button relative to the panel's top-right corner; `-1` = automatic (below EMI's tabs when EMI is present) |

## Public API

Other BepInEx plugins can reference `RecipeIngredientJump.dll` and use `CasualtiesUnknown.RecipeIngredientJump.RecipeQuery`:

```csharp
using System.Collections.Generic;
using CasualtiesUnknown.RecipeIngredientJump;

// Recipes that produce an item
List<Recipe> producers = RecipeQuery.FindRecipesProducing("rope");

// Recipes that produce a liquid
List<Recipe> liquidProducers = RecipeQuery.FindRecipesProducing("water", isLiquid: true);

// Recipes that consume an item (usages)
List<Recipe> consumers = RecipeQuery.FindRecipesUsing("rope");

// Recipes that consume a live Item instance (uses the game's own matching logic)
List<Recipe> consumers2 = RecipeQuery.FindRecipesUsing(item);

// Usage query with match details
List<RecipeUsage> usages = RecipeQuery.FindUsages("rope");
foreach (RecipeUsage usage in usages)
{
    // usage.Recipe     matched recipe
    // usage.MatchCount how many times the material is used in that recipe
    // usage.ByQuality  whether it matched through a quality requirement
}
```

| Parameter | Default | Description |
| --- | --- | --- |
| `isLiquid` | `false` | Whether the queried resource is a liquid |
| `includeHidden` | `false` for `FindRecipes*`, `true` for `FindUsages` | Include recipes that are not unlocked yet (`visible == false`) |
| `includeRepairs` | `false` for `FindRecipesProducing`, `true` otherwise | Include repair recipes |

## Compatibility

- **EMI for CU**: the uses button is automatically placed below EMI's "RECIPE / TREE / CATALOG" tabs, no overlap
- **QoL Unknown / ItemCountDisplay**: this plugin only appends TMP links and its own icons; it never modifies other mods' objects
- **KrokMP and other multiplayer mods**: everything is local UI only, no network traffic
- Jumps only call the game's `PlayerCamera.SelectRecipe`; the left recipe list is never rebuilt or scrolled

## Known limitations

- Ingredient jumps only search recipes that are currently unlocked (INT gate); locked recipes are not considered
- The uses list does show locked recipes, but they are greyed out and not clickable
- Preview card and icon position constants are tuned for the game's default resolution; feedback is welcome if they are offset at extreme resolutions
- If a game update changes method signatures, the Harmony patches will fail to apply (the game will not crash)

## Building

Requirements:

- .NET SDK (able to target `net472`)
- DLLs from `<Game>\CasualtiesUnknown_Data\Managed`:
  `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, `UnityEngine.UIModule.dll`,
  `UnityEngine.TextRenderingModule.dll`, `Unity.TextMeshPro.dll`, `Assembly-CSharp.dll`
- DLLs from `BepInEx\core`: `BepInEx.dll`, `0Harmony.dll`

`GameRoot` in `RecipeIngredientJump.csproj` points to the game root; the default `..\..` works when the repository is placed at `<Game>/mods/RecipeIngredientJump/`. Build with:

```powershell
dotnet build -c Release
```

On success the DLL is copied to `BepInEx/plugins/RecipeIngredientJump/` automatically.

## Credits

This project was developed with the assistance of an AI large language model.

Code: DeepSeek (`deepseek-v4-flash-vision-exp`)

Button and list style reference: EMI for CU (exmeow)

## License

[MIT](LICENSE)
