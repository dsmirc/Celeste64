
namespace Celeste64;

public class Menu
{
	public const float Spacing = 4 * Game.RelativeScale;
	public const float SpacerHeight = 12 * Game.RelativeScale;

	public abstract class Item
	{
		public virtual string Label { get; } = string.Empty;
		public virtual bool Selectable { get; } = true;
		public virtual bool Pressed() => false;
		public virtual void Slide(int dir) {}
	}

	public class Spacer : Item
	{
        public override bool Selectable => false;
    }
	
	public class Slider: Item
	{
		private readonly List<string> labels = [];
		private readonly int min;
		private readonly int max;
		private readonly Func<int> get;
		private readonly Action<int> set;
	
		public Slider(string label, int min, int max, Func<int> get, Action<int> set)
		{
			for (int i = 0, n = (max - min); i <= n; i ++)
				labels.Add($"{label} [{new string('|', i)}{new string('.', n - i)}]");
			this.min = min;
			this.max = max;
			this.get = get;
			this.set = set;
		}

        public override string Label => labels[get() - min];
        public override void Slide(int dir) => set(Calc.Clamp(get() + dir, min, max));
    }

	public class Option(string label, Action? action = null) : Item
	{
		private readonly string label = label;
		private readonly Action? action = action;
        public override string Label => label;
        public override bool Pressed()
		{
			if (action != null)
			{
				Audio.Play(Sfx.ui_select);
				action();
				return true;
			}
			return false;
		}
    }

	public class Toggle(string label, Action action, Func<bool> get)  : Item
	{
		private readonly string labelOff = $"{label} : OFF";
		private readonly string labelOn  = $"{label} :  ON";
		private readonly Action action = action;
        public override string Label => get() ? labelOn : labelOff;
        public override bool Pressed()
		{
			action();
			if (get())
				Audio.Play(Sfx.main_menu_toggle_on);
			else
				Audio.Play(Sfx.main_menu_toggle_off);
			return true;
		}
	}

	public int Index
	{
		get => index;
		set => index = value;
	}
	public bool Focused = true;

	private readonly SpriteFont font;
	private readonly List<Item> items = [];
	private int index = 0;

	public string UpSound = Sfx.ui_move;
	public string DownSound = Sfx.ui_move;

	public Vec2 Size
	{
		get
		{
			Vec2 size = Vec2.Zero;

			foreach (var item in items)
			{
				if (string.IsNullOrEmpty(item.Label))
				{
					size.Y += SpacerHeight;
				}
				else
				{
					size.X = MathF.Max(size.X, font.WidthOf(item.Label));
					size.Y += font.LineHeight;
				}
				size.Y += Spacing;
			}

			if (items.Count > 0)
				size.Y -= Spacing;

			return size;
		}
	}

	public Menu()
	{
		font = Assets.Fonts.First().Value;
	}

	public Menu Add(Item item)
	{
		items.Add(item);
		return this;
	}

	public void Update()
	{
		if (items.Count > 0 && Focused)
		{
			var was = index;
			var step = 0;
			if (Controls.Menu.Vertical.Positive.Pressed)
				step = 1;
			if (Controls.Menu.Vertical.Negative.Pressed)
				step = -1;

			index += step;
			while (!items[(items.Count + index) % items.Count].Selectable)
				index += step;
			index = (items.Count + index) % items.Count;

			if (was != index)
				Audio.Play(step < 0 ? UpSound : DownSound);

			if (Controls.Menu.Horizontal.Negative.Pressed)
				items[index].Slide(-1);
			if (Controls.Menu.Horizontal.Positive.Pressed)
				items[index].Slide(1);

			if (Controls.Confirm.Pressed && items[index].Pressed())
				Controls.Consume();
		}
	}

	public void Render(Batcher batch, Vec2 position)
	{
		var size = Size;
		batch.PushMatrix(-size / 2);
		for (int i = 0; i < items.Count; i ++)
		{
			if (string.IsNullOrEmpty(items[i].Label))
			{
				position.Y += SpacerHeight;
				continue;
			}

			var at = position + new Vec2(size.X / 2, 0);
			var text = items[i].Label;
			var justify = new Vec2(0.5f, 0);
			var color = index == i && Focused ? (Time.BetweenInterval(0.1f) ? 0x84FF54 : 0xFCFF59) : Color.White;
			
			UI.Text(batch, text, at, justify, color);

			position.Y += font.LineHeight;
			position.Y += Spacing;
		}
		batch.PopMatrix();
	}
}