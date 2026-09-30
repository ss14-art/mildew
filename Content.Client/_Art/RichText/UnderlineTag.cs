using JetBrains.Annotations;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Art.RichText;

[UsedImplicitly]
public sealed class UnderlineTag : IMarkupTagHandler
{
    public string Name => "uline";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        PaperDecorationTagHelper.PushDecorations(
            context,
            PaperDecorationTagHelper.GetDecorations(context) with { Underline = true });
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}