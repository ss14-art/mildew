using JetBrains.Annotations;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Art.RichText;

[UsedImplicitly]
public sealed class StrikethroughTag : IMarkupTagHandler
{
    public string Name => "cut";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        PaperDecorationTagHelper.PushDecorations(
            context,
            PaperDecorationTagHelper.GetDecorations(context) with { Strikethrough = true });
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}