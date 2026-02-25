using LyteNyteGrid.Enums;
using LyteNyteGrid.Models;

namespace LyteNyteGrid.Services;

/// <summary>
/// Internal state container for the grid, managing the computed layout state.
/// Acts as the Blazor equivalent of the React context providers.
/// </summary>
/// <typeparam name="T">Row data type.</typeparam>
public class GridState<T> : IGridApi<T>
{
    private readonly GridDataSource<T> _dataSource;

    public GridState(GridDataSource<T> dataSource)
    {
        _dataSource = dataSource;
    }

    /// <summary>Access to the underlying data source.</summary>
    public GridDataSource<T> DataSource => _dataSource;

    // Layout state
    public ColumnView<T> View { get; set; } = new();
    public IReadOnlyList<ColumnDefinition<T>> Columns { get; set; } = [];
    public int[] XPositions { get; set; } = [0];
    public int[] YPositions { get; set; } = [0];
    public ViewBounds Bounds { get; set; }

    // Dimensions
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    public int ScrollLeft { get; set; }
    public int ScrollTop { get; set; }

    // Configuration
    public int HeaderHeight { get; set; } = 40;
    public int HeaderGroupHeight { get; set; } = 40;
    public int RowHeight { get; set; } = 40;
    public bool Rtl { get; set; }
    public bool ColumnSizeToFit { get; set; }
    public EditMode EditMode { get; set; } = EditMode.ReadOnly;
    public EditClickActivator EditClickActivator { get; set; } = EditClickActivator.DoubleClick;
    public RowSelectionMode SelectionMode { get; set; } = RowSelectionMode.None;
    public RowSelectionActivator SelectionActivator { get; set; } = RowSelectionActivator.SingleClick;
    public bool RowAlternate { get; set; } = true;

    // Detail rows
    public HashSet<string> DetailExpansions { get; set; } = new();
    public int DetailRowHeight { get; set; } = 200;
    public Dictionary<string, int> DetailHeightCache { get; set; } = new();

    // Group expansions
    public Dictionary<string, bool> GroupExpansions { get; set; } = new();
    public bool GroupDefaultExpansion { get; set; } = true;

    // Column group expansions
    public Dictionary<string, bool> ColumnGroupExpansions { get; set; } = new();
    public bool ColumnGroupDefaultExpansion { get; set; } = true;

    // Edit state
    public (string ColumnId, int RowIndex)? ActiveEdit { get; set; }
    public object? EditValue { get; set; }

    // Styles
    public GridStyle? Styles { get; set; }

    // Events
    public event Action? StateChanged;

    public void NotifyStateChanged() => StateChanged?.Invoke();

    /// <summary>
    /// Recomputes the full layout: column view, positions, and bounds.
    /// </summary>
    public void RecomputeLayout()
    {
        View = ColumnView<T>.Build(Columns, ColumnGroupExpansions, ColumnGroupDefaultExpansion);
        XPositions = PositionCalculator.ComputeColumnPositions(View, ColumnSizeToFit, ViewportWidth);

        int totalRows = _dataSource.TotalRowCount;
        YPositions = PositionCalculator.ComputeRowPositions(
            totalRows,
            RowHeight,
            detailHeights: DetailHeightCache.Count > 0 ? DetailHeightCache : null,
            rowIdForIndex: i => _dataSource.RowByIndex(i)?.Id);

        int totalHeaderHeight = HeaderHeight + (View.MaxHeaderDepth > 0 ? View.MaxHeaderDepth * HeaderGroupHeight : 0);

        Bounds = PositionCalculator.ComputeBounds(
            XPositions,
            YPositions,
            ScrollLeft,
            ScrollTop,
            ViewportWidth,
            ViewportHeight,
            totalHeaderHeight,
            View.StartPinned.Count,
            View.EndPinned.Count,
            _dataSource.TopRowCount,
            _dataSource.BottomRowCount);
    }

    public int TotalHeaderHeight =>
        HeaderHeight + (View.MaxHeaderDepth > 0 ? View.MaxHeaderDepth * HeaderGroupHeight : 0);

    public int TotalContentHeight => YPositions.Length > 0 ? YPositions[^1] : 0;
    public int TotalContentWidth => XPositions.Length > 0 ? XPositions[^1] : 0;

    // IGridApi implementation
    public ColumnDefinition<T>? ColumnById(string id) =>
        View.ColumnIdToIndex.TryGetValue(id, out var idx) ? View.VisibleColumns[idx] : null;

    public ColumnDefinition<T>? ColumnByIndex(int index) =>
        index >= 0 && index < View.VisibleColumns.Count ? View.VisibleColumns[index] : null;

    public object? ColumnField(string columnId, RowNode row)
    {
        var col = ColumnById(columnId);
        return col?.GetValueFromNode(row);
    }

    public void ColumnMove(string[] moveColumnIds, string targetColumnId, bool before = false)
    {
        var columns = new List<ColumnDefinition<T>>(Columns);
        var toMove = columns.Where(c => moveColumnIds.Contains(c.Id)).ToList();
        foreach (var col in toMove) columns.Remove(col);

        var targetIdx = columns.FindIndex(c => c.Id == targetColumnId);
        if (targetIdx < 0) return;
        if (!before) targetIdx++;

        columns.InsertRange(targetIdx, toMove);
        Columns = columns;
        RecomputeLayout();
        NotifyStateChanged();
    }

    public void ColumnResize(Dictionary<string, int> sizes)
    {
        foreach (var (id, width) in sizes)
        {
            var col = Columns.FirstOrDefault(c => c.Id == id);
            if (col is not null)
            {
                col.Width = Math.Max(col.MinWidth, col.MaxWidth.HasValue ? Math.Min(width, col.MaxWidth.Value) : width);
            }
        }
        RecomputeLayout();
        NotifyStateChanged();
    }

    public void ColumnToggleGroup(string groupId, bool? state = null)
    {
        var current = ColumnGroupExpansions.GetValueOrDefault(groupId, ColumnGroupDefaultExpansion);
        ColumnGroupExpansions[groupId] = state ?? !current;
        RecomputeLayout();
        NotifyStateChanged();
    }

    public ColumnView<T> GetColumnView() => View;

    public RowNode? RowByIndex(int index) => _dataSource.RowByIndex(index);

    public bool RowDetailExpanded(string rowId) => DetailExpansions.Contains(rowId);

    public void RowDetailToggle(string rowId, bool? state = null)
    {
        var current = DetailExpansions.Contains(rowId);
        var target = state ?? !current;
        if (target)
            DetailExpansions.Add(rowId);
        else
            DetailExpansions.Remove(rowId);
        RecomputeLayout();
        NotifyStateChanged();
    }

    public void RowGroupToggle(string rowId, bool? state = null)
    {
        _dataSource.ToggleGroupExpansion(rowId, state);
        RecomputeLayout();
        NotifyStateChanged();
    }

    public bool RowIsExpanded(RowNode row) =>
        row is RowGroup ? _dataSource.IsGroupExpanded(row.Id) : false;

    public bool RowIsExpandable(RowNode row) => row is RowGroup;

    public (int RowCount, int TopCount, int BottomCount, int CenterCount) RowView() =>
        (_dataSource.TotalRowCount, _dataSource.TopRowCount, _dataSource.BottomRowCount, _dataSource.CenterRowCount);

    public void RowSelect(string rowId, bool? deselect = null)
    {
        if (deselect == true)
            _dataSource.Selection.Deselect(rowId);
        else
            _dataSource.Selection.Toggle(rowId);
        NotifyStateChanged();
    }

    public void RowSelectAll(bool deselect = false)
    {
        if (deselect)
            _dataSource.Selection.Clear();
        else
            _dataSource.Selection.SelectAll();
        NotifyStateChanged();
    }

    public RowSelectionState GetSelectionState() => _dataSource.Selection;

    public void EditBegin(string columnId, int rowIndex)
    {
        if (EditMode == EditMode.ReadOnly) return;
        var col = ColumnById(columnId);
        if (col is null) return;

        var row = RowByIndex(rowIndex);
        if (row is null) return;

        var editable = col.EditableFn is not null
            ? col.EditableFn(new CellContext<T> { Row = row, Column = col, RowIndex = rowIndex, ColIndex = 0, Api = this })
            : col.Editable;

        if (!editable) return;

        ActiveEdit = (columnId, rowIndex);
        EditValue = col.GetValueFromNode(row);
        NotifyStateChanged();
    }

    public void EditEnd(bool cancel = false)
    {
        if (ActiveEdit is null) return;

        if (!cancel && ActiveEdit is var (colId, rowIdx))
        {
            var col = ColumnById(colId);
            var row = RowByIndex(rowIdx);
            if (col is not null && row is RowLeaf<T> leaf && leaf.Data is not null)
            {
                // Apply the edit via reflection or field setter
                if (col.FieldName is not null)
                {
                    var prop = leaf.Data.GetType().GetProperty(col.FieldName);
                    if (prop is not null && prop.CanWrite)
                    {
                        prop.SetValue(leaf.Data, EditValue);
                    }
                }
            }
        }

        ActiveEdit = null;
        EditValue = null;
        NotifyStateChanged();
    }

    public bool EditIsCellActive(string columnId, int rowIndex) =>
        ActiveEdit is var (colId, ri) && colId == columnId && ri == rowIndex;

    public void AddRows(IEnumerable<T> rows, int? index = null)
    {
        _dataSource.AddRows(rows, index);
        RecomputeLayout();
        NotifyStateChanged();
    }

    public void DeleteRows(IEnumerable<string> rowIds)
    {
        _dataSource.DeleteRows(rowIds);
        RecomputeLayout();
        NotifyStateChanged();
    }

    public void UpdateRow(string rowId, T newData)
    {
        _dataSource.UpdateRow(rowId, newData);
        RecomputeLayout();
        NotifyStateChanged();
    }

    public void ScrollToRow(int rowIndex)
    {
        if (rowIndex >= 0 && rowIndex < YPositions.Length - 1)
        {
            ScrollTop = YPositions[rowIndex];
            RecomputeLayout();
            NotifyStateChanged();
        }
    }

    public void ScrollToColumn(string columnId)
    {
        if (View.ColumnIdToIndex.TryGetValue(columnId, out var idx) && idx < XPositions.Length - 1)
        {
            ScrollLeft = XPositions[idx];
            RecomputeLayout();
            NotifyStateChanged();
        }
    }

    public IReadOnlyList<IReadOnlyList<object?>> ExportData(int? rowStart = null, int? rowEnd = null, int? colStart = null, int? colEnd = null)
    {
        var rs = rowStart ?? 0;
        var re = rowEnd ?? _dataSource.TotalRowCount;
        var cs = colStart ?? 0;
        var ce = colEnd ?? View.VisibleColumns.Count;

        var result = new List<IReadOnlyList<object?>>();
        for (int r = rs; r < re; r++)
        {
            var row = _dataSource.RowByIndex(r);
            if (row is null) continue;

            var rowData = new List<object?>();
            for (int c = cs; c < ce; c++)
            {
                var col = View.VisibleColumns[c];
                rowData.Add(col.GetValueFromNode(row));
            }
            result.Add(rowData);
        }
        return result;
    }
}
