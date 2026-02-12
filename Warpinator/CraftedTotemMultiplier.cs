using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using StardewModdingAPI.Events;
using StardewValley;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Warpinator;

/// <summary>
/// This class manages the functionality surrounding how we make the crafting recipes for warp totems
/// more effective by multiplying their output.
/// </summary>
/// <remarks>
/// This naming is a bit confusing because all stock recipes (and probably most mod recipes) only craft
/// a single output.  We use the "multiplier" idea just in case some mod has a recipe that results in
/// more than one totem - we don't want to downgrade it.
/// </remarks>
public class CraftedTotemMultiplier : ModLet
{
    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
    }

    [EventPriority(EventPriority.Low-1)] // Causes us to load as late as possible, which we want because we're updating existing recipes.
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
        {
            if (ModEntry.Config.TotemRecipeYield > 1)
            {
                e.Edit(editor =>
                {
                    this.UpdateTotemRecipes(editor.AsDictionary<string, string>().Data);
                });
            }
        }
    }

    private static readonly Regex recipePattern =
        new(@"^[^/]+/[^/]+/(?<itemId>[^/ ]+)( (?<quantity>\d+))?/", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private void UpdateTotemRecipes(IDictionary<string, string> allRecipes)
    {
        Dictionary<string, string> replacedRecipes = new();
        foreach (var pair in allRecipes)
        {
            string recipeId = pair.Key;
            string recipeFormula = pair.Value;

            var match = recipePattern.Match(recipeFormula);
            if (!match.Success)
            {
                this.LogWarning($"Either I don't know recipes or this one is broken: {recipeId} {recipeFormula}");
                continue;
            }

            string yieldItemId = match.Groups["itemId"].Value;

            var item = ItemRegistry.Create(yieldItemId);
            if (item is null)
            {
                this.LogWarning($"Either I don't know recipes or this one has a bad yield item id: {recipeId} {recipeFormula}");
                continue;
            }

            if (item.GetContextTags().Contains("totem_item") && item.QualifiedItemId != "(O)681" /* rain totem */)
            {
                string yieldQuantityStr = match.Groups[I("quantity")].Value;
                int yieldQuantity = string.IsNullOrEmpty(yieldQuantityStr) ? 1 : int.Parse(yieldQuantityStr);
                int newYield = yieldQuantity * ModEntry.Config.TotemRecipeYield;

                replacedRecipes[recipeId] = recipeFormula.Substring(0, match.Groups["itemId"].Index)
                                            + yieldItemId + " " + newYield.ToString(CultureInfo.InvariantCulture)
                                            + "/" + recipeFormula.Substring(match.Length);
            }
        }

        foreach (var pair in replacedRecipes)
        {
            allRecipes[pair.Key] = pair.Value;
        }
    }
}
