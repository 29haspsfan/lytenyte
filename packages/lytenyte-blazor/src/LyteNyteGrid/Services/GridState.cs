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

    // Row alternate: supports None, Enabled (true), Root ("root") modes
    public RowAlternateMode RowAlternate { get; set; } = RowAlternateMode.Enabled;

    // Cell selection
    public CellSelectionMode CellSelectionMode { get; set; } = CellSelectionMode.None;
    public List<CellSelectionRect> CellSelections { get; set; } = new();
    public bool CellSelectionExcludeMarker { get; set; }

    // Floating row
    public bool FloatingRowEnabled { get; set; }
    public int FloatingRowHeight { get; set; } = 40;

    // Full-width row predicate
    public Func<int, bool>? RowFullWidthPredicate { get; set; }

    // Row group column
    public bool ShowRowGroupColumn { get; set; } = true;

    // Animation settings
    public AnimationSettings? RowAnimate { get; set; }
    public AnimationSettings? ColumnAnimate { get; set; }

    // Viewport
    public bool SuppressScrollFlash { get; set; }
    public int? ViewportInitialWidth { get; set; }
    public int? ViewportInitialHeight { get; set; }

    // Row drag-drop
    public string[]? RowDropAccept { get; set; }

    // Virtualization control
    public bool VirtualizeRows { get; set; } = true;
    public bool VirtualizeCols { get; set; } = true;

    // Keyboard navigation
    public (int RowIndex, int ColIndex)? FocusedCell { get; set; }

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

    // Row height mode (for auto / fill / function support)
    public RowHeightMode? RowHeightMode { get; set; }

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
        int effectiveRowHeight = RowHeightMode is not null
            ? RowHeightMode.Resolve(0, totalRows, ViewportHeight - TotalHeaderHeight)
            : RowHeight;

        Func<int, int>? heightFn = null;
        if (RowHeightMode?.HeightFunction is not null)
        {
            heightFn = RowHeightMode.HeightFunction;
        }
        else if (RowHeightMode is { IsFill: true } fillMode)
        {
            var available = ViewportHeight - TotalHeaderHeight;
            heightFn = _ => Math.Max(fillMode.FillMinHeight, totalRows > 0 ? available / totalRows : fillMode.FillMinHeight);
        }
        else if (RowHeightMode is { IsAuto: true } autoMode)
        {
            effectiveRowHeight = autoMode.GuessHeight;
        }

        YPositions = PositionCalculator.ComputeRowPositions(
            totalRows,
            effectiveRowHeight,
            rowHeightFn: heightFn,
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
        HeaderHeight
        + (View.MaxHeaderDepth > 0 ? View.MaxHeaderDepth * HeaderGroupHeight : 0)
        + (FloatingRowEnabled ? FloatingRowHeight : 0);

    public int TotalContentHeight => YPositions.Length > 0 ? YPositions[^1] : 0;
    public int TotalContentWidth => XPositions.Length > 0 ? XPositions[^1] : 0;

    // ---- IGridApi implementation ----

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

    public Dictionary<string, int> ColumnAutosize(bool dryRun = false, bool includeHeader = true, string[]? columnIds = null)
    {
        var targetColumns = columnIds is not null
            ? View.VisibleColumns.Where(c => columnIds.Contains(c.Id)).ToList()
            : View.VisibleColumns.ToList();

        var result = new Dictionary<string, int>();
        foreach (var col in targetColumns)
        {
            int maxWidth = col.MinWidth;

            if (includeHeader && col.Name is not null)
            {
                maxWidth = Math.Max(maxWidth, col.Name.Length * 8 + 16);
            }

            for (int r = 0; r < _dataSource.TotalRowCount; r++)
            {
                var row = _dataSource.RowByIndex(r);
                if (row is null) continue;

                var value = col.GetValueFromNode(row);
                if (value is not null)
                {
                    int estimatedWidth = value.ToString()?.Length * 8 + 16 ?? col.MinWidth;
                    maxWidth = Math.Max(maxWidth, estimatedWidth);
                }
            }

            if (col.MaxWidth.HasValue)
                maxWidth = Math.Min(maxWidth, col.MaxWidth.Value);

            result[col.Id] = maxWidth;
        }

        if (!dryRun)
        {
            ColumnResize(result);
        }

        return result;
    }

    public void ColumnUpdate(Dictionary<string, object> updates)
    {
        foreach (var (columnId, value) in updates)
        {
            var col = Columns.FirstOrDefault(c => c.Id == columnId);
            if (col is null) continue;

            if (value is Dictionary<string, object> props)
            {
                foreach (var (propName, propValue) in props)
                {
                    switch (propName)
                    {
                        case "Name" when propValue is string s:
                            col.Name = s;
                            break;
                        case "Width" when propValue is int w:
                            col.Width = Math.Max(col.MinWidth, col.MaxWidth.HasValue ? Math.Min(w, col.MaxWidth.Value) : w);
                            break;
                        case "MinWidth" when propValue is int mw:
                            col.MinWidth = mw;
                            break;
                        case "MaxWidth" when propValue is int? mxw:
                            col.MaxWidth = mxw;
                            break;
                        case "Hide" when propValue is bool h:
                            col.Hide = h;
                            break;
                        case "Pin" when propValue is ColumnPin p:
                            col.Pin = p;
                            break;
                        case "Sortable" when propValue is bool sb:
                            col.Sortable = sb;
                            break;
                        case "Resizable" when propValue is bool rb:
                            col.Resizable = rb;
                            break;
                        case "Movable" when propValue is bool mb:
                            col.Movable = mb;
                            break;
                        case "Editable" when propValue is bool eb:
                            col.Editable = eb;
                            break;
                    }
                }
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

    public int RowDetailHeight(string rowId)
    {
        return DetailHeightCache.TryGetValue(rowId, out var height) ? height : DetailRowHeight;
    }

    public void SetRowDetailHeight(string rowId, int height)
    {
        DetailHeightCache[rowId] = height;
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

    public bool RowIsAggregated(RowNode row) => row is RowAggregated;

    public bool IsFullWidthRow(int rowIndex)
    {
        return RowFullWidthPredicate?.Invoke(rowIndex) ?? false;
    }

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

    // ---- Cell Selection ----

    public IReadOnlyList<CellSelectionRect> GetCellSelections() => CellSelections;

    public void CellSelectionAdd(CellSelectionRect rect)
    {
        if (CellSelectionMode == CellSelectionMode.None) return;

        if (CellSelectionMode == CellSelectionMode.Range)
            CellSelections.Clear();

        CellSelections.Add(rect);
        NotifyStateChanged();
    }

    public void CellSelectionRemove(int index)
    {
        if (index >= 0 && index < CellSelections.Count)
        {
            CellSelections.RemoveAt(index);
            NotifyStateChanged();
        }
    }

    public void CellSelectionClear()
    {
        CellSelections.Clear();
        NotifyStateChanged();
    }

    public bool IsCellSelected(int rowIndex, int colIndex)
    {
        return CellSelections.Any(r => r.Contains(rowIndex, colIndex));
    }

    // ---- Editing ----

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

    public void EditUpdateRows(Dictionary<string, T> rowUpdates)
    {
        foreach (var (rowId, newData) in rowUpdates)
        {
            _dataSource.UpdateRow(rowId, newData);
        }
        RecomputeLayout();
        NotifyStateChanged();
    }

    public void EditUpdateCells(Dictionary<string, List<(object? value, string columnId)>> cellUpdates)
    {
        foreach (var (rowId, cells) in cellUpdates)
        {
            var row = _dataSource.RowById(rowId);
            if (row is not RowLeaf<T> leaf || leaf.Data is null) continue;

            foreach (var (value, columnId) in cells)
            {
                var col = ColumnById(columnId);
                if (col?.FieldName is null) continue;

                var prop = leaf.Data.GetType().GetProperty(col.FieldName);
                if (prop is not null && prop.CanWrite)
                {
                    prop.SetValue(leaf.Data, value);
                }
            }
        }
        RecomputeLayout();
        NotifyStateChanged();
    }

    // ---- Data operations ----

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

    // ---- Scroll ----

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

    // ---- Keyboard navigation ----

    public void MoveFocus(int rowDelta, int colDelta)
    {
        var totalRows = _dataSource.TotalRowCount;
        var totalCols = View.VisibleColumns.Count;
        if (totalRows == 0 || totalCols == 0) return;

        var current = FocusedCell ?? (0, 0);
        var newRow = Math.Clamp(current.RowIndex + rowDelta, 0, totalRows - 1);
        var newCol = Math.Clamp(current.ColIndex + colDelta, 0, totalCols - 1);

        FocusedCell = (newRow, newCol);
        NotifyStateChanged();
    }

    public void FocusCell(int rowIndex, int colIndex)
    {
        FocusedCell = (rowIndex, colIndex);
        NotifyStateChanged();
    }

    public void FocusFirstColumn()
    {
        if (FocusedCell is { } current)
            FocusedCell = (current.RowIndex, 0);
        else
            FocusedCell = (0, 0);
        NotifyStateChanged();
    }

    public void FocusLastColumn()
    {
        var lastCol = Math.Max(0, View.VisibleColumns.Count - 1);
        if (FocusedCell is { } current)
            FocusedCell = (current.RowIndex, lastCol);
        else
            FocusedCell = (0, lastCol);
        NotifyStateChanged();
    }

    public void FocusFirstRow()
    {
        if (FocusedCell is { } current)
            FocusedCell = (0, current.ColIndex);
        else
            FocusedCell = (0, 0);
        NotifyStateChanged();
    }

    public void FocusLastRow()
    {
        var lastRow = Math.Max(0, _dataSource.TotalRowCount - 1);
        if (FocusedCell is { } current)
            FocusedCell = (lastRow, current.ColIndex);
        else
            FocusedCell = (lastRow, 0);
        NotifyStateChanged();
    }

    // ---- Export ----

    public IReadOnlyList<IReadOnlyList<object?>> ExportData(int? rowStart = null, int? rowEnd = null, int? colStart = null, int? colEnd = null)
    {
        return ExportDataFull(rowStart, rowEnd, colStart, colEnd).Data;
    }

    public ExportDataResult<T> ExportDataFull(int? rowStart = null, int? rowEnd = null, int? colStart = null, int? colEnd = null)
    {
        var rs = rowStart ?? 0;
        var re = rowEnd ?? _dataSource.TotalRowCount;
        var cs = colStart ?? 0;
        var ce = colEnd ?? View.VisibleColumns.Count;

        var exportedColumns = View.VisibleColumns.Skip(cs).Take(ce - cs).ToList();
        var headers = exportedColumns.Select(c => c.Name ?? c.Id).ToList();

        int maxDepth = View.MaxHeaderDepth;
        var groupHeaders = new List<IReadOnlyList<string?>>();
        for (int depth = 0; depth < maxDepth; depth++)
        {
            var level = new List<string?>();
            foreach (var col in exportedColumns)
            {
                if (col.GroupPath is not null && depth < col.GroupPath.Length)
                    level.Add(col.GroupPath[depth]);
                else
                    level.Add(null);
            }
            groupHeaders.Add(level);
        }

        var data = new List<IReadOnlyList<object?>>();
        for (int r = rs; r < re; r++)
        {
            var row = _dataSource.RowByIndex(r);
            if (row is null) continue;

            var rowData = new List<object?>();
            foreach (var col in exportedColumns)
            {
                rowData.Add(col.GetValueFromNode(row));
            }
            data.Add(rowData);
        }

        return new ExportDataResult<T>
        {
            Headers = headers,
            GroupHeaders = groupHeaders,
            Columns = exportedColumns,
            Data = data,
        };
    }
}
