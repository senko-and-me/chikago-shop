using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{
    private async Task TestInterfaceFixes()
    {
        const string save = "user://interface_fixes_test.json";
        Vector2I original = Root.Size;
        TradeScene game = null;
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        try
        {
            foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            {
                Root.Size = size;
                ProgressSaveStore.Save(save, new ProgressData
                {
                    SaveVersion = ProgressJson.CurrentVersion, Day = 1, Score = 1200, EnergyRemaining = 6, DayStarted = false,
                    MusicWorkshopUnlocked = true, ShowcaseItems = [0, -1, -1, 2, 8, 7]
                });
                game = scene.Instantiate<TradeScene>();
                game.SavePath = save;
                game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
                Root.AddChild(game);
                game.SetProcess(false);
                await CheckInstrumentShowcases(game, size);
                await CheckMapShadowDirections(game);
                await CheckPulloutPanels(game);
                await CheckPauseButtonStates(game);
                game.QueueFree();
                game = null;
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        finally
        {
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
            Root.Size = original;
            Paused = false;
        }
    }

    private async Task CheckInstrumentShowcases(TradeScene game, Vector2I size)
    {
        var camera = game.GetNode<WorldCamera>("Camera2D");
        var frame = game.GetNode<Control>("World/Showroom/RoomFrame");
        foreach (bool close in new[] { false, true })
        {
            if (close) camera.FocusOn(frame.GetGlobalRect().GetCenter(), camera.CloseupSize, 0);
            else camera.ReturnToOverview(0);
            using (await CapturePresentation($"showcase-instruments-{size.X}-{(close ? "close" : "normal")}")) { }
            string[] kinds = ["guitar", "violin", "banjo"];
            for (int i = 0; i < kinds.Length; i++)
            {
                var slot = game.GetNode<Button>($"World/Showroom/Showcases/Slot{i + 4}");
                var icon = slot.GetNode<TextureRect>("Icon");
                var original = GD.Load<Texture2D>($"res://art/items/{kinds[i]}.png");
                using var source = original.GetImage();
                Check(icon.Texture is AtlasTexture atlas && atlas.Atlas == original && atlas.Region == (Rect2)source.GetUsedRect(),
                    $"{kinds[i]} must trim transparent margins only on the showcase (texture={icon.Texture}, items={string.Join(",", game.ShowcaseItems)})");
                Vector2 baseFit = new Vector2(209, 115) / original.GetSize();
                Vector2 actual = icon.Size / icon.Texture.GetSize();
                Check(Mathf.IsEqualApprox(actual.X, actual.Y) && actual.X > Mathf.Min(baseFit.X, baseFit.Y) * 1.35f,
                    $"{kinds[i]} must stay in proportion and use more glass space after tilting");
                Vector2 up = Vector2.Up.Rotated(icon.Rotation);
                Check(up.X > 0.7f && up.Y < -0.35f,
                    $"{kinds[i]} must point to the upper right");
                Rect2I region = source.GetUsedRect();
                Rect2 glass = new(105, 26, 219, 130);
                Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
                for (int y = 0; y < region.Size.Y; y++)
                for (int x = 0; x < region.Size.X; x++)
                {
                    if (source.GetPixel(region.Position.X + x, region.Position.Y + y).A == 0) continue;
                    Vector2 ink = icon.GetTransform() * (new Vector2(x + 0.5f, y + 0.5f) * actual);
                    min = min.Min(ink);
                    max = max.Max(ink);
                }
                var inkBounds = new Rect2(min, max - min);
                Rect2 renderedInk = slot.GetGlobalTransformWithCanvas() * inkBounds;
                Check(glass.Encloses(inkBounds) && Root.GetVisibleRect().Encloses(renderedInk) &&
                    inkBounds.GetCenter().DistanceTo(glass.GetCenter()) < 2,
                    $"{kinds[i]} ink must be centered inside the glass at {size}, close={close}");
            }
        }
        var hat = game.GetNode<TextureRect>("World/ShowroomLeft/Showcases/Slot1/Icon");
        Check(hat.Texture.ResourcePath == "res://art/items/hat.png" &&
            hat.GetRect().IsEqualApprox(new Rect2(111, 35, 209, 115)), "Other showcase art must keep its original sizing");
    }

    private async Task CheckMapShadowDirections(TradeScene game)
    {
        var clock = game.GetNode<TextureRect>("HUD/DayClock/Root/DayTimer/Clock");
        var panel = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
        var slots = game.GetNode<TextureRect>("HUD/InventoryUI/InventorySlotBackground");
        var rightAndBottom = new[] { clock, panel.GetNode<TextureRect>("Visual/Back"),
            panel.GetNode<TextureRect>("Visual/Pack/Front"), panel.GetNode<TextureRect>("Visual/Day"),
            panel.GetNode<TextureRect>("Visual/Quota"), panel.GetNode<TextureRect>("Visual/Deadline") };
        var header = game.GetNode<NinePatchRect>("World/Atelier/Header");
        Check(!header.HasNode("DropShadow"), "Room titles must stay flat");
        foreach (string name in new[] { "ScorePanel", "EnergyPanel" })
        {
            var hud = game.GetNode<PanelContainer>("HUD/Root/" + name);
            CheckButtonShadow(hud);
            var material = (ShaderMaterial)hud.GetNode<ButtonDropShadow>("DropShadow").Material;
            float blur = material.GetShaderParameter("blur_radius").AsSingle();
            Vector2 offset = material.GetShaderParameter("face_position").AsVector2();
            Check(Mathf.IsEqualApprox(blur, 8.5f) && offset == new Vector2(-9.5f, 9.5f),
                $"The {name} shadow must be slightly smaller and stay left/down");
        }
        CheckButtonShadow(game.GetNode<Button>("World/Atelier/GetItemButton"));
        CheckButtonShadow(slots);
        foreach (var owner in rightAndBottom)
        {
            CheckButtonShadow(owner);
            var material = (ShaderMaterial)owner.GetNode<ButtonDropShadow>("DropShadow").Material;
            float blur = material.GetShaderParameter("blur_radius").AsSingle();
            Vector2 offset = material.GetShaderParameter("face_position").AsVector2();
            Check(offset.X >= blur - (owner == clock ? 2 : 0) && offset.Y >= blur, $"{owner.Name} shadow must stay to the right and below");
        }
        var strip = (ShaderMaterial)slots.GetNode<ButtonDropShadow>("DropShadow").Material;
        Vector2 stripOffset = strip.GetShaderParameter("face_position").AsVector2();
        Check(stripOffset.X == 0 && stripOffset.Y >= strip.GetShaderParameter("blur_radius").AsSingle() &&
            strip.GetShaderParameter("horizontal_spread").AsSingle() > 1.5f,
            "The slot strip must have equal left/right shadow and a shadow below");
        Check(slots.GetParent() != game.GetNode<InventoryUI>("HUD/InventoryUI").Scroller,
            "Slot shadows must stay outside the scroll clip");
        using (await CapturePresentation($"map-shadow-directions-{Root.Size.X}")) { }
        var camera = game.GetNode<WorldCamera>("Camera2D");
        foreach (string room in new[] { "Atelier", "MusicWorkshop" })
        {
            var button = game.GetNode<Button>($"World/{room}/GetItemButton");
            camera.FocusOn(button.GetGlobalRect().GetCenter(), camera.CloseupSize, 0);
            var shadow = button.GetNode<ButtonDropShadow>("DropShadow");
            using var styled = await CapturePresentation($"craft-shadow-{room}-{Root.Size.X}");
            shadow.Hide();
            using var plain = await CapturePresentation();
            shadow.Show();
            Check(!SameButtonPixels(styled, plain, ScreenBounds(button).Grow(15)),
                $"The {room} craft button must draw an actual shadow outside its face");
        }
    }

    private async Task CheckPulloutPanels(TradeScene game)
    {
        async Task Wait(double duration) => await ToSignal(CreateTimer(duration, true), SceneTreeTimer.SignalName.Timeout);
        var panel = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
        var toggle = game.GetNode<Button>("HUD/Root/ProgressToggleButton");
        var pack = panel.GetNode<Control>("Visual/Pack");
        var sticks = new[] { "Day", "Quota", "Deadline" }.Select(name => panel.GetNode<Control>("Visual/" + name)).ToArray();
        var away = Root.GetVisibleRect().GetCenter();
        MoveButtonPointer(away);
        await Wait(0.55);
        Rect2 packBounds = ScreenBounds(pack);
        Rect2[] rest = sticks.Select(ScreenBounds).ToArray();
        Vector2 tip = new(rest[0].End.X - 4, rest[0].GetCenter().Y);
        Check(tip.X > packBounds.End.X && ScreenBounds(toggle).HasPoint(tip),
            "Closed cigarettes must have visible tips and a matching hover region");
        for (int i = 0; i < 10; i++)
        {
            MoveButtonPointer(tip);
            await Wait(0.49);
            Check(panel.Expanded && sticks.All(stick => Mathf.IsEqualApprox(stick.Position.X, panel.OpenX)),
                $"Pack opening {i + 1} must end at the authored open position");
            MoveButtonPointer(away);
            await Wait(0.68);
            Check(!panel.Expanded && ScreenBounds(pack) == packBounds && sticks.Select(ScreenBounds).SequenceEqual(rest),
                $"Pack retraction {i + 1} must return to exactly the initial visible bounds");
        }

        var notebook = game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
        var page = notebook.GetNode<Control>("Page");
        var notes = page.GetNode<Control>("Notes");
        CheckButtonShadow(page);
        var paperShadow = (ShaderMaterial)page.GetNode<ButtonDropShadow>("DropShadow").Material;
        Vector2 paperOffset = paperShadow.GetShaderParameter("face_position").AsVector2();
        Check(paperOffset.X < 0 && paperOffset.Y > 0, "The moving notebook shadow must fall left and down");
        Rect2 tab = notebook.GetGlobalTransformWithCanvas() * notebook.HoverBounds;
        var farTab = new Vector2(tab.End.X - 2, tab.GetCenter().Y);
        MoveButtonPointer(farTab);
        await Wait(0.18);
        Check(notebook.Expanded && page.Position.X < notebook.ClosedX && notes.Modulate.A is > 0 and < 1,
            $"Notebook must slide and reveal the text on the first hover (expanded={notebook.Expanded}, x={page.Position.X}, alpha={notes.Modulate.A}, pointer={farTab}, hover={Root.GuiGetHoveredControl()?.GetPath()})");
        await Wait(1.3);
        Check(!notebook.Expanded && page.Position.X == notebook.ClosedX && notes.Modulate.A == 0,
            "The old tab held under a stationary pointer must close exactly once and stay closed");
        await Wait(0.55);
        Check(!notebook.Expanded && page.Position.X == notebook.ClosedX,
            "Keeping the pointer still must never trigger a second opening");
        for (int i = 0; i < 3; i++)
        {
            MoveButtonPointer(away);
            await Wait(0.04);
            MoveButtonPointer(new Vector2(tab.Position.X + 17, tab.GetCenter().Y));
            await Wait(0.62);
            Check(notebook.Expanded && page.Position.X == notebook.OpenX && notes.Modulate.A == 1,
                "Hovering the live page must keep it open with fully visible notes");
            MoveButtonPointer(away);
            await Wait(0.68);
            Check(!notebook.Expanded && page.Position.X == notebook.ClosedX,
                "Leaving the moving notebook must re-arm its closed tab");
        }
    }

    private async Task CheckPauseButtonStates(TradeScene game)
    {
        const string content = "PauseMenu/Root/Overlay/Center/Panel/Content/";
        var pause = game.GetNode<PauseMenu>("PauseMenu");
        pause.Open();
        var panel = game.GetNode<PanelContainer>("PauseMenu/Root/Overlay/Center/Panel");
        Check(!panel.HasNode("DropShadow"), "The pause frame must not have a map shadow");
        try
        {
            foreach (string section in new[] { "Menu", "Settings" })
            {
                if (section == "Settings") await ClickControl(game, content + "Menu/SettingsButton");
                MoveButtonPointer(new Vector2(2, 2));
                using (await CapturePresentation($"soft-shadow-pause-{section}-{Root.Size.X}")) { }
                var buttons = game.GetNode<Control>(content + section).FindChildren("*", "BaseButton", true, false)
                    .OfType<BaseButton>().Where(button => button.IsVisibleInTree());
                int checkedButtons = 0;
                foreach (var button in buttons)
                {
                    checkedButtons++;
                    Check(!button.HasNode("DropShadow"), "Pause and settings buttons must be flat");
                    Rect2 bounds = ScreenBounds(button);
                    var focus = Root.GuiGetFocusOwner();
                    using var idle = await CapturePresentation();
                    foreach (Vector2 exit in new[] {
                        new Vector2(bounds.Position.X - 6, bounds.GetCenter().Y),
                        new Vector2(bounds.End.X + 6, bounds.GetCenter().Y),
                        new Vector2(bounds.GetCenter().X, bounds.Position.Y - 3),
                        new Vector2(bounds.GetCenter().X, bounds.End.Y + 3) })
                    {
                        MoveButtonPointer(bounds.GetCenter());
                        using (await CapturePresentation()) { }
                        Check(button.IsHovered() && Root.GuiGetFocusOwner() == focus, $"{button.Name} hover must leave keyboard focus alone");
                        MoveButtonPointer(exit);
                        using var after = await CapturePresentation();
                        Check(!button.IsHovered() && button.Modulate == Colors.White && button.SelfModulate == Colors.White &&
                            SameButtonPixels(idle, after, bounds),
                            $"{section}/{button.Name} must return to the same appearance after leaving toward {exit - bounds.GetCenter()}");
                    }
                    MoveButtonPointer(new Vector2(2, 2));
                }
                Check(checkedButtons == (section == "Menu" ? 6 : 4), $"Every visible {section} button must be exercised");
            }
        }
        finally { pause.Close(); }
    }

    private bool SameButtonPixels(Image first, Image second, Rect2 bounds)
    {
        Vector2 scale = (Vector2)first.GetSize() / Root.GetVisibleRect().Size;
        Rect2 inner = bounds.Grow(-2);
        var area = new Rect2I((Vector2I)(inner.Position * scale), (Vector2I)(inner.Size * scale));
        using var idle = first.GetRegion(area);
        using var after = second.GetRegion(area);
        return idle.GetData().SequenceEqual(after.GetData());
    }
}
