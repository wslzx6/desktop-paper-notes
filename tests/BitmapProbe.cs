using System;
using System.Drawing;
using System.Runtime.InteropServices;
using DesktopPaperNotes;
class BitmapProbe
{
    [StructLayout(LayoutKind.Sequential)] struct NBitmap { public int Type, Width, Height, RowBytes; public short Planes, Bpp; public IntPtr Bits; }
    [DllImport("gdi32.dll")] static extern int GetObject(IntPtr h, int size, out NBitmap b);
    [STAThread] static void Main()
    {
        using (Bitmap b = NoteRenderer.Render(new Note { Text = "Alpha probe", Width = 450, Height = 390 }, false, false))
        {
            IntPtr h = b.GetHbitmap(Color.FromArgb(0));
            try
            {
                NBitmap nb; GetObject(h, Marshal.SizeOf(typeof(NBitmap)), out nb);
                Console.WriteLine("GDI+ body ARGB=" + b.GetPixel(30,80).ToArgb().ToString("X"));
                Console.WriteLine("HBITMAP=" + nb.Width + "x" + nb.Height + " stride=" + nb.RowBytes + " bpp=" + nb.Bpp + " bits=" + nb.Bits);
                if (nb.Bits != IntPtr.Zero)
                {
                    Console.WriteLine("Native top body ARGB=" + Marshal.ReadInt32(nb.Bits, 80 * nb.RowBytes + 30 * 4).ToString("X"));
                    Console.WriteLine("Native bottom body ARGB=" + Marshal.ReadInt32(nb.Bits, (nb.Height-1-80) * nb.RowBytes + 30 * 4).ToString("X"));
                }
            }
            finally { Native.DeleteObject(h); }
        }
    }
}
