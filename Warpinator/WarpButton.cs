using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

// ADAPTED FROM: https://github.com/tlitookilakin/WarpNetwork/blob/master/WarpNetwork/ui/WarpButton.cs

namespace Warpinator;

class WarpButton : ClickableComponent
{
	private bool wasHovered = false;
	private Color tint = Color.White;
	public readonly int index = 0;
	private static readonly Rectangle bg = new(384, 396, 15, 15);

    private StardewValley.Object totem;

    public StardewValley.Object Totem => this.totem;

	public WarpButton(Rectangle bounds, StardewValley.Object totem, int index) : base(bounds, "")
	{
		this.index = index;
        this.totem = totem;
    }

	public void changeTotem(StardewValley.Object totem)
    {
        this.totem = totem;
	}

    public void draw(SpriteBatch b)
	{
		if (this.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()))
		{
            this.tint = Color.Wheat;
			if (!this.wasHovered)
				Game1.playSound("shiny4");
            this.wasHovered = true;
		}
		else
		{
            this.tint = Color.White;
            this.wasHovered = false;
		}
		IClickableMenu.drawTextureBox(b, Game1.mouseCursors, WarpButton.bg, this.bounds.X, this.bounds.Y, this.bounds.Width, this.bounds.Height, this.tint, this.scale, false);
        // TODO: Maybe draw the warp totem?
		// b.Draw(this.texture, new Rectangle(this.bounds.X + 12, this.bounds.Y + 12, this.bounds.Height - 24, this.bounds.Height - 24), Color.White);
        string text = IF($"{this.totem.DisplayName} ({this.totem.Stack})"); // TODO: make nicer
        var textSize = Game1.dialogueFont.MeasureString(text);

		Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2(this.bounds.X + this.bounds.Height - 9, MathF.Round(this.bounds.Y - textSize.Y / 2f + this.bounds.Height / 2f + 6)), Game1.textColor);
	}
}

