using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestStoryPortraitExpressions()
    {
        string locale = TranslationServer.GetLocale();
        var player = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
        try
        {
            TranslationServer.SetLocale("ru");
            Root.AddChild(player);
            player.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            var left = player.GetNode<TextureRect>("Characters/Castello");
            var right = player.GetNode<TextureRect>("Characters/Frankie");

            void CheckPose(string castello, string frankie)
            {
                Check(left.Texture.ResourcePath.GetFile() == $"castello_{castello}.png" &&
                      right.Texture.ResourcePath.GetFile() == $"frankie_{frankie}.png",
                    $"Line {player.Session.Index}: expected {castello}/{frankie}, got {left.Texture.ResourcePath}/{right.Texture.ResourcePath}");
            }

            void GoTo(int index)
            {
                while (player.Session.Index < index) AdvanceMusicLine(player);
                player.RestorePage(0, false);
            }

            CheckPose("smile_closed", "smile_closed"); // The customer is speaking.
            await ToSignal(CreateTimer(0.75), SceneTreeTimer.SignalName.Timeout);
            GoTo(1);
            CheckPose("smile_open", "smile_closed");
            player._Process(0.18);
            CheckPose("smile_closed", "smile_closed");
            player._Process(0.08);
            CheckPose("smile_open", "smile_closed");
            GoTo(2);
            CheckPose("smile_closed", "smile_closed"); // Stage direction.
            await ToSignal(CreateTimer(0.65), SceneTreeTimer.SignalName.Timeout);
            GoTo(3);
            CheckPose("smile_closed", "smile_open");
            player.Advance();
            CheckPose("smile_closed", "smile_closed");
            GoTo(5);
            CheckPose("smile_closed", "smile_open");

            GoTo(6);
            CheckPose("serious_open", "serious");
            player._Process(0.8);
            using (await CapturePresentation("story-expression-serious")) { }
            player.Advance();
            CheckPose("serious_closed", "serious");
            GoTo(7);
            CheckPose("serious_closed", "smile_open");
            GoTo(8);
            CheckPose("smile_open", "serious");
            player._Process(0.8);
            using (await CapturePresentation("story-expression-mockery")) { }
            while (player.IsRevealing || !player.GetNode<RichTextLabel>("Panel/Content/Text").Text.EndsWith("главный дизайнер."))
                player.Advance();
            CheckPose("serious_closed", "serious"); // The line is complete before advancing.
            GoTo(9);
            CheckPose("serious_closed", "serious");
            player.Advance();
            CheckPose("serious_closed", "smile_closed");
            GoTo(10);
            CheckPose("serious_open", "smile_closed");
            GoTo(11);
            CheckPose("serious_closed", "angry_open");
            player._Process(0.8);
            using (await CapturePresentation("story-expression-angry")) { }
            player.Advance();
            CheckPose("serious_closed", "angry_closed");

            TranslationServer.SetLocale("en");
            CheckPose("serious_closed", "angry_open");
            await ToSignal(CreateTimer(.44), SceneTreeTimer.SignalName.Timeout);
            GoTo(12);
            CheckPose("serious_closed", "angry_open");
            await ToSignal(CreateTimer(.12), SceneTreeTimer.SignalName.Timeout);
            Check(right.Scale.IsEqualApprox(Vector2.One * 1.06f), "Consecutive Frankie lines must not restart the focus pulse");
            GoTo(13);
            CheckPose("serious_open", "angry_closed");
            GoTo(25);
            CheckPose("serious_closed", "angry_closed");
        }
        finally
        {
            player.QueueFree();
            TranslationServer.SetLocale(locale);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
