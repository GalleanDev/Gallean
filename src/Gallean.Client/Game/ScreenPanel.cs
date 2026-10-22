using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaAnimation = Avalonia.Animation.Animation;

namespace Gallean.Client;

internal sealed class ScreenPanel : Panel
{
    public static readonly AttachedProperty<bool> IsTopmostProperty =
        AvaloniaProperty.RegisterAttached<ScreenPanel, Control, bool>("IsTopmost", defaultValue: true, inherits: true);

    private readonly Dictionary<Control, AbstractScreenViewModel> screens = [];

    public static bool GetIsTopmost(Control control)
    {
        return control.GetValue(IsTopmostProperty);
    }

    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.ChildrenChanged(sender, e);

        if (e.OldItems is not null)
        {
            foreach (Control child in e.OldItems)
            {
                if (screens.Remove(child, out AbstractScreenViewModel? screen))
                {
                    screen.PropertyChanged -= OnScreenPropertyChanged;
                }
            }
        }

        if (e.NewItems is not null)
        {
            foreach (Control child in e.NewItems)
            {
                Reveal(child);

                if (child.DataContext is AbstractScreenViewModel screen)
                {
                    screens.Add(child, screen);
                    screen.PropertyChanged += OnScreenPropertyChanged;
                }
            }
        }

        UpdateVisibility();
        UpdateTopmost();
    }

    private static void Reveal(Control child)
    {
        if (child.DataContext is AbstractScreenViewModel screen && screen.RevealDuration > TimeSpan.Zero)
        {
            AvaloniaAnimation animation = new()
            {
                Delay = screen.RevealDelay,
                Duration = screen.RevealDuration,
                Easing = new SineEaseInOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 0.0) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 1.0) } }
                }
            };

            RenderOptions.SetRequiresFullOpacityHandling(child, true);

            child.Opacity = 0;
            animation.RunAsync(child).FileAndForget();
        }
    }

    private void OnScreenPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AbstractScreenViewModel.PaintsOwnBackground))
        {
            UpdateVisibility();
        }
    }

    private void UpdateVisibility()
    {
        bool isCovered = false;
        bool isBlurred = false;

        for (int index = Children.Count - 1; index >= 0; index--)
        {
            Control child = Children[index];

            child.IsVisible = !isCovered;
            child.Classes.Set("backdrop", isBlurred);

            if (child.DataContext is AbstractScreenViewModel { PaintsOwnBackground: true })
            {
                isCovered = true;
            }

            if (child.DataContext is AbstractScreenViewModel { BlurBackdrop: true })
            {
                isBlurred = true;
            }
        }
    }

    private void UpdateTopmost()
    {
        for (int index = 0; index < Children.Count; index++)
        {
            Control child = Children[index];
            bool isTopmost = index == Children.Count - 1;

            child.SetValue(IsTopmostProperty, isTopmost);

            KeyboardNavigationMode navigationMode;
            if (isTopmost)
            {
                navigationMode = KeyboardNavigationMode.Cycle;
            }
            else
            {
                navigationMode = KeyboardNavigationMode.None;
            }

            KeyboardNavigation.SetTabNavigation(child, navigationMode);
        }
    }
}
