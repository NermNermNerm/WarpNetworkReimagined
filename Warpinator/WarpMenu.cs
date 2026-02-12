using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NermNermNerm.Warpinator;
using StardewValley;
using StardewValley.Menus;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

// ADAPTED FROM:  https://github.com/tlitookilakin/WarpNetwork/blob/master/WarpNetwork/ui/WarpMenu.cs

namespace Warpinator;

class WarpMenu : IClickableMenu
{
	const int buttonH = 72;

	private readonly Rectangle panel = new(384, 373, 18, 18);
	private readonly string title;
	private readonly int titleW;
	private static readonly Color shadow = Color.Black * 0.33f;

	private List<WarpButton> buttons = new();
	private ClickableTextureComponent upArrow;
	private ClickableTextureComponent downArrow;
	private int index = 0;
	private bool autoAlign = false;
	private Rectangle mainPanel = new(0, 27, 0, 0);
	internal bool hovering = false;

    private readonly IReadOnlyList<StardewValley.Object> totems;

    private readonly ModEntry mod;

	// internal static Texture2D defaultIcon;

	public WarpMenu(ModEntry mod, IReadOnlyList<StardewValley.Object> totems, int x = 0, int y = 0, int width = 0, int height = 0)
        : base(x, y, width, height, true)
    {
        this.mod = mod;
        this.totems = totems;

		if (totems.Count < 1)
		{
			this.mod.LogWarning($"Warp menu created with no destinations!");
			this.exitThisMenuNoSound();
		}

        this.title = L("Choose Destination");
        var titleSize = Game1.dialogueFont.MeasureString(this.title);

		// Try and make the dialog so that it fits as neatly as it can.  The "+1" after locs.Count accounts for the title.
		// The *1.5 is a spitball estimate that accounts for the padding and margins.
		double estimatedHeight = Math.Max(Math.Min(this.totems.Count+1, 9), 4) * titleSize.Y * 1.5;

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
		this.index = Math.Clamp(newIndex, 0, Math.Max(0, this.totems.Count - this.buttons.Count));
		if (this.index == lastIndex)
			return;

        if (playSound)
			Game1.playSound("shwip");

        for (int i = 0; i < this.buttons.Count; i++)
			this.buttons[i].changeTotem(this.totems[i + this.index]);
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
				if (this.currentlySnappedComponent is WarpButton w2 && w2.index == this.buttons.Count - 1 && this.index < this.totems.Count - 1)
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
                    this.DoWarp(button.Totem);
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

    private void DoWarp(StardewValley.Object totem)
    {
        totem.performUseAction(Game1.currentLocation);
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
			if (this.buttons.Count <= i && this.totems.Count > i + this.index)
			{
                this.buttons.Add(new(bound, this.totems[i + this.index], i) { scale = 3f, myID = i });
			}
			else
			{
				if (this.totems.Count <= i + this.index)
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
