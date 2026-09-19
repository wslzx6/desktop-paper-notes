using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DesktopPaperNotes
{
    public static class SelfTests
    {
        private static int count;
        private static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); count++; }
        public static int Run(string root)
        {
            Directory.CreateDirectory(root);
            string run = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")); Directory.CreateDirectory(run);
            try
            {
                DataStore store = new DataStore(Path.Combine(run, "data"));
                AppData data = store.Load(); Check(data.Notes.Count == 0 && data.AutoStart, "clean initial settings");
                Note note = new Note { Title = "中文标题 📌", Text = "第一行\r\n第二行：保存测试 😀", X = -1200, Y = 130, ColorIndex = 3 };
                data.Notes.Add(note); store.Save(data);
                AppData read = store.Load(); Check(read.Notes[0].Text == note.Text && read.Notes[0].Title == note.Title && read.Notes[0].X == -1200, "Unicode and coordinates round-trip");
                string legacy = Path.Combine(run, "legacy.json"); File.WriteAllText(legacy, "{\"Version\":1,\"Notes\":[{\"Title\":\"旧便签\"}]}");
                Check(DataStore.Read(legacy).Notes[0].OpacityPercent == 100, "legacy note without opacity stays fully visible");
                note.OpacityPercent = 37; store.Save(data);
                Check(store.Load().Notes[0].OpacityPercent == 37, "per-note opacity survives save and restart");
                Note opacityBounds = new Note { OpacityPercent = -20 }; opacityBounds.Normalize(); Check(opacityBounds.WindowAlpha == 0, "opacity lower bound clamped");
                opacityBounds.OpacityPercent = 120; opacityBounds.Normalize(); Check(opacityBounds.WindowAlpha == 255, "opacity upper bound clamped");
                note.OpacityPercent = 100;
                note.Text = "更新后内容"; store.Save(data); Check(File.Exists(store.FilePath + ".bak"), "atomic replacement creates backup");
                Check(DataStore.Read(store.FilePath + ".bak").Notes[0].Text.StartsWith("第一行"), "backup contains previous revision");
                File.WriteAllText(store.FilePath, "{invalid-json");
                read = store.Load(); Check(read.Notes.Count == 1 && store.Warning != null && !store.ReadOnly, "corrupted primary recovers backup");
                Check(Directory.GetFiles(store.DirectoryPath, "*.damaged-*").Length == 1, "corrupted original preserved");
                DataStore noBackup = new DataStore(Path.Combine(run, "corrupted")); Directory.CreateDirectory(noBackup.DirectoryPath); File.WriteAllText(noBackup.FilePath, "bad");
                noBackup.Load(); Check(noBackup.ReadOnly && File.ReadAllText(noBackup.FilePath) == "bad", "unrecoverable file is not overwritten");
                bool refused = false; try { noBackup.Save(new AppData()); } catch (IOException) { refused = true; } Check(refused, "read-only protection enforced");
                string future = Path.Combine(run, "future.json"); DataStore.WriteAtomic(future, new AppData { Version = 999 });
                bool incompatible = false; try { DataStore.Read(future); } catch (NotSupportedException) { incompatible = true; } Check(incompatible, "newer schema not silently downgraded");
                Note invalid = new Note { Width = -1, Height = 99999, FontSize = Single.NaN, ColorIndex = 99 }; invalid.Normalize();
                Check(invalid.Width == 180 && invalid.Height == 1600 && invalid.FontSize == 12 && invalid.ColorIndex == 4, "invalid style and geometry normalized");
                Note offscreen = new Note { X = 5000, Y = -5000, Width = 300, Height = 260 };
                Placement.Restore(offscreen, new[] { new Rectangle(0, 0, 1920, 1040) }); Check(new Rectangle(0, 0, 1920, 1040).Contains(offscreen.Bounds), "disconnected monitor note restored");
                Note left = new Note { X = -1500, Y = 100 }; Placement.Restore(left, new[] { new Rectangle(-1920, 0, 1920, 1040), new Rectangle(0, 0, 1920, 1040) }); Check(left.X == -1500, "negative monitor coordinates preserved");
                string markdown = "# 标题\n- [x] **完成**和*斜体*\n> 引用与[链接](https://example.com)\n```\ncode();\n```";
                System.Collections.Generic.List<MarkdownBlock> markdownBlocks = MarkdownRenderer.Parse(markdown);
                Check(markdownBlocks.Count == 4 && markdownBlocks[0].Heading == 1, "Markdown heading and fenced code parsed");
                Check(markdownBlocks[1].Prefix == "☑" && markdownBlocks[1].Runs.Any(r => (r.Style & MarkdownTextStyle.Bold) != 0), "Markdown task and bold parsed");
                Check(markdownBlocks[1].Runs.Any(r => (r.Style & MarkdownTextStyle.Italic) != 0), "Markdown italic parsed");
                Check(markdownBlocks[2].Quote && markdownBlocks[2].Runs.Any(r => (r.Style & MarkdownTextStyle.Link) != 0), "Markdown quote and link parsed");
                Check(markdownBlocks[3].Code && markdownBlocks[3].Runs[0].Text == "code();", "Markdown code block parsed");
                Check(MarkdownRenderer.ToPlainText("- **事项**") == "• 事项", "Markdown syntax removed from list preview");
                int editStart, editLength;
                string inlineEdit = MarkdownEditing.Inline("选择文字", 0, 2, "*", "*", out editStart, out editLength);
                Check(inlineEdit == "*选择*文字" && editStart == 1 && editLength == 2, "italic toolbar wraps selected text and preserves selection");
                inlineEdit = MarkdownEditing.Inline(inlineEdit, 0, 4, "*", "*", out editStart, out editLength);
                Check(inlineEdit == "选择文字" && editStart == 0 && editLength == 2, "inline toolbar toggles existing markers off");
                string lineEdit = MarkdownEditing.PrefixLines("第一行\r\n第二行", 0, 7, "- [ ] ", out editStart, out editLength);
                Check(lineEdit == "- [ ] 第一行\r\n- [ ] 第二行", "task toolbar prefixes every selected line");
                note.Text = markdown;
                using (Bitmap image = NoteRenderer.Render(note, false, false))
                {
                    image.Save(Path.Combine(run, "note-render.png"));
                    Check(image.GetPixel(0, 0).A == 0 && image.GetPixel(30, 80).A == 255, "transparent canvas and opaque note body");
                }
                File.WriteAllText(Path.Combine(run, "desktop-before.txt"), Native.DescribeDesktop());
                IntPtr host = Native.FindNoteHost(true); Check(host != IntPtr.Zero, "desktop note host found");
                note.X = Screen.PrimaryScreen.WorkingArea.Right - 340; note.Y = Screen.PrimaryScreen.WorkingArea.Top + 120;
                using (DesktopLayer layer = new DesktopLayer())
                {
                    layer.Recover(new[] { note }); IntPtr hwnd = layer.WindowFor(note.Id);
                    Check(hwnd != IntPtr.Zero && Native.GetParent(hwnd) == host, "note attached to wallpaper host");
                    IntPtr view = Native.ShellView;
                    if (Native.GetParent(view) == host)
                    {
                        Check(Native.GetWindow(hwnd, 3) == view, "note is an independent sibling immediately below desktop icons");
                        IntPtr sibling = Native.GetWindow(hwnd, 2); bool wallpaperBelow = false;
                        while (sibling != IntPtr.Zero) { if (Native.Class(sibling) == "WorkerW") { wallpaperBelow = true; break; } sibling = Native.GetWindow(sibling, 2); }
                        Check(wallpaperBelow, "wallpaper compositor is below note layer");
                    }
                    int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
                    uint key, flags; byte alpha;
                    Check(Native.GetLayeredWindowAttributes(hwnd, out key, out alpha, out flags) && alpha == 255 && (flags & 2) != 0, "desktop layer has an opaque redirected paint surface");
                    note.OpacityPercent = 50; layer.Redraw(note);
                    Check(Native.GetLayeredWindowAttributes(hwnd, out key, out alpha, out flags) && alpha == 128, "changing transparency updates real desktop window alpha");
                    note.OpacityPercent = 0; layer.Redraw(note);
                    Check(Native.GetLayeredWindowAttributes(hwnd, out key, out alpha, out flags) && alpha == 0, "fully transparent desktop note supported");
                    note.OpacityPercent = 100; layer.Redraw(note);
                    Check(Native.GetLayeredWindowAttributes(hwnd, out key, out alpha, out flags) && alpha == 255, "transparent note can become visible again");
                    Check((ex & Native.WS_EX_TRANSPARENT) != 0 && (ex & Native.WS_EX_NOACTIVATE) != 0, "locked note is click-through and cannot activate");
                    Native.RECT bounds; Native.GetWindowRect(hwnd, out bounds);
                    Check(bounds.Left == note.X - 8 && bounds.Top == note.Y - 8, "desktop child uses correct screen coordinates");
                    note.X -= 120; layer.Move(note); Native.GetWindowRect(hwnd, out bounds);
                    Check(bounds.Left == note.X - 8, "note move updates native geometry");
                    using (DesktopIconHitTest hit = new DesktopIconHitTest()) Check(!hit.IsIcon(new Point(Screen.PrimaryScreen.WorkingArea.Right - 20, Screen.PrimaryScreen.WorkingArea.Bottom - 20)), "empty desktop point passes icon hit-test");
                }
                string life = Path.Combine(run, "lifecycle"); DataStore seed = new DataStore(life); seed.Save(new AppData { Notes = new System.Collections.Generic.List<Note> { note } });
                NotesApp app = new NotesApp(life, true, true);
                try
                {
                    Check(!app.Editing && app.Manager == null && app.Data.Notes.Count == 1, "background startup restores locked without manager");
                    app.OpenManager(); Check(app.Editing && app.Manager.Visible, "opening manager enables desktop editing");
                    Check(app.Manager.Controls.Find("lockButton", true).Single().Cursor == Cursors.Arrow && app.Manager.Controls.Find("addButton", true).Single().Cursor == Cursors.Arrow && app.Manager.Controls.Find("desktopEditButton", true).Single().Cursor == Cursors.Arrow, "manager buttons keep the standard arrow cursor");
                    ModernSlider slider = app.Manager.Controls.Find("transparency", true).OfType<ModernSlider>().Single();
                    Check(slider.Parent.ClientRectangle.Contains(slider.Bounds), "transparency slider fits high DPI layout");
                    TextBox markdownBody = app.Manager.Controls.Find("markdownBody", true).OfType<TextBox>().Single();
                    MarkdownToolbar managerToolbar = app.Manager.Controls.Find("markdownToolbar", true).OfType<MarkdownToolbar>().Single();
                    Check(managerToolbar.Parent.ClientRectangle.Contains(managerToolbar.Bounds) && managerToolbar.Parent.Bounds.Bottom <= markdownBody.Parent.Bounds.Top, "Markdown toolbar fits above manager editor");
                    Control titleCaption = app.Manager.Controls.Find("titleCaption", true).Single(), titleFrame = app.Manager.Controls.Find("titleFrame", true).Single();
                    Control bodyCaption = app.Manager.Controls.Find("bodyCaption", true).Single(), toolbarFrame = app.Manager.Controls.Find("toolbarFrame", true).Single();
                    Check(titleCaption.Bottom <= titleFrame.Top && bodyCaption.Bottom <= toolbarFrame.Top, "captions do not overlap editor surfaces at current DPI");
                    ToolStripItem lastToolbarButton = managerToolbar.Items["markdown_link"];
                    Check(lastToolbarButton.Visible && lastToolbarButton.Bounds.Right <= managerToolbar.ClientSize.Width, "all Markdown toolbar buttons remain visible");
                    markdownBody.Text = "选中文字"; markdownBody.Select(0, 2); ((ToolStripButton)managerToolbar.Items["markdown_italic"]).PerformClick();
                    Check(app.Selected.Text == "*选中*文字" && markdownBody.SelectedText == "选中", "manager italic button formats current selection");
                    slider.Value = 64; app.Flush();
                    Check(app.Selected.OpacityPercent == 36 && seed.Load().Notes[0].OpacityPercent == 36, "manager slider changes and saves selected note only");
                    app.AddNote(); Check(app.Data.Notes.Count == 2, "multiple notes supported");
                    app.DeleteNote(app.Selected); Check(app.Data.Notes.Count == 1, "delete updates desktop and data");
                    app.UndoDelete(); Check(app.Data.Notes.Count == 2, "undo restores deleted note");
                    Check(slider.Value == 0 && app.Selected.OpacityPercent == 100 && app.Data.Notes[0].OpacityPercent == 36, "selection restores independent transparency values");
                    app.Manager.WindowState = FormWindowState.Minimized; app.EditNote(app.Selected);
                    Check(app.Editor != null && app.Editor.Visible, "native desktop editor opens");
                    MarkdownToolbar desktopToolbar = app.Editor.Controls.OfType<MarkdownToolbar>().Single();
                    Panel desktopHeader = app.Editor.Controls.OfType<Panel>().First(p => p.Dock == DockStyle.Top);
                    Check(desktopToolbar.Top == desktopHeader.Bottom, "desktop Markdown toolbar sits below title bar");
                    Control grip = app.Editor.Controls.Cast<Control>().First(c => c is ResizeHandle);
                    Check(app.Editor.ClientRectangle.Contains(grip.Bounds), "editor resize grip lies inside visible bounds");
                    Panel header = app.Editor.Controls.Cast<Control>().OfType<Panel>().First(p => p.Dock == DockStyle.Top);
                    Button finish = header.Controls.OfType<Button>().First();
                    Check(header.ClientRectangle.Contains(finish.Bounds), "finish editing button lies inside visible header");
                    Check(finish.Cursor == Cursors.Arrow, "desktop editor finish button keeps the standard arrow cursor");
                    app.Editor.Close(); Check(app.Editor == null, "finishing editor restores desktop layer");
                    app.Manager.Close(); Check(!app.Editing && !app.Manager.Visible && !app.Manager.IsDisposed, "closing manager locks and retains process");
                    Check(seed.Load().Notes.Count == 2, "last state saved before hiding");
                    app.OpenManager(); Check(app.Editing, "reopening unlocks editor");
                    app.Lock(); Check(!app.Editing && (Native.GetWindowLong(app.Layer.WindowFor(note.Id), Native.GWL_EXSTYLE) & Native.WS_EX_TRANSPARENT) != 0, "lock preserves passive wallpaper layer");
                }
                finally { app.Quit(); }
                NotesApp reopened = new NotesApp(life, true, true);
                try
                {
                    Check(reopened.Data.Notes.Count == 2 && reopened.Data.Notes[0].Text == "*选中*文字" && reopened.Data.Notes[0].OpacityPercent == 36, "full exit and new app instance restore toolbar edits and opacity");
                    Check(reopened.Layer.WindowFor(note.Id) != IntPtr.Zero && !reopened.Editing, "fresh background startup restores desktop window");
                }
                finally { reopened.Quit(); }
                string portable = StorageLocation.Resolve(Path.Combine(run, "portable"), false);
                Check(Path.IsPathRooted(portable) && portable == Path.Combine(run, "portable", "data"), "normal startup uses absolute executable-relative data folder");
                StorageLocation.MigrateLegacy(portable, life);
                Check(new DataStore(portable).Load().Notes.Count == 2, "old user storage migrates without losing notes");
                AppData newer = new DataStore(portable).Load(); newer.Notes[0].Title = "迁移后修改"; new DataStore(portable).Save(newer);
                StorageLocation.MigrateLegacy(portable, life);
                Check(new DataStore(portable).Load().Notes[0].Title == "迁移后修改", "subsequent startups do not overwrite portable data with legacy data");
                File.WriteAllText(Path.Combine(run, "desktop-after.txt"), Native.DescribeDesktop());
                File.WriteAllText(Path.Combine(root, "latest.txt"), "PASS: " + count + " checks\r\n" + run + "\r\n"); return 0;
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(root, "latest.txt"), "FAIL after " + count + " checks\r\n" + ex + "\r\n" + run); return 1; }
        }
    }
}
