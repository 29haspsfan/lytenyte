namespace LyteNyteGrid.Models;

/// <summary>
/// Base type for all row nodes in the grid.
/// A row is either a leaf (terminal data row) or a group (branch with children).
/// </summary>
public abstract class RowNode
{
    /// <summary>Unique identifier for this row.</summary>
    public required string Id { get; init; }

    /// <summary>Whether this row is currently loading data.</summary>
    public bool Loading { get; init; }

    /// <summary>An error associated with this row, if any.</summary>
    public object? Error { get; init; }

    public bool IsLeaf => this is RowLeaf;
    public bool IsGroup => this is RowGroup;
    public bool IsAggregated => this is RowAggregated;
}

/// <summary>
/// Represents a leaf row containing actual data. This is a terminal node with no children.
/// </summary>
public class RowLeaf : RowNode
{
    /// <summary>The data payload for this row.</summary>
    public object? Data { get; set; }

    /// <summary>Depth level in the hierarchy (React: depth).</summary>
    public int Depth { get; init; }

    /// <summary>ID of the parent row, if any (React: parentId).</summary>
    public string? ParentId { get; init; }
}

/// <summary>
/// Strongly typed leaf row.
/// </summary>
public class RowLeaf<T> : RowLeaf
{
    /// <summary>The strongly typed data payload for this row.</summary>
    public new T? Data
    {
        get => (T?)base.Data;
        set => base.Data = value;
    }
}

/// <summary>
/// Represents a group (branch) row which may contain children rows.
/// Group rows support expansion/collapse behavior.
/// </summary>
public class RowGroup : RowNode
{
    /// <summary>The group key used to organize this branch in the hierarchy.</summary>
    public string? Key { get; init; }

    /// <summary>Group-level aggregated data.</summary>
    public Dictionary<string, object?> Data { get; init; } = new();

    /// <summary>Depth level from the root for visual indenting.</summary>
    public int Depth { get; init; }

    /// <summary>Whether the group is currently expanded (React: expanded).</summary>
    public bool Expanded { get; init; }

    /// <summary>Whether the group can be expanded (React: expandable).</summary>
    public bool Expandable { get; init; }

    /// <summary>Whether this is the last group at its depth level (React: last).</summary>
    public bool Last { get; init; }

    /// <summary>ID of the parent row, if any (React: parentId).</summary>
    public string? ParentId { get; init; }

    /// <summary>An error that applies when the group fails to load children.</summary>
    public object? ErrorGroup { get; init; }

    /// <summary>Whether the group expansion is currently loading.</summary>
    public bool LoadingGroup { get; init; }
}

/// <summary>
/// Represents an aggregated row (e.g., footer/summary). React kind = "aggregated".
/// </summary>
public class RowAggregated : RowNode
{
    /// <summary>Aggregated data keyed by field/column id.</summary>
    public Dictionary<string, object?> Data { get; init; } = new();

    /// <summary>Depth level in the hierarchy.</summary>
    public int Depth { get; init; }

    /// <summary>ID of the parent row, if any.</summary>
    public string? ParentId { get; init; }
}
