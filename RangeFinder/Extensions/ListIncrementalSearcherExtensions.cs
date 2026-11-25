using RangeFinder.Domain.Utilities;

namespace RangeFinder.Extensions;

internal static class ListIncrementalSearcherExtensions
{
    extension(ListIncrementalSearcher listIncrementalSearcher)
    {
        public void SearchInGrid(char c, DataGridView gridView, int colIndex)
        {
            var currentSearch = listIncrementalSearcher.AddChar(c);
            var hasAccum = currentSearch.Length > 1;
            var numRows = gridView.RowCount;

            var activeRowIndexPreSearch = gridView.CurrentCell?.RowIndex ?? 0;

            for (var passNum = 0; passNum < 2; passNum++)
            {
                // First look for the accumulated string.
                // If we can't find it, reset the accumulated string and go around again.
                var fromIndex = hasAccum ? activeRowIndexPreSearch : activeRowIndexPreSearch + 1;
                var toIndex = hasAccum ? activeRowIndexPreSearch - 1 : activeRowIndexPreSearch;
                fromIndex = ModuloClamp(fromIndex, 0, numRows - 1);
                toIndex = ModuloClamp(toIndex, 0, numRows - 1);

                var currIndex = fromIndex;
                while (currIndex != toIndex)
                {
                    var currRow = gridView.Rows[currIndex];
                    var checkVal = currRow.Cells[colIndex].Value;
                    if (checkVal is string colText
                        && colText.StartsWith(currentSearch, StringComparison.CurrentCultureIgnoreCase))
                    {
                        gridView.ClearSelection();
                        currRow.Selected = true;
                        gridView.CurrentCell = currRow.Cells[0];
                        ScrollToRow(gridView, currIndex);
                        return;
                    }

                    currIndex = ModuloClamp(currIndex + 1, 0, numRows - 1);
                }

                currentSearch = listIncrementalSearcher.ResetToChar(c);
                hasAccum = false;
            }
        }
    }

    private static int ModuloClamp(int value, int min, int max)
    {
        if (value == min - 1)
            return max;
        if (value == max + 1)
            return min;
        return value;
    }

    private static void ScrollToRow(DataGridView dgv, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.RowCount)
            return;

        // How many rows are currently visible (including header)
        int visibleCount = dgv.DisplayedRowCount(false);

        // First visible row index (0-based, row 0 is header)
        int firstVisible = dgv.FirstDisplayedScrollingRowIndex;

        // Check if the row is outside the visible range
        if (rowIndex < firstVisible || rowIndex >= firstVisible + visibleCount)
        {
            // Scroll so the row is visible
            if (rowIndex < firstVisible)
            {
                dgv.FirstDisplayedScrollingRowIndex = rowIndex;
            }
            else
            {
                dgv.FirstDisplayedScrollingRowIndex = rowIndex - visibleCount + 1;
            }
        }
    }
}