using StardewModdingAPI;
using StardewModdingAPI.Events;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Warpinator;

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
            name: () => L("Ten-minute intervals eaten by warping home"),
            getValue: () => ModEntry.Config.WarpHomeTimeCost,
            setValue: value => ModEntry.Config.WarpHomeTimeCost = value,
            tooltip: () => L("The number of 10-minute intervals that spin by when using the initial version of the tool - e.g. 3 would make warping home take 30 minutes.")
        );
    }
}
