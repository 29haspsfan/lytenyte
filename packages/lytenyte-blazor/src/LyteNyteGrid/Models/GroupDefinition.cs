namespace LyteNyteGrid.Models;

/// <summary>
/// Defines a grouping dimension for the data source.
/// </summary>
/// <typeparam name="T">Row data type.</typeparam>
public class GroupDefinition<T>
{
    /// <summary>Column ID or field name to group by.</summary>
    public required string ColumnId { get; init; }

    /// <summary>Custom group key extractor. If null, uses the column's field value.</summary>
    public Func<T, string?>? GroupKeyFn { get; set; }
}
