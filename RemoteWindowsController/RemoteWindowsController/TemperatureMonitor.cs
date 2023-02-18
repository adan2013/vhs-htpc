using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using OpenHardwareMonitor.Hardware;

namespace RemoteWindowsController
{
    public class TemperatureMonitor
    {
        private const int PROBE_INTERVAL = 8000;

        public int currentCpuTemperature = 0;
        public int maxCpuTemperature = 0;
        public int minCpuTemperature = 0;
        public int currentCpuUsage = 0;

        private Timer timer;
        public delegate void DataUpdatedDelegate(TemperatureMonitor monitor);
        public event DataUpdatedDelegate DataUpdated;
        
        public TemperatureMonitor()
        {
            try
            {
                readCpuData(true);
                timer = new Timer(callback, null, 0, PROBE_INTERVAL);
            } catch
            {
                System.Diagnostics.Trace.WriteLine("Temperature monitor DISABLED");
            }
        }

        private void callback(object state)
        {
            try
            {
                readCpuData();
            }
            catch { }
        }

        public async void readCpuData(bool showDebugLog = false)
        {
            try
            {
                if (showDebugLog) System.Diagnostics.Trace.WriteLine("Reading CPU informations..." + Environment.NewLine);
                SystemInfo systemInfo = await ReadSystemInfoAsync();

                foreach (SystemInfo.CoreInfo cInfo in systemInfo.CoreInfos)
                {
                    if (showDebugLog) System.Diagnostics.Trace.WriteLine($"Name: {cInfo.Name} | Load: {cInfo.Load}% | Temp: {cInfo.Temp}C");
                    if (cInfo.Name.Contains("CPU Total") && cInfo.Load > 0)
                    {
                        int loadValue = (int)cInfo.Load;
                        if (loadValue < 0) loadValue = 0;
                        if (loadValue > 100) loadValue = 100;
                        currentCpuUsage = loadValue;
                    }
                    if (cInfo.Name.Contains("CPU Package") && cInfo.Temp > 0)
                    {
                        int tempValue = (int)cInfo.Temp;
                        if (tempValue < 0) tempValue = 0;
                        if (tempValue > 99) tempValue = 99;
                        currentCpuTemperature = tempValue;
                        maxCpuTemperature = Math.Max(maxCpuTemperature, currentCpuTemperature);
                        minCpuTemperature = Math.Min(minCpuTemperature == 0 ? 100 : minCpuTemperature, currentCpuTemperature);
                    }
                }
                DataUpdated?.Invoke(this);
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine("Error - readCpuData");
            }
        }

        private static async Task<SystemInfo> ReadSystemInfoAsync()
        {
            return await Task.Run(() =>
            {
                SystemInfo systemInfo = new SystemInfo();

                SystemVisitor updateVisitor = new SystemVisitor();
                Computer computer = new Computer();

                try
                {
                    computer.Open();
                    computer.CPUEnabled = true;

                    computer.Accept(updateVisitor);

                    foreach (IHardware hw in computer.Hardware
                        .Where(hw => hw.HardwareType == HardwareType.CPU))
                    {
                        foreach (ISensor sensor in hw.Sensors)
                        {
                            switch (sensor.SensorType)
                            {
                                case SensorType.Load:
                                    systemInfo.AddOrUpdateCoreLoad(
                                    sensor.Name, sensor.Value.GetValueOrDefault(0));

                                    break;
                                case SensorType.Temperature:
                                    systemInfo.AddOrUpdateCoreTemp(
                                    sensor.Name, sensor.Value.GetValueOrDefault(0));

                                    break;
                            }
                        }
                    }
                }
                catch
                {
                    System.Diagnostics.Trace.WriteLine("Error - read task");
                }
                finally
                {
                    computer.Close();
                }

                return systemInfo;
            });
        }
    }

    public class SystemInfo
    {
        public class CoreInfo
        {
            public string Name { get; set; }
            public double Load { get; set; }
            public double Temp { get; set; }
        }

        public List<CoreInfo> CoreInfos = new List<CoreInfo>();

        private CoreInfo GetCoreInfo(string name)
        {
            CoreInfo coreInfo = CoreInfos.SingleOrDefault(c => c.Name == name);
            if (coreInfo is null)
            {
                coreInfo = new CoreInfo { Name = name };
                CoreInfos.Add(coreInfo);
            }

            return coreInfo;
        }

        public void AddOrUpdateCoreTemp(string name, double temp)
        {
            CoreInfo coreInfo = GetCoreInfo(name);
            coreInfo.Temp = temp;
        }

        public void AddOrUpdateCoreLoad(string name, double load)
        {
            CoreInfo coreInfo = GetCoreInfo(name);
            coreInfo.Load = load;
        }
    }

    public class SystemVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) { computer.Traverse(this); }

        public void VisitHardware(IHardware hardware)
        {
            if (hardware != null)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
