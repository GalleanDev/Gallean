using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;

namespace Gallean.Client;

internal sealed partial class BuildingTabView : UserControl
{
    private const double DetailsGap = 36;
    private const double DetailsRise = 6;

    public BuildingTabView()
    {
        InitializeComponent();
    }

    private static void PlaceDetails(CustomPopupPlacement placement)
    {
        placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.SlideY;

        double factor = UiScaleDecorator.GetScalingFactor(placement.Target);

        if (FitsOnRight(placement, DetailsGap * factor))
        {
            placement.Anchor = PopupAnchor.TopRight;
            placement.Gravity = PopupGravity.BottomRight;
            placement.Offset = new Point(DetailsGap * factor, -DetailsRise * factor);
        }
        else
        {
            placement.Anchor = PopupAnchor.TopLeft;
            placement.Gravity = PopupGravity.BottomLeft;
            placement.Offset = new Point(-DetailsGap * factor, -DetailsRise * factor);
        }
    }

    private static bool FitsOnRight(CustomPopupPlacement placement, double gap)
    {
        Visual target = placement.Target;
        TopLevel? topLevel = TopLevel.GetTopLevel(target);

        if (topLevel is null)
        {
            return true;
        }

        Point? rightEdge = target.TranslatePoint(new Point(target.Bounds.Width, 0), topLevel);

        if (rightEdge is null)
        {
            return true;
        }

        return rightEdge.Value.X + gap + placement.PopupSize.Width <= topLevel.ClientSize.Width;
    }
}
