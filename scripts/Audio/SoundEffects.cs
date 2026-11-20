using System;
using System.Collections.Generic;
using Godot;

// Presentation observer: input still belongs to the original controls and game rules.
public partial class SoundEffects : Node
{
    public enum Cue { Hover, Click, Settings, Sale, CustomerBell, RerollSuccess, RerollFailure, Stitch1, Stitch2, Stitch3 }
    private static readonly Cue[] StitchCues = { Cue.Stitch1, Cue.Stitch2, Cue.Stitch3 };
    private readonly RandomNumberGenerator _stitchRandom = new();
    public const string BusName = "SFX";
    public static SoundEffects Instance { get; private set; }
    [Export(PropertyHint.Range, "0,0.3,0.005")] public double HoverClickMergeSeconds { get; set; } = 0.1;
    public double Volume { get; private set; } = 1;
    public event Action<double> VolumeChanged;
    public event Action<Cue> Played;
    private readonly Dictionary<Cue, AudioStreamPlayer> _voices = new();
    private readonly Dictionary<BaseButton, Action> _buttons = new();
    private readonly Dictionary<TradeScene, (Action Arrived, Action Changed)> _games = new();
    private BaseButton _hovered, _hoverSoundButton, _mergedPress;
    private ulong _hoverSoundAt;
    private ulong _pressGeneration;
    private Vector2 _lastPointer = new(float.NaN, float.NaN);
    private bool _motionPending;

    public override void _Ready()
    {
        Instance = this;
        _stitchRandom.Randomize();
        ProcessMode = ProcessModeEnum.Always;
        if (AudioServer.GetBusIndex(BusName) < 0)
        {
            AudioServer.AddBus();
            AudioServer.SetBusName(AudioServer.BusCount - 1, BusName);
            AudioServer.SetBusSend(AudioServer.GetBusIndex(BusName), "Master");
        }
        LoadPreference();
        foreach (Cue cue in Enum.GetValues<Cue>())
        {
            // Scene-owned players retain the developer's individual Volume dB settings.
            var player = GetNode<AudioStreamPlayer>(cue.ToString());
            player.Bus = BusName;
            _voices.Add(cue, player);
        }
        GetTree().NodeAdded += Attach;
        GetTree().NodeRemoved += Detach;
        AttachExisting(GetTree().Root);
    }

    private void AttachExisting(Node node)
    {
        Attach(node);
        foreach (Node child in node.GetChildren()) AttachExisting(child);
    }

    public void LoadPreference()
    {
        var config = new ConfigFile();
        double volume = 1;
        if (config.Load(GameLocalization.SettingsPath) == Error.Ok)
        {
            var stored = config.GetValue("audio", "effects_volume", 1.0);
            if (stored.VariantType is Variant.Type.Float or Variant.Type.Int) volume = stored.AsDouble();
        }
        ApplyVolume(volume);
    }

    public Error SetVolume(double volume)
    {
        ApplyVolume(volume);
        var config = new ConfigFile();
        config.Load(GameLocalization.SettingsPath);
        config.SetValue("audio", "effects_volume", Volume);
        return config.Save(GameLocalization.SettingsPath);
    }

    private void ApplyVolume(double volume)
    {
        Volume = double.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : 1;
        int bus = AudioServer.GetBusIndex(BusName);
        AudioServer.SetBusMute(bus, Volume == 0);
        AudioServer.SetBusVolumeDb(bus, Volume > 0 ? Mathf.LinearToDb((float)Volume) : -80);
        VolumeChanged?.Invoke(Volume);
    }

    private void Attach(Node node)
    {
        if (node is BaseButton button && !_buttons.ContainsKey(button))
        {
            Action activate = () =>
            {
                if (!Interactive(button)) return;
                // Refusal is sounded once the new offer is known, rather than as a generic click.
                if (button.Name == "DeclineButton") return;
                Cue cue = button.Name.ToString() switch
                {
                    "SettingsButton" => Cue.Settings,
                    "AcceptButton" => Cue.Sale,
                    _ => Cue.Click
                };
                bool merged = cue == Cue.Click && _mergedPress == button;
                _mergedPress = null;
                if (!merged) Play(cue);
            };
            _buttons.Add(button, activate);
            button.Pressed += activate;
        }
        if (node is TradeScene game && !_games.ContainsKey(game))
        {
            Action arrived = () => Play(Cue.CustomerBell);
            Action changed = () =>
            {
                int baseline = game.GetItemPrice(game.ItemIndex);
                if (game.OfferedPrice > baseline) Play(Cue.RerollSuccess);
                else if (game.OfferedPrice < baseline) Play(Cue.RerollFailure);
            };
            _games.Add(game, (arrived, changed));
            game.BuyerArrived += arrived;
            game.BuyerOfferChanged += changed;
        }
    }

    private void Detach(Node node)
    {
        if (node is BaseButton button && _buttons.Remove(button, out var callback)) button.Pressed -= callback;
        if (node is TradeScene game && _games.Remove(game, out var callbacks))
        {
            game.BuyerArrived -= callbacks.Arrived;
            game.BuyerOfferChanged -= callbacks.Changed;
        }
        if (node == _hovered) { _hovered = null; CancelHover(); }
        if (node == _hoverSoundButton) _hoverSoundButton = null;
        if (node == _mergedPress) _mergedPress = null;
    }

    private static bool Interactive(BaseButton button) => GodotObject.IsInstanceValid(button) &&
        button.IsInsideTree() && !button.IsQueuedForDeletion() && button.IsVisibleInTree() &&
        !button.Disabled && button.CanProcess();

    private BaseButton PointerButton()
    {
        Control control = GetViewport().GuiGetHoveredControl();
        while (control != null && control is not BaseButton) control = control.GetParent() as Control;
        return control is BaseButton button && Interactive(button) ? button : null;
    }

    private void CancelHover()
    {
        _hoverSoundButton = null;
        _voices[Cue.Hover].Stop();
    }

    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseMotion motion)
        {
            if (motion.Position != _lastPointer || motion.Relative.LengthSquared() > 0)
                _motionPending = (motion.ButtonMask & MouseButtonMask.Left) == 0;
            _lastPointer = motion.Position;
        }
        else if (input is InputEventMouseButton { Pressed: true } or InputEventKey { Pressed: true })
        {
            _motionPending = false;
            _pressGeneration++;
            _mergedPress = null;
            var current = PointerButton();
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } &&
                current != null && current == _hovered && current == _hoverSoundButton &&
                (Time.GetTicksMsec() - _hoverSoundAt) / 1000.0 <= HoverClickMergeSeconds)
                _mergedPress = current;
            else CancelHover();
        }
        else if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
        {
            // GUI activation follows _Input. Clear an abandoned press only after GUI dispatch.
            ulong generation = _pressGeneration;
            Callable.From(() => { if (_pressGeneration == generation) _mergedPress = null; }).CallDeferred();
        }
    }

    public override void _Process(double delta)
    {
        var current = PointerButton();
        if (_hovered != null && current != _hovered) { _hovered = null; CancelHover(); }
        if (_motionPending)
        {
            _motionPending = false;
            if (current != _hovered)
            {
                CancelHover();
                _hovered = current;
                if (current != null)
                {
                    _hoverSoundButton = current;
                    _hoverSoundAt = Time.GetTicksMsec();
                    Play(Cue.Hover);
                }
            }
        }
    }

    public void PlayStitch()
    {
        CancelHover();
        Play(StitchCues[_stitchRandom.RandiRange(0, StitchCues.Length - 1)]);
    }

    private void Play(Cue cue)
    {
        if (cue is Cue.Click or Cue.Settings or Cue.Sale or Cue.RerollSuccess or Cue.RerollFailure) CancelHover();
        if (cue is Cue.RerollSuccess or Cue.RerollFailure)
        {
            _voices[Cue.RerollSuccess].Stop();
            _voices[Cue.RerollFailure].Stop();
        }
        _voices[cue].Play();
        Played?.Invoke(cue);
    }

    public override void _ExitTree()
    {
        GetTree().NodeAdded -= Attach;
        GetTree().NodeRemoved -= Detach;
        foreach (var (button, callback) in _buttons)
            if (GodotObject.IsInstanceValid(button)) button.Pressed -= callback;
        foreach (var (game, callback) in _games)
            if (GodotObject.IsInstanceValid(game))
            {
                game.BuyerArrived -= callback.Arrived;
                game.BuyerOfferChanged -= callback.Changed;
            }
        _buttons.Clear();
        _games.Clear();
        foreach (var player in _voices.Values)
        {
            player.Stop();
            player.Stream = null;
        }
        _voices.Clear();
        if (Instance == this) Instance = null;
    }
}
