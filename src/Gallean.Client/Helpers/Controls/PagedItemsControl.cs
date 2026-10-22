using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed class PagedItemsControl : ItemsControl
{
    private Button? previousButton;
    private Button? nextButton;

    public PagedItemsControl()
    {
        LayoutUpdated += OnLayoutUpdated;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (previousButton is not null)
        {
            previousButton.Click -= OnPreviousClick;
        }

        if (nextButton is not null)
        {
            nextButton.Click -= OnNextClick;
        }

        previousButton = e.NameScope.Find<Button>("PART_PreviousButton");
        nextButton = e.NameScope.Find<Button>("PART_NextButton");

        if (previousButton is not null)
        {
            previousButton.Click += OnPreviousClick;
        }

        if (nextButton is not null)
        {
            nextButton.Click += OnNextClick;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (ItemsPanelRoot is PagedStackPanel panel)
        {
            int firstIndex = panel.FirstIndex;

            panel.FirstIndex -= Math.Sign(e.Delta.Y) * panel.ScrollStep;

            e.Handled = panel.FirstIndex != firstIndex;
        }
    }

    private void OnPreviousClick(object? sender, RoutedEventArgs e)
    {
        if (ItemsPanelRoot is PagedStackPanel panel)
        {
            panel.FirstIndex -= panel.VisibleCount;
        }
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        if (ItemsPanelRoot is PagedStackPanel panel)
        {
            panel.FirstIndex += panel.VisibleCount;
        }
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (ItemsPanelRoot is PagedStackPanel panel)
        {
            previousButton?.IsEnabled = panel.FirstIndex > 0;
            nextButton?.IsEnabled = panel.FirstIndex < panel.MaxFirstIndex;
        }
    }
}
