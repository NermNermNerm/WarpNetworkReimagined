using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

// ADAPTED FROM: https://github.com/tlitookilakin/WarpNetwork/blob/master/WarpNetwork/ui/WarpButton.cs

namespace Warpinator;

class WarpButton : ClickableComponent
{
	private bool wasHovered = false;
	private Color tint = Color.White;
	public readonly int index = 0;
	private static readonly Rectangle bg = new(384, 396, 15, 15);

    private WarpMenu.Destination destination;

    public WarpMenu.Destination Destination => this.destination;

	public WarpButton(Rectangle bounds, WarpMenu.Destination destination, int index) : base(bounds, "")
	{
		this.index = index;
        this.destination = destination;
    }

	public void changeTotem(WarpMenu.Destination totem)
    {
        this.destination = totem;
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
        string text = (this.destination.target is null)
            ? IF($"{this.destination.totem!.DisplayName} ({this.destination.totem.Stack})")
            : (this.destination.obeliskWarpCode is not null
                ? LF($"{this.destination.target.DisplayName} via obelisk")
                : (this.destination.totem is null
                    ? LF($"{this.destination.target.DisplayName} via slow-warp")
                    : LF($"{this.destination.target.DisplayName} via totem ({this.destination.totem!.Stack})")));
        var textSize = Game1.dialogueFont.MeasureString(text);

		Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2(this.bounds.X + this.bounds.Height - 9, MathF.Round(this.bounds.Y - textSize.Y / 2f + this.bounds.Height / 2f + 6)), Game1.textColor);
	}
}

