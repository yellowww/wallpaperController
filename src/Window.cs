using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Management;

namespace desktopWallpaperController
{
    public static class Window
    {

        static string[] excludeProcesses = new string[]
        {
            "SystemSettings",
            "Music.UI",
            "ApplicationFrameHost",
            "TextInputHost",
            "NVIDIA Share"
        };
        [DllImport("Kernel32.DLL", CharSet = CharSet.Auto, SetLastError = true)]
        private extern static bool GetDevicePowerState( IntPtr hDevice, out bool fOn);

        public static int getDisplayStatus()
        {

            var query = "select * from WmiMonitorBasicDisplayParams";
            using (var wmiSearcher = new ManagementObjectSearcher("\\root\\wmi", query))
            {
                var results = wmiSearcher.Get();
                foreach (ManagementObject wmiObj in results)
                {
                    // get the "Active" property and cast to a boolean, which should 
                    // tell us if the display is active. I've interpreted this to mean "on"
                    var active = (Boolean)wmiObj["Active"];
                }
            }
            return 0;
        }
        public static bool checkIfVisible(IntPtr handle)
        {
            RECT wallpaperRect;
            GetWindowRect(handle, out wallpaperRect);
            List<RECT> allRects = GetAllRects(handle);
            return checkRects(allRects);
        } 
        static List<RECT> GetAllRects(IntPtr excludeHWnd)
        {
            List<Process> taskBarProcesses = Process.GetProcesses().
                                         Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                                         .ToList();
            uint[] processIds = new uint[taskBarProcesses.Count];
            for (int i = 0; i < processIds.Length; i++) processIds[i] = (uint)taskBarProcesses[i].Id;

            List<RECT> allRects = new List<RECT>();
            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                uint thisWindowPID;
                GetWindowThreadProcessId(wnd, out thisWindowPID);
                if (processIds.Contains(thisWindowPID) && IsWindowVisible(wnd) && wnd != excludeHWnd)
                {
                    int processIndex = Array.IndexOf(processIds, thisWindowPID);
                    if(taskBarProcesses[processIndex].MainWindowHandle != IntPtr.Zero && !excludeProcesses.Contains(taskBarProcesses[processIndex].ProcessName))
                    {
                        RECT thisRect;
                        //Console.WriteLine(taskBarProcesses[processIndex].ProcessName);
                        
                        GetWindowRect(wnd, out thisRect);
                        //logRect(thisRect);
                        allRects.Add(thisRect);
                    }

                }
                return true;
            }, IntPtr.Zero);

            return allRects;
        }
        static bool checkRects(List<RECT> allRects)
        {
            bool left = false;
            bool right = false;
            for(int i=0;i<allRects.Count;i++)
            {
                RECT thisRect = allRects[i];
                if (thisRect.top <=40 &&
                    thisRect.bottom >= 1030 &&
                    thisRect.left <= -1880 &&
                    thisRect.right >= -40) left = true;

                if (thisRect.top <= 40 &&
                    thisRect.bottom >= 1030 &&
                    thisRect.left <= 40 &&
                    thisRect.right >= 1880) right = true;


                

            }
            return !(left && right);
        }

        static void logRect(RECT rect)
        {
            Console.WriteLine(rect.left + " " + rect.top + " " + rect.right + " " + rect.bottom);
        }


        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr param);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, [Out] out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int GW_HWNDPREV = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}
