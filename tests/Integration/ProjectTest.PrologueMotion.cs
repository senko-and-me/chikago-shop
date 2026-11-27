using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestPrologueMotion()
    {
        Vector2I oldSize = Root.Size;
        string locale = TranslationServer.GetLocale();
        DialoguePlayer player = null;
        async Task Wait(double time) => await ToSignal(CreateTimer(time), SceneTreeTimer.SignalName.Timeout);
        try
        {
            foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                Root.Size = size;
                player = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
                Root.AddChild(player);
                player.SetProcess(false);
                await Wait(0.05); // Allow the dialogue panel to measure its first text page.
                var motion = player.GetNode<PrologueCharacterMotion>("Characters");
                var mafia = motion.GetNode<TextureRect>("Castello");
                Check(!player.HasNode("Characters/Customer") && player.Session.Current.Speaker == "Кастелло" &&
                    mafia.Visible && motion.IsMoving,
                    "The prologue must begin with Castello entering, with no customer portrait");
                await Wait(0.18);
                Check(motion.ArrivalProgress > 0 && motion.ArrivalProgress < 1 &&
                    mafia.Modulate.A > 0 && mafia.Modulate.A < 1 && mafia.OffsetLeft < 0,
                    "The entrance must visibly slide and fade toward the authored portrait position");
                using (await CapturePresentation("prologue-arrival-" + size.X)) { }
                TranslationServer.SetLocale("en");
                await Wait(0.6);
                Check(motion.ArrivalProgress == 1 && mafia.Modulate.A == 1 && mafia.OffsetLeft == 0,
                    "Language changes must retain the completed entrance");
                var catalog = GD.Load<Translation>("res://localization/en.tres");
                Check(catalog.GetMessage(player.Session.Current.Text).ToString().Contains("Frankie!"),
                    "Castello's new direct greeting must have an English translation");
                while (!player.Session.IsComplete)
                {
                    Check(player.Session.Current.Speaker != "Покупательница" &&
                        !player.HasNode("Characters/Customer"), "The customer must not appear on any prologue line");
                    if (player.Session.Index > 0)
                        Check(motion.ArrivalProgress == 1 && !motion.IsMoving,
                            "Paging and later speeches must not replay the entrance");
                    if (player.Session.Index == 2)
                    {
                        Root.Size = new Vector2I(size.X - 40, size.Y);
                        TranslationServer.SetLocale("ru");
                        await ToSignal(this, SignalName.ProcessFrame);
                        using (await CapturePresentation("prologue-two-characters-" + size.X)) { }
                    }
                    AdvanceMusicLine(player);
                }
                player.StopInteraction();
                player.QueueFree(); player = null;
                await ToSignal(this, SignalName.ProcessFrame);
            }
            player = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
            Root.AddChild(player);
            await Wait(0.05);
            AdvanceMusicLine(player); AdvanceMusicLine(player);
            var active = player.GetNode<PrologueCharacterMotion>("Characters");
            Check(active.IsMoving, "Rapid advance must not restart or lose the ongoing Castello arrival");
            player.StopInteraction();
            Check(!active.IsMoving, "Skip/cancel must kill all portrait movement immediately");
        }
        finally
        {
            player?.QueueFree();
            Root.Size = oldSize;
            TranslationServer.SetLocale(locale);
            await ToSignal(this, SignalName.ProcessFrame);
            await Wait(0.8);
        }
    }
}
