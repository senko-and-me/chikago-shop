using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{

    private async Task CheckShadowButtonInput(BaseButton button, string capture)
    {
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        Vector2 center = ScreenBounds(button).GetCenter();
        MoveButtonPointer(center);
        using (await CapturePresentation(capture)) { }
        Check(button.IsHovered(), $"Hover must reach {button.Name}, actual {Root.GuiGetHoveredControl()?.GetPath()} at {center}");
        PressButtonPointer(center, true);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(button.IsPressed(), "The shadow must not intercept a press");
        var outside = new Vector2(3, 3);
        MoveButtonPointer(outside);
        PressButtonPointer(outside, false);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(!button.IsPressed() && !button.IsHovered(), "The shadow must not leave a stale press or hover");
    }

    private static void CheckButtonShadow(Control button)
    {
        var shadow = button.GetNode<ButtonDropShadow>("DropShadow");
        var material = shadow.Material as ShaderMaterial;
        Check(shadow.ShowBehindParent && !shadow.IsProcessingInput() && material?.Shader.ResourcePath ==
            "res://shaders/ui_soft_shadow.gdshader" && material.GetShaderParameter("blur_radius").AsSingle() >= 5,
            "A shared blurred visual must draw behind the UI without receiving pointer or focus");
    }
}
