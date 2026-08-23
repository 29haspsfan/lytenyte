using LyteNyteGrid.Enums;

namespace LyteNyteGrid.Models;

/// <summary>
/// Computed column layout information including pinned/center columns and visibility.
/// </summary>
/// <typeparam name="T">Row data type.</typeparam>
public class ColumnView<T>
{
    /// <summary>All visible columns in display order.</summary>
    public IReadOnlyList<ColumnDefinition<T>> VisibleColumns { get; init; } = [];

    /// <summary>Columns pinned to the start.</summary>
    public IReadOnlyList<ColumnDefinition<T>> StartPinned { get; init; } = [];

    /// <summary>Unpinned center columns.</summary>
    public IReadOnlyList<ColumnDefinition<T>> Center { get; init; } = [];

    /// <summary>Columns pinned to the end.</summary>
    public IReadOnlyList<ColumnDefinition<T>> EndPinned { get; init; } = [];

    /// <summary>Maps column ID to its visible index.</summary>
    public IReadOnlyDictionary<string, int> ColumnIdToIndex { get; init; } = new Dictionary<string, int>();

    /// <summary>Maximum header group depth.</summary>
    public int MaxHeaderDepth { get; init; }

    /// <summary>
    /// Builds a ColumnView from a list of column definitions and group expansion state.
    /// </summary>
    public static ColumnView<T> Build(
        IReadOnlyList<ColumnDefinition<T>> columns,
        Dictionary<string, bool>? groupExpansions,
        bool groupDefaultExpansion)
    {
        var visible = new List<ColumnDefinition<T>>();
        var startPinned = new List<ColumnDefinition<T>>();
        var center = new List<ColumnDefinition<T>>();
        var endPinned = new List<ColumnDefinition<T>>();
        var idToIndex = new Dictionary<string, int>();
        int maxDepth = 0;

        foreach (var col in columns)
        {
            if (col.Hide)
                continue;

            // Check column group visibility
            if (col.GroupPath is { Length: > 0 } && col.GroupVisibility != ColumnGroupVisibility.Always)
            {
                var groupId = string.Join("->", col.GroupPath);
                var isExpanded = groupExpansions?.GetValueOrDefault(groupId, groupDefaultExpansion) ?? groupDefaultExpansion;

                if (col.GroupVisibility == ColumnGroupVisibility.Open && !isExpanded)
                    continue;
                if (col.GroupVisibility == ColumnGroupVisibility.Close && isExpanded)
                    continue;
            }

            int idx = visible.Count;
            idToIndex[col.Id] = idx;
            visible.Add(col);

            switch (col.Pin)
            {
                case ColumnPin.Start:
                    startPinned.Add(col);
                    break;
                case ColumnPin.End:
                    endPinned.Add(col);
                    break;
                default:
                    center.Add(col);
                    break;
            }

            if (col.GroupPath is not null)
                maxDepth = Math.Max(maxDepth, col.GroupPath.Length);
        }

        return new ColumnView<T>
        {
            VisibleColumns = visible,
            StartPinned = startPinned,
            Center = center,
            EndPinned = endPinned,
            ColumnIdToIndex = idToIndex,
            MaxHeaderDepth = maxDepth,
        };
    }
}
