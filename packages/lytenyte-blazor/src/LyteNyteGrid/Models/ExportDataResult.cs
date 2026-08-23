namespace LyteNyteGrid.Models;

/// <summary>
/// Result of an ExportData call, containing headers, group headers, column metadata, and data rows.
/// </summary>
/// <typeparam name="T">Row data type.</typeparam>
public class ExportDataResult<T>
{
    /// <summary>Header names for each exported column.</summary>
    public IReadOnlyList<string> Headers { get; init; } = [];

    /// <summary>Group header labels, one list per header-group depth level.</summary>
    public IReadOnlyList<IReadOnlyList<string?>> GroupHeaders { get; init; } = [];

    /// <summary>Column definitions for the exported columns.</summary>
    public IReadOnlyList<ColumnDefinition<T>> Columns { get; init; } = [];

    /// <summary>The exported data rows (each row is a list of cell values).</summary>
    public IReadOnlyList<IReadOnlyList<object?>> Data { get; init; } = [];
}
