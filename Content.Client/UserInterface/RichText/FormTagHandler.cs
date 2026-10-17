using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Converts [form] tags into clickable buttons that open fill-in dialogs.
/// </summary>
public sealed class FormTagHandler : IMarkupTagHandler
{
    public string Name => "form";

    private static int _formCounter;
    private static string _lastText = "";
    private static readonly Dictionary<string, int> FormPositions = [];

    public static void ResetFormCounter() => _formCounter = 0;

    public static void SetFormText(string text)
    {
        if (_lastText == text)
            return;

        FormPositions.Clear();
        _lastText = text;
        var pos = 0;
        var index = 0;
        while ((pos = text.IndexOf("[form]", pos, StringComparison.Ordinal)) != -1)
        {
            FormPositions[pos.ToString()] = index++;
            pos += 6;
        }
    }

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public string TextBefore(MarkupNode node) => "";
    public string TextAfter(MarkupNode node) => "";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        var btn = new Button
        {
            Text = Loc.GetString("paper-form-fill-button"),
            MinSize = new Vector2(32, PaperTagHelper.FontLineHeight + 2),
            MaxSize = new Vector2(32, PaperTagHelper.FontLineHeight + 2),
            Margin = new Thickness(1, 0, 1, 0),
            StyleClasses = { "ButtonSquare" },
            TextAlign = Label.AlignMode.Center,
            Name = $"form_{_formCounter++}"
        };

        btn.OnPressed += _ =>
        {
            if (PaperTagHelper.FindPaperWindow(btn) is { } paperWindow)
            {
                var buttonIndex = PaperTagHelper.CountButtonsBefore(btn, b => b.Text == Loc.GetString("paper-form-fill-button"));
                paperWindow.OpenFormDialog(buttonIndex);
            }
        };

        control = btn;
        return true;
    }
}
