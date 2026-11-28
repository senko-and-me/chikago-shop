using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestDialogueTypingAudio()
    {
        Check(OS.GetUserDataDir().Contains("SC218"), "Typing audio checks require isolated settings");
        double volume = SoundEffects.Instance.Volume;
        string locale = TranslationServer.GetLocale();
        DialoguePlayer dialogue = null;
        MafiaQuotaNotice notice = null;
        DialogueTypingAudio probe = null;
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        try
        {
            SoundEffects.Instance.PreviewVolume(0);
            Check(AudioServer.IsBusMute(AudioServer.GetBusIndex(SoundEffects.BusName)), "Typing voices must share the muted SFX bus");
            probe = GD.Load<PackedScene>("res://scenes/Audio/DialogueTypingAudio.tscn").Instantiate<DialogueTypingAudio>();
            Root.AddChild(probe);
            probe.SetProcess(false);
            Check(probe.Samples.Count == 4 && probe.Samples.All(sample => sample.GetLength() > 0), "All four provided recordings must be imported");
            for (int sample = 0; sample < probe.Samples.Count; sample++)
            {
                var wav = (AudioStreamWav)probe.Samples[sample];
                Check(wav.Format == AudioStreamWav.FormatEnum.Format16Bits, "Imported recordings must expose PCM for audible-onset verification");
                int stride = (wav.Stereo ? 2 : 1) * 2;
                int start = (int)(probe.StartOffsets[sample] * wav.MixRate) * stride;
                int end = Math.Min(wav.Data.Length, start + (int)(probe.SnippetSeconds * wav.MixRate) * stride);
                bool audible = false;
                byte[] pcm = wav.Data;
                for (int offset = start; offset + 1 < end; offset += 2)
                    audible |= Math.Abs((int)System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(offset, 2))) > 100;
                Check(audible, "Every configured snippet must contain an actual click rather than leading silence");
            }
            Check(probe.GetNode<AudioStreamPlayer>("Voice1").Bus == SoundEffects.BusName &&
                probe.GetNode<AudioStreamPlayer>("Voice2").Bus == SoundEffects.BusName, "Both voices must obey sound settings");
            var heard = new List<int>();
            probe.SamplePlayed += heard.Add;
            probe.BeginPage(new string('я', 300), true);
            for (int character = 1; character <= 128; character++)
            {
                probe.Advance(character, 0.05);
                Check(probe.ActiveVoiceCount is > 0 and <= 2, "Actual playback must remain bounded to two voices");
                probe._Process(0.05);
            }
            Check(heard.Count == 128 && heard.Distinct().Count() == 4 &&
                heard.Zip(heard.Skip(1)).All(pair => pair.First != pair.Second), "Random playback must use every recording without consecutive repeats");
            int count = heard.Count;
            probe.BeginPage(" ,.!?—\n", true);
            probe.Advance(7, 1);
            probe.BeginPage("Текст повествователя", false);
            probe.Advance(20, 1);
            probe.BeginPage("*Смеётся.* Да", true);
            probe.Advance(11, 1);
            Check(heard.Count == count, "Spaces, punctuation, narration and stage directions must stay silent");
            probe.Advance(13, 0.1);
            Check(heard.Count == ++count, "Speech following a stage direction must resume typing sounds");
            probe.BeginPage(new string('a', 1000), true);
            probe.Advance(1000, 10);
            Check(heard.Count == ++count, "A slow frame must produce one tick rather than a sound backlog");
            probe._Process(1);
            Check(probe.ActiveVoiceCount == 0, "Long recordings must stop after the short configured snippet");
            probe.BeginPage("Previously revealed", true, 19);
            probe.Advance(19, 1);
            Check(heard.Count == count, "Restored/reflowed visible characters must not replay their sounds");
            probe.Stop();

            dialogue = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
            Root.AddChild(dialogue);
            dialogue.SetProcess(false);
            await Frame();
            var audio = dialogue.GetNode<DialogueTypingAudio>("DialogueTypingAudio");
            int ticks = 0;
            audio.SamplePlayed += _ => ticks++;
            dialogue.Advance(); // Reveal punctuation-only opening.
            dialogue.Advance(); // Start Frankie's first spoken sentence.
            dialogue._Process(0.1);
            Check(ticks == 1 && audio.ActiveVoiceCount > 0, "Real prologue reveal must play a supplied sound");
            int before = ticks;
            dialogue.SetProcess(true);
            Paused = true;
            await Frame();
            await Frame();
            Check(ticks == before, "Pausing a pausable dialogue must pause its sound cadence");
            Paused = false;
            dialogue.SetProcess(false);
            TranslationServer.SetLocale("en");
            Check(audio.ActiveVoiceCount == 0 && ticks == before, "Language/page refresh must stop existing voices without catch-up sounds");
            dialogue._Process(0.1);
            Check(ticks == before + 1, "English letters must trigger the same random typing mechanism");
            before = ticks;
            dialogue.Advance();
            Check(audio.ActiveVoiceCount == 0 && ticks == before, "Instant full-text reveal must stop audio without sounding hidden letters");
            dialogue.StopInteraction();
            dialogue._Process(1);
            Check(ticks == before, "Stopped dialogue must remain silent");
            dialogue.QueueFree(); dialogue = null;
            await Frame();

            dialogue = GD.Load<PackedScene>("res://scenes/UI/FamilyDialoguePlayer.tscn").Instantiate<DialoguePlayer>();
            dialogue.IsPrologue = false;
            dialogue.UseWorldScene = true;
            dialogue.DialoguePath = "res://dialogue/quota_visitor.ru.json";
            Root.AddChild(dialogue);
            dialogue.SetProcess(false);
            await Frame();
            audio = dialogue.GetNode<DialogueTypingAudio>("DialogueTypingAudio");
            int familyTicks = 0;
            audio.SamplePlayed += _ => familyTicks++;
            dialogue._Process(0.1);
            Check(familyTicks == 1, "The son's world-scene conversation must use typing audio too");
            dialogue.GetNode<TextureButton>("SkipDialogue").EmitSignal(BaseButton.SignalName.Pressed);
            Check(audio.ActiveVoiceCount == 0, "Skipping the son's dialogue must stop its voices");
            dialogue.QueueFree(); dialogue = null;
            await Frame();

            notice = new MafiaQuotaNotice();
            Root.AddChild(notice);
            foreach (var outcome in new[] { (true, false), (true, true), (false, true) })
            {
                notice.Open(0, 0, outcome.Item1, outcome.Item2);
                notice.SetProcess(false);
                var quotaAudio = notice.GetNode<DialogueTypingAudio>("DialogueTypingAudio");
                int quotaTicks = 0;
                void Tick(int _) => quotaTicks++;
                quotaAudio.SamplePlayed += Tick;
                int skips = 0;
                while (skips++ < 30 &&
                    (notice.GetNode<Label>("StoryRoot/Speech/Content/Message").Text.TrimStart().StartsWith('*') ||
                     !notice.GetNode<Label>("StoryRoot/Speech/Content/Message").Text.Any(char.IsLetter)))
                {
                    notice._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
                    notice._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
                }
                notice._Process(0.1);
                Check(Paused && quotaTicks == 1 && quotaAudio.ActiveVoiceCount > 0,
                    $"Quota/ending letters must sound during gameplay pause: {outcome}, paused={Paused}, ticks={quotaTicks}, voices={quotaAudio.ActiveVoiceCount}, text={notice.GetNode<Label>("StoryRoot/Speech/Content/Message").Text}");
                notice._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
                Check(quotaTicks == 1 && quotaAudio.ActiveVoiceCount == 0, "Quota full-text reveal must not burst-play letters");
                notice.CloseSilently();
                await Frame();
                Check(quotaTicks == 1 && quotaAudio.ActiveVoiceCount == 0 && !Paused, "Closing quota/ending dialogue must stop and release audio");
                quotaAudio.SamplePlayed -= Tick;
            }
        }
        finally
        {
            notice?.CloseSilently();
            if (GodotObject.IsInstanceValid(notice)) notice.QueueFree();
            if (GodotObject.IsInstanceValid(dialogue)) dialogue.QueueFree();
            if (GodotObject.IsInstanceValid(probe)) probe.QueueFree();
            Paused = false;
            TranslationServer.SetLocale(locale);
            SoundEffects.Instance.PreviewVolume(volume);
            await Frame();
        }
    }
}
