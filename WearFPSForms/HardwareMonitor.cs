using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenHardwareMonitor.Hardware;
using System.Threading;

namespace WearFPSForms
{
    static class HardwareMonitor
    {
        static Thread t;
        //static volatile bool mRun;

        static IHardware cpuHardware = null;
        static ISensor cpuTempSensor = null;
        static ISensor cpuUsageSensor = null;
        static IHardware gpuHardware = null;
        static ISensor gpuTempSensor = null;
        static ISensor gpuUsageSensor = null;
        static IHardware superIOHardware = null;
        static ISensor cpuTempIOSensor = null;
        static ISensor cpuFreqSensor = null;
        static ISensor gpuFreqSensor = null;



        static public void initThreaded()
        {
            //mRun = true;
            t = new Thread(new ThreadStart(() =>
            {
                init();
            }));

            t.Start();

        }

        static public void stopThreaded()
        {
            //mRun = false;
        }

        static public void init()
        {
            Log.Info("Iniciando monitor de hardware...");

            //int i = 0;
            Computer computer = new Computer()
            {
                CPUEnabled = true,
                GPUEnabled = true,
                //RAMEnabled = true,
                MainboardEnabled = true/*,
                FanControllerEnabled = true,
                HDDEnabled = true*/
            };
            computer.Open();

            foreach (var hardware in computer.Hardware)
            {
                hardware.Update();
                foreach (var sub in hardware.SubHardware)
                {
                    sub.Update();
                }
            }


            foreach (var item in computer.Hardware)
            {
                if (item.HardwareType == HardwareType.CPU)
                {
                    cpuHardware = item;
                    foreach (var sensor in item.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            if (sensor.Index == 4) cpuTempSensor = sensor;
                        }
                        else if (sensor.SensorType == SensorType.Load)
                        {
                            if (sensor.Index == 0) cpuUsageSensor = sensor;
                        } else if (sensor.SensorType == SensorType.Clock)
                        {
                            if (sensor.Index == 1) cpuFreqSensor = sensor;
                        }
                    }
                }
                if (item.HardwareType == HardwareType.GpuAti || item.HardwareType == HardwareType.GpuNvidia)
                {
                    gpuHardware = item;
                    foreach (var sensor in item.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            gpuTempSensor = sensor;
                        }
                        else if (sensor.SensorType == SensorType.Load)
                        {
                            if (sensor.Index == 0) gpuUsageSensor = sensor;
                        } else if (sensor.SensorType == SensorType.Clock)
                        {
                            if (sensor.Index == 0) gpuFreqSensor = sensor;
                        }
                    }
                }
                if (item.HardwareType == HardwareType.Mainboard)
                {
                    foreach (var sub in item.SubHardware)
                    {
                        if (sub.HardwareType == HardwareType.SuperIO)
                        {
                            superIOHardware = sub;
                            foreach (var sensor in sub.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature)
                                {
                                    if (sensor.Index == 0) cpuTempIOSensor = sensor;
                                }
                            }
                        }
                    }
                }
            }

            Log.Debug("Monitor de hardware iniciado correctamente");

            /*Log.Debug(cpuHardware.ToString());
            Log.Debug(gpuHardware.ToString());
            Log.Debug(superIOHardware.ToString());
            Log.Debug(cpuTempIOSensor.ToString());
            Log.Debug(gpuTempSensor.ToString());
            Log.Debug(cpuUsageSensor.ToString());
            Log.Debug(gpuUsageSensor.ToString());
            Log.Debug(cpuTempSensor.ToString());*/

            Program.componentLoaded();

        }

        public static void update()
        {
            cpuHardware.Update();
            gpuHardware.Update();
            //superIOHardware.Update();
        }

        public static float CPUTemp
        {
            get
            {
                if (cpuTempSensor.Value.HasValue) return cpuTempSensor.Value.Value;
                else return -1f;
            }
        }

        public static string CPUName
        {
            get { return cpuHardware.Name; }
        }

        public static string GPUName
        {
            get { return gpuHardware.Name; }
        }

        public static float CPULoad
        {
            get
            {
                if (cpuUsageSensor.Value.HasValue) return cpuUsageSensor.Value.Value;
                else return -1f;
            }
        }

        public static float GPUTemp
        {
            get
            {
                if (gpuTempSensor.Value.HasValue) return gpuTempSensor.Value.Value;
                else return -1f;
            }
        }

        public static float GPULoad
        {
            get
            {
                if (gpuUsageSensor.Value.HasValue) return gpuUsageSensor.Value.Value;
                else return -1f;
            }
        }

        public static float CPUFreq
        {
            get
            {
                return (cpuFreqSensor.Value.HasValue) ? cpuFreqSensor.Value.Value : -1f;
            }
        }

        public static float GPUFreq
        {
            get
            {
                return (gpuFreqSensor.Value.HasValue) ? gpuFreqSensor.Value.Value : -1f;
            }
        }
    }
    
}
