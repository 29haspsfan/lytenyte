using LyteNyteGrid.Enums;

namespace LyteNyteGrid.Models;

/// <summary>
/// Imperative API for programmatic control of the grid.
/// Equivalent to the React Grid.API type.
/// </summary>
/// <typeparam name="T">Row data type.</typeparam>
public interface IGridApi<T>
{
    // Column operations
    ColumnDefinition<T>? ColumnById(string id);
    ColumnDefinition<T>? ColumnByIndex(int index);
    object? ColumnField(string columnId, RowNode row);
    void ColumnMove(string[] moveColumnIds, string targetColumnId, bool before = false);
    void ColumnResize(Dictionary<string, int> sizes);
    Dictionary<string, int> ColumnAutosize(bool dryRun = false, bool includeHeader = true, string[]? columnIds = null);
    void ColumnUpdate(Dictionary<string, object> updates);
    void ColumnToggleGroup(string groupId, bool? state = null);
    ColumnView<T> GetColumnView();

    // Row operations
    RowNode? RowByIndex(int index);
    bool RowDetailExpanded(string rowId);
    void RowDetailToggle(string rowId, bool? state = null);
    int RowDetailHeight(string rowId);
    void RowGroupToggle(string rowId, bool? state = null);
    bool RowIsExpanded(RowNode row);
    bool RowIsExpandable(RowNode row);
    bool RowIsAggregated(RowNode row);
    bool IsFullWidthRow(int rowIndex);
    (int RowCount, int TopCount, int BottomCount, int CenterCount) RowView();

    // Row selection
    void RowSelect(string rowId, bool? deselect = null);
    void RowSelectAll(bool deselect = false);
    RowSelectionState GetSelectionState();

    // Cell selection
    IReadOnlyList<CellSelectionRect> GetCellSelections();
    void CellSelectionAdd(CellSelectionRect rect);
    void CellSelectionRemove(int index);
    void CellSelectionClear();
    bool IsCellSelected(int rowIndex, int colIndex);

    // Editing
    void EditBegin(string columnId, int rowIndex);
    void EditEnd(bool cancel = false);
    bool EditIsCellActive(string columnId, int rowIndex);
    void EditUpdateRows(Dictionary<string, T> rowUpdates);
    void EditUpdateCells(Dictionary<string, List<(object? value, string columnId)>> cellUpdates);

    // Data operations
    void AddRows(IEnumerable<T> rows, int? index = null);
    void DeleteRows(IEnumerable<string> rowIds);
    void UpdateRow(string rowId, T newData);

    // Scroll
    void ScrollToRow(int rowIndex);
    void ScrollToColumn(string columnId);

    // Data export
    IReadOnlyList<IReadOnlyList<object?>> ExportData(int? rowStart = null, int? rowEnd = null, int? colStart = null, int? colEnd = null);
    ExportDataResult<T> ExportDataFull(int? rowStart = null, int? rowEnd = null, int? colStart = null, int? colEnd = null);
}
