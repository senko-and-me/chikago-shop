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

            CheckPose("smile_closed", "smile_open"); // Frankie reacts first.
            await ToSignal(CreateTimer(0.75), SceneTreeTimer.SignalName.Timeout);
            GoTo(1);
            CheckPose("smile_closed", "smile_open");
            player._Process(0.18);
            CheckPose("smile_closed", "smile_closed");
            player._Process(0.08);
            CheckPose("smile_closed", "smile_open");
            GoTo(2);
            CheckPose("smile_closed", "smile_open");
            GoTo(3);
            CheckPose("smile_open", "smile_closed"); // Castello's separate laugh.
            player.Advance();
            CheckPose("smile_closed", "smile_closed");
            GoTo(4);
            CheckPose("smile_open", "smile_closed");
            GoTo(5);
            CheckPose("serious_open", "smile_closed");
            player._Process(0.8);
            using (await CapturePresentation("story-expression-serious")) { }
            player.Advance();
            CheckPose("serious_closed", "smile_closed");
            GoTo(8);
            CheckPose("serious_closed", "serious"); // Frankie's confused speaking expression.
            player.Advance();
            CheckPose("serious_closed", "smile_closed");
            GoTo(9);
            CheckPose("serious_open", "smile_closed");
            GoTo(11);
            CheckPose("serious_closed", "angry_open");
            player._Process(0.8);
            using (await CapturePresentation("story-expression-angry")) { }
            player.Advance();
            CheckPose("serious_closed", "angry_closed");
            TranslationServer.SetLocale("en");
            CheckPose("serious_closed", "angry_open");
            GoTo(12);
            CheckPose("serious_open", "angry_closed");
            GoTo(13);
            CheckPose("serious_closed", "angry_open");
            GoTo(player.Session.Count - 1);
            CheckPose("serious_open", "angry_closed");
            player.Advance();
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
