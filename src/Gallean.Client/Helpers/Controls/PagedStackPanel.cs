using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial class PagedStackPanel : Panel
{
    private readonly List<Control> slots = [];
    private Size slotSize;
    private int columns;
    private int rows = 1;

    static PagedStackPanel()
    {
        AffectsArrange<PagedStackPanel>(FirstIndexProperty, OverflowProperty);
        AffectsMeasure<PagedStackPanel>(FillsAvailableSpaceProperty, MaxRowsProperty, SlotTemplateProperty);
    }

    [GeneratedStyledProperty(CoerceMethodName = nameof(CoerceFirstIndex))]
    public partial int FirstIndex { get; set; }

    [GeneratedStyledProperty]
    public partial double Overflow { get; set; }

    [GeneratedStyledProperty]
    public partial bool FillsAvailableSpace { get; set; }

    [GeneratedStyledProperty(DefaultValue = int.MaxValue, ValidateMethodName = nameof(IsValidMaxRows))]
    public partial int MaxRows { get; set; }

    [GeneratedStyledProperty]
    public partial ITemplate<Control>? SlotTemplate { get; set; }

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
        slotSize = MeasureSlotSize();
        columns = CountColumns(availableSize.Width);
        rows = CountRows(availableSize.Height);

        SetSlotCount(VisibleCount);
        CoerceValue(FirstIndexProperty);

        return new Size(columns * slotSize.Width, rows * slotSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        for (int index = 0; index < slots.Count; index++)
        {
            Rect rect = new(GetSlotOrigin(index), slotSize);

            slots[index].Arrange(rect);
        }

        for (int index = 0; index < Children.Count; index++)
        {
            Point origin = GetSlotOrigin(index - FirstIndex);
            Rect rect = new(origin.WithY(origin.Y + GetPageGap(index)), slotSize);

            Children[index].Arrange(rect);
        }

        Clip = new RectangleGeometry(new Rect(finalSize).Inflate(Overflow));

        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SlotTemplateProperty)
        {
            SetSlotCount(0);
        }
    }

    private static bool IsValidMaxRows(int value)
    {
        return value >= 1;
    }

    private static int CoerceFirstIndex(AvaloniaObject sender, int value)
    {
        PagedStackPanel panel = (PagedStackPanel)sender;

        int index = Math.Clamp(value, 0, panel.MaxFirstIndex);

        return index - (index % panel.ScrollStep);
    }

    private Size MeasureSlotSize()
    {
        if (slots.Count <= 0)
        {
            SetSlotCount(1);
        }

        double width = 0;
        double height = 0;

        foreach (Control child in Children.Concat(slots))
        {
            child.Measure(Size.Infinity);

            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    private void SetSlotCount(int count)
    {
        while (slots.Count > count)
        {
            RemoveSlot();
        }

        if (SlotTemplate is { } template)
        {
            while (slots.Count < count)
            {
                AddSlot(template.Build());
            }
        }
    }

    private void AddSlot(Control slot)
    {
        slot.ZIndex = -1;

        slots.Add(slot);
        LogicalChildren.Add(slot);
        VisualChildren.Add(slot);

        slot.Measure(Size.Infinity);
    }

    private void RemoveSlot()
    {
        Control slot = slots[^1];

        slots.RemoveAt(slots.Count - 1);
        LogicalChildren.Remove(slot);
        VisualChildren.Remove(slot);
    }

    private Point GetSlotOrigin(int offset)
    {
        int row = (int)Math.Floor(offset / (double)columns);
        int column = offset - (row * columns);

        return new Point(column * slotSize.Width, row * slotSize.Height);
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
        if (slotSize.Width > 0 && !double.IsInfinity(availableWidth))
        {
            int count = Math.Max(1, (int)(availableWidth / slotSize.Width));

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
        if (slotSize.Height > 0 && columns > 0 && !double.IsInfinity(availableHeight))
        {
            int count = Math.Clamp((int)(availableHeight / slotSize.Height), 1, MaxRows);

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
