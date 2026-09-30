using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using System.Text;

namespace Content.Client._Art.RichText;

public readonly record struct PaperTextDecorations(
    bool Strikethrough = false,
    bool Underline = false);

public sealed class PaperDecoratedFont : Font
{
    private readonly Font _inner;
    private readonly PaperTextDecorations _decorations;

    private PaperDecoratedFont(Font inner, PaperTextDecorations decorations)
    {
        _inner = inner;
        _decorations = decorations;
    }

    public static PaperTextDecorations GetDecorations(Font font)
    {
        return font is PaperDecoratedFont decorated ? decorated._decorations : default;
    }

    public static Font WithDecorations(Font font, PaperTextDecorations decorations)
    {
        var inner = font is PaperDecoratedFont decorated ? decorated._inner : font;
        return decorations == default ? inner : new PaperDecoratedFont(inner, decorations);
    }

    public override int GetAscent(float scale) => _inner.GetAscent(scale);

    public override int GetDescent(float scale) => _inner.GetDescent(scale);

    public override int GetHeight(float scale) => _inner.GetHeight(scale);

    public override int GetLineHeight(float scale) => _inner.GetLineHeight(scale);

    public override CharMetrics? GetCharMetrics(Rune rune, float scale, bool fallback = true)
    {
        return _inner.GetCharMetrics(rune, scale, fallback);
    }

	public override float DrawChar(
		DrawingHandleBase handle,
		Rune rune,
		Vector2 baseline,
		float scale,
		Color color,
		bool fallback = true)
	{
		var advance = _inner.DrawChar(handle, rune, baseline, scale, color, fallback);
		if (advance <= 0 || _decorations == default)
			return advance;

		var endX = baseline.X + advance;

		var thickness = (int) MathF.Max(1f, MathF.Round(_inner.GetAscent(scale) * 0.08f));

		if (_decorations.Strikethrough)
		{
			var y = MathF.Round(baseline.Y - _inner.GetAscent(scale) * 0.35f);
			DrawThickLine(handle, baseline.X, endX, y, thickness, color);
		}

		if (_decorations.Underline)
		{
			var y = MathF.Round(baseline.Y + MathF.Max(1f, _inner.GetDescent(scale) * 0.5f));
			DrawThickLine(handle, baseline.X, endX, y, thickness, color);
		}

		return advance;
	}

	private static void DrawThickLine(
		DrawingHandleBase handle,
		float startX,
		float endX,
		float y,
		int thickness,
		Color color)
	{
		for (var i = 0; i < thickness; i++)
		{
			var offset = i - (thickness - 1) / 2f;
			handle.DrawLine(
				new Vector2(startX, y + offset),
				new Vector2(endX, y + offset),
				color);
		}
	}
}

public static class PaperDecorationTagHelper
{
    public static Font GetCurrentFont(MarkupDrawingContext context)
    {
        if (!context.Font.TryPeek(out var font))
            throw new InvalidOperationException("A paper decoration tag requires a font in the drawing context.");

        return font;
    }

    public static PaperTextDecorations GetDecorations(MarkupDrawingContext context)
    {
        return PaperDecoratedFont.GetDecorations(GetCurrentFont(context));
    }

    public static void PushDecorations(MarkupDrawingContext context, PaperTextDecorations decorations)
    {
        context.Font.Push(PaperDecoratedFont.WithDecorations(GetCurrentFont(context), decorations));
    }
}