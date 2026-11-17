using Godot;

// Uses the same localized artwork as the main menu and reflects the actual window mode.
public partial class FullscreenToggle : TextureButton
{
    [Export] public Texture2D RussianOff { get; set; }
    [Export] public Texture2D RussianOn { get; set; }
    [Export] public Texture2D EnglishOff { get; set; }
    [Export] public Texture2D EnglishOn { get; set; }

    public override void _Ready()
    {
        Toggled += enabled =>
        {
            DisplayServer.WindowSetMode(enabled
                ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
            RefreshState();
        };
        RefreshState();
    }

    public void RefreshState()
    {
        bool fullscreen = DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen
            or DisplayServer.WindowMode.ExclusiveFullscreen;
        SetPressedNoSignal(fullscreen);
        bool english = GameLocalization.Language == "en";
        Texture2D texture = english ? (fullscreen ? EnglishOn : EnglishOff) : (fullscreen ? RussianOn : RussianOff);
        if (TextureNormal != texture) TextureNormal = texture;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && IsNodeReady()) RefreshState();
    }

    public override void _Process(double delta)
    {
        if (IsVisibleInTree()) RefreshState();
    }
}
