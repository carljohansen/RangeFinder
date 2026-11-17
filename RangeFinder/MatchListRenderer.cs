using RangeFinder.Controls;
using RangeFinder.Domain.TextSearch;

namespace RangeFinder;

internal class MatchListRenderer
{
    private Font? _highlightFont;

    private readonly Brush[] _brushes = [.. new[] { Color.Black, Color.DarkGray, Color.Orange, Color.FromArgb(unchecked((int)0xFFE0B0FF)), Color.CornflowerBlue, Color.DarkKhaki, Color.Green }.Select(c => new SolidBrush(c))];

    public readonly static MatchListRenderer Instance = new();

    private MatchListRenderer()
    {
    }

    private Font GetHighlightFont(ListView listView)
    {
        Font font = listView.Font;
        _highlightFont ??= new Font(font.FontFamily, font.Size, FontStyle.Bold, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
        return _highlightFont;
    }

    public void DrawMatchListViewSubItem(IFileLineDisplay? itemUnderlyingMatches, ListView listView, DrawListViewSubItemEventArgs e)
    {
        if (e.ItemIndex < 0)
            return;

        if (itemUnderlyingMatches == null)
        {
            return;
        }

        Font font = listView.Font;
        var highlightFont = GetHighlightFont(listView);

        using Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (e.ColumnIndex == 0)
        {
            if (itemUnderlyingMatches.LineNumber > 0)
            {
                // Line Number:
                string lineNumberText = itemUnderlyingMatches.LineNumber.ToString();
                Brush brush = _brushes[0];
                g.DrawString(lineNumberText, highlightFont, brush, e.Bounds.Left + 5, e.Bounds.Top + 3, StringFormat.GenericTypographic);
            }
        }
        else
        {
            var textSegments = itemUnderlyingMatches.GetSegments();
            float xPos = e.Bounds.Left;

            // Text:
            foreach (var segment in textSegments)
            {
                string segmentText = segment.SegmentText;
                Brush backgroundBrush = _brushes[Math.Min(segment.SegmentColourIndex, _brushes.Length - 1)];
                Brush textBrush = _brushes[segment.SegmentColourIndex == 1 ? 1 : 0];

                var segmentFont = font;
                float segmentWidth = g.MeasureString(segmentText, segmentFont, 0, MeasuringHelper.FormatForMeasuring).Width;
                if (segment.SegmentColourIndex > 1)
                {
                    g.FillRoundedRectangle(backgroundBrush, new Rectangle((int)xPos, e.Bounds.Top, (int)segmentWidth, e.Bounds.Height), new Size(9, 9));
                }

                g.DrawString(segmentText, segmentFont, textBrush, xPos, e.Bounds.Top + 3, StringFormat.GenericTypographic);

                xPos += segmentWidth;
            }
        }
    }
}