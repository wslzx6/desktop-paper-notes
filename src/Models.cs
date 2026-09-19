using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DesktopPaperNotes
{
    [DataContract]
    public sealed class Note
    {
        [DataMember] public string Id = Guid.NewGuid().ToString("N");
        [DataMember] public string Title = "便签";
        [DataMember] public string Text = "";
        [DataMember] public int X;
        [DataMember] public int Y;
        [DataMember] public int Width = 300;
        [DataMember] public int Height = 260;
        [DataMember] public int ColorIndex;
        [DataMember] public float FontSize = 12;
        [DataMember] public int OpacityPercent = 100;
        [DataMember] public string Monitor = "";
        [OnDeserializing]
        private void SetDefaults(StreamingContext context) { OpacityPercent = 100; }
        public byte WindowAlpha { get { return (byte)Math.Round(Math.Max(0, Math.Min(100, OpacityPercent)) * 255.0 / 100); } }
        public Rectangle Bounds { get { return new Rectangle(X, Y, Width, Height); } }
        public void Normalize()
        {
            if (String.IsNullOrEmpty(Id)) Id = Guid.NewGuid().ToString("N");
            Title = Title ?? "便签";
            Text = Text ?? "";
            Width = Math.Max(180, Math.Min(1600, Width));
            Height = Math.Max(140, Math.Min(1600, Height));
            ColorIndex = Math.Max(0, Math.Min(Palette.Colors.Length - 1, ColorIndex));
            if (Single.IsNaN(FontSize) || Single.IsInfinity(FontSize)) FontSize = 12;
            FontSize = Math.Max(9, Math.Min(28, FontSize));
            OpacityPercent = Math.Max(0, Math.Min(100, OpacityPercent));
        }
    }

    [DataContract]
    public sealed class AppData
    {
        [DataMember] public int Version = 1;
        [DataMember] public bool AutoStart = true;
        [DataMember] public List<Note> Notes = new List<Note>();
    }

    public static class Palette
    {
        public static readonly Color[] Colors = { Color.FromArgb(255, 241, 178), Color.FromArgb(212, 239, 221), Color.FromArgb(212, 231, 249), Color.FromArgb(242, 218, 233), Color.FromArgb(228, 219, 247) };
        public static readonly string[] Names = { "奶油黄", "薄荷绿", "雾蓝", "樱花粉", "浅紫" };
        public static readonly Color Ink = Color.FromArgb(34, 40, 36);
        public static readonly Color Accent = Color.FromArgb(42, 91, 69);
        public static readonly Color AccentHover = Color.FromArgb(34, 78, 58);
        public static readonly Color AccentSoft = Color.FromArgb(226, 237, 230);
        public static readonly Color Canvas = Color.FromArgb(239, 241, 237);
        public static readonly Color Surface = Color.FromArgb(252, 252, 249);
        public static readonly Color SoftSurface = Color.FromArgb(245, 247, 243);
        public static readonly Color Border = Color.FromArgb(215, 220, 214);
        public static readonly Color Muted = Color.FromArgb(102, 112, 105);
        public static readonly Color Danger = Color.FromArgb(156, 69, 63);
    }

    public sealed class DataStore
    {
        public readonly string DirectoryPath;
        public string FilePath { get { return Path.Combine(DirectoryPath, "notes.json"); } }
        public string Warning;
        public bool ReadOnly;
        private long generation;
        public DataStore(string directory) { DirectoryPath = directory; }
        public AppData Load()
        {
            Directory.CreateDirectory(DirectoryPath);
            string versions = Path.Combine(DirectoryPath, "versions");
            if (Directory.Exists(versions))
            {
                bool skipped = false;
                foreach (string candidate in Directory.GetFiles(versions, "*.json").OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal))
                {
                    long number; if (Int64.TryParse(Path.GetFileName(candidate).Substring(0, Math.Min(20, Path.GetFileName(candidate).Length)), out number)) generation = Math.Max(generation, number);
                    try
                    {
                        AppData committed = Read(candidate);
                        if (skipped) Warning = "已从上一次完整保存恢复。损坏历史文件已保留。";
                        if (File.Exists(FilePath))
                            try { Read(FilePath); }
                            catch (Exception ex) { if (!(ex is NotSupportedException)) { PreserveDamaged(); Warning = "主文件损坏，已恢复最后一次完整保存。"; } }
                        return committed;
                    }
                    catch (NotSupportedException ex) { ReadOnly = true; Warning = ex.Message; return new AppData(); }
                    catch (Exception ex) { skipped = true; Log("历史保存无法读取", ex); }
                }
            }
            if (!File.Exists(FilePath)) return new AppData();
            try { return Read(FilePath); }
            catch (Exception ex)
            {
                if (ex is NotSupportedException) { ReadOnly = true; Warning = ex.Message; return new AppData(); }
                Log("读取数据失败", ex);
                PreserveDamaged();
                try { AppData restored = Read(FilePath + ".bak"); Warning = "已从上一次备份恢复。损坏文件已保留。"; return restored; }
                catch { ReadOnly = true; Warning = "数据文件无法读取。原文件已保留，请先导入有效备份；为避免覆盖，暂停自动保存。"; return new AppData(); }
            }
        }
        public static AppData Read(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                AppData data = (AppData)new DataContractJsonSerializer(typeof(AppData)).ReadObject(stream);
                if (data == null) throw new InvalidDataException("文件中没有便签数据。");
                if (data.Version > 1) throw new NotSupportedException("数据来自更新版本的软件，已暂停保存，避免覆盖。");
                data.Notes = data.Notes ?? new List<Note>();
                data.Notes.RemoveAll(n => n == null);
                foreach (Note note in data.Notes) note.Normalize();
                HashSet<string> ids = new HashSet<string>();
                foreach (Note note in data.Notes) { if (!ids.Add(note.Id)) { note.Id = Guid.NewGuid().ToString("N"); ids.Add(note.Id); } }
                return data;
            }
        }
        public void Save(AppData data)
        {
            if (ReadOnly) throw new IOException("自动保存已暂停，请先导入有效备份。");
            string versions = Path.Combine(DirectoryPath, "versions"); Directory.CreateDirectory(versions);
            generation = Math.Max(DateTime.UtcNow.Ticks, generation + 1);
            string snapshot = Path.Combine(versions, generation.ToString("D20") + "-" + Guid.NewGuid().ToString("N") + ".json");
            try { WriteFlushed(snapshot, Serialize(data), FileMode.CreateNew); }
            catch { try { File.Delete(snapshot); } catch { } throw; }
            // Immutable flushed revisions are authoritative. Updating legacy files
            // is secondary and does not require rename/replace support (EFS and
            // projected filesystems can reject ReplaceFile/MoveFileEx).
            try
            {
                if (File.Exists(FilePath))
                {
                    try { Read(FilePath); WriteFlushed(FilePath + ".bak", File.ReadAllBytes(FilePath), FileMode.Create); } catch { }
                }
                WriteFlushed(FilePath, File.ReadAllBytes(snapshot), FileMode.Create);
            }
            catch (Exception ex) { Log("兼容数据副本更新失败，完整保存仍在 versions 中", ex); }
            string[] revisions = Directory.GetFiles(versions, "*.json").OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal).ToArray();
            foreach (string old in revisions.Skip(30)) { try { Read(old); File.Delete(old); } catch { } }
        }
        private void PreserveDamaged()
        { try { File.WriteAllBytes(FilePath + ".damaged-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), File.ReadAllBytes(FilePath)); } catch { } }
        private static byte[] Serialize(AppData data)
        { using (MemoryStream stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(AppData)).WriteObject(stream, data); return stream.ToArray(); } }
        private static void WriteFlushed(string path, byte[] bytes, FileMode mode)
        { using (FileStream stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); } }
        public static void WriteAtomic(string path, AppData data)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(dir);
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            string backupTemp = temp + ".bak";
            try
            {
                using (FileStream stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    new DataContractJsonSerializer(typeof(AppData)).WriteObject(stream, data);
                    stream.Flush(true);
                }
                if (File.Exists(path))
                {
                    // Keep the old primary intact until both the flushed backup
                    // and replacement are ready. ReplaceFile can fail on EFS files.
                    File.Copy(path, backupTemp, false);
                    using (FileStream backup = new FileStream(backupTemp, FileMode.Open, FileAccess.Write, FileShare.None)) backup.Flush(true);
                    RenameAtomic(backupTemp, path + ".bak");
                }
                RenameAtomic(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); if (File.Exists(backupTemp)) File.Delete(backupTemp); }
        }
        private static void RenameAtomic(string temp, string destination)
        {
            if (!Native.MoveFileEx(temp, destination, 0x9))
                throw new IOException("无法提交便签文件。", new Win32Exception(Marshal.GetLastWin32Error()));
        }
        public void Log(string message, Exception exception)
        {
            try { File.AppendAllText(Path.Combine(DirectoryPath, "app.log"), DateTime.Now.ToString("s") + " " + message + " " + exception + Environment.NewLine); } catch { }
        }
    }

    public static class Placement
    {
        public static bool Restore(Note note, Rectangle[] areas)
        {
            note.Normalize();
            if (areas.Length == 0) return false;
            Rectangle best = areas.OrderByDescending(a => { Rectangle r = Rectangle.Intersect(a, note.Bounds); return (long)r.Width * r.Height; }).First();
            int oldX = note.X, oldY = note.Y, oldW = note.Width, oldH = note.Height;
            note.Width = Math.Min(note.Width, best.Width);
            note.Height = Math.Min(note.Height, best.Height);
            note.X = Math.Max(best.Left, Math.Min(best.Right - note.Width, note.X));
            note.Y = Math.Max(best.Top, Math.Min(best.Bottom - note.Height, note.Y));
            return note.X != oldX || note.Y != oldY || note.Width != oldW || note.Height != oldH;
        }
    }
}
