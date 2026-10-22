using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Gallean.Client;

internal sealed class ModalPanel : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModalPanel, string?>(nameof(Title));

    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<ModalPanel, object?>(nameof(Footer));

    public static readonly StyledProperty<IInputElement?> InitialFocusProperty =
        AvaloniaProperty.Register<ModalPanel, IInputElement?>(nameof(InitialFocus));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public IInputElement? InitialFocus
    {
        get => GetValue(InitialFocusProperty);
        set => SetValue(InitialFocusProperty, value);
    }

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
