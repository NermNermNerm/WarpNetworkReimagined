using xTile.Layers;
using xTile.ObjectModel;
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
        var frontLayer = mountain.Map.GetLayer(I("Front"));
        var backLayer = mountain.Map.GetLayer(I("Back"));

// Load your texture
        Texture2D tex = this.Mod.Helper.ModContent.Load<Texture2D>("assets/tollbooth.png");

// Create a tilesheet
        TileSheet sheet = new TileSheet(
            id: "NorvinSheet",
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
        for (int dy = 0; dy < 80 / 16; ++dy) // The png is 80px tall
        {
            for (int dx = 0; dx < 64 / 16; ++dx) // and 64px wide.
            {
                // Clear any existing stuff.
                Vector2 xy = new Vector2(x + dx, y + dy);
                // Remove terrain features (trees, bushes, grass, hoe dirt, etc.)
                mountain.terrainFeatures.Remove(xy);

                // Remove objects (seeds, debris, placed items)
                mountain.objects.Remove(xy);

                // Remove large terrain features (stumps, logs, boulders)
                for (int i = mountain.largeTerrainFeatures.Count - 1; i >= 0; i--)
                {
                    var ltf = mountain.largeTerrainFeatures[i];

                    if (ltf.getBoundingBox().Intersects(new Rectangle((x + dx) * 64, (y + dy) * 64, 64, 64)))
                        mountain.largeTerrainFeatures.RemoveAt(i);
                }

                // Prevent future tree growth.
                var backTile = backLayer.Tiles[x + dx, y + dy];
                if (backTile is null)
                {
                    var backTileSheet = mountain.Map.GetTileSheet("spring_outdoorsTileSheet") ?? mountain.Map.TileSheets.First();
                    backTile = new StaticTile( backLayer, backTileSheet, BlendMode.Alpha, 0 /* doesn't matter */);
                    backLayer.Tiles[x, y] = backTile;
                }
                backTile.Properties[I("Diggable")] = new xTile.ObjectModel.PropertyValue(I("F"));
                // backTile.Properties[I("Passable")] = new xTile.ObjectModel.PropertyValue(dy <= 2 ? I("T") : I("F"));

                // if (dy < 3)
                // {
                //     sheet.TileIndexProperties[tileIndex][I("Passable")] = new PropertyValue(I("T"));
                // } // else it defaults to impassable

                (dy < 3 ? frontLayer : buildingsLayer).Tiles[x+dx, y+dy] = new StaticTile(buildingsLayer, sheet, BlendMode.Alpha, tileIndex++);
                // buildingsLayer.Tiles[x+dx, y+dy].Properties[I("Passable")] = new xTile.ObjectModel.PropertyValue(dy <= 2 ? I("T") : I("F"));
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
