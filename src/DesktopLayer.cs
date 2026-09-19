using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopPaperNotes
{
    public static class NoteRenderer
    {
        public const int Margin = 8;
        public static GraphicsPath Rounded(RectangleF rect, float radius)
        {
            GraphicsPath p = new GraphicsPath(); float d = radius * 2;
            p.AddArc(rect.Left, rect.Top, d, d, 180, 90);
            p.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            p.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
        }
        public static Bitmap Render(Note note, bool editing, bool selected)
        {
            Bitmap bitmap = new Bitmap(note.Width + Margin * 2, note.Height + Margin * 2, PixelFormat.Format32bppPArgb);
            bitmap.SetResolution(Ui.Dpi, Ui.Dpi);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent); g.SmoothingMode = SmoothingMode.AntiAlias;
                for (int i = 5; i >= 1; i--)
                    using (GraphicsPath shadow = Rounded(new RectangleF(Margin - i, Margin - i + 3, note.Width + i * 2, note.Height + i * 2), 9 + i))
                    using (Brush brush = new SolidBrush(Color.FromArgb(7, 0, 0, 0))) g.FillPath(brush, shadow);
                using (GraphicsPath body = Rounded(new RectangleF(Margin, Margin, note.Width - 1, note.Height - 1), 9))
                {
                    using (Brush b = new SolidBrush(Palette.Colors[note.ColorIndex])) g.FillPath(b, body);
                    if (editing && selected) using (Pen p = new Pen(Palette.Accent, 2)) g.DrawPath(p, body);
                }
                using (Pen p = new Pen(Color.FromArgb(22, Palette.Ink))) g.DrawLine(p, Margin + 16, Margin + 39, Margin + note.Width - 16, Margin + 39);
                using (Font titleFont = new Font("Microsoft YaHei UI", 9, FontStyle.Bold))
                using (Brush ink = new SolidBrush(Palette.Ink))
                using (StringFormat f = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                    g.DrawString(note.Title.Length == 0 ? "便签" : note.Title, titleFont, ink, new RectangleF(Margin + 16, Margin + 12, note.Width - 52, 22), f);
                string displayText = note.Text.Length == 0 && editing ? "双击这里，写点什么…" : note.Text;
                MarkdownRenderer.Draw(g, displayText, new RectangleF(Margin + 16, Margin + 53, note.Width - 32, note.Height - 69), note.FontSize, note.Text.Length == 0 ? Color.FromArgb(110, Palette.Ink) : Palette.Ink);
                if (editing)
                {
                    using (Pen p = new Pen(Color.FromArgb(95, Palette.Ink), 1))
                    {
                        for (int i = 0; i < 3; i++) g.DrawLine(p, Margin + note.Width - 12 - i * 4, Margin + note.Height - 6, Margin + note.Width - 6, Margin + note.Height - 12 - i * 4);
                        for (int i = 0; i < 3; i++) { g.DrawEllipse(p, Margin + note.Width - 29 + i * 5, Margin + 18, 1, 1); g.DrawEllipse(p, Margin + note.Width - 29 + i * 5, Margin + 23, 1, 1); }
                    }
                }
            }
            return bitmap;
        }
    }

    public sealed class NoteLayerWindow : Form
    {
        public Note Note;
        private IntPtr parent;
        private Bitmap image;
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { CreateParams p = base.CreateParams; p.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW; return p; }
        }
        public NoteLayerWindow(Note note, IntPtr host)
        {
            Note = note; parent = host; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None; StartPosition = FormStartPosition.Manual;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Bounds = new Rectangle(note.X - NoteRenderer.Margin, note.Y - NoteRenderer.Margin, note.Width + 16, note.Height + 16);
            IntPtr h = Handle;
            Native.SetWindowLong(h, Native.GWL_STYLE, (Native.GetWindowLong(h, Native.GWL_STYLE) & ~Native.WS_POPUP) | Native.WS_CHILD);
            Native.SetParent(h, parent);
            if (Native.GetParent(h) != parent) throw new Win32Exception(Marshal.GetLastWin32Error(), "无法连接桌面图层。");
            // Use an ordinary redirected paint surface. ULW child surfaces can be
            // absent from the Windows 11 desktop compositor despite success.
            if (!Native.SetLayeredWindowAttributes(h, 0, note.WindowAlpha, 2)) throw new Win32Exception(Marshal.GetLastWin32Error(), "无法启用便签显示表面。");
            Show(); MoveOnDesktop();
            Native.OrderNoteLayer(Handle, parent);
        }
        public void MoveOnDesktop()
        {
            if (IsDisposed || !Native.IsWindow(parent)) return;
            Native.POINT p = new Native.POINT(Note.X - NoteRenderer.Margin, Note.Y - NoteRenderer.Margin);
            Native.ScreenToClient(parent, ref p);
            Native.SetWindowPos(Handle, Native.HWND_TOP, p.X, p.Y, Note.Width + 16, Note.Height + 16, Native.SWP_NOACTIVATE | Native.SWP_NOZORDER);
        }
        public void Render(bool editing, bool selected)
        {
            if (!Native.SetLayeredWindowAttributes(Handle, 0, Note.WindowAlpha, 2)) throw new Win32Exception(Marshal.GetLastWin32Error(), "无法调整便签透明度。");
            Bitmap previous = image; image = NoteRenderer.Render(Note, editing, selected); if (previous != null) previous.Dispose();
            BackColor = Palette.Colors[Note.ColorIndex];
            Region old = Region; Region = new Region(new Rectangle(NoteRenderer.Margin, NoteRenderer.Margin, Note.Width, Note.Height)); if (old != null) old.Dispose();
            MoveOnDesktop(); Invalidate(); Update();
        }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(BackColor); }
        protected override void OnPaint(PaintEventArgs e) { if (image != null) e.Graphics.DrawImageUnscaled(image, 0, 0); }
        protected override void Dispose(bool disposing) { if (disposing && image != null) { image.Dispose(); image = null; } base.Dispose(disposing); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84) { m.Result = new IntPtr(-1); return; }
            if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; }
            base.WndProc(ref m);
        }
    }

    public sealed class DesktopLayer : IDisposable
    {
        private readonly Dictionary<string, NoteLayerWindow> windows = new Dictionary<string, NoteLayerWindow>();
        public IntPtr Host { get; private set; }
        public string Error { get; private set; }
        public bool Editing;
        public string SelectedId;
        public string EditingId;
        public bool Recover(IList<Note> notes)
        {
            IntPtr found = Native.FindNoteHost(true);
            if (found == IntPtr.Zero) { Error = "等待 Windows 桌面就绪…"; return false; }
            if (Host != found || !Native.IsWindow(Host))
            {
                Clear(); Host = found; Sync(notes); return true;
            }
            foreach (NoteLayerWindow w in windows.Values)
                if (!w.IsHandleCreated || !Native.IsWindow(w.Handle)) { Clear(); Sync(notes); return true; }
            Error = null; return true;
        }
        public void Sync(IList<Note> notes)
        {
            if (Host == IntPtr.Zero || !Native.IsWindow(Host)) return;
            List<string> remove = new List<string>();
            foreach (string id in windows.Keys) { bool exists = false; foreach (Note n in notes) if (n.Id == id) exists = true; if (!exists) remove.Add(id); }
            foreach (string id in remove) { windows[id].Dispose(); windows.Remove(id); }
            foreach (Note note in notes)
            {
                if (!windows.ContainsKey(note.Id)) windows[note.Id] = new NoteLayerWindow(note, Host);
                Redraw(note);
            }
            // Place each successive note at the top of our band. Icons stay above
            // the entire band; the wallpaper compositor stays below it.
            foreach (Note note in notes) Native.OrderNoteLayer(windows[note.Id].Handle, Host);
            Error = null;
        }
        public void Redraw(Note note)
        {
            NoteLayerWindow w;
            if (windows.TryGetValue(note.Id, out w) && !w.IsDisposed)
            {
                if (note.Id == EditingId) w.Hide();
                else { if (!w.Visible) w.Show(); w.Render(Editing, note.Id == SelectedId); }
            }
        }
        public void Move(Note note) { NoteLayerWindow w; if (windows.TryGetValue(note.Id, out w)) w.MoveOnDesktop(); }
        public IntPtr WindowFor(string id) { NoteLayerWindow w; return windows.TryGetValue(id, out w) ? w.Handle : IntPtr.Zero; }
        public void Clear() { foreach (NoteLayerWindow w in windows.Values) w.Dispose(); windows.Clear(); }
        public void Dispose() { Clear(); }
    }

    public sealed class DesktopInput : IDisposable
    {
        private readonly NotesApp app;
        private readonly Native.HookProc callback;
        private readonly DesktopIconHitTest icons = new DesktopIconHitTest();
        private IntPtr hook;
        private Note dragging;
        private bool resizing, consumedRight;
        private Point origin;
        private Rectangle initial;
        private string lastId;
        private Point lastPoint;
        private uint lastTime;
        public DesktopInput(NotesApp owner) { app = owner; callback = OnMouse; }
        public void Enable()
        {
            if (hook != IntPtr.Zero) return;
            hook = Native.SetWindowsHookEx(14, callback, Native.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "无法启用桌面编辑。");
        }
        public void Disable()
        {
            if (hook != IntPtr.Zero) Native.UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero; dragging = null; consumedRight = false; lastId = null;
            icons.Dispose();
        }
        private IntPtr OnMouse(int code, IntPtr message, IntPtr data)
        {
            if (code < 0 || !app.Editing) return Native.CallNextHookEx(hook, code, message, data);
            int msg = message.ToInt32(); Native.MSLLHOOKSTRUCT info = (Native.MSLLHOOKSTRUCT)Marshal.PtrToStructure(data, typeof(Native.MSLLHOOKSTRUCT));
            Point p = info.Pt.ToPoint();
            try
            {
                if (dragging != null)
                {
                    if (msg == Native.WM_MOUSEMOVE)
                    {
                        int dx = p.X - origin.X, dy = p.Y - origin.Y;
                        if (resizing) { dragging.Width = Math.Max(180, Math.Min(1600, initial.Width + dx)); dragging.Height = Math.Max(140, Math.Min(1600, initial.Height + dy)); app.QueueDraw(dragging); }
                        else { dragging.X = initial.X + dx; dragging.Y = initial.Y + dy; app.Layer.Move(dragging); }
                        // Keep normal cursor motion. The original button-down was
                        // consumed, so Explorer cannot start dragging an icon.
                        return Native.CallNextHookEx(hook, code, message, data);
                    }
                    if (msg == Native.WM_LBUTTONUP)
                    {
                        Note finished = dragging; dragging = null;
                        app.Post(delegate { app.FinishMove(finished); }); return new IntPtr(1);
                    }
                }
                if (msg == Native.WM_RBUTTONUP && consumedRight) { consumedRight = false; return new IntPtr(1); }
                if (msg != Native.WM_LBUTTONDOWN && msg != Native.WM_RBUTTONDOWN) return Native.CallNextHookEx(hook, code, message, data);
                if (!Native.IsDesktopPoint(p)) return Native.CallNextHookEx(hook, code, message, data);
                Note note = app.NoteAt(p);
                if (note == null || icons.IsIcon(p)) return Native.CallNextHookEx(hook, code, message, data);
                if (msg == Native.WM_RBUTTONDOWN) { consumedRight = true; app.Post(delegate { app.ShowNoteMenu(note, p); }); return new IntPtr(1); }
                bool doubleClick = lastId == note.Id && unchecked(info.Time - lastTime) <= SystemInformation.DoubleClickTime && Math.Abs(p.X - lastPoint.X) <= SystemInformation.DoubleClickSize.Width && Math.Abs(p.Y - lastPoint.Y) <= SystemInformation.DoubleClickSize.Height;
                lastId = note.Id; lastPoint = p; lastTime = info.Time;
                if (doubleClick) { lastId = null; app.Post(delegate { app.EditNote(note); }); }
                else
                {
                    app.Post(delegate { app.Select(note); });
                    if (p.Y < note.Y + 40 || (p.X >= note.X + note.Width - 20 && p.Y >= note.Y + note.Height - 20))
                    {
                        dragging = note; origin = p; initial = note.Bounds;
                        resizing = p.X >= note.X + note.Width - 20 && p.Y >= note.Y + note.Height - 20;
                    }
                }
                return new IntPtr(1);
            }
            catch (Exception ex) { app.Store.Log("桌面输入异常", ex); dragging = null; }
            return Native.CallNextHookEx(hook, code, message, data);
        }
        public void Dispose() { Disable(); }
    }
}
