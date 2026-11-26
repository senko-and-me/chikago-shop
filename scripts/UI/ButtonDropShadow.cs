using Godot;

// The draw-only child follows its owner without participating in container layout or GUI input.
public partial class ButtonDropShadow : Node2D
{
    public enum Direction { Soft, RightAndBottom, LeftAndBottom, SidesAndBottom }

    private Control _owner;
    private Direction _direction;
    private Vector2 _offsetAdjustment;
    private bool _solidFace;
    private float _opacity;
    private float _blurScale;
    private Texture2D _texture;
    private ShaderMaterial _shadow;
    private Rect2 _drawArea;
    private bool _hasFace;

    public static void Attach(Control owner, Direction direction = Direction.Soft, Vector2 offsetAdjustment = default,
        bool solidFace = false, float opacity = 0.42f, float blurScale = 1)
    {
        owner.AddChild(new ButtonDropShadow
        {
            Name = "DropShadow",
            _owner = owner,
            _direction = direction,
            _offsetAdjustment = offsetAdjustment,
            _solidFace = solidFace,
            _opacity = opacity,
            _blurScale = blurScale,
            ShowBehindParent = true
        });
    }

    public override void _Ready()
    {
        _shadow = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/ui_soft_shadow.gdshader") };
        Material = _shadow;
        _shadow.SetShaderParameter("shadow_opacity", _opacity);
        _owner.Resized += UpdateGeometry;
        RefreshFace();
        UpdateGeometry();
    }

    public override void _Process(double delta)
    {
        if (IsVisibleInTree()) RefreshFace();
    }

    private void RefreshFace()
    {
        Texture2D texture = _owner switch
        {
            TextureButton button => button.TextureNormal,
            TextureRect rect => rect.Texture,
            _ => null
        };
        bool hasFace = _solidFace || texture != null;
        if (!_solidFace && _owner is Button or PanelContainer)
        {
            var style = _owner.GetThemeStylebox(_owner is Button ? "normal" : "panel");
            switch (style)
            {
                case StyleBoxTexture paper:
                    texture = paper.Texture;
                    hasFace = texture != null;
                    break;
                case StyleBoxFlat flat:
                    hasFace = flat.DrawCenter && flat.BgColor.A > 0;
                    break;
            }
        }
        if (texture == _texture && hasFace == _hasFace) return;
        _texture = texture;
        _hasFace = hasFace;
        _shadow.SetShaderParameter("use_mask", _texture != null);
        if (_texture != null)
        {
            Texture2D mask = _texture;
            var region = new Vector4(0, 0, 1, 1);
            if (_texture is AtlasTexture atlas && atlas.Atlas != null)
            {
                mask = atlas.Atlas;
                Vector2 size = mask.GetSize();
                Rect2 clip = atlas.Region;
                Vector2 start = (clip.Position + Vector2.One * 0.5f) / size;
                Vector2 extent = (clip.Size - Vector2.One) / size;
                region = new Vector4(start.X, start.Y, extent.X, extent.Y);
            }
            _shadow.SetShaderParameter("face_mask", mask);
            _shadow.SetShaderParameter("mask_region", region);
        }
        UpdateGeometry();
    }

    private void UpdateGeometry()
    {
        var face = new Rect2(Vector2.Zero, _owner.Size);
        bool keepAspect = _owner is TextureButton { StretchMode: TextureButton.StretchModeEnum.KeepAspectCentered } or
            TextureRect { StretchMode: TextureRect.StretchModeEnum.KeepAspectCentered };
        if (_texture != null && keepAspect)
        {
            Vector2 textureSize = _texture.GetSize();
            Vector2 fit = face.Size / textureSize;
            Vector2 size = textureSize * Mathf.Min(fit.X, fit.Y);
            face = new Rect2((face.Size - size) / 2, size);
        }
        float blur = Mathf.Clamp(face.Size.Y * 0.17f, 8, 16);
        if (_direction == Direction.Soft)
        {
            blur *= _blurScale;
            face = face.Grow(1.5f);
            face.Position += new Vector2(3, 5);
        }
        else
        {
            blur = _direction switch
            {
                Direction.LeftAndBottom => 10,
                Direction.SidesAndBottom => 8,
                _ => Mathf.Clamp(face.Size.Y * 0.085f, 5, 10)
            };
            blur *= _blurScale;
            float xOffset = _direction switch
            {
                Direction.RightAndBottom => blur + 1,
                Direction.LeftAndBottom => -blur - 1,
                _ => 0
            };
            face.Position += new Vector2(xOffset, blur + 1);
        }
        face.Position += _offsetAdjustment;
        float horizontalSpread = _direction == Direction.SidesAndBottom ? 1.65f : 1;
        _shadow.SetShaderParameter("horizontal_spread", horizontalSpread);
        _shadow.SetShaderParameter("face_position", face.Position);
        _shadow.SetShaderParameter("face_size", face.Size);
        _shadow.SetShaderParameter("blur_radius", blur);
        float horizontalPadding = blur * horizontalSpread + 1;
        _drawArea = face.GrowIndividual(horizontalPadding, blur + 1, horizontalPadding, blur + 1);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_hasFace && _owner.Size.X > 0 && _owner.Size.Y > 0) DrawRect(_drawArea, Colors.White);
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_owner)) _owner.Resized -= UpdateGeometry;
    }
}
