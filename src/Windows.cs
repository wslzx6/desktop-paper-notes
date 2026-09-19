using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DesktopPaperNotes
{
    public sealed class ModernButton : Button
    {
        private bool hover, pressed;
        public bool Primary;
        public ModernButton() { FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; UseVisualStyleBackColor = false; Cursor = Cursors.Arrow; SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Palette.Surface : Parent.BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Color fill = Enabled ? BackColor : Color.FromArgb(238, 240, 237);
            if (hover && Enabled) fill = Primary ? Palette.AccentHover : Blend(fill, Palette.AccentSoft, pressed ? 55 : 32);
            using (var path = NoteRenderer.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), 6))
            using (Brush brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
            if (!Primary)
                using (var path = NoteRenderer.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), 6))
                using (Pen border = new Pen(hover ? Color.FromArgb(130, Palette.Accent) : Palette.Border)) e.Graphics.DrawPath(border, path);
            if (Focused && ShowFocusCues)
                using (var path = NoteRenderer.Rounded(new RectangleF(3, 3, Width - 7, Height - 7), 5))
                using (Pen focus = new Pen(Primary ? Color.White : Palette.Accent) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot }) e.Graphics.DrawPath(focus, path);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? ForeColor : Palette.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
        private static Color Blend(Color a, Color b, int percent)
        { return Color.FromArgb((a.R * (100 - percent) + b.R * percent) / 100, (a.G * (100 - percent) + b.G * percent) / 100, (a.B * (100 - percent) + b.B * percent) / 100); }
    }

    public sealed class SurfacePanel : Panel
    {
        public int Radius = 9;
        public Color FillColor = Palette.SoftSurface;
        public Color LineColor = Palette.Border;
        public SurfacePanel() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Palette.Canvas : Parent.BackColor); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = NoteRenderer.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), Radius))
            using (Brush brush = new SolidBrush(FillColor)) e.Graphics.FillPath(brush, path);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = NoteRenderer.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), Radius))
            using (Pen pen = new Pen(LineColor)) e.Graphics.DrawPath(pen, path);
        }
    }

    public sealed class EdgePanel : Panel
    {
        public bool BottomLine, RightLine, TopLine;
        public EdgePanel() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(Palette.Border))
            {
                if (BottomLine) e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
                if (TopLine) e.Graphics.DrawLine(pen, 0, 0, Width, 0);
                if (RightLine) e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height);
            }
        }
    }

    public sealed class AppMark : Control
    {
        public AppMark() { Size = new Size(38, 38); SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Palette.Surface : Parent.BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = NoteRenderer.Rounded(new RectangleF(1, 1, 35, 35), 9))
            using (Brush brush = new SolidBrush(Palette.Accent)) e.Graphics.FillPath(brush, path);
            using (var paper = NoteRenderer.Rounded(new RectangleF(10, 8, 18, 21), 3))
            using (Brush brush = new SolidBrush(Color.FromArgb(255, 241, 178))) e.Graphics.FillPath(brush, paper);
            using (Pen pen = new Pen(Color.FromArgb(110, Palette.Accent), 1))
            {
                e.Graphics.DrawLine(pen, 14, 14, 24, 14);
                e.Graphics.DrawLine(pen, 14, 18, 24, 18);
                e.Graphics.DrawLine(pen, 14, 22, 21, 22);
            }
        }
    }

    public sealed class SwatchButton : Control
    {
        private bool hover;
        public Color SwatchColor;
        public bool Selected;
        public SwatchButton(Color color)
        {
            SwatchColor = color; Size = new Size(30, 30); Margin = new Padding(0, 0, 7, 0); Cursor = Cursors.Arrow; TabStop = true; AccessibleRole = AccessibleRole.PushButton;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.Selectable, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { OnClick(EventArgs.Empty); e.Handled = true; } base.OnKeyDown(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Palette.Surface : Parent.BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle ring = new Rectangle(2, 2, Width - 5, Height - 5);
            if (Selected || hover || Focused) using (Pen pen = new Pen(Selected ? Palette.Accent : Palette.Border, Selected ? 2 : 1)) e.Graphics.DrawEllipse(pen, ring);
            Rectangle dot = new Rectangle(Selected ? 7 : 6, Selected ? 7 : 6, Width - (Selected ? 15 : 13), Height - (Selected ? 15 : 13));
            using (Brush brush = new SolidBrush(SwatchColor)) e.Graphics.FillEllipse(brush, dot);
            using (Pen border = new Pen(Color.FromArgb(30, Palette.Ink))) e.Graphics.DrawEllipse(border, dot);
        }
    }

    public sealed class ModernSlider : Control
    {
        private int value;
        private bool dragging;
        public int Minimum = 0, Maximum = 100, SmallChange = 1, LargeChange = 10;
        public event EventHandler ValueChanged;
        public int Value
        {
            get { return value; }
            set { int next = Math.Max(Minimum, Math.Min(Maximum, value)); if (this.value == next) return; this.value = next; Invalidate(); if (ValueChanged != null) ValueChanged(this, EventArgs.Empty); }
        }
        public ModernSlider()
        {
            Height = 32; TabStop = true; Cursor = Cursors.Arrow; AccessibleRole = AccessibleRole.Slider;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.Selectable, true);
        }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragging = true; Capture = true; SetFromX(e.X); Focus(); } base.OnMouseDown(e); }
        protected override void OnMouseMove(MouseEventArgs e) { if (dragging) SetFromX(e.X); base.OnMouseMove(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dragging = false; Capture = false; base.OnMouseUp(e); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) { Value -= SmallChange; e.Handled = true; }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) { Value += SmallChange; e.Handled = true; }
            else if (e.KeyCode == Keys.PageDown) { Value -= LargeChange; e.Handled = true; }
            else if (e.KeyCode == Keys.PageUp) { Value += LargeChange; e.Handled = true; }
            base.OnKeyDown(e);
        }
        private void SetFromX(int x) { int usable = Math.Max(1, Width - 20); Value = Minimum + (int)Math.Round((Maximum - Minimum) * Math.Max(0, Math.Min(usable, x - 10)) / (double)usable); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Palette.Surface : Parent.BackColor); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int y = Height / 2, left = 10, right = Width - 10; float ratio = Maximum == Minimum ? 0 : (Value - Minimum) / (float)(Maximum - Minimum); int x = left + (int)((right - left) * ratio);
            using (Pen p = new Pen(Palette.Border, 4) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round }) e.Graphics.DrawLine(p, left, y, right, y);
            using (Pen p = new Pen(Palette.Accent, 4) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round }) e.Graphics.DrawLine(p, left, y, x, y);
            using (Brush shadow = new SolidBrush(Color.FromArgb(28, Palette.Ink))) e.Graphics.FillEllipse(shadow, x - 8, y - 7, 17, 17);
            using (Brush thumb = new SolidBrush(Color.White)) e.Graphics.FillEllipse(thumb, x - 8, y - 9, 16, 16);
            using (Pen p = new Pen(Palette.Accent, 2)) e.Graphics.DrawEllipse(p, x - 8, y - 9, 16, 16);
            if (Focused && ShowFocusCues) using (Pen p = new Pen(Palette.Accent) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot }) e.Graphics.DrawRectangle(p, 1, 1, Width - 3, Height - 3);
        }
    }

    public sealed class NumberStepper : Control
    {
        private int value = 12, hoverPart;
        public int Minimum = 9, Maximum = 28;
        public event EventHandler ValueChanged;
        public int Value
        {
            get { return value; }
            set { int next = Math.Max(Minimum, Math.Min(Maximum, value)); if (this.value == next) return; this.value = next; Invalidate(); if (ValueChanged != null) ValueChanged(this, EventArgs.Empty); }
        }
        public NumberStepper() { Size = new Size(106, 32); TabStop = true; Cursor = Cursors.Arrow; AccessibleRole = AccessibleRole.SpinButton; SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.Selectable, true); }
        private int Part(int x) { return x < 32 ? -1 : (x >= Width - 32 ? 1 : 0); }
        protected override void OnMouseMove(MouseEventArgs e) { int next = Part(e.X); if (next != hoverPart) { hoverPart = next; Invalidate(); } base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e) { hoverPart = 0; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { Value += Part(e.X); Focus(); } base.OnMouseDown(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) { Value--; e.Handled = true; } else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) { Value++; e.Handled = true; } base.OnKeyDown(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Palette.Surface : Parent.BackColor); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = NoteRenderer.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), 6)) using (Brush b = new SolidBrush(Palette.SoftSurface)) e.Graphics.FillPath(b, path);
            if (hoverPart != 0) { Rectangle r = hoverPart < 0 ? new Rectangle(2, 2, 30, Height - 4) : new Rectangle(Width - 32, 2, 30, Height - 4); using (Brush b = new SolidBrush(Palette.AccentSoft)) e.Graphics.FillRectangle(b, r); }
            using (var path = NoteRenderer.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), 6)) using (Pen p = new Pen(Focused ? Palette.Accent : Palette.Border)) e.Graphics.DrawPath(p, path);
            using (Pen p = new Pen(Palette.Border)) { e.Graphics.DrawLine(p, 33, 7, 33, Height - 8); e.Graphics.DrawLine(p, Width - 34, 7, Width - 34, Height - 8); }
            using (Font symbol = new Font("Segoe UI", 11, FontStyle.Regular))
            {
                TextRenderer.DrawText(e.Graphics, "−", symbol, new Rectangle(1, 1, 32, Height - 2), Palette.Ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(e.Graphics, "+", symbol, new Rectangle(Width - 33, 1, 32, Height - 2), Palette.Ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            TextRenderer.DrawText(e.Graphics, Value.ToString(), Ui.Font(9, false), new Rectangle(34, 1, Width - 68, Height - 2), Palette.Ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public sealed class ModernToggle : CheckBox
    {
        public ModernToggle() { AutoSize = false; Size = new Size(270, 30); Cursor = Cursors.Arrow; SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Palette.Surface : Parent.BackColor); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            RectangleF track = new RectangleF(1, 5, 36, 20);
            using (var path = NoteRenderer.Rounded(track, 10)) using (Brush b = new SolidBrush(Checked ? Palette.Accent : Palette.Border)) e.Graphics.FillPath(b, path);
            int x = Checked ? 19 : 3; using (Brush b = new SolidBrush(Color.White)) e.Graphics.FillEllipse(b, x, 7, 16, 16);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(48, 0, Width - 48, Height), Enabled ? ForeColor : Palette.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    public static class Ui
    {
        private static float? dpi;
        public static float Dpi { get { if (!dpi.HasValue) using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) dpi = g.DpiX; return dpi.Value; } }
        public static Font Font(float size, bool bold) { return new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular); }
        public static Button Button(string text, int width, bool primary)
        {
            Button b = new ModernButton { Text = text, Width = width, Height = 38, Primary = primary, Cursor = Cursors.Arrow, Font = Font(9, false), BackColor = primary ? Palette.Accent : Palette.Surface, ForeColor = primary ? Color.White : Palette.Ink, Margin = new Padding(0, 0, 10, 0) };
            return b;
        }
        public static Label Label(string text, int x, int y, int width, float size, bool bold)
        { return new Label { Text = text, Location = new Point(x, y), Size = new Size(width, 28), Font = Font(size, bold), ForeColor = Palette.Ink, BackColor = Color.Transparent, AutoEllipsis = true }; }
    }

    public sealed class NoteCard : Control
    {
        public Note Note;
        public bool Selected;
        public NoteCard(Note note)
        { Note = note; Height = (int)(92 * Ui.Dpi / 96); Width = (int)(228 * Ui.Dpi / 96); Margin = new Padding(0, 0, 0, (int)(8 * Ui.Dpi / 96)); Cursor = Cursors.Arrow; SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float scale = Ui.Dpi / 96;
            using (var path = NoteRenderer.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), 7))
            {
                using (Brush b = new SolidBrush(Selected ? Palette.AccentSoft : Palette.Surface)) e.Graphics.FillPath(b, path);
                using (Pen p = new Pen(Selected ? Palette.Accent : Palette.Border, Selected ? 1.5f : 1)) e.Graphics.DrawPath(p, path);
            }
            using (Brush b = new SolidBrush(Palette.Colors[Note.ColorIndex])) e.Graphics.FillEllipse(b, (int)(14 * scale), (int)(15 * scale), (int)(11 * scale), (int)(11 * scale));
            using (Font f = Ui.Font(10, true)) TextRenderer.DrawText(e.Graphics, Note.Title.Length == 0 ? "便签" : Note.Title, f, new Rectangle((int)(34*scale), (int)(10*scale), Width - (int)(49*scale), (int)(24*scale)), Palette.Ink, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            using (Font f = Ui.Font(9, false)) TextRenderer.DrawText(e.Graphics, Note.Text.Length == 0 ? "还没有内容" : MarkdownRenderer.ToPlainText(Note.Text), f, new Rectangle((int)(14*scale), (int)(39*scale), Width - (int)(28*scale), (int)(22*scale)), Palette.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            using (Font f = Ui.Font(8, false)) TextRenderer.DrawText(e.Graphics, Note.Width + " × " + Note.Height + "  ·  " + Note.OpacityPercent + "%", f, new Rectangle((int)(14*scale), (int)(66*scale), Width - (int)(28*scale), (int)(17*scale)), Palette.Muted, TextFormatFlags.NoPrefix);
        }
    }

    public static class MarkdownIcons
    {
        public static Bitmap Create(string name)
        {
            int size = Math.Max(18, (int)Math.Round(18 * Ui.Dpi / 96)); Bitmap image = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                float s = size / 24f; using (Pen pen = new Pen(Palette.Ink, Math.Max(1.4f, 1.8f * s)) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
                using (Brush brush = new SolidBrush(Palette.Ink))
                {
                    if (name == "heading") DrawText(g, "H", 14 * s, FontStyle.Bold, new RectangleF(0, 1*s, 17*s, 21*s));
                    else if (name == "bold") DrawText(g, "B", 17 * s, FontStyle.Bold, new RectangleF(1*s, 0, 22*s, 24*s));
                    else if (name == "italic") DrawText(g, "I", 17 * s, FontStyle.Italic, new RectangleF(1*s, 0, 22*s, 24*s));
                    else if (name == "strike") { DrawText(g, "S", 16 * s, FontStyle.Regular, new RectangleF(1*s, 0, 22*s, 24*s)); g.DrawLine(pen, 4*s, 12*s, 20*s, 12*s); }
                    else if (name == "code") { g.DrawLines(pen, new[] { new PointF(9*s, 5*s), new PointF(3*s, 12*s), new PointF(9*s, 19*s) }); g.DrawLines(pen, new[] { new PointF(15*s, 5*s), new PointF(21*s, 12*s), new PointF(15*s, 19*s) }); g.DrawLine(pen, 14*s, 3*s, 10*s, 21*s); }
                    else if (name == "rule") g.DrawLine(pen, 3*s, 12*s, 21*s, 12*s);
                    else if (name == "quote") { g.FillEllipse(brush, 5*s, 7*s, 5*s, 5*s); g.FillEllipse(brush, 14*s, 7*s, 5*s, 5*s); g.DrawArc(pen, 4*s, 8*s, 8*s, 10*s, 5, 100); g.DrawArc(pen, 13*s, 8*s, 8*s, 10*s, 5, 100); }
                    else if (name == "bullet") { for (int y = 6; y <= 18; y += 6) { g.FillEllipse(brush, 3*s, (y-1)*s, 3*s, 3*s); g.DrawLine(pen, 9*s, y*s, 21*s, y*s); } }
                    else if (name == "numbered") { DrawTiny(g, "1", 1*s, 3*s, s); DrawTiny(g, "2", 1*s, 9*s, s); DrawTiny(g, "3", 1*s, 15*s, s); g.DrawLine(pen, 9*s, 6*s, 21*s, 6*s); g.DrawLine(pen, 9*s, 12*s, 21*s, 12*s); g.DrawLine(pen, 9*s, 18*s, 21*s, 18*s); }
                    else if (name == "task") { g.DrawRectangle(pen, 3*s, 5*s, 7*s, 7*s); g.DrawLines(pen, new[] { new PointF(4*s, 8*s), new PointF(6*s, 10*s), new PointF(10*s, 5*s) }); g.DrawLine(pen, 13*s, 8*s, 21*s, 8*s); g.DrawRectangle(pen, 3*s, 15*s, 7*s, 7*s); g.DrawLine(pen, 13*s, 18*s, 21*s, 18*s); }
                    else if (name == "link") { g.DrawArc(pen, 2*s, 7*s, 12*s, 10*s, 45, 270); g.DrawArc(pen, 10*s, 7*s, 12*s, 10*s, 225, 270); g.DrawLine(pen, 8*s, 12*s, 16*s, 12*s); }
                    if (name == "heading") DrawText(g, "1", 8 * s, FontStyle.Bold, new RectangleF(14*s, 10*s, 9*s, 11*s));
                }
            }
            return image;
        }
        private static void DrawText(Graphics g, string text, float size, FontStyle style, RectangleF bounds)
        {
            using (Font font = new Font("Segoe UI", size, style, GraphicsUnit.Pixel)) using (Brush brush = new SolidBrush(Palette.Ink)) using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }) g.DrawString(text, font, brush, bounds, format);
        }
        private static void DrawTiny(Graphics g, string text, float x, float y, float scale)
        { using (Font font = new Font("Segoe UI", 6 * scale, FontStyle.Bold, GraphicsUnit.Pixel)) using (Brush brush = new SolidBrush(Palette.Ink)) g.DrawString(text, font, brush, x, y); }
    }

    public sealed class MarkdownToolbar : ToolStrip
    {
        private readonly TextBox editor;
        public MarkdownToolbar(TextBox target)
        {
            editor = target; Name = "markdownToolbar"; AccessibleName = "Markdown 格式工具栏"; GripStyle = ToolStripGripStyle.Hidden; AutoSize = false; Height = 42;
            BackColor = Palette.SoftSurface; ForeColor = Palette.Ink; Padding = new Padding(7, 4, 7, 4); Renderer = new ModernToolStripRenderer(); CanOverflow = false; ShowItemToolTips = true;
            Add("heading", "一级标题", delegate { Prefix("# "); });
            Add("bold", "粗体", delegate { Inline("**", "**"); });
            Add("italic", "斜体", delegate { Inline("*", "*"); });
            Add("strike", "删除线", delegate { Inline("~~", "~~"); });
            Items.Add(new ToolStripSeparator());
            Add("code", "行内代码", delegate { Inline("`", "`"); });
            Add("rule", "分隔线", InsertRule);
            Add("quote", "引用", delegate { Prefix("> "); });
            Items.Add(new ToolStripSeparator());
            Add("bullet", "项目符号列表", delegate { Prefix("- "); });
            Add("numbered", "数字列表", delegate { Prefix("1. "); });
            Add("task", "任务列表", delegate { Prefix("- [ ] "); });
            Add("link", "链接", InsertLink);
        }
        private ToolStripButton Add(string name, string tip, EventHandler click)
        {
            ToolStripButton button = new ToolStripButton { Name = "markdown_" + name, AccessibleName = tip, ToolTipText = tip, AutoSize = false, Size = new Size(38, 32), Margin = new Padding(1, 0, 1, 0), DisplayStyle = ToolStripItemDisplayStyle.Image, Image = MarkdownIcons.Create(name), ImageScaling = ToolStripItemImageScaling.None };
            button.Click += click; Items.Add(button); return button;
        }
        private void Apply(string text, int start, int length)
        { editor.Text = text; editor.Focus(); editor.SelectionStart = start; editor.SelectionLength = length; }
        private void Inline(string left, string right)
        { int start, length; string text = MarkdownEditing.Inline(editor.Text, editor.SelectionStart, editor.SelectionLength, left, right, out start, out length); Apply(text, start, length); }
        private void Prefix(string prefix)
        { int start, length; string text = MarkdownEditing.PrefixLines(editor.Text, editor.SelectionStart, editor.SelectionLength, prefix, out start, out length); Apply(text, start, length); }
        private void InsertRule(object sender, EventArgs e)
        {
            int position = editor.SelectionStart, lineStart = 0; if (position > 0) { int previousLine = editor.Text.LastIndexOf('\n', position - 1); if (previousLine >= 0) lineStart = previousLine + 1; }
            string before = position == lineStart ? "" : Environment.NewLine; string insertion = before + "---" + Environment.NewLine;
            editor.Text = editor.Text.Substring(0, position) + insertion + editor.Text.Substring(position + editor.SelectionLength); editor.Focus(); editor.SelectionStart = position + insertion.Length; editor.SelectionLength = 0;
        }
        private void InsertLink(object sender, EventArgs e)
        {
            string selected = editor.SelectedText; int start = editor.SelectionStart;
            string label = selected.Length == 0 ? "链接文字" : selected; string insertion = "[" + label + "](https://)";
            editor.Text = editor.Text.Substring(0, start) + insertion + editor.Text.Substring(start + editor.SelectionLength); editor.Focus();
            editor.SelectionStart = start + label.Length + 3; editor.SelectionLength = 8;
        }
    }

    public sealed class ModernToolStripRenderer : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { e.Graphics.Clear(Palette.SoftSurface); }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripButton button = e.Item as ToolStripButton; if (button == null) return;
            Color fill = button.Pressed ? Color.FromArgb(210, 228, 217) : button.Selected ? Palette.AccentSoft : Color.Transparent;
            if (fill.A == 0) return; e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = NoteRenderer.Rounded(new RectangleF(2, 2, button.Width - 5, button.Height - 5), 6)) using (Brush brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
        }
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        { if (e.Image == null) return; int x = (e.Item.Width - e.Image.Width) / 2, y = (e.Item.Height - e.Image.Height) / 2; e.Graphics.DrawImageUnscaled(e.Image, x, y); }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        { using (Pen pen = new Pen(Palette.Border)) e.Graphics.DrawLine(pen, e.Item.Width / 2, 7, e.Item.Width / 2, e.Item.Height - 8); }
    }

    public sealed class ManagerWindow : Form
    {
        private readonly NotesApp app;
        private readonly FlowLayoutPanel list;
        private readonly TextBox title, body;
        private readonly Panel detail;
        private readonly Label state, count, detailTitle;
        private readonly NumberStepper fontSize;
        private readonly ModernSlider transparency;
        private readonly Label transparencyValue;
        private readonly CheckBox autoStart;
        private readonly List<SwatchButton> colorSwatches = new List<SwatchButton>();
        private bool refreshing;
        public bool AllowClose;

        public ManagerWindow(NotesApp owner)
        {
            SuspendLayout(); app = owner; Text = "桌面便签"; Icon = AppIcon.Create(); BackColor = Palette.Canvas;
            Font = Ui.Font(9, false); AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(920, 680); MinimumSize = new Size(860, 640);

            EdgePanel header = new EdgePanel { Size = new Size(920, 78), Dock = DockStyle.Top, BackColor = Palette.Surface, BottomLine = true };
            AppMark mark = new AppMark { Location = new Point(24, 19) }; header.Controls.Add(mark);
            Label heading = Ui.Label("桌面便签", 76, 11, 250, 14, true); heading.Height = 30; header.Controls.Add(heading);
            state = Ui.Label("编辑中  ·  所有更改都会自动保存", 77, 40, 520, 8, false); state.ForeColor = Palette.Muted; header.Controls.Add(state);
            Button lockButton = Ui.Button("完成并锁定", 150, true); lockButton.Name = "lockButton"; lockButton.Location = new Point(744, 20); lockButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lockButton.Click += delegate { app.Lock(); }; header.Controls.Add(lockButton); Controls.Add(header);

            EdgePanel footer = new EdgePanel { Size = new Size(920, 60), Dock = DockStyle.Bottom, BackColor = Palette.Surface, TopLine = true };
            autoStart = new ModernToggle { Text = "开机后自动在后台显示便签", Location = new Point(24, 15), Font = Ui.Font(9, false), ForeColor = Palette.Muted };
            autoStart.CheckedChanged += delegate { if (!refreshing) app.SetAutoStart(autoStart.Checked); }; footer.Controls.Add(autoStart);
            Button export = Ui.Button("导出备份", 92, false); export.Location = new Point(604, 11); export.Anchor = AnchorStyles.Right | AnchorStyles.Top; export.Click += delegate { app.Export(); }; footer.Controls.Add(export);
            Button import = Ui.Button("导入备份", 92, false); import.Location = new Point(704, 11); import.Anchor = AnchorStyles.Right | AnchorStyles.Top; import.Click += delegate { app.Import(); }; footer.Controls.Add(import);
            Button undo = Ui.Button("撤销删除", 92, false); undo.Location = new Point(804, 11); undo.Anchor = AnchorStyles.Right | AnchorStyles.Top; undo.Click += delegate { app.UndoDelete(); }; footer.Controls.Add(undo); Controls.Add(footer);

            EdgePanel left = new EdgePanel { Size = new Size(280, 542), Dock = DockStyle.Left, BackColor = Palette.Canvas, RightLine = true };
            count = Ui.Label("便签", 24, 19, 130, 11, true); left.Controls.Add(count);
            Button add = Ui.Button("＋  新建", 88, true); add.Name = "addButton"; add.Location = new Point(168, 14); add.Click += delegate { app.AddNote(); }; left.Controls.Add(add);
            list = new FlowLayoutPanel { Location = new Point(23, 67), Size = new Size(234, 453), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Palette.Canvas, Padding = new Padding(0, 1, 0, 0) };
            left.Controls.Add(list);

            detail = new Panel { Size = new Size(640, 542), Dock = DockStyle.Fill, BackColor = Palette.Surface };
            detailTitle = Ui.Label("编辑便签", 32, 17, 390, 13, true); detailTitle.Height = 32; detail.Controls.Add(detailTitle);
            Label titleLabel = Ui.Label("标题", 32, 57, 200, 8, true); titleLabel.Name = "titleCaption"; titleLabel.Height = 20; titleLabel.ForeColor = Palette.Muted; detail.Controls.Add(titleLabel);
            SurfacePanel titleFrame = new SurfacePanel { Name = "titleFrame", Location = new Point(32, 81), Size = new Size(576, 42), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Padding = new Padding(11, 8, 11, 5), Radius = 7, FillColor = Palette.SoftSurface };
            title = new TextBox { Dock = DockStyle.Fill, Font = Ui.Font(11, true), BorderStyle = BorderStyle.None, BackColor = Palette.SoftSurface, ForeColor = Palette.Ink, MaxLength = 160 };
            title.TextChanged += delegate { if (!refreshing && app.Selected != null) { app.Selected.Title = title.Text; app.Changed(app.Selected); RefreshCards(); } }; titleFrame.Controls.Add(title); detail.Controls.Add(titleFrame);

            Label bodyLabel = Ui.Label("内容", 32, 135, 80, 8, true); bodyLabel.Name = "bodyCaption"; bodyLabel.Height = 20; bodyLabel.ForeColor = Palette.Muted; detail.Controls.Add(bodyLabel);
            Label saveHint = Ui.Label("支持 Markdown  ·  自动保存", 370, 135, 238, 8, false); saveHint.Height = 20; saveHint.ForeColor = Palette.Muted; saveHint.TextAlign = ContentAlignment.TopRight; saveHint.Anchor = AnchorStyles.Top | AnchorStyles.Right; detail.Controls.Add(saveHint);
            SurfacePanel toolbarFrame = new SurfacePanel { Name = "toolbarFrame", Location = new Point(32, 159), Size = new Size(576, 44), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Padding = new Padding(2, 1, 2, 1), Radius = 7, FillColor = Palette.SoftSurface };
            detail.Controls.Add(toolbarFrame);
            SurfacePanel bodyFrame = new SurfacePanel { Location = new Point(32, 207), Size = new Size(576, 129), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, Padding = new Padding(11, 10, 7, 8), Radius = 7, FillColor = Palette.SoftSurface };
            body = new TextBox { Name = "markdownBody", Dock = DockStyle.Fill, Font = Ui.Font(12, false), BorderStyle = BorderStyle.None, BackColor = Palette.SoftSurface, ForeColor = Palette.Ink, Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, MaxLength = 1000000 };
            body.TextChanged += delegate { if (!refreshing && app.Selected != null) { app.Selected.Text = body.Text; app.Changed(app.Selected); RefreshCards(); } }; bodyFrame.Controls.Add(body); detail.Controls.Add(bodyFrame);
            MarkdownToolbar toolbar = new MarkdownToolbar(body) { Dock = DockStyle.Fill, Cursor = Cursors.Arrow }; toolbarFrame.Controls.Add(toolbar);

            Panel options = new Panel { Location = new Point(32, 354), Size = new Size(576, 38), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = Palette.Surface };
            Label colorLabel = Ui.Label("颜色", 0, 6, 44, 9, false); options.Controls.Add(colorLabel);
            FlowLayoutPanel colors = new FlowLayoutPanel { Location = new Point(49, 3), Size = new Size(188, 32), BackColor = Palette.Surface, WrapContents = false };
            ToolTip colorTips = new ToolTip();
            for (int i = 0; i < Palette.Colors.Length; i++)
            {
                int color = i; SwatchButton swatch = new SwatchButton(Palette.Colors[i]) { AccessibleName = Palette.Names[i] };
                colorTips.SetToolTip(swatch, Palette.Names[i]); swatch.Click += delegate { if (app.Selected != null) app.SetColor(app.Selected, color); }; colorSwatches.Add(swatch); colors.Controls.Add(swatch);
            }
            options.Controls.Add(colors);
            options.Controls.Add(Ui.Label("字号", 262, 6, 44, 9, false));
            fontSize = new NumberStepper { Name = "fontSize", AccessibleName = "便签字号", Location = new Point(310, 2), Minimum = 9, Maximum = 28, Value = 12 };
            fontSize.ValueChanged += delegate { if (!refreshing && app.Selected != null) { app.Selected.FontSize = fontSize.Value; body.Font = Ui.Font(app.Selected.FontSize, false); app.Changed(app.Selected); } }; options.Controls.Add(fontSize); detail.Controls.Add(options);

            Panel opacityRow = new Panel { Location = new Point(32, 402), Size = new Size(576, 48), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = Palette.Surface };
            opacityRow.Controls.Add(Ui.Label("透明度", 0, 8, 72, 9, false));
            transparencyValue = Ui.Label("0%", 520, 8, 54, 9, false); transparencyValue.TextAlign = ContentAlignment.TopRight; transparencyValue.Anchor = AnchorStyles.Top | AnchorStyles.Right; opacityRow.Controls.Add(transparencyValue);
            transparency = new ModernSlider { Name = "transparency", AccessibleName = "便签透明度", Location = new Point(77, 2), Size = new Size(434, 36), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Minimum = 0, Maximum = 100, SmallChange = 1, LargeChange = 10 };
            transparency.ValueChanged += delegate { transparencyValue.Text = transparency.Value + "%"; if (!refreshing && app.Selected != null) { app.Selected.OpacityPercent = 100 - transparency.Value; app.Changed(app.Selected); } }; opacityRow.Controls.Add(transparency); detail.Controls.Add(opacityRow);

            Panel actions = new Panel { Location = new Point(32, 472), Size = new Size(576, 42), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = Palette.Surface };
            Button desktop = Ui.Button("在桌面编辑", 130, true); desktop.Name = "desktopEditButton"; desktop.Location = new Point(0, 0); desktop.Click += delegate { if (app.Selected != null) { WindowState = FormWindowState.Minimized; app.EditNote(app.Selected); } }; actions.Controls.Add(desktop);
            Button locate = Ui.Button("桌面摆放", 106, false); locate.Location = new Point(140, 0); locate.Click += delegate { app.GoToDesktop(); }; actions.Controls.Add(locate);
            Button delete = Ui.Button("删除", 78, false); delete.Location = new Point(498, 0); delete.Anchor = AnchorStyles.Top | AnchorStyles.Right; delete.ForeColor = Palette.Danger; delete.Click += delegate { if (app.Selected != null) app.DeleteNote(app.Selected); }; actions.Controls.Add(delete); detail.Controls.Add(actions);

            Controls.Add(detail); Controls.Add(left);
            Controls.SetChildIndex(detail, 0); Controls.SetChildIndex(left, 1); Controls.SetChildIndex(footer, 2); Controls.SetChildIndex(header, 3);
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!AllowClose && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; app.Lock(); } };
            Shown += delegate { RefreshData(); };
            ResumeLayout(true);
        }

        public void SetStatus(string message, bool error) { state.Text = message; state.ForeColor = error ? Palette.Danger : Palette.Muted; }
        public void RefreshData()
        {
            refreshing = true;
            try
            {
                count.Text = "便签  ·  " + app.Data.Notes.Count;
                Control[] previous = new Control[list.Controls.Count]; list.Controls.CopyTo(previous, 0); foreach (Control c in previous) c.Dispose(); list.Controls.Clear();
                if (app.Data.Notes.Count == 0)
                {
                    Label empty = new Label { Text = "这里还没有便签。\n点击右上角的「新建」开始记录。", Width = 220, Height = 76, Font = Ui.Font(9, false), ForeColor = Palette.Muted, Margin = new Padding(1, 14, 0, 0) }; list.Controls.Add(empty);
                }
                foreach (Note n in app.Data.Notes)
                {
                    Note captured = n; NoteCard card = new NoteCard(n) { Selected = n == app.Selected, Width = Math.Max(180, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 3) };
                    card.Click += delegate { app.Select(captured); }; card.DoubleClick += delegate { WindowState = FormWindowState.Minimized; app.EditNote(captured); }; list.Controls.Add(card);
                }
                detail.Enabled = app.Selected != null; detailTitle.Text = app.Selected == null ? "选择或新建一张便签" : "编辑便签";
                title.Text = app.Selected == null ? "" : app.Selected.Title; body.Text = app.Selected == null ? "" : app.Selected.Text;
                fontSize.Value = app.Selected == null ? 12 : (int)Math.Round(app.Selected.FontSize);
                transparency.Value = app.Selected == null ? 0 : 100 - app.Selected.OpacityPercent;
                body.Font = Ui.Font(app.Selected == null ? 12 : app.Selected.FontSize, false);
                for (int i = 0; i < colorSwatches.Count; i++) { colorSwatches[i].Selected = app.Selected != null && app.Selected.ColorIndex == i; colorSwatches[i].Invalidate(); }
                autoStart.Checked = app.Data.AutoStart; autoStart.Enabled = !app.TestMode;
            }
            finally { refreshing = false; }
        }
        public void RefreshCards() { foreach (Control c in list.Controls) c.Invalidate(); }
    }

    public sealed class NoteEditor : Form
    {
        private readonly NotesApp app;
        public readonly Note Note;
        private bool initializing = true;
        private readonly TextBox body;
        public NoteEditor(NotesApp owner, Note note)
        {
            app = owner; Note = note; Text = (note.Title.Length == 0 ? "便签" : note.Title) + " · 桌面编辑"; Icon = AppIcon.Create(); AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = true;
            StartPosition = FormStartPosition.Manual; Bounds = note.Bounds; MinimumSize = new Size(180, 140); MaximumSize = new Size(1600, 1600);
            BackColor = Palette.Colors[note.ColorIndex]; Padding = new Padding(1); Font = Ui.Font(9, false);
            Panel header = new Panel { Size = new Size(note.Width, 40), Dock = DockStyle.Top, Cursor = Cursors.SizeAll, BackColor = BackColor };
            Label moveGrip = new Label { Text = "⋮", Location = new Point(3, 8), Size = new Size(19, 28), ForeColor = Palette.Accent, Cursor = Cursors.SizeAll, TextAlign = ContentAlignment.MiddleCenter };
            moveGrip.MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { Native.ReleaseCapture(); Native.SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero); } }; header.Controls.Add(moveGrip);
            TextBox title = new TextBox { Text = note.Title, Location = new Point(25, 12), Width = note.Width - 76, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, BorderStyle = BorderStyle.None, BackColor = BackColor, ForeColor = Palette.Ink, Font = Ui.Font(9, true), MaxLength = 160 };
            title.TextChanged += delegate { if (!initializing) { note.Title = title.Text; app.Changed(note); } }; header.Controls.Add(title);
            Button done = new Button { Text = "✓", Size = new Size(27, 27), Location = new Point(note.Width - 35, 6), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = BackColor, Cursor = Cursors.Arrow, TabStop = false }; done.FlatAppearance.BorderSize = 0; done.Click += delegate { Close(); }; header.Controls.Add(done);
            header.MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { Native.ReleaseCapture(); Native.SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero); } };
            Controls.Add(header);
            Panel content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 12, 15, 16), BackColor = BackColor };
            body = new TextBox { Text = note.Text, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, BackColor = BackColor, ForeColor = Palette.Ink, Font = Ui.Font(note.FontSize, false), MaxLength = 1000000 };
            body.TextChanged += delegate { if (!initializing) { note.Text = body.Text; app.Changed(note); } }; content.Controls.Add(body); Controls.Add(content); content.BringToFront();
            MarkdownToolbar toolbar = new MarkdownToolbar(body) { Dock = DockStyle.Top, BackColor = BackColor }; Controls.Add(toolbar); toolbar.BringToFront();
            ResizeHandle resizeGrip = new ResizeHandle { BackColor = BackColor, Location = new Point(ClientSize.Width - 24, ClientSize.Height - 24), Size = new Size(24, 24), Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Cursor = Cursors.SizeNWSE };
            Controls.Add(resizeGrip); resizeGrip.BringToFront();
            Move += delegate { SaveBounds(); }; Resize += delegate { SaveBounds(); Invalidate(); };
            ResizeEnd += delegate { app.FinishMove(note); };
            Timer focusTimer = new Timer { Interval = 180 };
            focusTimer.Tick += delegate { focusTimer.Stop(); if (!IsDisposed && Visible && !ContainsFocus) Close(); };
            Deactivate += delegate { if (!IsDisposed) focusTimer.Start(); };
            Activated += delegate { focusTimer.Stop(); };
            FormClosed += delegate { focusTimer.Dispose(); };
            FormClosed += delegate { app.EditorClosed(this); };
            initializing = false;
        }
        protected override void OnShown(EventArgs e) { base.OnShown(e); body.Focus(); body.SelectionStart = body.TextLength; }
        private void SaveBounds() { if (!initializing) { Note.X = Left; Note.Y = Top; Note.Width = Width; Note.Height = Height; app.MarkDirty(); } }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        { if (keyData == Keys.Escape || keyData == (Keys.Control | Keys.Enter)) { Close(); return true; } return base.ProcessCmdKey(ref msg, keyData); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84)
            {
                Point p = PointToClient(new Point(unchecked((short)(m.LParam.ToInt64() & 0xffff)), unchecked((short)((m.LParam.ToInt64() >> 16) & 0xffff))));
                if (p.X >= ClientSize.Width - 16 && p.Y >= ClientSize.Height - 16) { m.Result = new IntPtr(17); return; }
            }
            base.WndProc(ref m);
        }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using (Pen p = new Pen(Palette.Accent, 2)) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1); }
    }

    public sealed class ResizeHandle : Control
    {
        protected override void OnPaint(PaintEventArgs e)
        { base.OnPaint(e); using (Pen p = new Pen(Palette.Accent, 1)) for (int i = 0; i < 3; i++) e.Graphics.DrawLine(p, Width - 6 - i * 5, Height - 4, Width - 4, Height - 6 - i * 5); }
        protected override void OnMouseDown(MouseEventArgs e)
        { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { Native.ReleaseCapture(); Native.SendMessage(FindForm().Handle, 0xA1, new IntPtr(17), IntPtr.Zero); } }
    }
}
