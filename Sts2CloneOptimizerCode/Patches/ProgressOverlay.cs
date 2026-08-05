using Godot;

namespace Sts2CloneOptimizer.Patches;

/// <summary>
/// A top-of-screen overlay (localized caption + horizontal percentage bar) used while the mod
/// spreads a large batch of card-model clones across multiple frames. Shared by the rest-site
/// Clone throttle and the combat-start deck-populate throttle so both show identical UI.
///
/// Returned as a <see cref="CanvasLayer"/> drawn above the map/combat room; <c>QueueFree()</c> on
/// the layer frees both children.
/// </summary>
internal static class ProgressOverlay
{
    public static CanvasLayer Make(string caption, int total, out ProgressBar bar)
    {
        var layer = new CanvasLayer { Name = "Sts2CloneOptimizer_Progress", Layer = 128 };

        var label = new Label
        {
            Text = caption,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AnchorLeft = 0f; label.AnchorRight = 1f; label.AnchorTop = 0f; label.AnchorBottom = 0f;
        label.OffsetTop = 64f; label.OffsetBottom = 96f;
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
        layer.AddChild(label);

        bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = total,
            Value = 0,
            ShowPercentage = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        // Span the width with side margins, just under the caption, ~30px tall.
        bar.AnchorLeft = 0f; bar.AnchorRight = 1f; bar.AnchorTop = 0f; bar.AnchorBottom = 0f;
        bar.OffsetLeft = 280f; bar.OffsetRight = -280f; bar.OffsetTop = 102f; bar.OffsetBottom = 134f;
        bar.AddThemeFontSizeOverride("font_size", 20);
        layer.AddChild(bar);

        return layer;
    }
}
