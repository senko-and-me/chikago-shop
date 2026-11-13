
using System;
using ChikagoShop.Domain.Crafting;
using Godot;

public partial class PolishingSurface : Control
{
	// Artist artwork replaces the old sax variant; the minigame still has four choices.
	public static System.Collections.Generic.IReadOnlyList<string> InstrumentKinds { get; } =
		Array.AsReadOnly(new[] { "violin", "trumpet", "drums", "guitar" });
	private const string ViolinPath =
		"res://art/minigames/violin.png";

	private const string GuitarPath = "res://art/items/guitar.png";

	private const string ClothPath =
		"res://art/minigames/polishing_cloth.png";

	public PolishingSession Session { get; private set; } = null!;
	public string InstrumentKind { get; private set; } = "violin";

	public Rect2 InstrumentRect
	{
		get
		{
			Vector2 fitted = new Vector2(600, 480) *
				Mathf.Min(Size.X / 600, Size.Y / 480);
			return new Rect2((Size - fitted) / 2, fitted);
		}
	}

	private Texture2D _instrument = null!;
	private Texture2D _clothTexture;
	private Image _coatingImage = null!;
	private ImageTexture _coatingTexture = null!;
	private ShaderMaterial _material = null!;
	private int _pointer = -2;
	private Vector2 _cloth;
	private ulong _lastMove;
	private int _lastMisses;
	private ulong _missAt;
	private Vector2 _missPoint;

	private static readonly Shader CoatingShader = new()
	{
		Code = @"shader_type canvas_item;
uniform sampler2D silhouette;
uniform sampler2D coating : filter_linear;
uniform vec4 dirt_color : source_color;
uniform vec4 film_color : source_color;
void fragment() {
    vec4 art = texture(TEXTURE, UV);
    vec2 layers = texture(coating, UV).rg;
    float mask = texture(silhouette, UV).a;
    float grain = sin(UV.x * 350.0 + UV.y * 800.0) * .012;
    vec3 satin = mix(art.rgb, film_color.rgb + grain, layers.g * .55 * mask);
    COLOR = vec4(mix(satin, dirt_color.rgb + grain, layers.r * .82 * mask), art.a);
}"
	};

	public override void _Ready()
	{
		if (ResourceLoader.Exists(ClothPath))
			_clothTexture = GD.Load<Texture2D>(ClothPath);
	}

	public PolishingSession Begin(string kind)
	{
		EndPointer();
		InstrumentKind = kind;

		Texture2D mask;

		string rasterPath = kind switch
		{
			"violin" => ViolinPath,
			"guitar" => GuitarPath,
			"drums" => "res://art/items/drums.png",
			_ => null
		};
		if (rasterPath != null && ResourceLoader.Exists(rasterPath))
		{
			// Use the same fitted pixels for both artwork and the playable silhouette.
			Texture2D original = GD.Load<Texture2D>(rasterPath);
			Image source = original.GetImage();
			source.Convert(Image.Format.Rgba8);

			float scale = Math.Min(
				600f / source.GetWidth(),
				480f / source.GetHeight());

			int width = Math.Max(
				1, Mathf.RoundToInt(source.GetWidth() * scale));

			int height = Math.Max(
				1, Mathf.RoundToInt(source.GetHeight() * scale));

			source.Resize(
				width, height, Image.Interpolation.Lanczos);

			Image fitted = Image.CreateEmpty(
				600, 480, false, Image.Format.Rgba8);

			fitted.Fill(Colors.Transparent);

			fitted.BlendRect(
				source,
				new Rect2I(0, 0, width, height),
				new Vector2I(
					(600 - width) / 2,
					(480 - height) / 2));

			_instrument = ImageTexture.CreateFromImage(fitted);
			mask = _instrument;
		}
		else
		{
			_instrument = GD.Load<Texture2D>(
				$"res://art/minigames/polishing_{kind}.svg");

			mask = GD.Load<Texture2D>(
				$"res://art/minigames/polishing_{kind}_mask.svg");
		}

		Image maskImage = mask.GetImage();
		maskImage.Convert(Image.Format.Rgba8);

		byte[] rgba = maskImage.GetData();
		byte[] alpha = new byte[600 * 480];

		for (int i = 0; i < alpha.Length; i++)
			alpha[i] = rgba[i * 4 + 3];

		Session = new PolishingSession(alpha);
		_coatingImage = Image.CreateEmpty(
			Session.Columns, Session.Rows,
			false, Image.Format.Rgba8);

		_coatingTexture =
			ImageTexture.CreateFromImage(_coatingImage);

		_material = new ShaderMaterial
		{
			Shader = CoatingShader
		};

		_material.SetShaderParameter("silhouette", mask);
		_material.SetShaderParameter("coating", _coatingTexture);

		bool brass = kind is "trumpet" or "sax";

		_material.SetShaderParameter(
			"dirt_color",
			new Color(brass ? "6f634b" : "675547"));

		_material.SetShaderParameter(
			"film_color",
			new Color(brass ? "a49169" : "98795c"));

		_lastMisses = 0;
		UpdateCoating();

		return Session;
	}

	public Vector2 LogicalToScreen(Vector2 point)
	{
		return GlobalPosition + InstrumentRect.Position +
			point / new Vector2(600, 480) *
			InstrumentRect.Size;
	}

	private Vector2 ToLogical(Vector2 local)
	{
		return (local - InstrumentRect.Position) /
			InstrumentRect.Size * new Vector2(600, 480);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (Session == null ||
			Session.State != EmbroideryState.Playing)
			return;

		if (@event is InputEventMouseButton { Device: -1 })
			return;

		if (_pointer == -2 &&
			@event is InputEventMouseButton
			{ ButtonIndex: MouseButton.Left, Pressed: true } mouse)
		{
			BeginPointer(-1, mouse.Position);
		}
		else if (_pointer == -2 &&
			@event is InputEventScreenTouch { Pressed: true } touch)
		{
			BeginPointer(touch.Index, touch.Position);
		}

		AcceptEvent();
	}

	private void BeginPointer(int pointer, Vector2 local)
	{
		_pointer = pointer;
		_cloth = local;
		_lastMove = Time.GetTicksMsec();

		Vector2 logical = ToLogical(local);
		Session.BeginRub(logical.X, logical.Y);
		UpdateMiss(local);
	}

	public override void _Input(InputEvent @event)
	{
		if (_pointer == -2)
			return;

		if (@event is InputEventMouseButton { Device: -1 })
			return;

		if ((_pointer == -1 &&
			@event is InputEventMouseButton
			{ ButtonIndex: MouseButton.Left, Pressed: false }) ||
			(@event is InputEventScreenTouch
			{ Pressed: false } touch && touch.Index == _pointer))
		{
			EndPointer();
			GetViewport().SetInputAsHandled();
			return;
		}

		Vector2? position = @event switch
		{
			InputEventMouseMotion mouse when _pointer == -1
				=> mouse.Position,
			InputEventScreenDrag drag when drag.Index == _pointer
				=> drag.Position,
			_ => null
		};

		if (!position.HasValue)
			return;

		if (Session.State != EmbroideryState.Playing)
		{
			EndPointer();
			return;
		}

		Vector2 local =
			GetGlobalTransformWithCanvas().AffineInverse() *
			position.Value;

		_cloth = local;

		Vector2 logical = ToLogical(local);
		ulong now = Time.GetTicksMsec();

		Session.MoveRub(
			logical.X, logical.Y, now - _lastMove);

		_lastMove = now;
		UpdateMiss(local);
		UpdateCoating();

		GetViewport().SetInputAsHandled();
	}

	private void UpdateMiss(Vector2 local)
	{
		if (Session.Misses == _lastMisses)
			return;

		_lastMisses = Session.Misses;
		_missAt = Time.GetTicksMsec();
		_missPoint = local;
	}

	public void EndPointer()
	{
		_pointer = -2;
		Session?.EndRub();
		QueueRedraw();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut ||
			(what == NotificationVisibilityChanged &&
			 !IsVisibleInTree()))
		{
			EndPointer();
		}
	}

	private void UpdateCoating()
	{
		for (int i = 0; i < Session.Cells.Count; i++)
		{
			PolishingCell cell = Session.Cells[i];

			_coatingImage.SetPixel(
				i % Session.Columns,
				i / Session.Columns,
				new Color(
					(float)cell.Dirt,
					(float)cell.Film,
					0, 1));
		}

		_coatingTexture.Update(_coatingImage);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (Session == null || !IsVisibleInTree())
			return;

		if (Session.State != EmbroideryState.Playing &&
			_pointer != -2)
		{
			EndPointer();
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_instrument == null)
			return;

		if (_pointer != -2)
		{
			Vector2 pointer = _cloth;

			if (_clothTexture != null)
			{
				const float size = 108f;

				DrawTextureRect(
					_clothTexture,
					new Rect2(
						pointer - new Vector2(
							size * 0.5f, size * 0.5f),
						new Vector2(size, size)),
					false);
			}
			else
			{
				Vector2[] cloth =
				[
					pointer + new Vector2(-8, -12),
					pointer + new Vector2(24, -24),
					pointer + new Vector2(49, -7),
					pointer + new Vector2(40, 27),
					pointer + new Vector2(12, 40),
					pointer + new Vector2(-17, 20)
				];

				DrawColoredPolygon(
					cloth, new Color("e4dfd3"));

				DrawPolyline(
					[
						cloth[0], cloth[1], cloth[2],
						cloth[3], cloth[4], cloth[5], cloth[0]
					],
					new Color("898076"),
					2, true);
			}
		}

		if (_lastMisses > 0 &&
			Time.GetTicksMsec() - _missAt < 350)
		{
			DrawLine(
				_missPoint - new Vector2(7, 7),
				_missPoint + new Vector2(7, 7),
				new Color("b32137"),
				3, true);

			DrawLine(
				_missPoint - new Vector2(-7, 7),
				_missPoint + new Vector2(-7, 7),
				new Color("b32137"),
				3, true);
		}
	}

	private void EnsureArtwork()
	{
		if (!IsInsideTree())
			return;

		var art = GetNodeOrNull<TextureRect>("Instrument");

		if (art == null)
		{
			art = new TextureRect
			{
				Name = "Instrument",
				MouseFilter = MouseFilterEnum.Ignore,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				ShowBehindParent = true
			};

			AddChild(art);
		}

		art.Texture = _instrument;
		art.Material = _material;
		art.Position = InstrumentRect.Position;
		art.Size = InstrumentRect.Size;
	}

	public void RefreshArtwork()
	{
		EnsureArtwork();
	}
}
