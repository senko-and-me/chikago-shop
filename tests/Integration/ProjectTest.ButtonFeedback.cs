using System;
using System.IO;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestShowcaseFeedbackBounds()
    {
        var layer = new CanvasLayer { Layer = 150 };
        Root.AddChild(layer);
        var slot = GD.Load<PackedScene>("res://scenes/UI/ShowcaseSlot.tscn").Instantiate<Button>();
        layer.AddChild(slot);
        slot.Position = new Vector2(180, 140);
        Vector2 originalSize = slot.Size;
        int activations = 0;
        slot.Pressed += () => activations++;
        try
        {
            foreach (float scale in new[] { 0.4f, 0.7f })
            {
                slot.Scale = Vector2.One * scale;
                Rect2 expected = slot.GetGlobalTransformWithCanvas() * new Rect2(65, 0, 310, 200);
                MoveButtonPointer(expected.GetCenter());
                await SettleButtonFeedback();
                var frame = ButtonFeedbackController.Instance.GetVisualFrame();
                Check(frame.Bounds == expected && frame.Strength > 0.9f &&
                    frame.Bounds.Size.X < ScreenBounds(slot).Size.X * 0.75f,
                    "Showcase hover outline must fit the glass area at each world scale");
                PressButtonPointer(expected.GetCenter(), true);
                PressButtonPointer(expected.GetCenter(), false);
                await ToSignal(this, SignalName.ProcessFrame);
                Check(ButtonFeedbackController.Instance.GetVisualFrame().Bounds == expected && activations > 0,
                    "Showcase click feedback must use the compact frame without breaking activation");
                MoveButtonPointer(new Vector2(2, 2));
                slot.GrabFocus();
                ButtonFeedbackController.Instance.UseKeyboardNavigation();
                await SettleButtonFeedback();
                Check(ButtonFeedbackController.Instance.GetVisualFrame().Bounds == expected && slot.Size == originalSize &&
                    slot.GetThemeStylebox("focus") == slot.GetThemeStylebox("normal"),
                    "Keyboard focus must use the compact frame without a second full-width native border or resized hitbox");
                slot.ReleaseFocus();
            }
        }
        finally { layer.QueueFree(); }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestTextureButtonFeedbackBounds()
    {
        var menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
        Root.AddChild(menu);
        Vector2I originalSize = Root.Size;
        string originalLocale = TranslationServer.GetLocale();
        try
        {
            foreach (string language in new[] { "ru", "en" })
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            {
                TranslationServer.SetLocale(language);
                Root.Size = size;
                menu.OnSettingsPressed();
                Root.GuiReleaseFocus();
                MoveButtonPointer(new Vector2(2, 2));
                await SettleButtonFeedback();
                var back = menu.GetNode<TextureButton>("SettingsMenuContainer/Settings/BackButton");
                Rect2 control = ScreenBounds(back);
                Vector2 originalButtonSize = back.Size;
                using var idle = await CaptureButtonFeedback();
                MoveButtonPointer(control.GetCenter());
                await SettleButtonFeedback();
                var frame = ButtonFeedbackController.Instance.GetVisualFrame();
                Check(frame.Strength > 0.9f && control.Grow(1).Encloses(frame.Bounds) && frame.Bounds.Size.X > 0,
                    "Texture feedback must fit within visible artwork without exceeding the control");
                Check(back.Size == originalButtonSize, "Fitting feedback must not resize the button or its hitbox");
                using var hover = await CaptureButtonFeedback();
                if (language == "en")
                {
                    Check(frame.Bounds.Size.X < control.Size.X * 0.7f,
                        "The narrow English Back image must not inherit the wide Russian control outline");
                    Vector2 margin = new(control.Position.X + 5, control.GetCenter().Y);
                    Vector2 scale = new Vector2(hover.GetWidth(), hover.GetHeight()) / Root.GetVisibleRect().Size;
                    int x = (int)(margin.X * scale.X), y = (int)(margin.Y * scale.Y);
                    Color before = idle.GetPixel(x, y), after = hover.GetPixel(x, y);
                    Check(Mathf.Abs(before.R - after.R) < 2f / 255 && Mathf.Abs(before.G - after.G) < 2f / 255 &&
                        Mathf.Abs(before.B - after.B) < 2f / 255,
                        "Empty side margins must receive neither outline nor hover tint");
                    if (size.X == 1280)
                        hover.SavePng(Path.Combine(Path.GetTempPath(), "chikago-hf73-back-en.png"));
                }
                PressButtonPointer(control.GetCenter(), true);
                PressButtonPointer(control.GetCenter(), false);
                await ToSignal(this, SignalName.ProcessFrame);
                Check(menu.GetNode<Control>("MainMenuContainer").Visible,
                    "The original Back hitbox and action must still work");
            }
        }
        finally
        {
            menu.QueueFree();
            TranslationServer.SetLocale(originalLocale);
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task<Image> CaptureButtonFeedback(string name = null)
    {
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = Root.GetTexture().GetImage();
        Check(image != null && !image.IsEmpty(), "Button feedback tests need a rendered GPU viewport");
        if (name != null) image.SavePng(Path.Combine(Path.GetTempPath(), "chikago-hf68-" + name + ".png"));
        return image;
    }

    private void MoveButtonPointer(Vector2 point) => Root.PushInput(new InputEventMouseMotion
        { Position = point, GlobalPosition = point }, true);

    private void PressButtonPointer(Vector2 point, bool pressed) => Root.PushInput(new InputEventMouseButton
    {
        Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left,
        Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0
    }, true);

    private async Task SettleButtonFeedback()
    {
        await ToSignal(CreateTimer(0.25, true), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestButtonFeedback()
    {
        Check(DisplayServer.GetName() != "headless", "Run button feedback checks without --headless");
        var controller = ButtonFeedbackController.Instance;
        Check(controller != null, "Button feedback must be autoloaded");
        var layer = new CanvasLayer { Layer = 150, ProcessMode = Node.ProcessModeEnum.Always };
        Root.AddChild(layer);
        var background = new ColorRect { Color = new Color(0.12f, 0.12f, 0.12f), MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var accept = new Button { Name = "AcceptButton", Text = "Confirm / buy", Position = new Vector2(80, 100), Size = new Vector2(260, 70) };
        var decline = new Button { Name = "DeclineButton", Text = "Refuse / exit", Position = new Vector2(80, 200), Size = new Vector2(260, 70) };
        var toggle = new CheckButton { Name = "SettingsButton", Text = "Setting", Position = new Vector2(80, 300), Size = new Vector2(260, 70) };
        using var textureImage = Image.CreateEmpty(200, 60, false, Image.Format.Rgba8);
        textureImage.Fill(Colors.White);
        var textureButton = new TextureButton
        {
            Name = "ContinueButton", TextureNormal = ImageTexture.CreateFromImage(textureImage),
            Position = new Vector2(400, 100), Size = new Vector2(200, 60)
        };
        layer.AddChild(accept);
        layer.AddChild(decline);
        layer.AddChild(toggle);
        layer.AddChild(textureButton);
        int accepted = 0, declined = 0, toggled = 0, continued = 0;
        accept.Pressed += () => accepted++;
        decline.Pressed += () => declined++;
        toggle.Toggled += value => toggled++;
        textureButton.Pressed += () => continued++;
        Vector2I originalSize = Root.Size;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            MoveButtonPointer(new Vector2(700, 500));
            await SettleButtonFeedback();
            Rect2 originalBounds = accept.GetGlobalRect();
            Vector2 point = ScreenBounds(accept).GetCenter();
            MoveButtonPointer(point);
            await SettleButtonFeedback();
            var frame = controller.GetVisualFrame();
            Check(frame.Strength > 0.9f && frame.Flash == 0 && frame.Pressed == 0,
                "Hover must smoothly settle with no persistent color or press state");
            Check(accept.MouseDefaultCursorShape == Control.CursorShape.PointingHand && accept.GetGlobalRect() == originalBounds,
                "Hover must show a hand without changing layout or hitboxes");
            using (var image = await CaptureButtonFeedback("hover-gray")) CheckRenderedGray(image);

            decline.GrabFocus();
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Tab }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Tab }, true);
            await SettleButtonFeedback();
            var tabFocus = Root.GuiGetFocusOwner() as BaseButton;
            Check(tabFocus != null && controller.GetVisualFrame().Bounds == ScreenBounds(tabFocus),
                $"Keyboard navigation must show focus under a stationary pointer: focus={tabFocus?.Name}, actual={controller.GetVisualFrame().Bounds}, expected={(tabFocus != null ? ScreenBounds(tabFocus) : default)}");
            MoveButtonPointer(point);
            await SettleButtonFeedback();

            PressButtonPointer(point, true);
            using (var image = await CaptureButtonFeedback("confirm-flash"))
            {
                frame = controller.GetVisualFrame();
                Check(frame.Flash > 0 && frame.Accent.G > frame.Accent.R && frame.Pressed == 1,
                    "A confirm press must briefly flash green and show a depressed state");
                Check(CountRenderedColor(image, ScreenBounds(accept)) > 20, "Green action color must survive the monochrome filter");
                CheckRenderedGray(image, ScreenBounds(accept));
            }
            Check(accepted == 0, "Visual press feedback must never activate a release-mode button early");
            await SettleButtonFeedback();
            using (var image = await CaptureButtonFeedback("held-gray")) CheckRenderedGray(image);
            Check(controller.GetVisualFrame().Flash == 0 && controller.GetVisualFrame().Pressed == 1,
                "A held press must return to grayscale after the short flash");
            PressButtonPointer(point, false);
            Check(accepted == 1, "One mouse click must invoke the original action exactly once");
            await SettleButtonFeedback();

            point = ScreenBounds(decline).GetCenter();
            MoveButtonPointer(point);
            await SettleButtonFeedback();
            PressButtonPointer(point, true);
            using (var image = await CaptureButtonFeedback("refuse-flash"))
            {
                frame = controller.GetVisualFrame();
                Check(frame.Flash > 0 && frame.Accent.R > frame.Accent.G, "Refusal must briefly flash red");
                Check(CountRenderedColor(image, ScreenBounds(decline)) > 20, "The refusal flash must actually render");
                CheckRenderedGray(image, ScreenBounds(decline));
            }
            // Dragging out cancels the real click; the presentation layer must not replay it.
            MoveButtonPointer(new Vector2(700, 500));
            PressButtonPointer(new Vector2(700, 500), false);
            await SettleButtonFeedback();
            Check(declined == 0, "A cancelled mouse press must not execute an action");

            accept.Disabled = true;
            point = ScreenBounds(accept).GetCenter();
            MoveButtonPointer(point);
            PressButtonPointer(point, true);
            PressButtonPointer(point, false);
            await SettleButtonFeedback();
            Check(accepted == 1 && accept.MouseDefaultCursorShape == Control.CursorShape.Forbidden && controller.GetVisualFrame().Flash == 0,
                "Disabled controls must not flash or activate and must show the unavailable cursor");
            using (var image = await CaptureButtonFeedback("disabled-gray")) CheckRenderedGray(image);

            accept.Disabled = false;
            await SettleButtonFeedback();
            Check(controller.GetVisualFrame().Bounds == ScreenBounds(accept) && controller.GetVisualFrame().Flash == 0,
                "Re-enabling under a stationary pointer must restore only grayscale hover");
            MoveButtonPointer(new Vector2(700, 500));
            accept.GrabFocus();
            await SettleButtonFeedback();
            using (var image = await CaptureButtonFeedback("keyboard-focus")) CheckRenderedGray(image);
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Space }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Space }, true);
            Check(accepted == 2, "Keyboard activation must remain one original action");
            await SettleButtonFeedback();

            point = ScreenBounds(toggle).GetCenter();
            MoveButtonPointer(point);
            PressButtonPointer(point, true);
            PressButtonPointer(point, false);
            Check(toggled == 1 && toggle.ButtonPressed, "CheckButton must toggle exactly once");
            await SettleButtonFeedback();

            point = ScreenBounds(textureButton).GetCenter();
            MoveButtonPointer(point);
            PressButtonPointer(point, true);
            using (var image = await CaptureButtonFeedback("texture-button"))
                Check(CountRenderedColor(image, ScreenBounds(textureButton)) > 20, "Runtime TextureButtons need the same action flash");
            PressButtonPointer(point, false);
            Check(continued == 1, "TextureButton click must remain one action");
            textureButton.Hide();
            await ToSignal(this, SignalName.ProcessFrame);
            Check(controller.GetVisualFrame().Flash == 0, "Hiding a flashing button must remove its accent immediately");

            accept.GrabFocus();
            Paused = true;
            await SettleButtonFeedback();
            Check(controller.GetVisualFrame().Strength > 0, "Feedback must remain active for always-processing pause/card controls");
            Root.Size = new Vector2I(960, 540);
            await SettleButtonFeedback();
            Check(controller.GetVisualFrame().Bounds == ScreenBounds(accept), "Feedback must follow viewport resize");
            Paused = false;
            accept.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            await SettleButtonFeedback();
            using (var image = await CaptureButtonFeedback()) CheckRenderedGray(image);
            Check(controller.GetVisualFrame().Flash == 0, "Freed buttons must not leave stale color feedback");
        }
        finally
        {
            Paused = false;
            Root.Size = originalSize;
            layer.QueueFree();
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestButtonFeedbackInGame()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var camera = game.GetNode<Camera2D>("Camera2D");
        camera.SetProcess(false);
        Vector2I originalSize = Root.Size;
        var controller = ButtonFeedbackController.Instance;
        try
        {
            game.NewGame();
            game.Score = 1000;
            game.RefreshItemButtons();
            var craft = game.GetNode<Button>("World/Atelier/GetItemButton");
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (float zoom in new[] { 0.25f, 1.8f })
            {
                Root.Size = size;
                camera.Zoom = Vector2.One * zoom;
                camera.Position = craft.GetGlobalRect().GetCenter();
                camera.ForceUpdateScroll();
                MoveButtonPointer(new Vector2(-20, -20));
                craft.GrabFocus();
                await SettleButtonFeedback();
                Check(controller.GetVisualFrame().Bounds == ScreenBounds(craft).Intersection(Root.GetVisibleRect()),
                    "Real world-button feedback must track camera zoom and viewport size");
            }
            using (var image = await CaptureButtonFeedback("craft-focus")) CheckRenderedGray(image);

            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            await SettleButtonFeedback();
            var resume = game.GetNode<Button>("PauseMenu/Root/Overlay/Center/Panel/Content/Menu/ResumeButton");
            Check(controller.GetVisualFrame().Bounds == ScreenBounds(resume),
                "A focused pause button must show feedback while gameplay is paused");
            using (var image = await CaptureButtonFeedback("pause-focus")) CheckRenderedGray(image);
            const string pauseContent = "PauseMenu/Root/Overlay/Center/Panel/Content/";
            int cashBeforeHover = game.Score;
            int dayBeforeHover = game.CurrentDay;
            BaseButton lastHovered = resume;
            foreach (string name in new[] { "SettingsButton", "CheckpointButton", "NewGameButton", "MainMenuButton" })
            {
                var choice = game.GetNode<Button>(pauseContent + "Menu/" + name);
                MoveButtonPointer(ScreenBounds(choice).GetCenter());
                await SettleButtonFeedback();
                MoveButtonPointer(new Vector2(2, 2));
                await SettleButtonFeedback();
                Check(Root.GuiGetFocusOwner() == choice && controller.GetVisualFrame().Bounds == ScreenBounds(choice) &&
                    controller.GetVisualFrame().Strength > 0.9f && controller.GetVisualFrame().Flash == 0,
                    "Pause main menu must retain the last hovered button after leaving it, without an action flash");
                lastHovered = choice;
            }
            Check(game.Score == cashBeforeHover && game.CurrentDay == dayBeforeHover && Paused,
                "Hover selection must not execute menu actions or alter gameplay");
            pause.Close();
            pause.Open();
            await SettleButtonFeedback();
            Check(Root.GuiGetFocusOwner() == lastHovered, "Reopening pause must restore its remembered hover selection");
            await ClickControl(game, pauseContent + "Menu/SettingsButton");
            var russian = game.GetNode<Button>(pauseContent + "Settings/LanguageRow/RussianButton");
            MoveButtonPointer(ScreenBounds(russian).GetCenter());
            await SettleButtonFeedback();
            MoveButtonPointer(new Vector2(2, 2));
            await SettleButtonFeedback();
            Check(Root.GuiGetFocusOwner() != russian && controller.GetVisualFrame().Bounds != ScreenBounds(russian),
                "Persistent hover selection must not apply to pause settings controls");
            await PressEscapeForTest();
            Check(Root.GuiGetFocusOwner() == game.GetNode<Button>(pauseContent + "Menu/SettingsButton"),
                "Returning from settings must restore the last hovered main-menu choice");
            pause.Close();

            var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
            card.Open(1, 2, new[] { ChikagoShop.Domain.Days.DayUpgrade.Cash,
                ChikagoShop.Domain.Days.DayUpgrade.Energy, ChikagoShop.Domain.Days.DayUpgrade.Time }, 1000);
            await WaitForDayCardAnimation(card);
            await SettleButtonFeedback();
            Check(Root.GuiGetFocusOwner() == null && controller.GetVisualFrame().Strength == 0,
                "Opening day cards must not automatically focus or outline a purchase");
            using (var image = await CaptureButtonFeedback("card-no-selection")) CheckRenderedGray(image);
            int choicesMade = 0;
            card.Chosen += choice => choicesMade++;
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Right }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Right }, true);
            await SettleButtonFeedback();
            var focusedChoice = Root.GuiGetFocusOwner() as BaseButton;
            Check(focusedChoice != null && card.IsAncestorOf(focusedChoice) &&
                controller.GetVisualFrame().Bounds == ScreenBounds(focusedChoice),
                "The first arrow must reveal keyboard focus while paused");
            Check(choicesMade == 0, "Starting keyboard navigation must not purchase an upgrade");
            using (var image = await CaptureButtonFeedback("card-focus")) CheckRenderedGray(image);
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Right }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Right }, true);
            await SettleButtonFeedback();
            Check(Root.GuiGetFocusOwner() != focusedChoice && choicesMade == 0,
                "Subsequent arrows must navigate without buying");
            MoveButtonPointer(ScreenBounds(focusedChoice).GetCenter());
            await SettleButtonFeedback();
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Right }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Right }, true);
            await SettleButtonFeedback();
            var keyboardChoice = Root.GuiGetFocusOwner() as BaseButton;
            Check(keyboardChoice != null && keyboardChoice != focusedChoice &&
                controller.GetVisualFrame().Bounds == ScreenBounds(keyboardChoice),
                "Consumed card navigation must highlight keyboard selection even if the pointer rests over another choice");
            MoveButtonPointer(new Vector2(2, 2));
            card.Close(); // This visual fixture opened the card without a pending domain upgrade.
            foreach (int balance in new[] { 1000, 20, 0 })
            {
                card.Open(1, 2, new[] { ChikagoShop.Domain.Days.DayUpgrade.Cash,
                    ChikagoShop.Domain.Days.DayUpgrade.Energy, ChikagoShop.Domain.Days.DayUpgrade.Time }, balance);
                await WaitForDayCardAnimation(card);
                await SettleButtonFeedback();
                Check(Root.GuiGetFocusOwner() == null && controller.GetVisualFrame().Strength == 0,
                    "Reopening cards must reset selection even after earlier keyboard use");
                Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Down }, true);
                Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Down }, true);
                await SettleButtonFeedback();
                var selected = Root.GuiGetFocusOwner() as BaseButton;
                Check(selected != null && !selected.Disabled && (balance != 0 || selected.Name == "Skip"),
                    "Keyboard selection must skip disabled purchases and fall back to Skip");
                const string choicePath = "Root/Center/Card/Content/Choices/";
                BaseButton[] expected = balance == 0
                    ? new BaseButton[] { card.GetNode<Button>("Root/Center/Card/Content/Skip") }
                    : balance == 20
                        ? new BaseButton[] { card.GetNode<Button>(choicePath + "Cash/Content/Choose"),
                            card.GetNode<Button>("Root/Center/Card/Content/Skip") }
                        : new BaseButton[] { card.GetNode<Button>(choicePath + "Cash/Content/Choose"),
                            card.GetNode<Button>(choicePath + "Energy/Content/Choose"),
                            card.GetNode<Button>(choicePath + "Time/Content/Choose"),
                            card.GetNode<Button>("Root/Center/Card/Content/Skip") };
                Check(selected == expected[0], "First navigation must select the first available action");
                foreach (Key direction in new[] { Key.Right, Key.Down, Key.Left, Key.Up })
                for (int step = 1; step <= expected.Length; step++)
                {
                    Root.PushInput(new InputEventKey { Pressed = true, Keycode = direction }, true);
                    Root.PushInput(new InputEventKey { Pressed = false, Keycode = direction }, true);
                    await ToSignal(this, SignalName.ProcessFrame);
                    int index = direction is Key.Left or Key.Up
                        ? (expected.Length - step % expected.Length) % expected.Length : step % expected.Length;
                    Check(Root.GuiGetFocusOwner() == expected[index] && choicesMade == 0 && card.IsOpen,
                        $"{direction} must visit every available choice and wrap, balance={balance}, step={step}");
                }
                Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Tab }, true);
                Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Tab }, true);
                Check(Root.GuiGetFocusOwner() == expected[1 % expected.Length], "Tab must follow the same available choices");
                Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Tab, ShiftPressed = true }, true);
                Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Tab, ShiftPressed = true }, true);
                Check(Root.GuiGetFocusOwner() == expected[0] && choicesMade == 0 && card.IsOpen,
                    "Shift-Tab must traverse backward without activating Buy or Skip");
                card.Close();
            }

            var miniGame = game.GetNode<EmbroideryGame>("EmbroideryGame");
            miniGame.Open();
            await SettleButtonFeedback();
            var focusedAction = Root.GuiGetFocusOwner() as BaseButton;
            Check(miniGame.IsOpen && focusedAction != null && miniGame.IsAncestorOf(focusedAction) &&
                controller.GetVisualFrame().Bounds == ScreenBounds(focusedAction),
                "Crafting action feedback must remain active behind the covering-modal grayscale policy");
            using (var image = await CaptureButtonFeedback("minigame-focus")) CheckRenderedGray(image);
            miniGame._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
        }
        finally
        {
            Paused = false;
            game.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
        Check(controller.GetVisualFrame().Strength == 0, "Leaving the game must clear its button feedback");
    }
}
