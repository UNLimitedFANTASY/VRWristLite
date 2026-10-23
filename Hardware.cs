using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace WristLite
{
    internal sealed class HardwareProbe : IDisposable
    {
        private bool nvmlReady;
        private IntPtr gpuHandle;
        private bool nvmlTried;
        private readonly List<PerformanceCounter> gpuCounters = new List<PerformanceCounter>();
        private DateTime nextCounterScan = DateTime.MinValue;
        private DateTime nextSensorScan = DateTime.MinValue;
        private double? cpuTemperature;

        public void Read(PanelData data)
        {
            var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf(typeof(MemoryStatus)) };
            if (GlobalMemoryStatusEx(ref memory)) data.Ram = memory.MemoryLoad;
            data.CpuClock = ReadCpuClock();
            ReadNvidia(data);
            if (!data.Gpu.HasValue) data.Gpu = ReadWindowsGpu();
            if (DateTime.UtcNow >= nextSensorScan)
            {
                nextSensorScan = DateTime.UtcNow.AddSeconds(10);
                cpuTemperature = ReadCpuSensor();
            }
            data.CpuTemperature = cpuTemperature;
        }

        private static double? ReadCpuClock()
        {
            int count = Environment.ProcessorCount;
            int size = Marshal.SizeOf(typeof(ProcessorPower));
            IntPtr buffer = Marshal.AllocHGlobal(checked(count * size));
            try
            {
                if (CallNtPowerInformation(11, IntPtr.Zero, 0, buffer, count * size) != 0)
                    return null;
                double sum = 0;
                for (int i = 0; i < count; i++)
                {
                    var processor = (ProcessorPower)Marshal.PtrToStructure(
                        IntPtr.Add(buffer, i * size), typeof(ProcessorPower));
                    sum += processor.CurrentMhz;
                }
                return count == 0 ? (double?)null : sum / count;
            }
            catch { return null; }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private void ReadNvidia(PanelData data)
        {
            if (!nvmlTried)
            {
                nvmlTried = true;
                try
                {
                    nvmlReady = NvmlInit() == 0 && NvmlGetDevice(0, out gpuHandle) == 0;
                    if (!nvmlReady) { try { NvmlShutdown(); } catch { } }
                }
                catch (DllNotFoundException) { nvmlReady = false; }
                catch (EntryPointNotFoundException) { nvmlReady = false; }
            }
            if (!nvmlReady) return;
            try
            {
                NvmlUtilization utilization;
                uint value;
                if (NvmlGetUtilization(gpuHandle, out utilization) == 0)
                    data.Gpu = utilization.Gpu;
                if (NvmlGetClock(gpuHandle, 0, out value) == 0)
                    data.GpuClock = value;
                if (NvmlGetTemperature(gpuHandle, 0, out value) == 0)
                    data.GpuTemperature = value;
            }
            catch (DllNotFoundException) { nvmlReady = false; }
            catch (EntryPointNotFoundException) { nvmlReady = false; }
        }

        private double? ReadWindowsGpu()
        {
            if (DateTime.UtcNow >= nextCounterScan)
            {
                nextCounterScan = DateTime.UtcNow.AddSeconds(30);
                foreach (var counter in gpuCounters) counter.Dispose();
                gpuCounters.Clear();
                try
                {
                    if (PerformanceCounterCategory.Exists("GPU Engine"))
                    {
                        var category = new PerformanceCounterCategory("GPU Engine");
                        foreach (string instance in category.GetInstanceNames()
                            .Where(n => n.IndexOf("engtype_3D", StringComparison.OrdinalIgnoreCase) >= 0)
                            .Take(96))
                        {
                            var counter = new PerformanceCounter("GPU Engine",
                                "Utilization Percentage", instance, true);
                            counter.NextValue();
                            gpuCounters.Add(counter);
                        }
                    }
                }
                catch { foreach (var counter in gpuCounters) counter.Dispose(); gpuCounters.Clear(); }
            }
            if (gpuCounters.Count == 0) return null;
            double sum = 0;
            try
            {
                foreach (var counter in gpuCounters) sum += counter.NextValue();
                return Math.Max(0, Math.Min(100, sum));
            }
            catch { return null; }
        }

        private static double? ReadCpuSensor()
        {
            // Optional: Libre Hardware Monitor publishes CPU package temperatures via WMI.
            try
            {
                Process[] monitors = Process.GetProcessesByName("LibreHardwareMonitor");
                bool running = monitors.Length > 0;
                foreach (Process monitor in monitors) monitor.Dispose();
                if (!running) return null;
                using (var searcher = new ManagementObjectSearcher(
                    @"\\.\root\LibreHardwareMonitor", "SELECT Name, SensorType, Value FROM Sensor"))
                using (var sensors = searcher.Get())
                {
                    double? best = null;
                    foreach (ManagementObject sensor in sensors)
                    {
                        using (sensor)
                        {
                            string name = Convert.ToString(sensor["Name"]);
                            string type = Convert.ToString(sensor["SensorType"]);
                            if (type != "Temperature" ||
                                !(name.IndexOf("CPU Package", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  name.IndexOf("CPU CCD", StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                            double value = Convert.ToDouble(sensor["Value"]);
                            if (value > 0 && value < 130) best = !best.HasValue ? value : Math.Max(best.Value, value);
                        }
                    }
                    return best;
                }
            }
            catch { return null; }
        }

        public void Dispose()
        {
            foreach (var counter in gpuCounters) counter.Dispose();
            gpuCounters.Clear();
            if (nvmlReady) { try { NvmlShutdown(); } catch { } }
            nvmlReady = false;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatus
        {
            public uint Length, MemoryLoad;
            public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile;
            public ulong TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessorPower
        {
            public uint Number, MaxMhz, CurrentMhz, MhzLimit, MaxIdleState, CurrentIdleState;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct NvmlUtilization { public uint Gpu, Memory; }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
        [DllImport("powrprof.dll")]
        private static extern int CallNtPowerInformation(int informationLevel, IntPtr input,
            int inputLength, IntPtr output, int outputLength);
        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlInit();
        [DllImport("nvml.dll", EntryPoint = "nvmlShutdown", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlShutdown();
        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlGetDevice(uint index, out IntPtr device);
        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlGetUtilization(IntPtr device, out NvmlUtilization rates);
        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetClockInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlGetClock(IntPtr device, uint clockType, out uint mhz);
        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlGetTemperature(IntPtr device, uint sensorType, out uint celsius);
    }
}
