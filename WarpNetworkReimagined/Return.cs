namespace WarpNetworkReimagined;

/// <summary>
///   Manages stuff to do with the 'Return' feature of the wand
/// </summary>
public class Return : ModLet
{
    private const string ModDataKey = "WarpNetworkReimagined.ReturnDestination";

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        mod.Helper.Events.GameLoop.DayEnding += this.OnDayEnding;
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        Game1.player.modData.Remove(Return.ModDataKey);
    }

    public WarpDestination ReturnDestination
    {
        get
        {
            if (Game1.player.modData.TryGetValue(Return.ModDataKey, out string asString))
            {
                string[] parts = asString.Split(",", 3);
                if (parts.Length == 3 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    var location = Game1.getLocationFromName(parts[2]);
                    if (location is not null)
                    {
                        return new WarpDestination(location, null, null, new Point(x, y));
                    }
                }
            }

            return new WarpDestination(null, null, null, null);
        }
        private set
        {
            Game1.player.modData[Return.ModDataKey] = IF($"{value.TargetTile!.Value.X},{value.TargetTile!.Value.Y},{value.Target!.Name}");
        }
    }

    public void SetReturnPoint()
    {
        this.ReturnDestination = new WarpDestination(Game1.currentLocation, null, null, Game1.player.Tile.ToPoint());
    }

    public void DoWarp()
    {
        var d = this.ReturnDestination;
        Game1.player.modData.Remove(Return.ModDataKey);
        this.Mod.SlowWarp.DoWarpWithEffects(d.Target!, d.TargetTile!.Value, null);
    }
}
