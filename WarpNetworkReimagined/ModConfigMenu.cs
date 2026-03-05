namespace WarpNetworkReimagined;

public class ModConfigMenu
{
    private ModEntry mod = null!;

    public void Entry(ModEntry mod)
    {
        this.mod = mod;

        mod.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // get Generic Mod Config Menu's API (if it's installed)
        var configMenu = this.mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        // register mod configs
        configMenu.Register(
            mod: this.mod.ModManifest,
            reset: () => ModEntry.Config = new ModConfig(),
            save: () => this.mod.Helper.WriteConfig(ModEntry.Config)
        );

        configMenu.AddNumberOption(
            mod: this.mod.ModManifest,
            name: () => L("Slow-Warp cost:"),
            getValue: () => ModEntry.Config.SlowWarpTimeCost,
            setValue: value => ModEntry.Config.SlowWarpTimeCost = value,
            tooltip: () => L("The number of 10-minute intervals that spin by when using the totemless 'slow-warp' mode.   6 would make warping home take 60 minutes."),
            min: 0,
            max: 6
        );

        configMenu.AddNumberOption(
            mod: this.mod.ModManifest,
            name: () => L("Faster Slow-Warp cost:"),
            getValue: () => ModEntry.Config.FastWarpTimeCost,
            setValue: value => ModEntry.Config.FastWarpTimeCost = value,
            tooltip: () => L("The number of 10-minute intervals that spin by when using the 'slow-warp' mode after purchasing the upgrade.  3 would make warping home take 30 minutes."),
            min: 0,
            max: 6
        );

        configMenu.AddNumberOption(
            mod: this.mod.ModManifest,
            name: () => L("Crafted Totem Yield:"),
            getValue: () => ModEntry.Config.TotemRecipeYield,
            setValue: value =>
            {
                ModEntry.Config.TotemRecipeYield = value;
                this.mod.Helper.GameContent.InvalidateCache("Data/CraftingRecipes");
            },
            tooltip: () => L("Modifies all totem crafting recipes to produce this many totems - e.g. 2 would double the number of totems you get per craft."),
            min: 1,
            max: 5
        );
    }
}
