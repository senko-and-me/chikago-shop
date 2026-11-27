using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestStoryDialogueLayout()
    {
        var originalSize = Root.Size;
        string locale = TranslationServer.GetLocale();
        var menuScene = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
        var storyScene = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn");
        Node fixture = null;
        try
        {
            async Task CheckSpeaking(Control speaker, Control listener = null)
            {
                await ToSignal(CreateTimer(0.52), SceneTreeTimer.SignalName.Timeout);
                Check(speaker.Scale.IsEqualApprox(Vector2.One * 1.06f) &&
                    speaker.PivotOffset.IsEqualApprox(speaker.Size / 2) &&
                    (listener == null || listener.Scale.IsEqualApprox(Vector2.One)),
                    $"Incorrect speaking portrait {speaker.Name}: scale={speaker.Scale}, pivot={speaker.PivotOffset}, size={speaker.Size}, listener={listener?.Scale}");
                Check(Root.GetVisibleRect().Encloses(ScreenBounds(speaker)),
                    "The animated portrait must stay on screen");
            }
            foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            {
                Root.Size = size;
                var menu = menuScene.Instantiate<MainMenu>();
                fixture = menu;
                Root.AddChild(menu);
                menu.OnPlayPressed();
                menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
                await SkipStartupTraining(menu);
                TranslationServer.SetLocale("ru");
                var prologue = menu.ActiveDialogue;
                AdvanceMusicLine(prologue);
                var castello = prologue.GetNode<TextureRect>("Characters/Castello");
                Check(!prologue.HasNode("Characters/Customer"), "The prologue must contain only Castello and Frankie");
                await CheckSpeaking(prologue.GetNode<TextureRect>("Characters/Frankie"), castello);
                await ToSignal(CreateTimer(0.57), SceneTreeTimer.SignalName.Timeout);
                prologue.RestorePage(0, true);
                await ToSignal(CreateTimer(0.46), SceneTreeTimer.SignalName.Timeout);
                Check(castello.Scale.IsEqualApprox(Vector2.One), "Revealing the complete line must release the speaking focus");
                using (var frame = await CapturePresentation($"story-prologue-{size.X}"))
                    Check(frame.GetSize() == size, $"Expected a native {size} screenshot, got {frame.GetSize()}");
                CheckStoryComposition(prologue, "Panel", "Castello", "Frankie");
                prologue.StopInteraction();
                menu.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);

                var notice = new MafiaQuotaNotice();
                fixture = notice;
                Root.AddChild(notice);
                foreach (var ending in new[] { ("mafia", true, false), ("victory", true, true), ("defeat", false, false) })
                {
                    TranslationServer.SetLocale(ending.Item3 ? "en" : "ru");
                    notice.Open(750, 3500, ending.Item2, ending.Item3);
                    if (ending.Item1 != "mafia")
                    {
                        Check(!notice.GetNode<Control>("StoryRoot/Speech/Content/Actions/Restart").Visible,
                            "Result actions must remain hidden until the ending dialogue finishes");
                        notice._Process(5);
                        notice._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
                        Check(notice.ConversationIndex == 1 && !notice.StoryFinished,
                            "Revealing and advancing the opening stage direction must not skip the ending");
                    }
                    var left = notice.GetNode<TextureRect>("StoryRoot/Characters/Castello");
                    var right = notice.GetNode<TextureRect>("StoryRoot/Characters/Frankie");
                    var speaking = ending.Item1 == "mafia" ? right : left;
                    var listening = ending.Item1 == "mafia" ? left : right;
                    Check(speaking.Texture.ResourcePath.EndsWith("_open.png") &&
                          !listening.Texture.ResourcePath.EndsWith("_open.png"),
                        "Only the current speaker must speak in the quota notices");
                    await CheckSpeaking(speaking, listening);
                    notice._Process(5);
                    Check(left.Texture.ResourcePath.EndsWith("_closed.png"),
                        "Castello must close his mouth when the quota speech is fully visible");
                    await ToSignal(CreateTimer(0.46), SceneTreeTimer.SignalName.Timeout);
                    Check(left.Scale.IsEqualApprox(Vector2.One), "The notice portrait must shrink when the line completes");
                    using (await CapturePresentation($"story-{ending.Item1}-{size.X}")) { }
                    CheckStoryComposition(notice.GetNode<Control>("StoryRoot"), "Speech", "Castello", "Frankie");
                    var actions = notice.GetNode<Control>("StoryRoot/Speech/Content/Actions");
                    foreach (Button button in actions.GetChildren())
                        if (button.Visible)
                            Check(actions.GetGlobalRect().Grow(1).Encloses(button.GetGlobalRect()),
                                $"The {button.Name} action must fit inside the dialogue at {size}");
                    if (ending.Item1 != "mafia")
                    {
                        var session = ChikagoShop.Domain.Dialogue.DialogueSession.FromJson(FileAccess.GetFileAsString(
                            ending.Item1 == "victory" ? "res://dialogue/victory.ru.json" : "res://dialogue/defeat.ru.json"));
                        int[] seen = new int[session.Count];
                        for (int input = 0; input < 200 && !notice.StoryFinished; input++)
                        {
                            seen[notice.ConversationIndex]++;
                            notice._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
                        }
                        Check(notice.StoryFinished && notice.IsOpen && Paused &&
                            notice.GetNode<Control>("StoryRoot/Speech/Content/Actions/Restart").Visible &&
                            notice.GetNode<Control>("StoryRoot/Speech/Content/Actions/MainMenu").Visible &&
                            notice.GetNode<Control>("StoryRoot/Speech/Content/Actions/Checkpoint").Visible == !ending.Item2,
                            "Both endings must reach their result actions without closing or unpausing the scene");
                        for (int i = 1; i < seen.Length; i++) Check(seen[i] > 0, "No ending bubble may be lost");
                        notice.Close();
                        notice.Open(750, 3500, ending.Item2, ending.Item3);
                        notice.GetNode<TextureButton>("StoryRoot/SkipDialogue").EmitSignal(BaseButton.SignalName.Pressed);
                        Check(notice.StoryFinished && notice.IsOpen && Paused,
                            "Skipping an ending must reveal only its result actions and keep gameplay paused");
                    }
                    notice.Close();
                }
                notice.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);

                var son = GD.Load<PackedScene>("res://scenes/UI/FamilyDialoguePlayer.tscn").Instantiate<DialoguePlayer>();
                fixture = son;
                son.IsPrologue = false;
                son.DialoguePath = "res://dialogue/quota_visitor.ru.json";
                Root.AddChild(son);
                await ToSignal(this, SignalName.ProcessFrame);
                Check(son.UseWorldScene && !son.GetNode<Control>("Background").Visible &&
                    !son.GetNode<Control>("Shade").Visible && !son.GetNode<Control>("Characters").Visible &&
                    !son.HasNode("Counter"),
                    "Family UI must leave the real world visible and must not create a replacement room, cast or counter");
                son.RestorePage(0, true);
                await ToSignal(this, SignalName.ProcessFrame);
                Check(Root.GetVisibleRect().Encloses(son.GetNode<Control>("Panel/Content").GetGlobalRect()),
                    "The prologue speech window must remain readable over the world");
                AdvanceMusicLine(son);
                AdvanceMusicLine(son);
                Check(son.Session.Current.Speaker == "\u0424\u0440\u0435\u043d\u043a\u0438", "The father must answer the son");
                son.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        finally
        {
            if (GodotObject.IsInstanceValid(fixture)) fixture.QueueFree();
            Paused = false;
            Root.Size = originalSize;
            TranslationServer.SetLocale(locale);
        }
    }

    private static void CheckStoryComposition(Control root, string panel, params string[] portraits)
    {
        Rect2 bounds = root.GetGlobalRect();
        Rect2 content = root.GetNode<Control>(panel + "/Content").GetGlobalRect();
        Check(bounds.Grow(1).Encloses(content), "The dialogue text and choices must remain on screen");
        var cloud = root.GetNode<Control>(panel);
        Check(cloud.GetIndex() > root.GetNode<Control>("Characters").GetIndex(),
            "The dialogue cloud must draw in front of the cut-off portrait edges");
        foreach (string name in portraits)
        {
            var portrait = root.GetNode<TextureRect>("Characters/" + name);
            Rect2 slot = portrait.GetGlobalRect();
            Vector2 texture = portrait.Texture.GetSize();
            float fit = Mathf.Min(slot.Size.X / texture.X, slot.Size.Y / texture.Y);
            Vector2 drawSize = texture * fit;
            Rect2 art = new(slot.GetCenter() - drawSize / 2, drawSize);
            Check(portrait.IsVisibleInTree() && portrait.Modulate.A == 1 &&
                portrait.StretchMode == TextureRect.StretchModeEnum.KeepAspectCentered &&
                bounds.Encloses(art) && art.End.Y > cloud.GetGlobalRect().Position.Y - 43 &&
                art.End.Y < content.Position.Y + bounds.Size.Y * 0.06f &&
                art.Size.Y >= bounds.Size.Y * 0.45f,
                $"{name} must tuck into the cloud, remain in proportion and at least 45% of screen height: art={art}, bounds={bounds}, content={content}, cloud={cloud.GetGlobalRect()}, scale={portrait.Scale}, alpha={portrait.Modulate.A}");
        }
    }
}
