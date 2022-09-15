using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Identifer
{
    public class ScreenHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetDesktopWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        public static DirectBitmap CaptureDesktop()
        {
            return CaptureWindow(GetDesktopWindow(), out _);
        }

        public static DirectBitmap CaptureActiveWindow(out Point position)
        {
            return CaptureWindow(GetForegroundWindow(), out position);
        }

        public static DirectBitmap CaptureWindow(IntPtr handle, out Point position)
        {
            var rect = new Rect();
            GetWindowRect(handle, ref rect);
            var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            if (bounds.Width == 0 || bounds.Height == 0)
            {
                position = Point.Empty;
                return null;
            }
            var result = new DirectBitmap(bounds.Width, bounds.Height);

            position = new Point(bounds.Left, bounds.Top);
            using (var graphics = Graphics.FromImage(result.Bitmap))
            {
                graphics.CopyFromScreen(position, Point.Empty, bounds.Size);
            }

            return result;
        }
    }
}
