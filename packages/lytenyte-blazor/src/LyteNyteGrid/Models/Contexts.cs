namespace LyteNyteGrid.Models;

/// <summary>
/// Context passed to cell-related callbacks and templates.
/// </summary>
public class CellContext<T>
{
    public required RowNode Row { get; init; }
    public required ColumnDefinition<T> Column { get; init; }
    public required int RowIndex { get; init; }
    public required int ColIndex { get; init; }
    public required IGridApi<T> Api { get; init; }
}

/// <summary>
/// Context passed to cell renderer templates.
/// </summary>
public class CellRendererContext<T> : CellContext<T>
{
    public required object? Value { get; init; }
    public required bool Selected { get; init; }
    public required bool DetailExpanded { get; init; }

    /// <summary>Whether the selection is indeterminate (tri-state, e.g. grouped checkbox selection).</summary>
    public bool Indeterminate { get; init; }
}

/// <summary>
/// Context passed to header renderer templates.
/// </summary>
public class HeaderContext<T>
{
    public required ColumnDefinition<T> Column { get; init; }
    public required IGridApi<T> Api { get; init; }
}

/// <summary>
/// Context passed to header group renderer templates.
/// </summary>
public class HeaderGroupContext<T>
{
    public required bool Collapsible { get; init; }
    public required bool Collapsed { get; init; }
    public required string[] GroupPath { get; init; }
    public required IReadOnlyList<ColumnDefinition<T>> Columns { get; init; }
    public required IGridApi<T> Api { get; init; }
}

/// <summary>
/// Context passed to edit renderer templates.
/// </summary>
public class EditContext<T> : CellContext<T>
{
    public required object? EditValue { get; init; }
    public required Action<object?> ChangeValue { get; init; }
    public required Action Commit { get; init; }
    public required Action Cancel { get; init; }
}

/// <summary>
/// Context passed to row detail renderer templates.
/// </summary>
public class RowDetailContext<T>
{
    public required RowNode Row { get; init; }
    public required int RowIndex { get; init; }
    public required IGridApi<T> Api { get; init; }
}

/// <summary>
/// Context for row event callbacks.
/// </summary>
public class RowEventContext<T>
{
    public required RowNode Row { get; init; }
    public required int RowIndex { get; init; }
    public required IGridApi<T> Api { get; init; }
}
