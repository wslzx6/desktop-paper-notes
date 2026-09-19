using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace DesktopPaperNotes
{
    public static class MarkdownEditing
    {
        public static string Inline(string text, int selectionStart, int selectionLength, string left, string right, out int newStart, out int newLength)
        {
            text = text ?? ""; selectionStart = Math.Max(0, Math.Min(text.Length, selectionStart)); selectionLength = Math.Max(0, Math.Min(text.Length - selectionStart, selectionLength));
            string selected = text.Substring(selectionStart, selectionLength);
            if (selected.StartsWith(left) && selected.EndsWith(right) && selected.Length >= left.Length + right.Length)
            {
                string inside = selected.Substring(left.Length, selected.Length - left.Length - right.Length);
                newStart = selectionStart; newLength = inside.Length;
                return text.Substring(0, selectionStart) + inside + text.Substring(selectionStart + selectionLength);
            }
            newStart = selectionStart + left.Length; newLength = selectionLength;
            return text.Substring(0, selectionStart) + left + selected + right + text.Substring(selectionStart + selectionLength);
        }

        public static string PrefixLines(string text, int selectionStart, int selectionLength, string prefix, out int newStart, out int newLength)
        {
            text = text ?? ""; selectionStart = Math.Max(0, Math.Min(text.Length, selectionStart)); selectionLength = Math.Max(0, Math.Min(text.Length - selectionStart, selectionLength));
            int first = 0; if (selectionStart > 0) { int previousLine = text.LastIndexOf('\n', selectionStart - 1); if (previousLine >= 0) first = previousLine + 1; }
            int selectedEnd = selectionStart + selectionLength; int probe = selectionLength == 0 ? selectedEnd : Math.Max(selectionStart, selectedEnd - 1);
            int last = text.IndexOf('\n', probe); if (last < 0) last = text.Length;
            string segment = text.Substring(first, last - first); string[] lines = segment.Split('\n'); bool remove = true;
            foreach (string raw in lines) { string line = raw.TrimEnd('\r'); if (!line.StartsWith(prefix)) { remove = false; break; } }
            StringBuilder changed = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string ending = lines[i].EndsWith("\r") ? "\r" : ""; string line = lines[i].TrimEnd('\r');
                changed.Append(remove ? line.Substring(prefix.Length) : prefix + line).Append(ending); if (i + 1 < lines.Length) changed.Append('\n');
            }
            newStart = first; newLength = changed.Length; return text.Substring(0, first) + changed + text.Substring(last);
        }
    }

    [Flags]
    public enum MarkdownTextStyle { Normal = 0, Bold = 1, Italic = 2, Strikeout = 4, Code = 8, Link = 16 }

    public sealed class MarkdownRun
    {
        public string Text;
        public MarkdownTextStyle Style;
        public MarkdownRun(string text, MarkdownTextStyle style) { Text = text; Style = style; }
    }

    public sealed class MarkdownBlock
    {
        public int Heading;
        public bool Quote, Code, Rule;
        public string Prefix = "";
        public readonly List<MarkdownRun> Runs = new List<MarkdownRun>();
    }

    public static class MarkdownRenderer
    {
        public static List<MarkdownBlock> Parse(string markdown)
        {
            List<MarkdownBlock> blocks = new List<MarkdownBlock>(); bool fenced = false;
            foreach (string original in (markdown ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = original;
                if (line.TrimStart().StartsWith("```")) { fenced = !fenced; continue; }
                MarkdownBlock block = new MarkdownBlock { Code = fenced };
                if (fenced) block.Runs.Add(new MarkdownRun(line, MarkdownTextStyle.Code));
                else
                {
                    string trimmed = line.TrimStart();
                    if (IsRule(trimmed)) block.Rule = true;
                    else
                    {
                        if (trimmed.StartsWith(">")) { block.Quote = true; trimmed = trimmed.Substring(1).TrimStart(); }
                        int hashes = 0; while (hashes < trimmed.Length && hashes < 3 && trimmed[hashes] == '#') hashes++;
                        if (hashes > 0 && hashes < trimmed.Length && Char.IsWhiteSpace(trimmed[hashes])) { block.Heading = hashes; trimmed = trimmed.Substring(hashes).TrimStart(); }
                        else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ ")) { block.Prefix = "•"; trimmed = trimmed.Substring(2); }
                        else
                        {
                            int digits = 0; while (digits < trimmed.Length && Char.IsDigit(trimmed[digits])) digits++;
                            if (digits > 0 && digits + 1 < trimmed.Length && trimmed[digits] == '.' && Char.IsWhiteSpace(trimmed[digits + 1])) { block.Prefix = trimmed.Substring(0, digits + 1); trimmed = trimmed.Substring(digits + 1).TrimStart(); }
                        }
                        if ((block.Prefix == "•" || block.Prefix.Length > 0) && trimmed.Length >= 3 && trimmed[0] == '[' && trimmed[2] == ']' && (trimmed[1] == ' ' || trimmed[1] == 'x' || trimmed[1] == 'X'))
                        { block.Prefix = trimmed[1] == ' ' ? "☐" : "☑"; trimmed = trimmed.Substring(3).TrimStart(); }
                        ParseInline(trimmed, MarkdownTextStyle.Normal, block.Runs);
                    }
                }
                blocks.Add(block);
            }
            return blocks;
        }

        private static bool IsRule(string text)
        {
            string compact = text.Replace(" ", "");
            if (compact.Length < 3) return false;
            char marker = compact[0]; if (marker != '-' && marker != '*' && marker != '_') return false;
            for (int i = 1; i < compact.Length; i++) if (compact[i] != marker) return false;
            return true;
        }

        private static void ParseInline(string text, MarkdownTextStyle inherited, List<MarkdownRun> output)
        {
            StringBuilder plain = new StringBuilder();
            Action flush = delegate { if (plain.Length > 0) { output.Add(new MarkdownRun(plain.ToString(), inherited)); plain.Length = 0; } };
            for (int i = 0; i < text.Length;)
            {
                if (text[i] == '\\' && i + 1 < text.Length) { plain.Append(text[i + 1]); i += 2; continue; }
                if (text[i] == '[')
                {
                    int close = text.IndexOf(']', i + 1); int openUrl = close < 0 ? -1 : close + 1;
                    if (close > i && openUrl < text.Length && text[openUrl] == '(')
                    {
                        int closeUrl = text.IndexOf(')', openUrl + 1);
                        if (closeUrl > openUrl) { flush(); ParseInline(text.Substring(i + 1, close - i - 1), inherited | MarkdownTextStyle.Link, output); i = closeUrl + 1; continue; }
                    }
                }
                string marker = null; MarkdownTextStyle style = MarkdownTextStyle.Normal;
                if (i + 1 < text.Length && text.Substring(i, 2) == "**") { marker = "**"; style = MarkdownTextStyle.Bold; }
                else if (i + 1 < text.Length && text.Substring(i, 2) == "~~") { marker = "~~"; style = MarkdownTextStyle.Strikeout; }
                else if (text[i] == '`') { marker = "`"; style = MarkdownTextStyle.Code; }
                else if (text[i] == '*' || text[i] == '_') { marker = text[i].ToString(); style = MarkdownTextStyle.Italic; }
                if (marker != null)
                {
                    int close = text.IndexOf(marker, i + marker.Length, StringComparison.Ordinal);
                    if (close >= i + marker.Length) { flush(); ParseInline(text.Substring(i + marker.Length, close - i - marker.Length), inherited | style, output); i = close + marker.Length; continue; }
                }
                plain.Append(text[i]); i++;
            }
            flush();
        }

        public static string ToPlainText(string markdown)
        {
            StringBuilder text = new StringBuilder();
            foreach (MarkdownBlock block in Parse(markdown))
            {
                if (text.Length > 0) text.Append(' ');
                if (block.Prefix.Length > 0) text.Append(block.Prefix).Append(' ');
                foreach (MarkdownRun run in block.Runs) text.Append(run.Text);
            }
            return text.ToString().Trim();
        }

        public static void Draw(Graphics graphics, string markdown, RectangleF bounds, float baseSize, Color ink)
        {
            GraphicsStateScope state = new GraphicsStateScope(graphics); graphics.SetClip(bounds);
            try
            {
                float y = bounds.Top; List<MarkdownBlock> blocks = Parse(markdown);
                using (FontCache fonts = new FontCache())
                using (Brush normalBrush = new SolidBrush(ink))
                using (Brush linkBrush = new SolidBrush(Color.FromArgb(42, 94, 137)))
                using (Brush codeBrush = new SolidBrush(Color.FromArgb(28, 47, 51, 57)))
                using (Pen quotePen = new Pen(Color.FromArgb(115, Palette.Accent), 3))
                using (Pen rulePen = new Pen(Color.FromArgb(55, Palette.Ink), 1))
                {
                    foreach (MarkdownBlock block in blocks)
                    {
                        if (y >= bounds.Bottom) break;
                        if (block.Rule) { y += 6; graphics.DrawLine(rulePen, bounds.Left, y, bounds.Right, y); y += 8; continue; }
                        float size = block.Heading == 1 ? baseSize * 1.5f : block.Heading == 2 ? baseSize * 1.3f : block.Heading == 3 ? baseSize * 1.15f : baseSize;
                        MarkdownTextStyle baseStyle = block.Heading > 0 ? MarkdownTextStyle.Bold : MarkdownTextStyle.Normal;
                        float x = bounds.Left, right = bounds.Right;
                        if (block.Quote) { graphics.DrawLine(quotePen, x + 2, y, x + 2, y + fonts.Get(size, baseStyle).GetHeight(graphics) + 3); x += 12; }
                        if (block.Prefix.Length > 0)
                        {
                            Font prefixFont = fonts.Get(size, MarkdownTextStyle.Bold); graphics.DrawString(block.Prefix, prefixFont, normalBrush, x, y, FontCache.Format);
                            x += Math.Max(18, graphics.MeasureString(block.Prefix, prefixFont, PointF.Empty, FontCache.Format).Width + 7);
                        }
                        float startX = x, lineHeight = fonts.Get(size, baseStyle).GetHeight(graphics) + 3;
                        if (block.Runs.Count == 0) { y += lineHeight * .65f; continue; }
                        foreach (MarkdownRun run in block.Runs)
                        {
                            MarkdownTextStyle runStyle = run.Style | baseStyle; Font font = fonts.Get(size, runStyle);
                            TextElementEnumerator characters = StringInfo.GetTextElementEnumerator(run.Text);
                            while (characters.MoveNext())
                            {
                                string character = characters.GetTextElement(); SizeF measured = graphics.MeasureString(character, font, PointF.Empty, FontCache.Format);
                                if (x + measured.Width > right && x > startX) { x = startX; y += lineHeight; if (y >= bounds.Bottom) break; }
                                if ((runStyle & MarkdownTextStyle.Code) != 0) graphics.FillRectangle(codeBrush, x - 1, y, measured.Width + 2, lineHeight);
                                graphics.DrawString(character, font, (runStyle & MarkdownTextStyle.Link) != 0 ? linkBrush : normalBrush, x, y, FontCache.Format);
                                x += measured.Width;
                            }
                            if (y >= bounds.Bottom) break;
                        }
                        y += lineHeight + (block.Heading > 0 ? 3 : 1);
                    }
                }
            }
            finally { state.Dispose(); }
        }

        private sealed class FontCache : IDisposable
        {
            private readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();
            public static readonly StringFormat Format = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap };
            public Font Get(float size, MarkdownTextStyle style)
            {
                FontStyle fontStyle = FontStyle.Regular;
                if ((style & MarkdownTextStyle.Bold) != 0) fontStyle |= FontStyle.Bold;
                if ((style & MarkdownTextStyle.Italic) != 0) fontStyle |= FontStyle.Italic;
                if ((style & MarkdownTextStyle.Strikeout) != 0) fontStyle |= FontStyle.Strikeout;
                string family = (style & MarkdownTextStyle.Code) != 0 ? "Consolas" : "Microsoft YaHei UI";
                string key = family + "|" + size.ToString(CultureInfo.InvariantCulture) + "|" + (int)fontStyle;
                Font font; if (!fonts.TryGetValue(key, out font)) fonts[key] = font = new Font(family, size, fontStyle);
                return font;
            }
            public void Dispose() { foreach (Font font in fonts.Values) font.Dispose(); }
        }

        private sealed class GraphicsStateScope : IDisposable
        {
            private readonly Graphics graphics; private readonly System.Drawing.Drawing2D.GraphicsState state;
            public GraphicsStateScope(Graphics value) { graphics = value; state = value.Save(); }
            public void Dispose() { graphics.Restore(state); }
        }
    }
}
