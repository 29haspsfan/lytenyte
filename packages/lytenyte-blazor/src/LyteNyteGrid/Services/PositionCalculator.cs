using LyteNyteGrid.Enums;
using LyteNyteGrid.Models;

namespace LyteNyteGrid.Services;

/// <summary>
/// Computes column x-positions and row y-positions for CSS Grid layout.
/// Equivalent to the React computeColumnPositions and computeRowPositions functions.
/// </summary>
public static class PositionCalculator
{
    /// <summary>
    /// Computes the cumulative x-positions for each visible column.
    /// Returns an array where xPositions[i] is the start pixel of column i.
    /// </summary>
    public static int[] ComputeColumnPositions<T>(ColumnView<T> view, bool sizeToFit, int viewportWidth)
    {
        var columns = view.VisibleColumns;
        if (columns.Count == 0)
            return [];

        var widths = new int[columns.Count];
        for (int i = 0; i < columns.Count; i++)
            widths[i] = columns[i].Width;

        // Size-to-fit: distribute remaining width proportionally to center columns
        if (sizeToFit && viewportWidth > 0)
        {
            int pinnedWidth = 0;
            int centerWidth = 0;
            var centerIndices = new List<int>();

            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Pin != ColumnPin.None)
                    pinnedWidth += widths[i];
                else
                {
                    centerWidth += widths[i];
                    centerIndices.Add(i);
                }
            }

            int availableWidth = viewportWidth - pinnedWidth;
            if (availableWidth > centerWidth && centerIndices.Count > 0)
            {
                double ratio = (double)availableWidth / centerWidth;
                foreach (var idx in centerIndices)
                {
                    widths[idx] = (int)(widths[idx] * ratio);
                }
            }
        }

        var positions = new int[columns.Count + 1];
        positions[0] = 0;
        for (int i = 0; i < columns.Count; i++)
        {
            positions[i + 1] = positions[i] + widths[i];
        }

        return positions;
    }

    /// <summary>
    /// Computes the cumulative y-positions for each visible row.
    /// Returns an array where yPositions[i] is the start pixel of row i.
    /// </summary>
    public static int[] ComputeRowPositions(
        int rowCount,
        int defaultRowHeight,
        Func<int, int>? rowHeightFn = null,
        Dictionary<string, int>? detailHeights = null,
        Func<int, string?>? rowIdForIndex = null)
    {
        if (rowCount == 0)
            return [0];

        var positions = new int[rowCount + 1];
        positions[0] = 0;

        for (int i = 0; i < rowCount; i++)
        {
            int rowHeight = rowHeightFn?.Invoke(i) ?? defaultRowHeight;

            // Add detail row height if expanded
            if (detailHeights is not null && rowIdForIndex is not null)
            {
                var rowId = rowIdForIndex(i);
                if (rowId is not null && detailHeights.TryGetValue(rowId, out var detailHeight))
                    rowHeight += detailHeight;
            }

            positions[i + 1] = positions[i] + rowHeight;
        }

        return positions;
    }

    /// <summary>
    /// Computes the virtualization bounds: which rows and columns are visible in the viewport.
    /// </summary>
    public static ViewBounds ComputeBounds(
        int[] xPositions,
        int[] yPositions,
        int scrollLeft,
        int scrollTop,
        int viewportWidth,
        int viewportHeight,
        int headerHeight,
        int startPinnedCount,
        int endPinnedCount,
        int topRowCount,
        int bottomRowCount,
        int rowOverscanTop = 2,
        int rowOverscanBottom = 2,
        int colOverscanStart = 1,
        int colOverscanEnd = 1)
    {
        int colCount = xPositions.Length - 1;
        int rowCount = yPositions.Length - 1;

        // Find visible column range (for center unpinned columns only)
        int colStart = BinarySearchPosition(xPositions, scrollLeft, startPinnedCount, colCount - endPinnedCount);
        int colEnd = BinarySearchPosition(xPositions, scrollLeft + viewportWidth, startPinnedCount, colCount - endPinnedCount);

        colStart = Math.Max(startPinnedCount, colStart - colOverscanStart);
        colEnd = Math.Min(colCount - endPinnedCount, colEnd + colOverscanEnd);

        // Find visible row range (for center rows only)
        int rowStart = BinarySearchPosition(yPositions, scrollTop, topRowCount, rowCount - bottomRowCount);
        int rowEnd = BinarySearchPosition(yPositions, scrollTop + viewportHeight - headerHeight, topRowCount, rowCount - bottomRowCount);

        rowStart = Math.Max(topRowCount, rowStart - rowOverscanTop);
        rowEnd = Math.Min(rowCount - bottomRowCount, rowEnd + rowOverscanBottom);

        return new ViewBounds
        {
            ColStart = colStart,
            ColEnd = colEnd,
            RowStart = rowStart,
            RowEnd = rowEnd,
        };
    }

    private static int BinarySearchPosition(int[] positions, int target, int lo, int hi)
    {
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (positions[mid] < target)
                lo = mid + 1;
            else
                hi = mid;
        }
        return Math.Max(0, lo - 1);
    }
}

public struct ViewBounds
{
    public int ColStart;
    public int ColEnd;
    public int RowStart;
    public int RowEnd;
}
