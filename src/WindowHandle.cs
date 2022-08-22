using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace desktopWallpaperController
{
    static class WindowHandle
    {
        public static Graphics g;
        static IntPtr workerw = IntPtr.Zero;
        static IntPtr dc;

        // define Enums and delegates
        [Flags]
        enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8,
            SMTO_ERRORONEXIT = 0x20
        }

        [Flags()]
        protected enum DeviceContextValues : uint
        {
            Window = 0x00000001,
            Cache = 0x00000002,
            NoResetAttrs = 0x00000004,
            ClipChildren = 0x00000008,
            ClipSiblings = 0x00000010,
            ParentClip = 0x00000020,
            ExcludeRgn = 0x00000040,
            IntersectRgn = 0x00000080,
            ExcludeUpdate = 0x00000100,
            IntersectUpdate = 0x00000200,
            LockWindowUpdate = 0x00000400,
            Validate = 0x00200000,
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // import user32
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr SendMessageTimeout(IntPtr windowHandle, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags flags, uint timeout, out IntPtr result);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        static extern IntPtr GetDCEx(IntPtr hWnd, IntPtr hrgnClip, DeviceContextValues flags);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        public static void getGraphicsContext()
        {
            IntPtr progmanHandle = FindWindow("Progman", null);

            IntPtr result = IntPtr.Zero;
            SendMessageTimeout(progmanHandle,
                       0x052C,
                       new IntPtr(0),
                       IntPtr.Zero,
                       SendMessageTimeoutFlags.SMTO_NORMAL,
                       1000,
                       out result);

            
            EnumWindows(new EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = FindWindowEx(tophandle,
                                            IntPtr.Zero,
                                            "SHELLDLL_DefView",
                                            null);

                if (p != IntPtr.Zero)
                {
                    // Gets the WorkerW Window after the current one.
                    workerw = FindWindowEx(IntPtr.Zero,
                                               tophandle,
                                               "WorkerW",
                                               null);
                }

                return true;
            }), IntPtr.Zero);

            dc = GetDCEx(workerw, IntPtr.Zero, (DeviceContextValues)0x403);
            if (dc != IntPtr.Zero)
            {
                // Create a Graphics instance from the Device Context
                g = Graphics.FromHdc(dc);
            }
        }

        public static void hideConsoleWindow()
        {
            const int SW_HIDE = 0;
            const int SW_SHOW = 5;
            IntPtr consoleHandle = GetConsoleWindow();
            ShowWindow(consoleHandle, SW_HIDE);
        }

        public static bool checkVisibility()
        {
            return Window.checkIfVisible(workerw);
        }

        public static void releaseDC()
        {
            ReleaseDC(workerw, dc);
        }



    }
}
