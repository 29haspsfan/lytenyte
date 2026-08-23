namespace LyteNyteGrid.Enums;

/// <summary>
/// Controls how cell (as opposed to row) selection works.
/// </summary>
public enum CellSelectionMode
{
    /// <summary>Cell selection is disabled.</summary>
    None,

    /// <summary>A single contiguous rectangle can be selected.</summary>
    Range,

    /// <summary>Multiple non-contiguous rectangles can be selected.</summary>
    MultiRange
}
