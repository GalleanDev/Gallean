using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Gallean.Client;

internal sealed partial class ModalPanel : ContentControl
{
    [GeneratedStyledProperty]
    public partial TextSpans? Title { get; set; }

    [GeneratedStyledProperty]
    public partial string? Subtitle { get; set; }

    [GeneratedStyledProperty]
    public partial IconMoniker? SubtitleIcon { get; set; }

    [GeneratedStyledProperty]
    public partial object? Footer { get; set; }

    [GeneratedStyledProperty]
    public partial IInputElement? InitialFocus { get; set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (e.RootVisual is InputElement root)
        {
            root.AddHandler(GotFocusEvent, OnRootGotFocus);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (e.RootVisual is InputElement root)
        {
            root.RemoveHandler(GotFocusEvent, OnRootGotFocus);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        IInputElement? focused = topLevel?.FocusManager.GetFocusedElement();

        FocusInitialElement(focused);
    }

    private void OnRootGotFocus(object? sender, FocusChangedEventArgs e)
    {
        FocusInitialElement(e.Source);
    }

    private void FocusInitialElement(object? oldFocus)
    {
        if (oldFocus is Visual visual && visual.IsVisualAncestorOf(this))
        {
            InitialFocus?.Focus();
        }
    }
}
