using System;
using System.Collections.Generic;
using System.Text;
using LibreHardwareMonitor.Hardware;

namespace desktopWallpaperController
{
    static class HardwareMonitor {


        public static Computer computer;
        static IHardware GPU;
        static IHardware RAM;
        public static void init()
        {
            computer = new Computer
            {
                IsCpuEnabled = false,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false
            };
            computer.Open();
            computer.Accept(new UpdateVisitor());           
            foreach (IHardware hardware in computer.Hardware)
            {
                if (hardware.Name.Contains("NVIDIA")) GPU = hardware;
                if (hardware.Name.Contains("Memory")) RAM = hardware;
            }

            
        }

        public static float[] GetGPU()
        {
            string[] sensorNames = new string[]
            {
                "D3D 3D",
                "Copy",
                "Cuda",
                "Video Encode",
                "Video Decode",
                "Memory Total",
                "Memory Used",
                "Package",
                "Hot Spot"
            };
            float[] sensorData = new float[] {0,0,0,0,0,0,0,0,0};
            foreach (ISensor sensor in GPU.Sensors)
            {
                for (int i = 0; i < sensorNames.Length; i++) if (sensor.Name.Contains(sensorNames[i])) sensorData[i] += (float)sensor.Value;
                
            }
            return sensorData;
        }

        public static float[] GetRAM()
        {
            string[] sensorNames = new string[]
            {
                "Memory Used",
                "Memory Available"
            };
            float[] sensorData = new float[] { 0, 0 };
            foreach (ISensor sensor in RAM.Sensors)
            {
                for (int i = 0; i < sensorNames.Length; i++) if (sensor.Name == (sensorNames[i])) sensorData[i] = (float)sensor.Value;

            }
            return sensorData;
        }



    }

    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
