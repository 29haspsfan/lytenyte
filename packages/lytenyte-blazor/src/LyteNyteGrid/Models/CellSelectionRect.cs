namespace LyteNyteGrid.Models;

/// <summary>
/// Represents a rectangular cell selection region defined by row and column ranges.
/// </summary>
public class CellSelectionRect
{
    /// <summary>Start row index (inclusive).</summary>
    public int RowStart { get; set; }

    /// <summary>End row index (inclusive).</summary>
    public int RowEnd { get; set; }

    /// <summary>Start column index (inclusive).</summary>
    public int ColStart { get; set; }

    /// <summary>End column index (inclusive).</summary>
    public int ColEnd { get; set; }

    /// <summary>
    /// Returns true if the given row/column indices fall within this selection rectangle.
    /// </summary>
    public bool Contains(int rowIndex, int colIndex)
    {
        return rowIndex >= Math.Min(RowStart, RowEnd)
            && rowIndex <= Math.Max(RowStart, RowEnd)
            && colIndex >= Math.Min(ColStart, ColEnd)
            && colIndex <= Math.Max(ColStart, ColEnd);
    }
}
