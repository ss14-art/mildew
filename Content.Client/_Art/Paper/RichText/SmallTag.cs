using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Client.Graphics;

namespace Content.Client._Art.RichText;

[UsedImplicitly]
public sealed class SmallTag : IMarkupTagHandler
{
    private const int MaxDecrease = 6;
    private const int MinSize = 6;

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "small";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var decrease = (int) Math.Clamp(node.Value.LongValue ?? 1, 1, MaxDecrease);
        var size = Math.Max(FontTag.DefaultSize - decrease, MinSize);

        var hadSize = node.Attributes.TryGetValue("size", out var previous);
        node.Attributes["size"] = new MarkupParameter(size);
        Font font;
        try
        {
            font = FontTag.CreateFont(
                context.Font,
                node,
                _resourceCache,
                _prototypeManager,
                FontTag.DefaultFont);
        }
        finally
        {
            if (hadSize)
                node.Attributes["size"] = previous;
            else
                node.Attributes.Remove("size");
        }

        context.Font.Push(PaperDecoratedFont.WithDecorations(
            font,
            PaperDecorationTagHelper.GetDecorations(context)));
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}