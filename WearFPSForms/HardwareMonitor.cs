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

        /*static IHardware cpuHardware = null;
        static ISensor cpuTempSensor = null;
        static ISensor cpuUsageSensor = null;
        static IHardware gpuHardware = null;
        static ISensor gpuTempSensor = null;
        static ISensor gpuUsageSensor = null;
        static IHardware superIOHardware = null;
        static ISensor cpuTempIOSensor = null;
        static ISensor cpuFreqSensor = null;
        static ISensor gpuFreqSensor = null;*/

        private class cCPU
        {
            public IHardware hardware = null;
            public ISensor temp = null;
            public ISensor load = null;
            public ISensor clock = null;

            public void findSensors()
            {
                this.hardware.Update();
                foreach (var sensor in this.hardware.Sensors)
                {
                    Log.Data("CPU Sensor: " + sensor.Name + " (" + sensor.SensorType.ToString() + ")");
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        if (sensor.Name.Equals("CPU Package", StringComparison.InvariantCultureIgnoreCase)) this.temp = sensor;
                    }
                    else if (sensor.SensorType == SensorType.Load)
                    {
                        if (sensor.Name.Equals("CPU Total", StringComparison.InvariantCultureIgnoreCase)) this.load = sensor;
                    }
                    else if (sensor.SensorType == SensorType.Clock)
                    {
                        if (sensor.Index == 1) this.clock = sensor;
                    }
                }
            }
            public void update()
            {
                this.hardware.Update();
            }
        }

        private class cGPU
        {
            public IHardware hardware = null;
            public ISensor temp = null;
            public ISensor load = null;
            public ISensor clock = null;
            private bool online = false;
            public void findSensors()
            {
                this.hardware.Update();
                temp = null; load = null; clock = null;
                foreach (var sensor in this.hardware.Sensors)
                {
                    Log.Data("GPU Sensor: " + sensor.Name + " (" + sensor.SensorType.ToString() + ")");
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        this.temp = sensor;
                    }
                    else if (sensor.SensorType == SensorType.Load)
                    {
                        if (sensor.Name.Equals("GPU Core", StringComparison.InvariantCultureIgnoreCase)) this.load = sensor;
                    }
                    else if (sensor.SensorType == SensorType.Clock)
                    {
                        if (sensor.Name.Equals("GPU Core", StringComparison.InvariantCultureIgnoreCase)) this.clock = sensor;
                    }
                }
                if (temp == null || load == null || clock == null) this.online = false;
                else this.online = true;
            }
            public void update() {
                this.hardware.Update();
                if (temp == null || load == null || clock == null ||
                    !temp.Value.HasValue || !load.Value.HasValue || !clock.Value.HasValue ||
                    temp.Value <= 0 || load.Value < 0 || clock.Value <= 0)
                {
                    this.online = false;
                }
                else this.online = true;
            }
            public bool isOnline()
            {
                return this.online;
            }
        }

        static cCPU cpu = new cCPU();
        static cGPU gpu = new cGPU();

        static Computer computer = null;

        static Object updateLocker = new object();


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
            gpuMonThread.Abort();
        }

        static public void init()
        {
            Log.Info("Iniciando monitor de hardware...");

            newComputer();
            
            //int i = 0;
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

        private static void newComputer()
        {
            lock (updateLocker)
            {
                computer = new Computer()
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
                    Log.Data("Hardware item: " + item.Name + " (" + item.HardwareType.ToString() + ")");
                    if (item.HardwareType == HardwareType.CPU)
                    {
                        cpu.hardware = item;
                        cpu.findSensors();

                    }
                    if (item.HardwareType == HardwareType.GpuAti || item.HardwareType == HardwareType.GpuNvidia)
                    {
                        gpu.hardware = item;
                        gpu.findSensors();
                    }
                    /*if (item.HardwareType == HardwareType.Mainboard)
                    {
                        foreach (var sensor in item.Sensors)
                        {
                            Log.Data("Mainboard Sensor: " + sensor.Name + " (" + sensor.SensorType.ToString() + ")");
                        }
                        foreach (var sub in item.SubHardware)
                        {
                            Log.Data("Mainboard Subhardware: " + sub.Name + " (" + sub.HardwareType.ToString() + ")");
                            if (sub.HardwareType == HardwareType.SuperIO)
                            {
                                superIOHardware = sub;
                                foreach (var sensor in sub.Sensors)
                                {
                                    Log.Data(sub.HardwareType + " Sensor: " + sensor.Name + " (" + sensor.SensorType.ToString() + ")");
                                    if (sensor.SensorType == SensorType.Temperature)
                                    {
                                        if (sensor.Index == 0) cpuTempIOSensor = sensor;
                                    }
                                }
                            }
                        }
                    }*/
                }

                Log.Debug("Monitor de hardware iniciado correctamente");
                //if (!gpu.isOnline())
                //{
                    startGPUMonitorThread();
                    gpu.hardware.SensorAdded += new SensorEventHandler(sensorAdded);
                    gpu.hardware.SensorRemoved += new SensorEventHandler(sensorRemoved);
                //}
            }
        }

        private static void sensorAdded(ISensor sensor)
        {
            Log.Info("GPU Sensor added: " + sensor.Name);
            //gpu.findSensors();
            reset.Set();
        }
        private static void sensorRemoved(ISensor sensor)
        {
            Log.Info("GPU sensor removed: " + sensor.Name);
            //gpu.findSensors();
            reset.Set();
        }

        private static void hardwareAdded(IHardware hardware)
        {
            Log.Info("Hardware added: " + hardware.Name + " (" + hardware.HardwareType + ")");
            if (hardware.HardwareType == HardwareType.GpuAti || hardware.HardwareType == HardwareType.GpuNvidia)
            {
                gpu.hardware = hardware;
                gpu.update();
            }
        }

        private static void hardwareRemoved(IHardware hardware)
        {
            Log.Info("Hardware removed: " + hardware.Name + " (" + hardware.HardwareType + ")");
        }

        private static Thread gpuMonThread = null;
        private static bool runGpuMonThread = false;
        private static ManualResetEvent reset = new ManualResetEvent(false);
        private static void startGPUMonitorThread()
        {
            runGpuMonThread = true;
            gpuMonThread = new Thread(new ThreadStart(gpuMonitor));
            gpuMonThread.Start();
        }

        private static void stopGPUMonitorThread()
        {
            runGpuMonThread = false;
        }

        private static void gpuMonitor()
        {
            while (runGpuMonThread)
            {
                reset.WaitOne();
                newComputer();
                Thread.Sleep(5000);
                reset.Reset();
            }
        }

        public static void update()
        {
            lock (updateLocker)
            {
                cpu.update();
                gpu.update();
            }
            //if (!gpu.clock.Value.HasValue || (gpu.clock.Value.HasValue && gpu.clock.Value <= 0f)) startGPUMonitorThread();
            //superIOHardware.Update();
        }

        public static float CPUTemp
        {
            get
            {
                if (cpu.hardware != null && cpu.temp != null && cpu.temp.Value.HasValue) return cpu.temp.Value.Value;
                else return -1f;
            }
        }

        public static string CPUName
        {
            get { if (cpu.hardware != null) return cpu.hardware.Name; else return ""; }
        }

        public static string GPUName
        {
            get { if (gpu.hardware != null) return gpu.hardware.Name; else return ""; }
        }

        public static float CPULoad
        {
            get
            {
                if (cpu.hardware != null && cpu.load != null && cpu.load.Value.HasValue) return cpu.load.Value.Value;
                else return -1f;
            }
        }

        public static float GPUTemp
        {
            get
            {
                return (gpu.isOnline()) ? gpu.temp.Value.Value : -1f;
            }
        }

        public static float GPULoad
        {
            get
            {
                return (gpu.isOnline()) ? gpu.load.Value.Value : -1f;

            }
        }

        public static float CPUFreq
        {
            get
            {
                return (cpu.hardware != null && cpu.clock != null && cpu.clock.Value.HasValue) ? cpu.clock.Value.Value : -1f;
            }
        }

        public static float GPUFreq
        {
            get
            {
                return (gpu.isOnline()) ? gpu.clock.Value.Value : -1f;
            }
        }
    }
    
}
