using xTile.Tiles;

namespace NermNermNerm.Warpinator;

public class WarpShop  : ModLet
{
    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        mod.Helper.Events.GameLoop.DayStarted += this.GameLoopOnDayStarted;

    }

    private void GameLoopOnDayStarted(object? sender, DayStartedEventArgs e)
    {
        var mountain = Game1.getLocationFromName("Mountain");
        var buildingsLayer = mountain.Map.GetLayer(I("Buildings"));

// Load your texture
        Texture2D tex = this.Mod.Helper.ModContent.Load<Texture2D>("assets/tollbooth.png");

// Create a tilesheet
        TileSheet sheet = new TileSheet(
            id: "NorvinSheet",          // the name you will reference later
            map: mountain.Map,
            imageSource: this.Mod.Helper.ModContent.GetInternalAssetName("assets/tollbooth.png").BaseName,
            sheetSize: new xTile.Dimensions.Size(tex.Width / 16, tex.Height / 16),
            tileSize: new xTile.Dimensions.Size(16, 16)
        );

// Add it to the map
        mountain.Map.AddTileSheet(sheet);

// VERY IMPORTANT: refresh the map’s internal tilesheet references
        mountain.Map.LoadTileSheets(Game1.mapDisplayDevice);

        int x = 84;
        int y = 20;
        int tileIndex = 0;
        for (int dy = 0; dy < 80 / 16; ++dy) // The png is 80px wide
        {
            for (int dx = 0; dx < 64 / 16; ++dx) // and 64px tall.
            {
                buildingsLayer.Tiles[x+dx, y+dy] = new StaticTile(buildingsLayer, sheet, BlendMode.Alpha, tileIndex++);
            }
        }

        /* Something from warpnetwork:

                                 Point tilePos = warp.Position;
           if (id == "farm")
           {
               Utils.TryGetActualFarmPoint(ref tilePos, map.Data, Name);
           }
           var spot = new Location(tilePos.X, tilePos.Y).Above;

           ModEntry.monitor.Log($"Adding access point for destination '{id}' @ {spot.X}, {spot.Y}");

           Tile tile = Buildings.Tiles[spot];
           if (tile is null)
               ModEntry.monitor.Log($"No tile in building layer, could not add access point: '{id}' @ {spot.X}, {spot.Y}", LogLevel.Warn);
           else
               tile.Properties["Action"] = "warpnetwork " + id;
*/
    }
}
