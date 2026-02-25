namespace LyteNyteGrid.Enums;

/// <summary>
/// Controls the visibility behavior of a column within a column group.
/// </summary>
public enum ColumnGroupVisibility
{
    /// <summary>The column is always visible regardless of the group's state.</summary>
    Always,
    /// <summary>The column is visible only when the group is collapsed.</summary>
    Close,
    /// <summary>The column is visible only when the group is expanded.</summary>
    Open
}
