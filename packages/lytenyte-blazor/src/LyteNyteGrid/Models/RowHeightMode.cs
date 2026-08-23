namespace LyteNyteGrid.Models;

/// <summary>
/// Describes how row height is determined. This supports the React
/// rowHeight prop which can be a number, "auto", "fill:N", or a function.
/// </summary>
public class RowHeightMode
{
    /// <summary>Fixed pixel height (null when mode is Auto, Fill, or Function).</summary>
    public int? FixedHeight { get; init; }

    /// <summary>True when rows should auto-measure their content height.</summary>
    public bool IsAuto { get; init; }

    /// <summary>True when rows should fill the available viewport space.</summary>
    public bool IsFill { get; init; }

    /// <summary>Minimum pixel height used with Fill mode.</summary>
    public int FillMinHeight { get; init; }

    /// <summary>Guess height used when measuring auto rows (before content is measured).</summary>
    public int GuessHeight { get; init; } = 40;

    /// <summary>A function that returns the height for a given row index.</summary>
    public Func<int, int>? HeightFunction { get; init; }

    /// <summary>Creates a fixed-height mode.</summary>
    public static RowHeightMode Fixed(int height) => new() { FixedHeight = height };

    /// <summary>Creates an auto-measuring mode with an optional guess height.</summary>
    public static RowHeightMode Auto(int guessHeight = 40) => new() { IsAuto = true, GuessHeight = guessHeight };

    /// <summary>Creates a fill mode with a minimum height.</summary>
    public static RowHeightMode Fill(int minHeight = 0) => new() { IsFill = true, FillMinHeight = minHeight };

    /// <summary>Creates a function-based mode.</summary>
    public static RowHeightMode Function(Func<int, int> fn) => new() { HeightFunction = fn };

    /// <summary>
    /// Resolves the effective pixel height for a given row index and available viewport height.
    /// </summary>
    public int Resolve(int rowIndex, int totalRows, int availableHeight)
    {
        if (HeightFunction is not null)
            return HeightFunction(rowIndex);

        if (IsAuto)
            return GuessHeight;

        if (IsFill && totalRows > 0)
            return Math.Max(FillMinHeight, availableHeight / totalRows);

        return FixedHeight ?? 40;
    }
}
