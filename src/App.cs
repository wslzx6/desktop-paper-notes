using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DesktopPaperNotes
{
    public static class AppIcon
    {
        public static Icon Create()
        {
            using (Bitmap b = new Bitmap(48, 48))
            using (Graphics g = Graphics.FromImage(b))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var p = NoteRenderer.Rounded(new RectangleF(1, 1, 46, 46), 10)) using (Brush brush = new SolidBrush(Palette.Accent)) g.FillPath(brush, p);
                using (var p = NoteRenderer.Rounded(new RectangleF(11, 9, 27, 30), 3)) using (Brush brush = new SolidBrush(Palette.Colors[0])) g.FillPath(brush, p);
                using (Pen pen = new Pen(Palette.Accent, 2)) { g.DrawLine(pen, 16, 17, 32, 17); g.DrawLine(pen, 16, 23, 32, 23); g.DrawLine(pen, 16, 29, 26, 29); }
                IntPtr h = b.GetHicon();
                try { using (Icon original = Icon.FromHandle(h)) return (Icon)original.Clone(); }
                finally { Native.DestroyIcon(h); }
            }
        }
    }

    public sealed class CommandWindow : Form
    {
        public static readonly uint ShowMessage = Native.RegisterWindowMessage("DesktopPaperNotes.ShowManager.1");
        public static readonly uint TaskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");
        private readonly NotesApp app;
        public CommandWindow(NotesApp owner) { app = owner; ShowInTaskbar = false; FormBorderStyle = FormBorderStyle.None; Text = "DesktopPaperNotes.Command"; IntPtr unused = Handle; }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == ShowMessage) { app.Post(delegate { app.OpenManager(); }); return; }
            if (m.Msg == TaskbarCreated) { app.Post(delegate { app.RecoverShell(); }); }
            if (m.Msg == 0x11) { app.Flush(); m.Result = new IntPtr(1); return; }
            base.WndProc(ref m);
        }
    }

    public sealed class NotesApp : ApplicationContext
    {
        public readonly DataStore Store;
        public AppData Data;
        public readonly bool TestMode;
        public readonly DesktopLayer Layer = new DesktopLayer();
        public Note Selected;
        public bool Editing { get; private set; }
        public ManagerWindow Manager { get; private set; }
        internal NoteEditor Editor { get { return editor; } }
        private readonly CommandWindow command;
        private readonly NotifyIcon tray;
        private readonly DesktopInput input;
        private readonly System.Windows.Forms.Timer saveTimer, shellTimer, drawTimer;
        private readonly HashSet<Note> drawQueue = new HashSet<Note>();
        private readonly Stack<Note> deleted = new Stack<Note>();
        private NoteEditor editor;
        private bool dirty, exiting;
        private string saveError;
        private string displayError;
        public NotesApp(string directory, bool background, bool testMode)
        {
            TestMode = testMode; Store = new DataStore(directory); Data = Store.Load();
            try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-diagnostics.txt"), DateTime.Now.ToString("s") + "\r\nDirectory=" + Path.GetFullPath(directory) + "\r\nLocalAppData=" + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\r\nEnvironmentLocalAppData=" + Environment.GetEnvironmentVariable("LOCALAPPDATA") + "\r\nUser=" + System.Security.Principal.WindowsIdentity.GetCurrent().Name + "\r\nNotes=" + Data.Notes.Count + "\r\nPrimaryExists=" + File.Exists(Store.FilePath) + "\r\nVersionsExist=" + Directory.Exists(Path.Combine(directory, "versions")) + "\r\nReadOnly=" + Store.ReadOnly + "\r\nWarning=" + Store.Warning); } catch { }
            command = new CommandWindow(this); input = new DesktopInput(this);
            saveTimer = new System.Windows.Forms.Timer { Interval = 450 }; saveTimer.Tick += delegate { saveTimer.Stop(); Flush(); };
            drawTimer = new System.Windows.Forms.Timer { Interval = 24 }; drawTimer.Tick += delegate { drawTimer.Stop(); foreach (Note n in drawQueue.ToArray()) SafeDraw(n); drawQueue.Clear(); };
            shellTimer = new System.Windows.Forms.Timer { Interval = 3000 }; shellTimer.Tick += delegate { RecoverShell(); };
            tray = new NotifyIcon { Icon = AppIcon.Create(), Text = "桌面便签 · 已锁定", Visible = true };
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("打开编辑", null, delegate { OpenManager(); });
            menu.Items.Add("新建便签", null, delegate { OpenManager(); AddNote(); });
            menu.Items.Add("完成并锁定", null, delegate { Lock(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出软件", null, delegate { Quit(); }); tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { OpenManager(); };
            SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
            SystemEvents.SessionEnding += OnSessionEnding;
            SystemEvents.PowerModeChanged += OnPowerChanged;
            RestorePositions(); RecoverShell(); shellTimer.Start();
            if (!TestMode)
            {
                try { Startup.Set(Data.AutoStart); }
                catch (Exception ex) { Store.Log("开机启动设置失败", ex); saveError = "开机启动设置失败，可在设置中重试。"; }
            }
            if (!background) Post(delegate { OpenManager(); });
        }
        public void Post(Action action)
        {
            if (!exiting && command.IsHandleCreated && !command.IsDisposed) command.BeginInvoke(action);
        }
        public void OpenManager()
        {
            if (exiting) return;
            if (Manager == null || Manager.IsDisposed) Manager = new ManagerWindow(this);
            try { input.Enable(); Editing = Layer.Editing = true; Layer.Sync(Data.Notes); }
            catch (Exception ex) { input.Disable(); Editing = Layer.Editing = false; Store.Log("开启编辑失败", ex); MessageBox.Show("无法开启桌面编辑：" + ex.Message, "桌面便签"); }
            if (Selected == null) Selected = Data.Notes.FirstOrDefault();
            Manager.Show(); Manager.WindowState = FormWindowState.Normal; Manager.RefreshData(); Manager.Activate(); Native.SetForegroundWindow(Manager.Handle);
            tray.Text = Editing ? "桌面便签 · 编辑中" : "桌面便签 · 已锁定"; UpdateStatus();
            if (Store.Warning != null) { string warning = Store.Warning; Store.Warning = null; MessageBox.Show(Manager, warning, "数据恢复", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        public void Lock()
        {
            input.Disable(); Editing = Layer.Editing = false;
            CloseEditor(); dirty = true; Flush();
            try { Layer.Sync(Data.Notes); } catch (Exception ex) { Store.Log("锁定绘制失败", ex); }
            if (Manager != null) Manager.Hide(); tray.Text = "桌面便签 · 已锁定";
            if (dirty && saveError != null) { tray.BalloonTipTitle = "便签尚未保存"; tray.BalloonTipText = saveError; tray.ShowBalloonTip(5000); }
        }
        public void GoToDesktop()
        {
            CloseEditor(); if (Manager != null) Manager.WindowState = FormWindowState.Minimized;
            object shell = null;
            try { Type type = Type.GetTypeFromProgID("Shell.Application"); shell = Activator.CreateInstance(type); type.InvokeMember("MinimizeAll", BindingFlags.InvokeMethod, null, shell, null); }
            catch (Exception ex) { Store.Log("显示桌面失败", ex); }
            finally { if (shell != null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell); }
            IntPtr view = Native.ShellView;
            if (view != IntPtr.Zero) Native.SetForegroundWindow(Native.GetAncestor(view, 2));
            tray.BalloonTipTitle = "桌面编辑已开启"; tray.BalloonTipText = "拖动标题栏移动；右下角调整大小；双击正文编辑。完成后从托盘锁定。"; tray.ShowBalloonTip(3500);
        }
        public Note NoteAt(Point point)
        {
            for (int i = Data.Notes.Count - 1; i >= 0; i--) if (Data.Notes[i].Bounds.Contains(point)) return Data.Notes[i]; return null;
        }
        public void Select(Note note)
        {
            if (!Data.Notes.Contains(note)) return;
            Selected = note; Layer.SelectedId = note.Id;
            try { Layer.Sync(Data.Notes); } catch (Exception ex) { Store.Log("选择绘制失败", ex); }
            if (Manager != null) Manager.RefreshData();
        }
        public void AddNote()
        {
            if (!Editing) OpenManager();
            Screen screen = Screen.FromPoint(Cursor.Position); Rectangle area = screen.WorkingArea;
            int offset = (Data.Notes.Count % 7) * 28;
            float scale = Ui.Dpi / 96;
            Note note = new Note { Title = "便签 " + (Data.Notes.Count + 1), Width = (int)(300 * scale), Height = (int)(260 * scale), X = area.Left + Math.Max(20, area.Width - (int)(370 * scale) - offset), Y = area.Top + 90 + offset, ColorIndex = Data.Notes.Count % Palette.Colors.Length, Monitor = screen.DeviceName };
            Placement.Restore(note, Screen.AllScreens.Select(s => s.WorkingArea).ToArray());
            Data.Notes.Add(note); Layer.Sync(Data.Notes); Select(note); MarkDirty(); Flush();
        }
        public void EditNote(Note note)
        {
            if (!Editing || !Data.Notes.Contains(note)) return;
            CloseEditor(); Select(note); Layer.EditingId = note.Id; Layer.Redraw(note);
            editor = new NoteEditor(this, note); editor.Show(); editor.Activate(); Native.SetForegroundWindow(editor.Handle);
        }
        public void EditorClosed(NoteEditor closed)
        {
            if (editor == closed) editor = null;
            Layer.EditingId = null; FinishMove(closed.Note);
            if (Manager != null && !Manager.IsDisposed) Manager.RefreshData();
        }
        private void CloseEditor() { if (editor != null && !editor.IsDisposed) editor.Close(); editor = null; Layer.EditingId = null; }
        public void Changed(Note note) { MarkDirty(); QueueDraw(note); }
        public void QueueDraw(Note note) { drawQueue.Add(note); if (!drawTimer.Enabled) drawTimer.Start(); }
        private void SafeDraw(Note note) { try { Layer.Redraw(note); } catch (Exception ex) { Store.Log("绘制失败", ex); displayError = "桌面显示异常，正在重试。"; } }
        public void MarkDirty() { dirty = true; saveTimer.Stop(); saveTimer.Start(); UpdateStatus(); }
        public void Flush()
        {
            if (!dirty) return;
            try { Store.Save(Data); dirty = false; saveError = null; saveTimer.Interval = 450; }
            catch (Exception ex) { Store.Log("保存失败", ex); saveError = "保存失败：" + ex.Message; if (!exiting) { saveTimer.Interval = 3000; saveTimer.Start(); } }
            UpdateStatus();
        }
        public void FinishMove(Note note)
        {
            if (!Data.Notes.Contains(note)) return;
            Placement.Restore(note, Screen.AllScreens.Select(s => s.WorkingArea).ToArray()); note.Monitor = Screen.FromRectangle(note.Bounds).DeviceName;
            Changed(note); SafeDraw(note); Flush(); if (Manager != null) Manager.RefreshCards();
        }
        public void DeleteNote(Note note)
        {
            CloseEditor(); if (!Data.Notes.Remove(note)) return;
            deleted.Push(note); Selected = Data.Notes.LastOrDefault(); Layer.SelectedId = Selected == null ? null : Selected.Id;
            Layer.Sync(Data.Notes); MarkDirty(); Flush(); if (Manager != null) Manager.RefreshData();
        }
        public void UndoDelete()
        {
            if (deleted.Count == 0) return; Note note = deleted.Pop(); Data.Notes.Add(note); Layer.Sync(Data.Notes); Select(note); MarkDirty(); Flush();
        }
        public void SetColor(Note note, int color) { CloseEditor(); note.ColorIndex = color; Changed(note); Flush(); if (Manager != null) Manager.RefreshData(); }
        public void ShowNoteMenu(Note note, Point point)
        {
            Select(note); ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("编辑内容", null, delegate { EditNote(note); });
            ToolStripMenuItem colors = new ToolStripMenuItem("便签颜色");
            for (int i = 0; i < Palette.Colors.Length; i++) { int c = i; colors.DropDownItems.Add(Palette.Names[i], null, delegate { SetColor(note, c); }); }
            menu.Items.Add(colors); menu.Items.Add("删除便签", null, delegate { DeleteNote(note); });
            menu.Closed += delegate { Post(delegate { menu.Dispose(); }); }; menu.Show(point);
        }
        public void SetAutoStart(bool enabled)
        {
            if (TestMode) return;
            try { Startup.Set(enabled); Data.AutoStart = enabled; MarkDirty(); Flush(); }
            catch (Exception ex) { Store.Log("开机启动设置失败", ex); MessageBox.Show(Manager, "设置失败：" + ex.Message, "开机启动"); if (Manager != null) Manager.RefreshData(); }
        }
        public void Export()
        {
            CloseEditor(); Flush();
            using (SaveFileDialog dialog = new SaveFileDialog { Filter = "便签备份 (*.json)|*.json", FileName = "桌面便签备份-" + DateTime.Now.ToString("yyyyMMdd") + ".json" })
                if (dialog.ShowDialog(Manager) == DialogResult.OK)
                    try { DataStore.WriteAtomic(dialog.FileName, Data); MessageBox.Show(Manager, "备份已导出。", "桌面便签"); }
                    catch (Exception ex) { MessageBox.Show(Manager, ex.Message, "导出失败"); }
        }
        public void Import()
        {
            using (OpenFileDialog dialog = new OpenFileDialog { Filter = "便签备份 (*.json)|*.json" })
            {
                if (dialog.ShowDialog(Manager) != DialogResult.OK) return;
                try
                {
                    AppData incoming = DataStore.Read(dialog.FileName);
                    if (MessageBox.Show(Manager, "将恢复备份中的 " + incoming.Notes.Count + " 张便签。当前便签会先另存一份备份，再替换。", "恢复备份", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                    CloseEditor(); DataStore.WriteAtomic(Path.Combine(Store.DirectoryPath, "before-import-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".json"), Data);
                    incoming.AutoStart = Data.AutoStart; Data = incoming; Store.ReadOnly = false; deleted.Clear(); Selected = Data.Notes.FirstOrDefault();
                    RestorePositions(); Layer.Sync(Data.Notes); MarkDirty(); Flush(); Manager.RefreshData();
                }
                catch (Exception ex) { MessageBox.Show(Manager, "导入失败：" + ex.Message, "桌面便签"); }
            }
        }
        private void RestorePositions()
        {
            Rectangle[] areas = Screen.AllScreens.Select(s => s.WorkingArea).ToArray(); bool changed = false;
            foreach (Note n in Data.Notes) if (Placement.Restore(n, areas)) changed = true;
            if (changed) MarkDirty();
        }
        public void RecoverShell()
        {
            if (exiting) return;
            try { if (Layer.Recover(Data.Notes)) { if (displayError != null) Layer.Sync(Data.Notes); displayError = null; } if (!tray.Visible) tray.Visible = true; }
            catch (Exception ex) { Store.Log("恢复桌面失败", ex); displayError = "桌面连接失败，正在重试。"; }
            UpdateStatus();
        }
        private void OnDisplayChanged(object sender, EventArgs e) { Post(delegate { CloseEditor(); RestorePositions(); Layer.Clear(); RecoverShell(); Layer.Sync(Data.Notes); }); }
        private void FlushOnUiThread()
        {
            if (exiting || command.IsDisposed || !command.IsHandleCreated) return;
            try { if (command.InvokeRequired) command.Invoke((Action)Flush); else Flush(); }
            catch (Exception ex) { Store.Log("系统事件保存失败", ex); }
        }
        private void OnSessionEnding(object sender, SessionEndingEventArgs e) { FlushOnUiThread(); }
        private void OnPowerChanged(object sender, PowerModeChangedEventArgs e) { if (e.Mode == PowerModes.Suspend) FlushOnUiThread(); else if (e.Mode == PowerModes.Resume) Post(RecoverShell); }
        private void UpdateStatus()
        {
            if (Manager == null || Manager.IsDisposed) return;
            string error = saveError ?? displayError ?? Layer.Error;
            Manager.SetStatus(error ?? (dirty ? "● 编辑中 · 正在保存…" : "● 编辑中 · 已保存 · 关闭窗口即可锁定"), error != null);
        }
        public void Quit()
        {
            CloseEditor(); Flush();
            if (dirty && MessageBox.Show(Manager, "还有未保存的修改。退出可能丢失这些修改，是否仍要退出？", "保存未完成", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            exiting = true; input.Dispose(); saveTimer.Dispose(); drawTimer.Dispose(); shellTimer.Dispose();
            SystemEvents.DisplaySettingsChanged -= OnDisplayChanged; SystemEvents.SessionEnding -= OnSessionEnding; SystemEvents.PowerModeChanged -= OnPowerChanged;
            Layer.Dispose(); tray.Visible = false; tray.Dispose();
            if (Manager != null) { Manager.AllowClose = true; Manager.Close(); Manager.Dispose(); }
            command.Dispose(); ExitThread();
        }
    }

    public static class Startup
    {
        public const string ValueName = "DesktopPaperNotes";
        public static void Set(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (enabled) key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\" --background");
                else key.DeleteValue(ValueName, false);
            }
        }
    }

    public static class StorageLocation
    {
        public static string Resolve(string executableDirectory, bool testMode)
        {
            return Path.GetFullPath(Path.Combine(executableDirectory, testMode ? "test-data" : "data"));
        }
        public static void MigrateLegacy(string destination, string legacyDirectory)
        {
            if (File.Exists(Path.Combine(destination, "notes.json")) || Directory.Exists(Path.Combine(destination, "versions"))) return;
            if (!Directory.Exists(legacyDirectory)) return;
            DataStore legacy = new DataStore(legacyDirectory);
            AppData data = legacy.Load();
            if (legacy.ReadOnly) throw new IOException(legacy.Warning);
            if (!File.Exists(legacy.FilePath) && !Directory.Exists(Path.Combine(legacyDirectory, "versions"))) return;
            new DataStore(destination).Save(data);
        }
    }

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Native.SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Contains("--self-test")) return SelfTests.Run(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-results"));
            if (args.Contains("--diagnose")) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "desktop-diagnostics.txt"), Native.DescribeDesktop()); return 0; }
            if (args.Contains("--make-icon")) { using (Icon icon = AppIcon.Create()) using (FileStream stream = File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico"))) icon.Save(stream); return 0; }
            if (args.Contains("--storage-check"))
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPaperNotes", "storage-check-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                DataStore probe = new DataStore(folder);
                try { AppData d = new AppData(); d.Notes.Add(new Note { Text = "第一次" }); probe.Save(d); d.Notes[0].Text = "第二次"; probe.Save(d); d.Notes[0].Text = "第三次"; probe.Save(d); if (probe.Load().Notes[0].Text != "第三次" || DataStore.Read(probe.FilePath + ".bak").Notes[0].Text != "第二次") throw new IOException("保存验证失败"); File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage-check.txt"), "PASS " + folder); return 0; }
                catch (Exception ex) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage-check.txt"), ex.ToString()); return 1; }
            }
            bool testMode = args.Contains("--test-mode");
            string directory = StorageLocation.Resolve(AppDomain.CurrentDomain.BaseDirectory, testMode);
            bool created;
            using (Mutex mutex = new Mutex(true, "Local\\DesktopPaperNotes." + (testMode ? "Test." : "") + Environment.UserName, out created))
            {
                if (!created) { if (!args.Contains("--background")) Native.PostMessage(new IntPtr(0xffff), CommandWindow.ShowMessage, IntPtr.Zero, IntPtr.Zero); return 0; }
                NotesApp app = null;
                try
                {
                    if (!testMode) StorageLocation.MigrateLegacy(directory, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPaperNotes"));
                    app = new NotesApp(directory, args.Contains("--background"), testMode);
                    Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) { app.Store.Log("界面异常", e.Exception); MessageBox.Show("操作未完成：" + e.Exception.Message, "桌面便签"); };
                    Application.Run(app); return 0;
                }
                catch (Exception ex)
                {
                    if (app != null) app.Store.Log("启动失败", ex);
                    try { Directory.CreateDirectory(directory); File.AppendAllText(Path.Combine(directory, "app.log"), ex.ToString() + Environment.NewLine); } catch { }
                    MessageBox.Show("无法启动桌面便签：" + ex.Message, "桌面便签", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1;
                }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }
}
