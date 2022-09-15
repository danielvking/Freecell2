using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Identifer
{
    public class ClickHelper
    {

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

#pragma warning disable 649
        internal struct INPUT
        {
            public UInt32 Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        internal struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }

#pragma warning restore 649

        public enum MouseButton
        {
            LEFT = 0,
            RIGHT = 1,
            MIDDLE = 2
        }

        private static readonly uint[] _mouseDown = new uint[] { 0x0002, 0x0008, 0x0020 };
        private static readonly uint[] _mouseUp = new uint[] { 0x0004, 0x0010, 0x0040 };


        public static void Click(MouseButton button = MouseButton.LEFT)
        {
            Hold(button);
            Release(button);
        }

        public static async Task Click(int milliseconds, MouseButton button = MouseButton.LEFT)
        {
            Hold(button);
            await Task.Delay(milliseconds);
            Release(button);
        }

        public static void Hold(MouseButton button = MouseButton.LEFT)
        {
            var inputMouseDown = new INPUT();
            inputMouseDown.Type = 0; /// input type mouse
            inputMouseDown.Data.Mouse.Flags = _mouseDown[(int)button];

            var inputs = new INPUT[] { inputMouseDown };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void Release(MouseButton button = MouseButton.LEFT)
        {
            var inputMouseUp = new INPUT();
            inputMouseUp.Type = 0; /// input type mouse
            inputMouseUp.Data.Mouse.Flags = _mouseUp[(int)button];

            var inputs = new INPUT[] { inputMouseUp };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static async Task<bool> MoveMouse(Point pos, int milliseconds, double steepness = 1.0)
        {
            var screenX = GetSystemMetrics(0);
            var screenY = GetSystemMetrics(1);

            void Move(Point pos)
            {
                var inputMouseMove = new INPUT();
                inputMouseMove.Type = 0; /// input type mouse
                inputMouseMove.Data.Mouse.Flags = 0x8000 | 0x0001; /// move absolute
                inputMouseMove.Data.Mouse.MouseData = 0;
                inputMouseMove.Data.Mouse.X = (pos.X << 16) / screenX;
                inputMouseMove.Data.Mouse.Y = (pos.Y << 16) / screenY;
                var inputs = new INPUT[] { inputMouseMove };
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            }

            GetCursorPos(out var startPos);

            var dt = 5;
            var dx = 1.0 * (pos.X - startPos.X) / milliseconds;
            var dy = 1.0 * (pos.Y - startPos.Y) / milliseconds;
            for (var t = 0;  t < milliseconds; t += dt)
            {
                var tNorm = 1.0 * t / milliseconds;
                var sign = tNorm < 0.5 ? 1 : -1;
                tNorm = (1 - sign * Math.Pow(Math.Abs(Math.Cos(Math.PI * tNorm)), 1 / steepness)) / 2;
                var tMod = tNorm * milliseconds;
                var newPos = new Point((int)(tMod * dx + startPos.X), (int)(tMod * dy + startPos.Y));
                Move(newPos);
                GetCursorPos(out newPos);
                await Task.Delay(Math.Min(dt, milliseconds - t));

                GetCursorPos(out var nowPos);
                if (nowPos != newPos) return false; // The user moved the mouse, abort
            }

            Move(pos);
            return true;
        }

        public static async Task<bool> Delay(int milliseconds)
        {
            GetCursorPos(out var startPos);
            await Task.Delay(milliseconds);
            GetCursorPos(out var newPos);
            return startPos == newPos;
        }
    }
}
