using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DesktopPaperNotes
{
    public static class Native
    {
        public const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
        public const int WS_CHILD = 0x40000000, WS_POPUP = unchecked((int)0x80000000);
        public const int WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20, WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOOLWINDOW = 0x80;
        public const int WM_MOUSEMOVE = 0x200, WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202, WM_RBUTTONDOWN = 0x204, WM_RBUTTONUP = 0x205;
        public const int SWP_NOACTIVATE = 0x10, SWP_NOZORDER = 4, SWP_FRAMECHANGED = 0x20;
        public static readonly IntPtr HWND_TOP = IntPtr.Zero;
        public delegate bool EnumProc(IntPtr window, IntPtr data);
        public delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } public Point ToPoint() { return new Point(X, Y); } }
        [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int Width, Height; public SIZE(int w, int h) { Width = w; Height = h; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
        [StructLayout(LayoutKind.Sequential)] public struct MSLLHOOKSTRUCT { public POINT Pt; public uint MouseData, Flags, Time; public UIntPtr ExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; public override string ToString() { return Left + "," + Top + " " + (Right - Left) + "x" + (Bottom - Top); } }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string className, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr data);
        [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr window);
        [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr window, uint flags);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr window, uint command);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out RECT rect);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll", SetLastError = true)] public static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SendMessageTimeout(IntPtr window, uint message, IntPtr wp, IntPtr lp, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint message, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern uint RegisterWindowMessage(string message);
        [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr window, ref POINT point);
        [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destDc, ref POINT dest, ref SIZE size, IntPtr sourceDc, ref POINT source, uint colorKey, ref BLENDFUNCTION blend, uint flags);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool SetLayeredWindowAttributes(IntPtr window, uint colorKey, byte alpha, uint flags);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool GetLayeredWindowAttributes(IntPtr window, out uint colorKey, out byte alpha, out uint flags);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern bool MoveFileEx(string existing, string replacement, uint flags);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern IntPtr OpenProcess(uint access, bool inherit, uint id);
        [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll")] public static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, UIntPtr size, uint type, uint protect);
        [DllImport("kernel32.dll")] public static extern bool VirtualFreeEx(IntPtr process, IntPtr address, UIntPtr size, uint type);
        [DllImport("kernel32.dll")] public static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] bytes, UIntPtr size, out UIntPtr written);

        public static string Class(IntPtr window) { StringBuilder s = new StringBuilder(256); GetClassName(window, s, s.Capacity); return s.ToString(); }
        public static IntPtr ShellView
        {
            get
            {
                IntPtr view = IntPtr.Zero;
                EnumWindows(delegate(IntPtr w, IntPtr d) { IntPtr v = FindWindowEx(w, IntPtr.Zero, "SHELLDLL_DefView", null); if (v != IntPtr.Zero) { view = v; return false; } return true; }, IntPtr.Zero);
                return view;
            }
        }
        private static IntPtr ExistingDesktopHost()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr child = IntPtr.Zero;
            while ((child = FindWindowEx(progman, child, "WorkerW", null)) != IntPtr.Zero)
                if (IsWindowVisible(child) && FindWindowEx(child, IntPtr.Zero, "SHELLDLL_DefView", null) == IntPtr.Zero) return child;
            IntPtr view = ShellView;
            if (view == IntPtr.Zero) return IntPtr.Zero;
            IntPtr owner = GetParent(view);
            // Older Windows versions use a top-level WorkerW immediately below the icon host.
            IntPtr next = GetWindow(owner, 2);
            while (next != IntPtr.Zero)
            {
                if (Class(next) == "WorkerW" && IsWindowVisible(next) && FindWindowEx(next, IntPtr.Zero, "SHELLDLL_DefView", null) == IntPtr.Zero) return next;
                next = GetWindow(next, 2);
            }
            return IntPtr.Zero;
        }
        public static IntPtr FindDesktopHost(bool create)
        {
            IntPtr host = ExistingDesktopHost();
            if (host != IntPtr.Zero || !create) return host;
            IntPtr progman = FindWindow("Progman", null), result;
            if (progman == IntPtr.Zero) return IntPtr.Zero;
            SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), IntPtr.Zero, 2, 500, out result);
            SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), new IntPtr(1), 2, 500, out result);
            host = ExistingDesktopHost();
            if (host == IntPtr.Zero) { SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 2, 500, out result); host = ExistingDesktopHost(); }
            return host;
        }
        public static IntPtr FindNoteHost(bool create)
        {
            IntPtr view = ShellView;
            if (view != IntPtr.Zero)
            {
                IntPtr icons = GetParent(view);
                // Windows 11's wallpaper WorkerW can be presented as one compositor
                // surface (e.g. Wallpaper Engine). Its child layered HWNDs are not
                // necessarily composed. Use an independent sibling under DefView.
                if (Class(icons) == "Progman" && FindWindowEx(icons, IntPtr.Zero, "WorkerW", null) != IntPtr.Zero) return icons;
            }
            return FindDesktopHost(create);
        }
        public static void OrderNoteLayer(IntPtr hwnd, IntPtr host)
        {
            IntPtr view = ShellView;
            if (view != IntPtr.Zero && GetParent(view) == host)
                SetWindowPos(hwnd, view, 0, 0, 0, 0, 0x13); // NOACTIVATE | NOSIZE | NOMOVE
            else SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x13);
        }
        public static bool IsDesktopPoint(Point point)
        {
            IntPtr window = WindowFromPoint(new POINT(point.X, point.Y));
            IntPtr root = GetAncestor(window, 2);
            string type = Class(root);
            if (type != "Progman" && type != "WorkerW") return false;
            IntPtr view = ShellView;
            return view != IntPtr.Zero && root == GetAncestor(view, 2);
        }
        public static string DescribeDesktop()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("OS: " + Environment.OSVersion + " / process x64: " + Environment.Is64BitProcess);
            foreach (Screen screen in Screen.AllScreens) output.AppendLine(screen.DeviceName + " bounds=" + screen.Bounds + " work=" + screen.WorkingArea);
            EnumWindows(delegate(IntPtr window, IntPtr data)
            {
                if (Class(window) == "Progman" || Class(window) == "WorkerW")
                {
                    output.AppendLine(DescribeWindow(window));
                    EnumChildWindows(window, delegate(IntPtr child, IntPtr unused) { output.AppendLine("  " + DescribeWindow(child)); return true; }, IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);
            output.AppendLine("Existing host: 0x" + FindDesktopHost(false).ToInt64().ToString("X"));
            return output.ToString();
        }
        private static string DescribeWindow(IntPtr window)
        {
            RECT r; GetWindowRect(window, out r);
            return "0x" + window.ToInt64().ToString("X") + " " + Class(window) + " parent=0x" + GetParent(window).ToInt64().ToString("X") + " visible=" + IsWindowVisible(window) + " style=" + GetWindowLong(window, GWL_STYLE).ToString("X") + " ex=" + GetWindowLong(window, GWL_EXSTYLE).ToString("X") + " bounds=" + r;
        }
    }

    // LVM_HITTEST takes a pointer in Explorer's address space. Only a small temporary
    // LVHITTESTINFO buffer is allocated; no desktop items or Explorer settings change.
    public sealed class DesktopIconHitTest : IDisposable
    {
        private IntPtr list, process, buffer;
        public bool IsIcon(Point screenPoint)
        {
            IntPtr view = Native.ShellView;
            IntPtr current = Native.FindWindowEx(view, IntPtr.Zero, "SysListView32", null);
            if (current == IntPtr.Zero) return true; // Fail closed: never swallow unknown desktop input.
            if (current != list || process == IntPtr.Zero)
            {
                Dispose(); list = current;
                uint id; Native.GetWindowThreadProcessId(list, out id);
                process = Native.OpenProcess(0x28, false, id);
                if (process == IntPtr.Zero) return true;
                buffer = Native.VirtualAllocEx(process, IntPtr.Zero, new UIntPtr(24), 0x3000, 4);
                if (buffer == IntPtr.Zero) { Dispose(); return true; }
            }
            Native.POINT p = new Native.POINT(screenPoint.X, screenPoint.Y);
            Native.ScreenToClient(list, ref p);
            byte[] bytes = new byte[24];
            Array.Copy(BitConverter.GetBytes(p.X), 0, bytes, 0, 4);
            Array.Copy(BitConverter.GetBytes(p.Y), 0, bytes, 4, 4);
            UIntPtr written;
            if (!Native.WriteProcessMemory(process, buffer, bytes, new UIntPtr(24), out written)) { Dispose(); return true; }
            IntPtr result;
            if (Native.SendMessageTimeout(list, 0x1012, IntPtr.Zero, buffer, 2, 35, out result) == IntPtr.Zero) return true;
            return result.ToInt64() >= 0;
        }
        public void Dispose()
        {
            if (buffer != IntPtr.Zero && process != IntPtr.Zero) Native.VirtualFreeEx(process, buffer, UIntPtr.Zero, 0x8000);
            if (process != IntPtr.Zero) Native.CloseHandle(process);
            buffer = process = list = IntPtr.Zero;
        }
    }
}
