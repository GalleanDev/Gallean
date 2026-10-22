using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class PagedStackPanel : Panel
{
    public static readonly StyledProperty<int> FirstIndexProperty =
        AvaloniaProperty.Register<PagedStackPanel, int>(nameof(FirstIndex), coerce: CoerceFirstIndex);

    public static readonly StyledProperty<double> OverflowProperty =
        AvaloniaProperty.Register<PagedStackPanel, double>(nameof(Overflow));

    public static readonly StyledProperty<bool> FillsAvailableSpaceProperty =
        AvaloniaProperty.Register<PagedStackPanel, bool>(nameof(FillsAvailableSpace));

    public static readonly StyledProperty<Size> MinSlotSizeProperty =
        AvaloniaProperty.Register<PagedStackPanel, Size>(nameof(MinSlotSize));

    private Size slot;
    private int columns;
    private int rows = 1;

    static PagedStackPanel()
    {
        AffectsArrange<PagedStackPanel>(FirstIndexProperty, OverflowProperty);
        AffectsMeasure<PagedStackPanel>(FillsAvailableSpaceProperty, MinSlotSizeProperty);
    }

    public int FirstIndex
    {
        get => GetValue(FirstIndexProperty);
        set => SetValue(FirstIndexProperty, value);
    }

    public double Overflow
    {
        get => GetValue(OverflowProperty);
        set => SetValue(OverflowProperty, value);
    }

    public bool FillsAvailableSpace
    {
        get => GetValue(FillsAvailableSpaceProperty);
        set => SetValue(FillsAvailableSpaceProperty, value);
    }

    public Size MinSlotSize
    {
        get => GetValue(MinSlotSizeProperty);
        set => SetValue(MinSlotSizeProperty, value);
    }

    public int VisibleCount => columns * rows;

    public int ScrollStep => rows > 1 ? columns : 1;

    public int MaxFirstIndex
    {
        get
        {
            int hiddenCount = Math.Max(0, Children.Count - VisibleCount);
            int hiddenSteps = (hiddenCount + ScrollStep - 1) / ScrollStep;

            return hiddenSteps * ScrollStep;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = MinSlotSize.Width;
        double height = MinSlotSize.Height;

        foreach (Control child in Children)
        {
            child.Measure(Size.Infinity);

            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        slot = new Size(width, height);
        columns = CountColumns(availableSize.Width);
        rows = CountRows(availableSize.Height);

        CoerceValue(FirstIndexProperty);

        return new Size(columns * slot.Width, rows * slot.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        for (int index = 0; index < Children.Count; index++)
        {
            int offset = index - FirstIndex;
            int row = (int)Math.Floor(offset / (double)columns);
            int column = offset - (row * columns);
            Point origin = new(column * slot.Width, (row * slot.Height) + GetPageGap(index));

            Children[index].Arrange(new Rect(origin, slot));
        }

        Clip = new RectangleGeometry(new Rect(finalSize).Inflate(Overflow));

        return finalSize;
    }

    private static int CoerceFirstIndex(AvaloniaObject sender, int value)
    {
        PagedStackPanel panel = (PagedStackPanel)sender;

        int index = Math.Clamp(value, 0, panel.MaxFirstIndex);

        return index - (index % panel.ScrollStep);
    }

    private double GetPageGap(int index)
    {
        if (index < FirstIndex)
        {
            return -2 * Overflow;
        }
        else if (index >= FirstIndex + VisibleCount)
        {
            return 2 * Overflow;
        }
        else
        {
            return 0;
        }
    }

    private int CountColumns(double availableWidth)
    {
        if (slot.Width > 0 && !double.IsInfinity(availableWidth))
        {
            int count = Math.Max(1, (int)(availableWidth / slot.Width));

            if (FillsAvailableSpace)
            {
                return count;
            }
            else
            {
                return Math.Min(Children.Count, count);
            }
        }
        else
        {
            return Children.Count;
        }
    }

    private int CountRows(double availableHeight)
    {
        if (slot.Height > 0 && columns > 0 && !double.IsInfinity(availableHeight))
        {
            int count = Math.Max(1, (int)(availableHeight / slot.Height));

            if (FillsAvailableSpace)
            {
                return count;
            }
            else
            {
                return Math.Clamp((Children.Count + columns - 1) / columns, 1, count);
            }
        }
        else
        {
            return 1;
        }
    }
}
