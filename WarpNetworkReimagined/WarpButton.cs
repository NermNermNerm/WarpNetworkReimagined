
// ADAPTED FROM: https://github.com/tlitookilakin/WarpNetwork/blob/master/WarpNetwork/ui/WarpButton.cs

namespace WarpNetworkReimagined;

class WarpButton : ClickableComponent
{
	private bool wasHovered = false;
	private Color tint = Color.White;
	public readonly int index = 0;
	private static readonly Rectangle bg = new(384, 396, 15, 15);

    private WarpDestination destination;

    public WarpDestination WarpDestination => this.destination;

	public WarpButton(Rectangle bounds, WarpDestination warpDestination, int index) : base(bounds, "")
	{
		this.index = index;
        this.destination = warpDestination;
    }

	public void changeTotem(WarpDestination totem)
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
        string text = this.destination.ButtonTitle;
        var textSize = Game1.dialogueFont.MeasureString(text);

		Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2(this.bounds.X + this.bounds.Height - 9, MathF.Round(this.bounds.Y - textSize.Y / 2f + this.bounds.Height / 2f + 6)), Game1.textColor);
	}
}

