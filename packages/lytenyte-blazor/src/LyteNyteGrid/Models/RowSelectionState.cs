namespace LyteNyteGrid.Models;

/// <summary>
/// Describes how parent-child selection is linked.
/// </summary>
public enum RowSelectionLinkMode
{
    /// <summary>Parent and child selections are linked (selecting parent selects children and vice versa).</summary>
    Linked,
    /// <summary>Each row is selected independently.</summary>
    Isolated
}

/// <summary>
/// Represents the current row selection state of the grid.
/// </summary>
public class RowSelectionState
{
    /// <summary>Set of selected row IDs.</summary>
    public HashSet<string> SelectedIds { get; set; } = new();

    /// <summary>Whether all rows are selected.</summary>
    public bool AllSelected { get; set; }

    /// <summary>
    /// When AllSelected is true, this contains IDs that are explicitly deselected.
    /// </summary>
    public HashSet<string> DeselectedIds { get; set; } = new();

    /// <summary>
    /// Controls how parent-child row selection is linked (React: linked vs isolated modes).
    /// Defaults to Linked for backward compatibility.
    /// </summary>
    public RowSelectionLinkMode LinkMode { get; set; } = RowSelectionLinkMode.Linked;

    public bool IsSelected(string rowId)
    {
        if (AllSelected)
            return !DeselectedIds.Contains(rowId);
        return SelectedIds.Contains(rowId);
    }

    public void Select(string rowId)
    {
        if (AllSelected)
            DeselectedIds.Remove(rowId);
        else
            SelectedIds.Add(rowId);
    }

    public void Deselect(string rowId)
    {
        if (AllSelected)
            DeselectedIds.Add(rowId);
        else
            SelectedIds.Remove(rowId);
    }

    public void Toggle(string rowId)
    {
        if (IsSelected(rowId))
            Deselect(rowId);
        else
            Select(rowId);
    }

    public void Clear()
    {
        AllSelected = false;
        SelectedIds.Clear();
        DeselectedIds.Clear();
    }

    public void SelectAll()
    {
        AllSelected = true;
        SelectedIds.Clear();
        DeselectedIds.Clear();
    }
}
