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
                var customer = motion.GetNode<TextureRect>("Customer");
                Check(customer.Visible && customer.Modulate.A == 1 && !mafia.Visible && !motion.IsMoving,
                    "A restarted prologue must open with the customer and no mafia entrance yet");
                AdvanceMusicLine(player);
                Check(player.Session.Index == 1 && mafia.Visible && motion.IsMoving && customer.Visible,
                    "Castello must enter on his first line while the customer hears his offer to pay");
                await Wait(0.18);
                Check(motion.ArrivalProgress > 0 && motion.ArrivalProgress < 1 &&
                    mafia.Modulate.A > 0 && mafia.Modulate.A < 1 && mafia.OffsetLeft < 0,
                    "The entrance must visibly slide and fade toward the authored portrait position");
                using (await CapturePresentation("prologue-arrival-" + size.X)) { }
                TranslationServer.SetLocale("en");
                await Wait(0.6);
                Check(motion.ArrivalProgress == 1 && mafia.Modulate.A == 1 && mafia.OffsetLeft == 0 &&
                    customer.Visible && motion.DepartureProgress == 0,
                    "Localizing or paging his speech must not replay entrance or send the customer away early");
                AdvanceMusicLine(player);
                await Wait(0.2);
                Check(player.Session.Index == 2 && motion.DepartureProgress > 0 && motion.DepartureProgress < 1 &&
                    customer.OffsetLeft < 0 && customer.Modulate.A > 0 && customer.Modulate.A < 1,
                    "After payment the customer must walk away during Frankie's turn narration");
                float progress = motion.DepartureProgress;
                Root.Size = new Vector2I(size.X - 40, size.Y);
                TranslationServer.SetLocale("ru");
                await ToSignal(this, SignalName.ProcessFrame);
                Check(motion.DepartureProgress >= progress,
                    "Resize and locale changes must preserve movement progress");
                using (await CapturePresentation("prologue-departure-" + size.X)) { }
                await Wait(0.5);
                Check(!customer.Visible && customer.Modulate.A == 0 && mafia.Visible && !motion.IsMoving,
                    "The departed customer must stay hidden while Castello remains for the conversation");
                for (int i = 0; i < 3; i++) AdvanceMusicLine(player);
                TranslationServer.SetLocale("en");
                Check(!customer.Visible && motion.DepartureProgress == 1,
                    "Later speeches and translation refresh must not bring the customer back");
                player.StopInteraction();
                player.QueueFree(); player = null;
                await ToSignal(this, SignalName.ProcessFrame);
            }
            player = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
            Root.AddChild(player);
            await Wait(0.05);
            AdvanceMusicLine(player); AdvanceMusicLine(player);
            var active = player.GetNode<PrologueCharacterMotion>("Characters");
            Check(active.IsMoving, "Rapid advance must safely allow arrival and departure to overlap");
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
