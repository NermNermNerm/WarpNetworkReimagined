using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;
using Warpinator;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Warpinator;

public class ModEntry
    : Mod, ISimpleLog
{
    public const string MarionBerryToolId = "NermNermNerm.Warpinator.MarionBerry";
    public const string MarionBerryToolQiid = ItemRegistry.type_tool + ModEntry.MarionBerryToolId;

    public const string TwoTileSpritesPseudoPath = "Mods/NermNermNerm/Warpinator/Sprites";
    public const string OneTileSpritesPseudoPath = "Mods/NermNermNerm/Warpinator/1x1Sprites";

    public readonly Powers Powers = new Powers();

    public static ModEntry Instance = null!;

    public static ModConfig Config = null!;
    public ModConfigMenu ConfigMenu = new ModConfigMenu();

    public Harmony Harmony = null!;

    public readonly TotemInventory TotemInventory = new();
    public readonly CraftedTotemMultiplier CraftedTotemMultiplier = new();

    public ModEntry() { }

    public override void Entry(IModHelper helper)
    {
        Instance = this;

        Initialize(this);
        this.Helper.Events.Content.LocaleChanged += (_, _) => this.Helper.GameContent.InvalidateCache("Data/Objects");

        this.Harmony = new Harmony(this.ModManifest.UniqueID);

        Config = this.Helper.ReadConfig<ModConfig>();
        this.ConfigMenu.Entry(this);
        this.Powers.Entry(this);

        Patches.Patch(this);

        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

        this.TotemInventory.Entry(this);
        this.CraftedTotemMultiplier.Entry(this);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(OneTileSpritesPseudoPath))
        {
            e.LoadFromModFile<Texture2D>("assets/1x1_Sprites.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
        {
            e.Edit(editor =>
            {
                EditToolAssets(editor.AsDictionary<string, ToolData>().Data);
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(TwoTileSpritesPseudoPath))
        {
            e.LoadFromModFile<Texture2D>("assets/Sprites.png", AssetLoadPriority.Exclusive);
        }

        // else if (e.NameWithoutLocale.StartsWith("Characters/Dialogue/"))
        // {
        //     e.Edit(editor =>
        //     {
        //         ConversationKeys.EditAssets(e.NameWithoutLocale, editor.AsDictionary<string, string>().Data);
        //     });
        // }
    }


    internal static void EditToolAssets(IDictionary<string, ToolData> data)
    {
        data[ModEntry.MarionBerryToolId] = new ToolData
        {
            ClassName = I("Wand"),
            Name = I("Warp Home Gadget"),
            AttachmentSlots = 0,
            SalePrice = 0,
            DisplayName = L("teleporter"),
            Description = L("Norvin the bridge troll's old Marionberry(tm) teleporter."),
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 7,
            MenuSpriteIndex = -1,
            UpgradeLevel = 0,
            ConventionalUpgradeFrom = null,
            UpgradeFrom = null,
            CanBeLostOnDeath = false,
            SetProperties = null,
        };
    }

    public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
    {
        if (isOnceOnly)
        {
            this.Monitor.LogOnce(message, level);
        }
        else
        {
            this.Monitor.Log(message, level);
        }
    }

    public bool IsWandUseBlocked => Game1.eventUp
                                 || Game1.isFestival()
                                 || Game1.fadeToBlack
                                 || Game1.player.swimming.Value
                                 || Game1.player.bathingClothes.Value
                                 || Game1.player.onBridge.Value;

    public void UseWandWithMenu()
    {
        var totems = this.TotemInventory.GetTotemInventory();
        if (!totems.Any(t => t.QualifiedItemId == I("(O)688")))
        {
            var warpTotemFarm = ItemRegistry.Create<StardewValley.Object>("(O)688");
            warpTotemFarm.Stack = 0; // Because you can't actually do this with Create
            totems.Add(warpTotemFarm);
        }

        totems.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCulture));
        Game1.activeClickableMenu = new WarpMenu(this, totems);
    }

    public void UseWandInNoTotemMode()
    {
        int numMinutesToPass = ModEntry.Config.WarpHomeTimeCost * 10;
        int newTime = Utility.ModifyTime(Game1.timeOfDay, numMinutesToPass);
        if (newTime > 2600)
        {
            Game1.addHUDMessage(new HUDMessage(L("It's too late to use the marionberry now")));
            return;
        }

        var house = Utility.getHomeOfFarmer(Game1.player);
        // TODO: ^^ Probably not what the people really want - they want to go to the home with the bed that
        //   they woke up in, not their cabin.
        var tile = house.warps.Where(w => w.TargetName == I("Farm")).Select(w => new Point(w.X, w.Y)).First();

        this.DoWarpEffects(() =>
        {
             Game1.warpFarmer(house.Name, tile.X, tile.Y, false);
             SafelySetTime(newTime);
        }, Game1.player, Game1.player.currentLocation);
    }

    private void DoWarpEffects(Action action, Farmer who, GameLocation where)
    {
        for (int index = 0; index < 12; ++index)
            Game1.Multiplayer.broadcastSprites(where, new TemporaryAnimatedSprite(
                354,
                Game1.random.Next(25, 75), 6, 1,
                new Vector2(
                    Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192),
                    Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)),
                false,
                Game1.random.NextDouble() < 0.5)
            );
        Game1.playSound("wand");
        Game1.displayFarmer = false;
        who.temporarilyInvincible = true;
        who.temporaryInvincibilityTimer = -2000;
        who.freezePause = 1000;
        Game1.flashAlpha = 1f;
        int num = 0;
        var tile = who.TilePoint;
        for (int index = tile.X + 8; index >= tile.X - 8; --index)
        {
            Game1.Multiplayer.broadcastSprites(where, new TemporaryAnimatedSprite(6, new Vector2(index, tile.Y) * 64f, Color.White, 8, false, 50f, 0, -1, -1f, -1, 0)
            {
                layerDepth = 1f,
                delayBeforeAnimationStart = num * 25,
                motion = new Vector2(-0.25f, 0.0f)
            });
            ++num;
        }

        DelayedAction.fadeAfterDelay(new Game1.afterFadeFunction(() =>
        {
            action();
            Game1.changeMusicTrack(I("none"));
            Game1.fadeToBlackAlpha = 0.99f;
            Game1.screenGlow = false;
            Game1.player.temporarilyInvincible = false;
            Game1.player.temporaryInvincibilityTimer = 0;
            Game1.displayFarmer = true;
        }), 1000);
    }


    // LIFTED FROM: https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI.Mods.ConsoleCommands/Framework/Commands/World/SetTimeCommand.cs#L58
    /// <summary>Safely transition to the given time, allowing NPCs to update their schedule.</summary>
    /// <param name="time">The time of day.</param>
    public static void SafelySetTime(int time)
    {
        // transition to new time
        int intervals = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, time) / 10;
        if (intervals > 0)
        {
            for (int i = 0; i < intervals; i++)
                Game1.performTenMinuteClockUpdate();
        }
        else if (intervals < 0)
        {
            for (int i = 0; i > intervals; i--)
            {
                Game1.timeOfDay = Utility.ModifyTime(Game1.timeOfDay, -20); // offset 20 mins so game updates to next interval
                Game1.performTenMinuteClockUpdate();
            }
        }

        // reset ambient light
        // White is the default non-raining color. If it's raining or dark out, UpdateGameClock
        // below will update it automatically.
        Game1.outdoorLight = Color.White;
        Game1.ambientLight = Color.White;

        // run clock update (to correct lighting, etc)
        Game1.gameTimeInterval = 0;
        Game1.UpdateGameClock(Game1.currentGameTime);
    }
}
