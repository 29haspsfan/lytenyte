using LyteNyteGrid.Enums;
using LyteNyteGrid.Models;

namespace LyteNyteGrid.Services;

/// <summary>
/// Client-side data source that manages row data, sorting, filtering, grouping, and selection.
/// Equivalent to the React useClientDataSource hook.
/// </summary>
/// <typeparam name="T">Row data type.</typeparam>
public class GridDataSource<T>
{
    private List<T> _data = new();
    private List<T> _topData = new();
    private List<T> _bottomData = new();
    private List<RowNode> _flattenedRows = new();
    private List<RowNode> _topRows = new();
    private List<RowNode> _bottomRows = new();
    private List<RowNode> _centerRows = new();

    private IReadOnlyList<ColumnDefinition<T>> _columns = [];
    private IReadOnlyList<SortDimension<T>>? _sortDimensions;
    private IReadOnlyList<FilterDefinition>? _filters;
    private IReadOnlyList<GroupDefinition<T>>? _groupDimensions;

    private readonly Dictionary<string, bool> _rowGroupExpansions = new();
    private bool _rowGroupDefaultExpansion = true;

    private Func<T, string>? _leafIdFn;
    private int _leafIdCounter;

    public event Action? DataChanged;

    public RowSelectionState Selection { get; } = new();

    public int TotalRowCount => _flattenedRows.Count;
    public int TopRowCount => _topRows.Count;
    public int BottomRowCount => _bottomRows.Count;
    public int CenterRowCount => _centerRows.Count;

    public IReadOnlyList<RowNode> FlattenedRows => _flattenedRows;
    public IReadOnlyList<RowNode> TopRows => _topRows;
    public IReadOnlyList<RowNode> CenterRows => _centerRows;
    public IReadOnlyList<RowNode> BottomRows => _bottomRows;

    public void Configure(
        IReadOnlyList<ColumnDefinition<T>> columns,
        Func<T, string>? leafIdFn = null)
    {
        _columns = columns;
        _leafIdFn = leafIdFn;
    }

    public void SetData(IEnumerable<T> data, IEnumerable<T>? topData = null, IEnumerable<T>? bottomData = null)
    {
        _data = new List<T>(data);
        _topData = topData is not null ? new List<T>(topData) : new List<T>();
        _bottomData = bottomData is not null ? new List<T>(bottomData) : new List<T>();
        Refresh();
    }

    public void SetSort(IReadOnlyList<SortDimension<T>>? dimensions)
    {
        _sortDimensions = dimensions;
        Refresh();
    }

    public void SetFilters(IReadOnlyList<FilterDefinition>? filters)
    {
        _filters = filters;
        Refresh();
    }

    public void SetGroups(IReadOnlyList<GroupDefinition<T>>? groups)
    {
        _groupDimensions = groups;
        Refresh();
    }

    public void SetGroupExpansion(string groupId, bool expanded)
    {
        _rowGroupExpansions[groupId] = expanded;
        Refresh();
    }

    public void SetGroupDefaultExpansion(bool expanded)
    {
        _rowGroupDefaultExpansion = expanded;
        Refresh();
    }

    public void AddRows(IEnumerable<T> rows, int? index = null)
    {
        if (index.HasValue)
            _data.InsertRange(index.Value, rows);
        else
            _data.AddRange(rows);
        Refresh();
    }

    public void DeleteRows(IEnumerable<string> rowIds)
    {
        var ids = new HashSet<string>(rowIds);
        _data.RemoveAll(item =>
        {
            var id = GetLeafId(item);
            return ids.Contains(id);
        });
        Refresh();
    }

    public void UpdateRow(string rowId, T newData)
    {
        for (int i = 0; i < _data.Count; i++)
        {
            if (GetLeafId(_data[i]) == rowId)
            {
                _data[i] = newData;
                break;
            }
        }
        Refresh();
    }

    public RowNode? RowByIndex(int index)
    {
        if (index < 0 || index >= _flattenedRows.Count)
            return null;
        return _flattenedRows[index];
    }

    public RowNode? RowById(string id)
    {
        return _flattenedRows.FirstOrDefault(r => r.Id == id);
    }

    public bool IsGroupExpanded(string groupId)
    {
        return _rowGroupExpansions.GetValueOrDefault(groupId, _rowGroupDefaultExpansion);
    }

    public void ToggleGroupExpansion(string groupId, bool? state = null)
    {
        var current = IsGroupExpanded(groupId);
        _rowGroupExpansions[groupId] = state ?? !current;
        Refresh();
    }

    /// <summary>
    /// Refreshes the computed data pipeline: filter -> sort -> group -> flatten.
    /// </summary>
    public void Refresh()
    {
        // Step 1: Create leaf nodes
        var leafNodes = _data.Select(CreateLeafNode).ToList();

        // Step 2: Apply filters
        var filtered = ApplyFilters(leafNodes);

        // Step 3: Apply sorting
        var sorted = ApplySort(filtered);

        // Step 4: Apply grouping (or flatten directly)
        if (_groupDimensions is { Count: > 0 })
        {
            _centerRows = ApplyGrouping(sorted);
        }
        else
        {
            _centerRows = sorted.Cast<RowNode>().ToList();
        }

        // Top/bottom pinned rows
        _topRows = _topData.Select(CreateLeafNode).Cast<RowNode>().ToList();
        _bottomRows = _bottomData.Select(CreateLeafNode).Cast<RowNode>().ToList();

        // Flatten all into display order
        _flattenedRows = new List<RowNode>(_topRows.Count + _centerRows.Count + _bottomRows.Count);
        _flattenedRows.AddRange(_topRows);
        _flattenedRows.AddRange(_centerRows);
        _flattenedRows.AddRange(_bottomRows);

        DataChanged?.Invoke();
    }

    private RowLeaf<T> CreateLeafNode(T data)
    {
        return new RowLeaf<T>
        {
            Id = GetLeafId(data),
            Data = data,
        };
    }

    private string GetLeafId(T data)
    {
        if (_leafIdFn is not null)
            return _leafIdFn(data);

        // Fallback: use index-based ID
        return $"row-{_leafIdCounter++}";
    }

    private List<RowLeaf<T>> ApplyFilters(List<RowLeaf<T>> nodes)
    {
        if (_filters is not { Count: > 0 })
            return nodes;

        return nodes.Where(leaf =>
        {
            if (leaf.Data is null) return false;

            foreach (var filter in _filters)
            {
                var column = _columns.FirstOrDefault(c => c.Id == filter.ColumnId);
                if (column is null) continue;

                var value = column.GetValue(leaf.Data);

                if (filter is CustomFilter<T> customFilter)
                {
                    if (!customFilter.Predicate(leaf.Data))
                        return false;
                }
                else if (filter is StringFilter sf)
                {
                    if (!EvaluateStringFilter(value?.ToString(), sf))
                        return false;
                }
                else if (filter is NumberFilter nf)
                {
                    if (!EvaluateNumberFilter(value, nf))
                        return false;
                }
                else if (filter is DateFilter df)
                {
                    if (!EvaluateDateFilter(value, df))
                        return false;
                }
            }
            return true;
        }).ToList();
    }

    private List<RowLeaf<T>> ApplySort(List<RowLeaf<T>> nodes)
    {
        if (_sortDimensions is not { Count: > 0 })
            return nodes;

        var sorted = new List<RowLeaf<T>>(nodes);
        sorted.Sort((a, b) =>
        {
            foreach (var dim in _sortDimensions)
            {
                var column = _columns.FirstOrDefault(c => c.Id == dim.ColumnId);
                if (column is null) continue;

                var aVal = a.Data is not null ? column.GetValue(a.Data) : null;
                var bVal = b.Data is not null ? column.GetValue(b.Data) : null;

                int result;
                if (dim.Comparator is not null)
                {
                    result = dim.Comparator(aVal, bVal);
                }
                else if (column.SortComparator is not null)
                {
                    result = column.SortComparator(aVal, bVal);
                }
                else
                {
                    result = DefaultCompare(aVal, bVal);
                }

                if (dim.Direction == SortDirection.Descending)
                    result = -result;

                if (result != 0)
                    return result;
            }
            return 0;
        });
        return sorted;
    }

    private List<RowNode> ApplyGrouping(List<RowLeaf<T>> leafs)
    {
        if (_groupDimensions is not { Count: > 0 })
            return leafs.Cast<RowNode>().ToList();

        return BuildGroupTree(leafs, 0, Array.Empty<string>());
    }

    private List<RowNode> BuildGroupTree(List<RowLeaf<T>> leafs, int dimIndex, string[] parentPath)
    {
        if (dimIndex >= _groupDimensions!.Count)
            return leafs.Cast<RowNode>().ToList();

        var dim = _groupDimensions[dimIndex];
        var column = _columns.FirstOrDefault(c => c.Id == dim.ColumnId);

        // Group leaves by key
        var groups = new Dictionary<string, List<RowLeaf<T>>>();
        var groupOrder = new List<string>();

        foreach (var leaf in leafs)
        {
            string? key;
            if (dim.GroupKeyFn is not null && leaf.Data is not null)
            {
                key = dim.GroupKeyFn(leaf.Data);
            }
            else if (column is not null && leaf.Data is not null)
            {
                key = column.GetValue(leaf.Data)?.ToString();
            }
            else
            {
                key = null;
            }

            var groupKey = key ?? "(null)";
            if (!groups.TryGetValue(groupKey, out var list))
            {
                list = new List<RowLeaf<T>>();
                groups[groupKey] = list;
                groupOrder.Add(groupKey);
            }
            list.Add(leaf);
        }

        var result = new List<RowNode>();
        foreach (var groupKey in groupOrder)
        {
            var path = parentPath.Append(groupKey).ToArray();
            var groupId = string.Join("->", path);

            // Compute aggregates for the group
            var aggregates = new Dictionary<string, object?>();
            if (column is not null)
            {
                foreach (var aggCol in _columns.Where(c => c.AggregateFn is not null))
                {
                    var values = groups[groupKey]
                        .Where(l => l.Data is not null)
                        .Select(l => aggCol.GetValue(l.Data!));
                    aggregates[aggCol.Id] = aggCol.AggregateFn!(values);
                }
            }

            var groupNode = new RowGroup
            {
                Id = groupId,
                Key = groupKey,
                Depth = dimIndex,
                Data = aggregates,
            };

            result.Add(groupNode);

            if (IsGroupExpanded(groupId))
            {
                var children = BuildGroupTree(groups[groupKey], dimIndex + 1, path);
                result.AddRange(children);
            }
        }

        return result;
    }

    private static int DefaultCompare(object? a, object? b)
    {
        if (a is null && b is null) return 0;
        if (a is null) return -1;
        if (b is null) return 1;

        if (a is IComparable ca)
            return ca.CompareTo(b);

        return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }

    private static bool EvaluateStringFilter(string? value, StringFilter filter)
    {
        if (value is null)
            return filter.Operator is FilterStringOperator.Equals && filter.Value is null;

        var filterVal = filter.Value ?? "";
        var comparison = filter.CaseInsensitive
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (filter.TrimWhitespace)
        {
            value = value.Trim();
            filterVal = filterVal.Trim();
        }

        return filter.Operator switch
        {
            FilterStringOperator.Equals => string.Equals(value, filterVal, comparison),
            FilterStringOperator.NotEquals => !string.Equals(value, filterVal, comparison),
            FilterStringOperator.Contains => value.Contains(filterVal, comparison),
            FilterStringOperator.NotContains => !value.Contains(filterVal, comparison),
            FilterStringOperator.BeginsWith => value.StartsWith(filterVal, comparison),
            FilterStringOperator.NotBeginsWith => !value.StartsWith(filterVal, comparison),
            FilterStringOperator.EndsWith => value.EndsWith(filterVal, comparison),
            FilterStringOperator.NotEndsWith => !value.EndsWith(filterVal, comparison),
            FilterStringOperator.LessThan => string.Compare(value, filterVal, comparison) < 0,
            FilterStringOperator.LessThanOrEquals => string.Compare(value, filterVal, comparison) <= 0,
            FilterStringOperator.GreaterThan => string.Compare(value, filterVal, comparison) > 0,
            FilterStringOperator.GreaterThanOrEquals => string.Compare(value, filterVal, comparison) >= 0,
            FilterStringOperator.Length => value.Length == (int.TryParse(filterVal, out var len) ? len : -1),
            FilterStringOperator.NotLength => value.Length != (int.TryParse(filterVal, out var nlen) ? nlen : -1),
            FilterStringOperator.LengthLessThan => int.TryParse(filterVal, out var llt) && value.Length < llt,
            FilterStringOperator.LengthLessThanOrEquals => int.TryParse(filterVal, out var llte) && value.Length <= llte,
            FilterStringOperator.LengthGreaterThan => int.TryParse(filterVal, out var lgt) && value.Length > lgt,
            FilterStringOperator.LengthGreaterThanOrEquals => int.TryParse(filterVal, out var lgte) && value.Length >= lgte,
            FilterStringOperator.Matches => System.Text.RegularExpressions.Regex.IsMatch(value, filterVal),
            _ => true,
        };
    }

    private static bool EvaluateNumberFilter(object? value, NumberFilter filter)
    {
        if (value is null)
            return filter.Operator == FilterNumberOperator.Equals && filter.Value is null;
        if (filter.Value is null)
            return filter.Operator == FilterNumberOperator.NotEquals;

        if (!double.TryParse(value.ToString(), out var numVal))
            return false;

        var filterVal = filter.Value.Value;
        if (filter.AbsoluteValue)
            numVal = Math.Abs(numVal);

        var epsilon = filter.Epsilon ?? 0;

        return filter.Operator switch
        {
            FilterNumberOperator.Equals => Math.Abs(numVal - filterVal) <= epsilon,
            FilterNumberOperator.NotEquals => Math.Abs(numVal - filterVal) > epsilon,
            FilterNumberOperator.GreaterThan => numVal > filterVal,
            FilterNumberOperator.GreaterThanOrEquals => numVal >= filterVal - epsilon,
            FilterNumberOperator.LessThan => numVal < filterVal,
            FilterNumberOperator.LessThanOrEquals => numVal <= filterVal + epsilon,
            _ => true,
        };
    }

    private static bool EvaluateDateFilter(object? value, DateFilter filter)
    {
        if (value is null)
            return filter.Operator == FilterDateOperator.Equals && filter.DateValue is null;

        DateTimeOffset dateVal;
        if (value is DateTimeOffset dto)
            dateVal = dto;
        else if (value is DateTime dt)
            dateVal = new DateTimeOffset(dt);
        else if (DateTimeOffset.TryParse(value.ToString(), out var parsed))
            dateVal = parsed;
        else
            return false;

        var now = DateTimeOffset.Now;

        return filter.Operator switch
        {
            FilterDateOperator.Equals => filter.DateValue.HasValue && CompareDates(dateVal, filter.DateValue.Value, filter.IncludeTime) == 0,
            FilterDateOperator.NotEquals => filter.DateValue.HasValue && CompareDates(dateVal, filter.DateValue.Value, filter.IncludeTime) != 0,
            FilterDateOperator.Before => filter.DateValue.HasValue && CompareDates(dateVal, filter.DateValue.Value, filter.IncludeTime) < 0,
            FilterDateOperator.BeforeOrEquals => filter.DateValue.HasValue && CompareDates(dateVal, filter.DateValue.Value, filter.IncludeTime) <= 0,
            FilterDateOperator.After => filter.DateValue.HasValue && CompareDates(dateVal, filter.DateValue.Value, filter.IncludeTime) > 0,
            FilterDateOperator.AfterOrEquals => filter.DateValue.HasValue && CompareDates(dateVal, filter.DateValue.Value, filter.IncludeTime) >= 0,
            FilterDateOperator.Today => dateVal.Date == now.Date,
            FilterDateOperator.Yesterday => dateVal.Date == now.Date.AddDays(-1),
            FilterDateOperator.Tomorrow => dateVal.Date == now.Date.AddDays(1),
            FilterDateOperator.ThisWeek => IsInWeek(dateVal, now, 0),
            FilterDateOperator.LastWeek => IsInWeek(dateVal, now, -1),
            FilterDateOperator.NextWeek => IsInWeek(dateVal, now, 1),
            FilterDateOperator.ThisMonth => dateVal.Year == now.Year && dateVal.Month == now.Month,
            FilterDateOperator.LastMonth => IsInMonth(dateVal, now, -1),
            FilterDateOperator.NextMonth => IsInMonth(dateVal, now, 1),
            FilterDateOperator.ThisYear => dateVal.Year == now.Year,
            FilterDateOperator.LastYear => dateVal.Year == now.Year - 1,
            FilterDateOperator.NextYear => dateVal.Year == now.Year + 1,
            FilterDateOperator.YearToDate => dateVal.Year == now.Year && dateVal.Date <= now.Date,
            FilterDateOperator.IsWeekend => dateVal.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            FilterDateOperator.IsWeekday => dateVal.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday,
            FilterDateOperator.NDaysAgo => filter.NumericValue.HasValue && dateVal.Date == now.Date.AddDays(-filter.NumericValue.Value),
            FilterDateOperator.NDaysAhead => filter.NumericValue.HasValue && dateVal.Date == now.Date.AddDays(filter.NumericValue.Value),
            _ => true,
        };
    }

    private static int CompareDates(DateTimeOffset a, DateTimeOffset b, bool includeTime)
    {
        if (includeTime)
            return DateTimeOffset.Compare(a, b);
        return a.Date.CompareTo(b.Date);
    }

    private static bool IsInWeek(DateTimeOffset date, DateTimeOffset reference, int weekOffset)
    {
        var startOfWeek = reference.Date.AddDays(-(int)reference.DayOfWeek + (weekOffset * 7));
        var endOfWeek = startOfWeek.AddDays(7);
        return date.Date >= startOfWeek && date.Date < endOfWeek;
    }

    private static bool IsInMonth(DateTimeOffset date, DateTimeOffset reference, int monthOffset)
    {
        var target = reference.AddMonths(monthOffset);
        return date.Year == target.Year && date.Month == target.Month;
    }
}
