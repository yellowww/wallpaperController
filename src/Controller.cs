using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;

namespace desktopWallpaperController
{
    class Controller
    {
        static void Main(string[] args)
        {
            
            WindowHandle.hideConsoleWindow();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            HardwareMonitor.init();
            Render.initiate();
            WindowHandle.getGraphicsContext();
            
            
            Render.doFrame(WindowHandle.g);

            HardwareMonitor.computer.Close();
            WindowHandle.releaseDC();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            HardwareMonitor.computer.Close();
            WindowHandle.releaseDC();
        }
    }
}
