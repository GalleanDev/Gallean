namespace Gallean.Engine.Tests;

public sealed class UnitGridTests
{
    private record TestUnit(string Id, bool IsLarge);

    [Test]
    public void EmptyTest()
    {
        TestUnit unit = new("1", false);
        UnitGrid<TestUnit> occupied = new UnitGrid<TestUnit>.Builder()
            .Place(unit, GridPosition.FrontBottom, unit.IsLarge)
            .Build();

        Assert.That(UnitGrid<TestUnit>.Empty.IsEmpty, Is.True);
        Assert.That(occupied.IsEmpty, Is.False);
    }

    [Test]
    public void PlaceSmallUnitTest()
    {
        TestUnit unit = new("1", false);
        UnitGrid<TestUnit>.Builder builder = new();

        builder.Place(unit, new GridPosition(0, 0), unit.IsLarge);
        UnitGrid<TestUnit> grid = builder.Build();

        (TestUnit Unit, GridPosition Position)[] expectedUnits = [(unit, new GridPosition(0, 0))];

        Assert.That(grid[new GridPosition(0, 0)], Is.SameAs(unit));
        Assert.That(grid.GetUnits(), Is.EqualTo(expectedUnits));
    }

    [Test]
    public void PlaceLargeUnitTest()
    {
        TestUnit unit = new("1", true);
        UnitGrid<TestUnit>.Builder builder = new();

        builder.Place(unit, new GridPosition(0, 0), unit.IsLarge);
        UnitGrid<TestUnit> grid = builder.Build();

        (TestUnit Unit, GridPosition Position)[] expectedUnits = [(unit, new GridPosition(0, 0))];

        Assert.That(grid[new GridPosition(0, 0)], Is.SameAs(unit));
        Assert.That(grid[new GridPosition(1, 0)], Is.SameAs(unit));
        Assert.That(grid.GetUnits(), Is.EqualTo(expectedUnits));
    }

    [Test]
    public void PlaceUnitOccupiedSlotTest()
    {
        TestUnit unit1 = new("1", false);
        TestUnit unit2 = new("2", false);
        UnitGrid<TestUnit>.Builder builder = new();

        builder.Place(unit1, new GridPosition(0, 0), unit1.IsLarge);
        Assert.Throws<InvalidOperationException>(() => builder.Place(unit2, new GridPosition(0, 0), unit2.IsLarge));
    }

    [Test]
    public void GetUnitsTest()
    {
        TestUnit unit1 = new("1", false);
        TestUnit unit2 = new("2", false);
        UnitGrid<TestUnit>.Builder builder = new();

        builder.Place(unit1, new GridPosition(0, 0), unit1.IsLarge);
        builder.Place(unit2, new GridPosition(1, 2), unit2.IsLarge);
        UnitGrid<TestUnit> grid = builder.Build();

        (TestUnit Unit, GridPosition Position)[] expectedUnits = [(unit1, new GridPosition(0, 0)), (unit2, new GridPosition(1, 2))];

        Assert.That(grid.GetUnits(), Is.EqualTo(expectedUnits));
    }

    [Test]
    public void ReplaceUnitTest()
    {
        TestUnit original = new("1", false);
        TestUnit replacement = new("1_new", false);

        UnitGrid<TestUnit>.Builder builder = new();
        builder.Place(original, new GridPosition(1, 1), original.IsLarge);
        UnitGrid<TestUnit> grid = builder.Build();

        UnitGrid<TestUnit> newGrid = grid.Replace(u => u.Id == "1", replacement);

        (TestUnit Unit, GridPosition Position)[] expectedNewUnits = [(replacement, new GridPosition(1, 1))];
        (TestUnit Unit, GridPosition Position)[] expectedOriginalUnits = [(original, new GridPosition(1, 1))];

        Assert.That(newGrid, Is.Not.SameAs(grid));
        Assert.That(newGrid[new GridPosition(1, 1)], Is.SameAs(replacement));
        Assert.That(newGrid.GetUnits(), Is.EqualTo(expectedNewUnits));

        Assert.That(grid[new GridPosition(1, 1)], Is.SameAs(original));
        Assert.That(grid.GetUnits(), Is.EqualTo(expectedOriginalUnits));
    }

    [Test]
    public void BuildFromExistingTest()
    {
        TestUnit unit1 = new("1", false);
        UnitGrid<TestUnit>.Builder builder1 = new();
        builder1.Place(unit1, new GridPosition(0, 0), unit1.IsLarge);
        UnitGrid<TestUnit> existingGrid = builder1.Build();

        UnitGrid<TestUnit>.Builder builder2 = new(existingGrid);
        TestUnit unit2 = new("2", true);
        builder2.Place(unit2, new GridPosition(1, 1), unit2.IsLarge);
        UnitGrid<TestUnit> newGrid = builder2.Build();

        Assert.That(newGrid[new GridPosition(0, 0)], Is.SameAs(unit1));
        Assert.That(newGrid[new GridPosition(0, 1)], Is.SameAs(unit2));
        Assert.That(newGrid[new GridPosition(1, 1)], Is.SameAs(unit2));

        Assert.That(existingGrid[new GridPosition(1, 1)], Is.Null);
    }

    [Test]
    public void OverlayTest()
    {
        TestUnit baseUnit = new("base", false);
        TestUnit overlayUnit = new("overlay", false);

        UnitGrid<TestUnit> grid = new UnitGrid<TestUnit>.Builder()
            .Place(baseUnit, GridPosition.FrontCenter, false)
            .PlaceOverlay(overlayUnit, GridPosition.FrontCenter, false)
            .Build();

        (TestUnit Unit, GridPosition Position)[] expectedUnits = [(overlayUnit, GridPosition.FrontCenter)];
        (TestUnit Unit, GridPosition Position)[] expectedBaseUnits = [(baseUnit, GridPosition.FrontCenter)];

        Assert.That(grid[GridPosition.FrontCenter], Is.SameAs(overlayUnit));
        Assert.That(grid.GetUnits(), Is.EqualTo(expectedUnits));
        Assert.That(grid.GetBaseGrid().GetUnits(), Is.EqualTo(expectedBaseUnits));
        Assert.That(grid.GetOverlayGrid().GetUnits(), Is.EqualTo(expectedUnits));
    }

    [Test]
    public void OverlayLargeBaseUnitTest()
    {
        TestUnit baseUnit = new("base", true);
        TestUnit overlayUnit = new("overlay", false);

        UnitGrid<TestUnit> grid = new UnitGrid<TestUnit>.Builder()
            .Place(baseUnit, GridPosition.FrontCenter, true)
            .PlaceOverlay(overlayUnit, GridPosition.FrontCenter, false)
            .Build();

        (TestUnit Unit, GridPosition Position)[] expectedUnits = [(overlayUnit, GridPosition.FrontCenter)];

        Assert.That(grid[GridPosition.FrontCenter], Is.SameAs(overlayUnit));
        Assert.That(grid[GridPosition.BackCenter], Is.Null);
        Assert.That(grid.GetUnits(), Is.EqualTo(expectedUnits));
        Assert.That(grid.IsLargeAt(GridPosition.FrontCenter), Is.False);
    }

    [Test]
    public void LargeOverlayTest()
    {
        TestUnit frontUnit = new("front", false);
        TestUnit backUnit = new("back", false);
        TestUnit overlayUnit = new("overlay", true);

        UnitGrid<TestUnit> grid = new UnitGrid<TestUnit>.Builder()
            .Place(frontUnit, GridPosition.FrontCenter, false)
            .Place(backUnit, GridPosition.BackCenter, false)
            .PlaceOverlay(overlayUnit, GridPosition.FrontCenter, true)
            .Build();

        (TestUnit Unit, GridPosition Position)[] expectedUnits = [(overlayUnit, GridPosition.FrontCenter)];
        (TestUnit Unit, GridPosition Position)[] expectedBaseUnits =
            [(frontUnit, GridPosition.FrontCenter), (backUnit, GridPosition.BackCenter)];

        Assert.That(grid[GridPosition.FrontCenter], Is.SameAs(overlayUnit));
        Assert.That(grid[GridPosition.BackCenter], Is.SameAs(overlayUnit));
        Assert.That(grid.IsLargeAt(GridPosition.FrontCenter), Is.True);
        Assert.That(grid.GetUnits(), Is.EqualTo(expectedUnits));
        Assert.That(grid.GetBaseGrid().GetUnits(), Is.EqualTo(expectedBaseUnits));
    }

    [Test]
    public void ReplaceOverlaidUnitTest()
    {
        TestUnit baseUnit = new("base", false);
        TestUnit replacement = new("base_new", false);
        TestUnit overlayUnit = new("overlay", false);

        UnitGrid<TestUnit> grid = new UnitGrid<TestUnit>.Builder()
            .Place(baseUnit, GridPosition.FrontCenter, false)
            .PlaceOverlay(overlayUnit, GridPosition.FrontCenter, false)
            .Build();

        UnitGrid<TestUnit> newGrid = grid.Replace(unit => unit.Id == "base", replacement);

        UnitGrid<TestUnit> expectedGrid = new UnitGrid<TestUnit>.Builder()
            .Place(replacement, GridPosition.FrontCenter, false)
            .PlaceOverlay(overlayUnit, GridPosition.FrontCenter, false)
            .Build();

        Assert.That(newGrid, Is.EqualTo(expectedGrid));
    }

    [Test]
    public void EmptyOverlayEqualityTest()
    {
        TestUnit baseUnit = new("base", false);
        TestUnit overlayUnit = new("overlay", false);

        UnitGrid<TestUnit> baseOnlyGrid = new UnitGrid<TestUnit>.Builder()
            .Place(baseUnit, GridPosition.FrontCenter, false)
            .Build();

        UnitGrid<TestUnit> layeredGrid = new UnitGrid<TestUnit>.Builder(baseOnlyGrid)
            .PlaceOverlay(overlayUnit, GridPosition.FrontCenter, false)
            .Build();

        UnitGrid<TestUnit> restoredGrid = new UnitGrid<TestUnit>.Builder(layeredGrid)
            .RemoveOverlay(GridPosition.FrontCenter)
            .Build();

        Assert.That(layeredGrid, Is.Not.EqualTo(baseOnlyGrid));
        Assert.That(restoredGrid, Is.EqualTo(baseOnlyGrid));
        Assert.That(restoredGrid.GetHashCode(), Is.EqualTo(baseOnlyGrid.GetHashCode()));
    }

    [Test]
    public void PlaceOverlayOccupiedSlotTest()
    {
        TestUnit unit1 = new("1", false);
        TestUnit unit2 = new("2", false);
        UnitGrid<TestUnit>.Builder builder = new();

        builder.PlaceOverlay(unit1, GridPosition.FrontBottom, false);

        Assert.Throws<InvalidOperationException>(() => builder.PlaceOverlay(unit2, GridPosition.FrontBottom, false));
    }
}
