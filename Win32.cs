using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.Samples.Kinect.HackISUName
{
    public class Win32
    {
        public const int WM_SCROLL = 276; // Horizontal scroll
        public const int WM_VSCROLL = 277; // Vertical scroll
        public const int SB_LINEUP = 0; // Scrolls one line up
        public const int SB_LINELEFT = 0;// Scrolls one cell left
        public const int SB_LINEDOWN = 1; // Scrolls one line down
        public const int SB_LINERIGHT = 1;// Scrolls one cell right
        public const int SB_PAGEUP = 2; // Scrolls one page up
        public const int SB_PAGELEFT = 2;// Scrolls one page left
        public const int SB_PAGEDOWN = 3; // Scrolls one page down
        public const int SB_PAGERIGTH = 3; // Scrolls one page right
        public const int SB_PAGETOP = 6; // Scrolls to the upper left
        public const int SB_LEFT = 6; // Scrolls to the left
        public const int SB_PAGEBOTTOM = 7; // Scrolls to the upper right
        public const int SB_RIGHT = 7; // Scrolls to the right
        public const int SB_ENDSCROLL = 8; // Ends scroll

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            SWP_ASYNCWINDOWPOS = 0x4000,
            SWP_DEFERERASE = 0x2000,
            SWP_DRAWFRAME = 0x0020,
            SWP_FRAMECHANGED = 0x0020,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOACTIVATE = 0x0010,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOMOVE = 0x0002,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOREDRAW = 0x0008,
            SWP_NOREPOSITION = 0x0200,
            SWP_NOSENDCHANGING = 0x0400,
            SWP_NOSIZE = 0x0001,
            SWP_NOZORDER = 0x0004,
            SWP_SHOWWINDOW = 0x0040,
        }

        public const int SW_MAXIMIZE = 3;
        public const int SW_MINIMIZE = 6;
        public static int SW_RESTORE = 9;

        [Flags]
        public enum MouseEventFlags
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Move = 0x00000001,
            Absolute = 0x00008000,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

        public const int VK_BROWSER_BACK        = 0xA6;
        public const int VK_BROWSER_FORWARD     = 0xA7;
        public const int VK_BROWSER_REFRESH     = 0xA8;
        public const int VK_BROWSER_STOP        = 0xA9;
        public const int VK_BROWSER_SEARCH      = 0xAA;
        public const int VK_BROWSER_FAVORITES   = 0xAB;
        public const int VK_BROWSER_HOME        = 0xAC;

        public const int VK_VOLUME_MUTE         = 0xAD;
        public const int VK_VOLUME_DOWN         = 0xAE;
        public const int VK_VOLUME_UP           = 0xAF;
        public const int VK_MEDIA_NEXT_TRACK    = 0xB0;
        public const int VK_MEDIA_PREV_TRACK    = 0xB1;
        public const int VK_MEDIA_STOP          = 0xB2;
        public const int VK_MEDIA_PLAY_PAUSE    = 0xB3;
        public const int VK_LAUNCH_MAIL         = 0xB4;
        public const int VK_LAUNCH_MEDIA_SELECT = 0xB5;
        public const int VK_LAUNCH_APP1         = 0xB6;
        public const int VK_LAUNCH_APP2 = 0xB7;
        public const int WM_APPCOMMAND = 0x0319;
        public const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        public const int APPCOMMAND_VOLUME_UP = 0xA0000;
        public const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, String lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, IntPtr prcScroll,
           IntPtr prcClip, IntPtr hrgnUpdate, IntPtr prcUpdate, uint flags);

        [DllImport("user32.dll")]
        public static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    }
}
