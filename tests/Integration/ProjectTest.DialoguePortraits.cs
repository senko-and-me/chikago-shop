using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestDialoguePortraits()
    {
        Vector2I originalSize = Root.Size;
        string locale = TranslationServer.GetLocale();
        DialoguePlayer player = null;
        try
        {
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                Root.Size = size;
                player = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
                foreach (string name in new[] { "Frankie", "Castello", "Customer" })
                    Check(player.HasNode("Characters/" + name), "All characters must be placed in the scene before _Ready");
                Root.AddChild(player);
                player.SetProcess(false);
                foreach (int index in new[] { 0, 1, 2, 3 })
                {
                    while (player.Session.Index < index) AdvanceMusicLine(player);
                    foreach (string language in new[] { "ru", "en" })
                    {
                        TranslationServer.SetLocale(language);
                        await ToSignal(CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
                        foreach (var entry in new[] { ("Frankie", "Френки"), ("Castello", "Кастелло"), ("Customer", "Покупательница") })
                        {
                            var portrait = player.GetNode<TextureRect>("Characters/" + entry.Item1);
                            float tone = ((ShaderMaterial)portrait.Material).GetShaderParameter("lighten").AsSingle();
                            Check(Mathf.IsEqualApprox(tone, player.Session.Current.Speaker == entry.Item2 ? 0 : 0.32f),
                                "Only the current speaker must keep the original tone; narration lightens everyone in either locale");
                            Check(portrait.MouseFilter == Control.MouseFilterEnum.Ignore &&
                                Root.GetVisibleRect().Encloses(portrait.GetGlobalRect()),
                                "Scene portraits must fit both viewport sizes without intercepting dialogue clicks");
                        }
                    }
                    if (player.IsRevealing) player.Advance();
                    if (size.X == 1280 && index == 0)
                        using (var image = await CapturePresentation("dialogue-customer")) { }
                    if (size.X == 1280 && index == 3)
                        using (var image = await CapturePresentation("dialogue-frankie")) { }
                }
                player.QueueFree();
                player = null;
                await ToSignal(this, SignalName.ProcessFrame);
            }
            // Render a black opaque texel next to transparency to verify real lightening,
            // rather than brightness multiplication (which leaves black unchanged).
            var viewport = new SubViewport { Size = new Vector2I(32, 16), TransparentBg = true,
                World2D = new World2D(), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
            Root.AddChild(viewport);
            try
            {
                using var source = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
                source.SetPixel(0, 0, Colors.Black);
                source.SetPixel(0, 1, new Color(96f / 255, 96f / 255, 96f / 255)); // Frankie's opaque gray hat.
                source.SetPixel(1, 1, new Color(144f / 255, 144f / 255, 144f / 255)); // Castello's under-eye shading.
                var texture = ImageTexture.CreateFromImage(source);
                foreach (int index in new[] { 0, 1 })
                {
                    var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/dialogue_portrait.gdshader") };
                    material.SetShaderParameter("lighten", index == 0 ? 0 : 0.32f);
                    viewport.AddChild(new Sprite2D { Texture = texture, Material = material, Centered = false,
                        Position = new Vector2(index * 16, 0), Scale = Vector2.One * 8, TextureFilter = CanvasItem.TextureFilterEnum.Nearest });
                }
                for (int i = 0; i < 3; i++) await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var pixels = viewport.GetTexture().GetImage();
                Check(pixels.GetPixel(4, 4).R < 0.01f && pixels.GetPixel(4, 4).A > 0.99f &&
                    pixels.GetPixel(20, 4).R > 0.28f && pixels.GetPixel(20, 4).R < 0.36f &&
                    pixels.GetPixel(12, 4).A < 0.01f && pixels.GetPixel(28, 4).A < 0.01f,
                    "GPU output must keep active black unchanged, lift inactive black, and preserve transparent silhouettes");
                Color hat = pixels.GetPixel(20, 12), face = pixels.GetPixel(28, 12);
                Check(hat.R > 0.37f && hat.R < 0.42f && hat.A > 0.99f,
                    "The inactive gray hat must retain its midtone instead of washing into the gray room background");
                Check(Mathf.Abs(face.R - 144f / 255) < 0.01f && face.A > 0.99f &&
                    Mathf.Abs(pixels.GetPixel(4, 12).R - 96f / 255) < 0.01f,
                    "Under-eye midtones must not turn into bright patches, and the speaker's source colors must remain unchanged");
            }
            finally { viewport.QueueFree(); }
        }
        finally
        {
            player?.QueueFree();
            Root.Size = originalSize;
            TranslationServer.SetLocale(locale);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
