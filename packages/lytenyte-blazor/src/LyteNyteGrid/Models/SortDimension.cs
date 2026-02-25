using LyteNyteGrid.Enums;

namespace LyteNyteGrid.Models;

/// <summary>
/// Defines a sort dimension for the grid data source.
/// </summary>
/// <typeparam name="T">Row data type.</typeparam>
public class SortDimension<T>
{
    /// <summary>Column ID or field name to sort by.</summary>
    public required string ColumnId { get; init; }

    /// <summary>Sort direction.</summary>
    public SortDirection Direction { get; set; } = SortDirection.Ascending;

    /// <summary>Custom comparator. If null, default comparison is used.</summary>
    public Comparison<object?>? Comparator { get; set; }
}
