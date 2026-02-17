using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NermNermNerm.Warpinator;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;
using xTile.Dimensions;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

// ADAPTED FROM:  https://github.com/tlitookilakin/WarpNetwork/blob/master/WarpNetwork/ui/WarpMenu.cs

namespace Warpinator;

class WarpMenu : IClickableMenu
{
	const int buttonH = 72;

	private readonly Rectangle panel = new(384, 373, 18, 18);
	private readonly string title;
	private readonly int titleW;
	private static readonly Color shadow = Color.Black * 0.33f;

	private readonly List<WarpButton> buttons = new();
	private readonly ClickableTextureComponent upArrow;
	private readonly ClickableTextureComponent downArrow;
	private int index = 0;
	private readonly bool autoAlign = false;
	private Rectangle mainPanel = new(0, 27, 0, 0);

    /// <summary>
    ///   A destination for warping.
    /// </summary>
    /// <param name="target">
    /// If known, the target of the warp.  It will only be null if <paramref name="totem"/> is not null
    /// and its destination can't be inferred from the name.</param>
    /// <param name="totem">
    /// If not null, the target is reached via a warp totem in the player's wallet.  Else travel will
    /// be via slow warp or obelisk.
    /// </param>
    /// <param name="obeliskWarpCode">
    /// If not null, <paramref name="totem"/> will be null and travel should be accomplished by running
    /// <code>who.currentLocation.performAction(text, who, new Location((int) tileLocation.X, (int) tileLocation.Y)</code>
    /// on the text.
    /// If null and <paramref name="totem"/> is null, then travel should be accomplished via slow-warp.
    /// </param>
    public record Destination(GameLocation? target, StardewValley.Object? totem, string? obeliskWarpCode);

    private readonly List<Destination> destinations;

    private readonly ModEntry mod;

	// internal static Texture2D defaultIcon;

    private static Regex obeliskDefaultActionPattern =
        new Regex(@"^\s*ObeliskWarp\s+(?<destinationLocation>[^\s]+) ", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	public WarpMenu(ModEntry mod, IReadOnlyList<StardewValley.Object> totems, int x = 0, int y = 0, int width = 0, int height = 0)
        : base(x, y, width, height, true)
    {
        this.mod = mod;
        this.destinations = new();

        List<string> obeliskLocationNames = new();
        if (mod.IsObeliskUseEnabled)
        {
            // This is pretty much a fallback, in case the user hasn't set a warp spot for the farm.
            var frontDoorSpot = Utility.getHomeOfFarmer(Game1.player).getFrontDoorSpot();
            obeliskLocationNames.Add(I("Farm"));
            this.destinations.Add(new Destination(
                    Game1.getFarm(),
                    null,
                    IF($"ObeliskWarp Farm {frontDoorSpot.X} {frontDoorSpot.Y} false")
                ));
            foreach (var building in Game1.getFarm().buildings)
            {
                var data = building.GetData();
                if (data is null) continue;

                string? defaultAction = data.DefaultAction;
                if (defaultAction is not null)
                {
                    defaultAction = TokenParser.ParseText(defaultAction); // I can't imagine how this would do anything for obelisks, but the base game does it for all buildings.
                    Match m = WarpMenu.obeliskDefaultActionPattern.Match(defaultAction);
                    if (m.Success)
                    {
                        string locationName = m.Groups[I("target")].Value!;

                        GameLocation? targetLocation = Game1.getLocationFromName(locationName);
                        if (targetLocation is null)
                        {
                            mod.LogWarning($"Found a building on the farm that looks like an obelisk, '{data.Name}', but it's targetting an unknown location, '{locationName}'.");
                        }
                        else if (!obeliskLocationNames.Contains(locationName))
                        {
                            obeliskLocationNames.Add(locationName);
                            this.destinations.Add(new Destination(targetLocation, null, defaultAction));
                        }
                    }
                    // else it's not an obelisk
                }
                // else it's not an obelisk
            }
        }

        foreach (var location in mod.SlowWarpDestinations)
        {
            if (!obeliskLocationNames.Contains(location.Name))
            {
                this.destinations.Add(new Destination(location, null, null));
            }
        }

        foreach (var totem in totems)
        {
            var inferredLocation = this.FindLocationForTotem(totem);
            // Add the totem as a way to get to the destination unless there is already an obelisk method.
            if (!obeliskLocationNames.Any(o => o == inferredLocation?.Name))
            {
                this.destinations.Add(new Destination(inferredLocation, totem, null));
            }
        }

		if (this.destinations.Count < 1)
		{
			this.mod.LogWarning($"Warp menu created with no destinations!");
			this.exitThisMenuNoSound();
		}

        int destinationComparer(Destination lhs, Destination rhs)
        {
            if (lhs.target is null || rhs.target is null)
            {
                // Unknown totems go first.
                if (rhs.target is not null)
                {
                    return -1;
                }
                if (lhs.target is not null)
                {
                    return 1;
                }

                // totem must be non-null because target is null.
                return string.Compare(lhs.totem!.DisplayName, rhs.totem!.DisplayName, StringComparison.CurrentCulture);
            }

            int locationComparisonResult = string.Compare(lhs.target.DisplayName, rhs.target.DisplayName, StringComparison.CurrentCulture);
            if (locationComparisonResult != 0)
            {
                return locationComparisonResult;
            }

            // The only way we get here is for cases where we have both a totem and a slow-warp way to reach a
            // destination, we'll put the slow-warp case first.  (That is, exactlyl one of lhs and rhs must have
            // a non-null totem value).
            return lhs.totem is null ? -1 : 1;
        }
        this.destinations.Sort(destinationComparer);

        this.title = L("Choose Destination");
        var titleSize = Game1.dialogueFont.MeasureString(this.title);

		// Try and make the dialog so that it fits as neatly as it can.  The "+1" after locs.Count accounts for the title.
		// The *1.5 is a spitball estimate that accounts for the padding and margins.
		double estimatedHeight = Math.Max(Math.Min(this.destinations.Count+1, 9), 4) * titleSize.Y * 1.5;

        this.autoAlign = x == 0 && y == 0;
		this.width = width != 0 ? width : 600;
		this.height = height != 0 ? height : (int)estimatedHeight;
		this.titleW = (int)titleSize.X + 33 + 36;
		this.upArrow = new(new Rectangle(0, 0, 33, 36), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 3f);
		this.downArrow = new(new Rectangle(0, 0, 33, 36), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 3f);
		// defaultIcon = ModEntry.helper.GameContent.Load<Texture2D>(ModEntry.AssetPath + "/Icons/DEFAULT");

		if (this.autoAlign)
			this.align();

		this.resized();

		if (Game1.options.SnappyMenus)
			this.snapToDefaultClickableComponent();
	}

    private static readonly Regex totemNameRegex = new Regex("^Warp Totem: (?<target>.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private GameLocation? FindLocationForTotem(StardewValley.Object totem)
    {
        switch (totem.QualifiedItemId)
        {
            case "(O)688": return Game1.getFarm();
            case "(O)689": return Game1.getLocationFromName("mountain");
            case "(O)690": return Game1.getLocationFromName("beach");
            case "(O)261": return Game1.getLocationFromName("desert");
            case "(O)886": return Game1.getLocationFromName("islandsouth");
        }

        GameLocation? location = null;
        if (totem.modData.TryGetValue("NermNermNerm.Warpinator.Target", out string targetValue))
        {
            location = Game1.getLocationFromName(targetValue);
            if (location is null)
            {
                this.mod.LogErrorOnce($"The author of the mod that inserted {totem.QualifiedItemId} set NermNermNerm.Warpinator.Target, but the value, '{targetValue}', doesn't seem to be a location name.");
            }
        }

        // The only way to do this that I can figure is to look at the name.
        Match m = WarpMenu.totemNameRegex.Match(totem.Name);
        if (m.Success)
        {
            string locationName = m.Groups[I("target")].Value;
            location = Game1.getLocationFromName(locationName) ?? Game1.getLocationFromName(locationName, isStructure: true);
        }

        if (location is null)
        {
            this.mod.LogWarningOnce($"Can't figure out the target of a warp totem called: {totem.Name}  -- qiid: {totem.QualifiedItemId}");
        }

        return location;
    }

	public override void snapToDefaultClickableComponent()
	{
		if (this.buttons.Count < 1)
		{
			this.exitThisMenu();
			return;
		}

		this.currentlySnappedComponent = this.buttons[0];
		this.snapCursorToCurrentSnappedComponent();
	}

	private void setIndex(int newIndex, bool playSound = true)
	{
		int lastIndex = this.index;
		this.index = Math.Clamp(newIndex, 0, Math.Max(0, this.destinations.Count - this.buttons.Count));
		if (this.index == lastIndex)
			return;

        if (playSound)
			Game1.playSound("shwip");

        for (int i = 0; i < this.buttons.Count; i++)
			this.buttons[i].changeTotem(this.destinations[i + this.index]);
	}

	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		if (this.autoAlign)
		{
			this.align();
			this.resized();
		}
	}

	public override void receiveScrollWheelAction(int direction)
	{
		base.receiveScrollWheelAction(direction);
		if (direction > 0)
			this.setIndex(this.index - 1);
		else if (direction < 0)
			this.setIndex(this.index + 1);
	}

	public override void applyMovementKey(int direction)
	{
		if (this.currentlySnappedComponent == null)
			this.snapToDefaultClickableComponent();

		switch (direction)
		{
			case 0:
				if (this.currentlySnappedComponent is WarpButton w && w.index == 0 && this.index > 0)
					this.setIndex(this.index - 1);
				else if (this.currentlySnappedComponent is not null)
					this.setCurrentlySnappedComponentTo(this.currentlySnappedComponent.upNeighborID);
				break;
			case 2:
				if (this.currentlySnappedComponent is WarpButton w2 && w2.index == this.buttons.Count - 1 && this.index < this.destinations.Count - 1)
					this.setIndex(this.index + 1);
				else if (this.currentlySnappedComponent != null)
					this.setCurrentlySnappedComponentTo(this.currentlySnappedComponent.downNeighborID);
				break;
		}
	}

	public override void setCurrentlySnappedComponentTo(int id)
	{
		if (id >= 0 && id < this.buttons.Count)
			this.currentlySnappedComponent = this.buttons[id];
		else
			this.snapToDefaultClickableComponent();
		this.snapCursorToCurrentSnappedComponent();
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (this.mainPanel.Contains(x - this.xPositionOnScreen, y - this.yPositionOnScreen))
		{
			foreach (WarpButton button in this.buttons)
			{
				if (button.containsPoint(x, y))
				{
					this.mod.LogTrace($"Destination selected! Closing menu and warping...");
                    this.DoWarp(button.Destination);
					this.exitThisMenuNoSound();
				}
			}
		}
		else if (this.upArrow.containsPoint(x, y))
		{
			this.setIndex(this.index - 1);
		}
		else if (this.downArrow.containsPoint(x, y))
		{
			this.setIndex(this.index + 1);
		}
		base.receiveLeftClick(x, y, playSound);
	}

    private void DoWarp(Destination destination)
    {
        if (Utility.isFestivalDay() && Game1.whereIsTodaysFest == destination.target?.Name &&
            Utility.getStartTimeOfFestival() < Game1.timeOfDay)
        {
            Game1.addHUDMessage(new HUDMessage(L("You can't warp there now - today's festival is being set up.}")));
        }


        if (destination.totem is not null)
        {
            this.mod.TotemInventory.ReduceCount(destination.totem);
            destination.totem.performUseAction(Game1.currentLocation);
        }
        else if (destination.obeliskWarpCode is not null)
        {
            var location = new Location((int)Game1.player.Tile.X, (int)Game1.player.Tile.Y);
            Game1.player.currentLocation.performAction(destination.obeliskWarpCode, Game1.player, location);
            // ^ That returns a bool - maybe we could log it?  not sure that'd be really helpful diagnostic.
        }
        else if (destination.target is not null) { // This condition should be guaranteed true, but this lets the static analysis know.
            int numMinutesToPass = Game1.IsMultiplayer ? 0 :  ModEntry.Config.WarpHomeTimeCost * 10;
            int newTime = -1;
            if (numMinutesToPass > 0)
            {
                newTime = Utility.ModifyTime(Game1.timeOfDay, numMinutesToPass);
                if (newTime > 2600)
                {
                    Game1.addHUDMessage(new HUDMessage(L("It's too late to use the marionberry slow-warp now")));
                    return;
                }
            }

            this.UseWandInNoTotemMode(destination.target, newTime);
        }
    }

    private void UseWandInNoTotemMode(GameLocation target, int newTime)
    {
        Point tile = new();
        if (DataLoader.Locations(Game1.content).TryGetValue(target.Name, out var data) &&
            data.DefaultArrivalTile.HasValue)
        {
            tile = data.DefaultArrivalTile.Value;
        }
        else
        {
            this.mod.LogWarning($"Failed to warp to '{target.Name}': could not find a default arrival tile.");
            Game1.chatBox.addErrorMessage(LF($"Could not find the warp totem at {target.Name} - unable to warp there."));
            return;
        }

        this.DoWarpEffects(() =>
        {
            Game1.warpFarmer(target.Name, tile.X, tile.Y, false);
            if (newTime >= 0)
            {
                SafelySetTime(newTime);
            }
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

    private void align()
	{
		var port = Game1.uiViewport.Size;
		this.xPositionOnScreen = port.Width / 2 - this.width / 2;
		this.yPositionOnScreen = port.Height / 2 - this.height / 2;
	}

    private void resized()
	{
        this.mainPanel.Width = this.width;
        this.mainPanel.Height = (this.height - this.mainPanel.Y - 27) / WarpMenu.buttonH * WarpMenu.buttonH + 27;
        this.upperRightCloseButton.bounds.Location = new(this.xPositionOnScreen + this.mainPanel.X + this.mainPanel.Width - 32, this.yPositionOnScreen + this.mainPanel.Y - 16);
        this.upArrow.bounds.Location = new(this.xPositionOnScreen + this.mainPanel.X + this.mainPanel.Width + 6, this.yPositionOnScreen + this.mainPanel.Y + 33);
        this.downArrow.bounds.Location = new(this.xPositionOnScreen + this.mainPanel.X + this.mainPanel.Width + 6, this.yPositionOnScreen + this.mainPanel.Y + this.mainPanel.Height - 48);
		for (int i = 0; i * WarpMenu.buttonH < this.mainPanel.Height - WarpMenu.buttonH - 12; i += 1)
		{
			Rectangle bound = new(this.xPositionOnScreen + 12 + this.mainPanel.X, this.yPositionOnScreen + i * WarpMenu.buttonH + 15 + this.mainPanel.Y, this.mainPanel.Width - 24, WarpMenu.buttonH);
			if (this.buttons.Count <= i && this.destinations.Count > i + this.index)
			{
                this.buttons.Add(new(bound, this.destinations[i + this.index], i) { scale = 3f, myID = i });
			}
			else
			{
				if (this.destinations.Count <= i + this.index)
				{
					if (this.buttons.Count > i)
					{
                        this.buttons.RemoveAt(i);
					}
				}
				else
				{
                    this.buttons[i].bounds = bound;
                    this.buttons[i].myID = i;
				}
			}
		}
		ClickableComponent.ChainNeighborsUpDown(this.buttons);
	}

	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
        this.upArrow.tryHover(x, y, .2f);
        this.downArrow.tryHover(x, y, .2f);
	}

	public override void draw(SpriteBatch b)
	{
		if (!Game1.options.showMenuBackground)
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
		else
            this.drawBackground(b);
		IClickableMenu.drawTextureBox(b, Game1.mouseCursors, this.panel, this.xPositionOnScreen + this.mainPanel.X, this.yPositionOnScreen + this.mainPanel.Y, this.mainPanel.Width, this.mainPanel.Height,
			Color.White, 3f);
		foreach (WarpButton button in this.buttons)
			button.draw(b);
        this.drawTitleBox(b, this.title);
        this.upArrow.draw(b);
        this.downArrow.draw(b);
		base.draw(b);
        this.drawMouse(b, false);
	}

	private void drawTitleBox(SpriteBatch b, string text)
	{
		int offset = (this.width - this.titleW) / 2;
		//shadows
		b.Draw(Game1.mouseCursors, new Rectangle(this.xPositionOnScreen + offset - 6, this.yPositionOnScreen - 4, 36, 54), new Rectangle(325, 318, 12, 18), WarpMenu.shadow);
		b.Draw(Game1.mouseCursors, new Rectangle(this.xPositionOnScreen + offset + 36 - 6, this.yPositionOnScreen - 4, this.titleW - 36 - 36, 54), new Rectangle(337, 318, 1, 18), WarpMenu.shadow);
		b.Draw(Game1.mouseCursors, new Rectangle(this.xPositionOnScreen + this.width - offset - 42, this.yPositionOnScreen - 4, 36, 54), new Rectangle(338, 318, 12, 18), WarpMenu.shadow);
		//scroll
		b.Draw(Game1.mouseCursors, new Rectangle(this.xPositionOnScreen + offset, this.yPositionOnScreen - 10, 36, 54), new Rectangle(325, 318, 12, 18), Color.White);
		b.Draw(Game1.mouseCursors, new Rectangle(this.xPositionOnScreen + offset + 36, this.yPositionOnScreen - 10, this.titleW - 36 - 36, 54), new Rectangle(337, 318, 1, 18), Color.White);
		b.Draw(Game1.mouseCursors, new Rectangle(this.xPositionOnScreen + this.width - offset - 36, this.yPositionOnScreen - 10, 36, 54), new Rectangle(338, 318, 12, 18), Color.White);
		//text
		Utility.drawTextWithShadow(b, this.title, Game1.dialogueFont, new Vector2(this.xPositionOnScreen + offset + 36, this.yPositionOnScreen - 8), Game1.textColor);
	}
}
