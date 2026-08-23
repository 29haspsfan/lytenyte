using LyteNyteGrid.Enums;
using Microsoft.AspNetCore.Components;

namespace LyteNyteGrid.Models;

/// <summary>
/// Defines a column in the grid. Equivalent to the React Column type.
/// </summary>
/// <typeparam name="T">The row data type.</typeparam>
public class ColumnDefinition<T>
{
    /// <summary>Unique identifier for this column.</summary>
    public required string Id { get; init; }

    /// <summary>Display name shown in the header (React: name).</summary>
    public string? Name { get; set; }

    /// <summary>Display name shown in the header (legacy alias for Name).</summary>
    [Obsolete("Use Name instead. HeaderName is retained for backward compatibility.")]
    public string? HeaderName
    {
        get => Name;
        set => Name = value;
    }

    /// <summary>Column data type hint: "string", "number", "date", "datetime", or custom.</summary>
    public string? Type { get; set; }

    /// <summary>
    /// The field accessor for extracting a value from the row data.
    /// Can be a property name (string) or a function.
    /// </summary>
    public Func<T, object?>? Field { get; set; }

    /// <summary>The field name (property path) as a string for reflection-based access.</summary>
    public string? FieldName { get; set; }

    /// <summary>Pin this column to start or end.</summary>
    public ColumnPin Pin { get; set; } = ColumnPin.None;

    /// <summary>Width of the column in pixels.</summary>
    public int Width { get; set; } = 200;

    /// <summary>Minimum width in pixels.</summary>
    public int MinWidth { get; set; } = 50;

    /// <summary>Maximum width in pixels.</summary>
    public int? MaxWidth { get; set; }

    /// <summary>Flex width factor for proportional sizing.</summary>
    public int? WidthFlex { get; set; }

    /// <summary>Whether this column can be resized by the user.</summary>
    public bool Resizable { get; set; } = true;

    /// <summary>Whether this column can be sorted.</summary>
    public bool Sortable { get; set; } = true;

    /// <summary>Current sort direction, or null if not sorted.</summary>
    public SortDirection? Sort { get; set; }

    /// <summary>Sort priority when multiple columns are sorted (lower = higher priority).</summary>
    public int? SortIndex { get; set; }

    /// <summary>Whether this column can be reordered by dragging.</summary>
    public bool Movable { get; set; } = true;

    /// <summary>Whether the column is hidden (React: hide). Inverted semantics from Visible.</summary>
    public bool Hide { get; set; }

    /// <summary>Whether the column is visible (legacy, inverse of Hide).</summary>
    [Obsolete("Use Hide instead. Visible is retained for backward compatibility.")]
    public bool Visible
    {
        get => !Hide;
        set => Hide = !value;
    }

    /// <summary>Column groups this column belongs to (outermost to innermost). React: groupPath.</summary>
    public string[]? GroupPath { get; set; }

    /// <summary>Column groups (legacy alias for GroupPath).</summary>
    [Obsolete("Use GroupPath instead. ColumnGroup is retained for backward compatibility.")]
    public string[]? ColumnGroup
    {
        get => GroupPath;
        set => GroupPath = value;
    }

    /// <summary>Visibility behavior within a column group (React: groupVisibility).</summary>
    public ColumnGroupVisibility GroupVisibility { get; set; } = ColumnGroupVisibility.Always;

    /// <summary>Visibility behavior within a column group (legacy alias for GroupVisibility).</summary>
    [Obsolete("Use GroupVisibility instead. ColumnGroupShow is retained for backward compatibility.")]
    public ColumnGroupVisibility ColumnGroupShow
    {
        get => GroupVisibility;
        set => GroupVisibility = value;
    }

    /// <summary>Column span: how many columns this cell should span.</summary>
    public int ColSpan { get; set; } = 1;

    /// <summary>Dynamic column span function.</summary>
    public Func<CellContext<T>, int>? ColSpanFn { get; set; }

    /// <summary>Row span: how many rows this cell should span.</summary>
    public int RowSpan { get; set; } = 1;

    /// <summary>Dynamic row span function.</summary>
    public Func<CellContext<T>, int>? RowSpanFn { get; set; }

    /// <summary>Whether this column is editable.</summary>
    public bool Editable { get; set; }

    /// <summary>Dynamic editable predicate.</summary>
    public Func<CellContext<T>, bool>? EditableFn { get; set; }

    /// <summary>Start editing when a printable key is pressed (React: editOnPrintable).</summary>
    public bool EditOnPrintable { get; set; }

    /// <summary>Custom cell renderer template.</summary>
    public RenderFragment<CellRendererContext<T>>? CellTemplate { get; set; }

    /// <summary>Custom header renderer template.</summary>
    public RenderFragment<HeaderContext<T>>? HeaderTemplate { get; set; }

    /// <summary>Custom edit renderer template.</summary>
    public RenderFragment<EditContext<T>>? EditTemplate { get; set; }

    /// <summary>Custom template for floating/summary row cells.</summary>
    public RenderFragment<CellRendererContext<T>>? FloatingCellTemplate { get; set; }

    /// <summary>Custom function to measure cell content width for column autosize.</summary>
    public Func<CellRendererContext<T>, double>? AutosizeCellFn { get; set; }

    /// <summary>Custom function to measure header content width for column autosize.</summary>
    public Func<HeaderContext<T>, double>? AutosizeHeaderFn { get; set; }

    /// <summary>Custom CSS class for cells in this column.</summary>
    public string? CellClass { get; set; }

    /// <summary>Custom CSS class for the header cell.</summary>
    public string? HeaderClass { get; set; }

    /// <summary>Custom CSS style for cells in this column.</summary>
    public string? CellStyle { get; set; }

    /// <summary>Custom comparator for sorting this column.</summary>
    public Comparison<object?>? SortComparator { get; set; }

    /// <summary>Custom filter function for this column.</summary>
    public Func<object?, bool>? FilterFn { get; set; }

    /// <summary>Aggregation function for grouped data.</summary>
    public Func<IEnumerable<object?>, object?>? AggregateFn { get; set; }

    /// <summary>
    /// Resolves the cell value for a given row.
    /// Uses the Field function if available, otherwise uses reflection on FieldName.
    /// </summary>
    public object? GetValue(T row)
    {
        if (Field is not null)
            return Field(row);

        if (FieldName is not null && row is not null)
            return GetPropertyValue(row, FieldName);

        return null;
    }

    /// <summary>
    /// Resolves the cell value from a RowNode.
    /// </summary>
    public object? GetValueFromNode(RowNode node)
    {
        if (node is RowLeaf<T> leaf && leaf.Data is not null)
            return GetValue(leaf.Data);

        if (node is RowLeaf plainLeaf && plainLeaf.Data is T data)
            return GetValue(data);

        if (node is RowGroup group && FieldName is not null)
            return group.Data.GetValueOrDefault(FieldName);

        if (node is RowAggregated aggregated && FieldName is not null)
            return aggregated.Data.GetValueOrDefault(FieldName);

        return null;
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        var type = obj.GetType();

        // Support nested property paths with dot notation
        if (propertyName.Contains('.'))
        {
            var parts = propertyName.Split('.', 2);
            var parentValue = type.GetProperty(parts[0])?.GetValue(obj);
            if (parentValue is null) return null;
            return GetPropertyValue(parentValue, parts[1]);
        }

        return type.GetProperty(propertyName)?.GetValue(obj);
    }
}
