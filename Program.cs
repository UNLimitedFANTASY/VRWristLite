// WristLite: a small, independent OpenVR wrist panel.
// The OpenVR binding in OpenVR.cs is generated Valve code; see LICENSE-OpenVR.txt.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using Valve.VR;

namespace WristLite
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WristLite");
            Directory.CreateDirectory(dataDir);
            Settings settings = Settings.Read(Path.Combine(dataDir, "settings.ini"));

            if (args.Any(a => a.Equals("--preview", StringComparison.OrdinalIgnoreCase)
                || a.Equals("--preview-classic", StringComparison.OrdinalIgnoreCase)
                || a.Equals("--preview-vrcx", StringComparison.OrdinalIgnoreCase)))
            {
                if (args.Any(a => a.Equals("--preview-classic", StringComparison.OrdinalIgnoreCase)))
                    settings.Appearance = "classic";
                if (args.Any(a => a.Equals("--preview-vrcx", StringComparison.OrdinalIgnoreCase)))
                    settings.Appearance = "vrcx";
                var sample = new PanelData
                {
                    Clock = DateTime.Now.ToString("HH:mm"),
                    Date = DateTime.Now.ToString("yyyy-MM-dd"),
                    Cpu = 24,
                    Ram = 58,
                    Gpu = 68,
                    CpuClock = 4200,
                    GpuClock = 1850,
                    GpuTemperature = 63,
                    Fps = 72,
                    Game = TimeSpan.FromHours(1.25),
                    Pc = TimeSpan.FromHours(5.5),
                    Devices = new List<DeviceInfo>
                    {
                        new DeviceInfo { Kind = "headset", Label = "头显", Battery = 84, Connected = true, Tracked = true },
                        new DeviceInfo { Kind = "leftController", Label = "左手", Battery = 76, Connected = true, Tracked = true },
                        new DeviceInfo { Kind = "rightController", Label = "右手", Battery = 62, Connected = true, Tracked = true },
                        new DeviceInfo { Kind = "tracker", Label = "追踪器", Battery = 91, Connected = true, Tracked = true }
                    }
                };
                string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    settings.Appearance == "classic" ? "preview-first-version.png" : "preview-vrcx.png");
                using (Bitmap bitmap = Renderer.Draw(settings, sample))
                    bitmap.Save(file, ImageFormat.Png);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Any(a => a.Equals("--preview-settings", StringComparison.OrdinalIgnoreCase)))
            {
                using (var form = new MainForm(settings, dataDir))
                using (var bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.Show();
                    Application.DoEvents();
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "preview-settings.png"), ImageFormat.Png);
                    form.Hide();
                }
                return;
            }
            bool firstInstance;
            using (var mutex = new System.Threading.Mutex(true,
                @"Local\WristLite.SingleInstance", out firstInstance))
            {
                if (!firstInstance || HasOlderWristLiteProcess())
                {
                    MessageBox.Show("检测到另一个 VRWristLite 或 WristLite 版本正在运行。请先从通知区域退出旧版本，再启动此版本。",
                        "VRWristLite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try { Application.Run(new MainForm(settings, dataDir)); }
                finally { mutex.ReleaseMutex(); }
            }
        }

        private static bool HasOlderWristLiteProcess()
        {
            int ownId = Process.GetCurrentProcess().Id;
            bool found = false;
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (process.Id != ownId && (process.ProcessName.StartsWith(
                        "WristLite", StringComparison.OrdinalIgnoreCase) || process.ProcessName.StartsWith(
                        "VRWristLite", StringComparison.OrdinalIgnoreCase))) found = true;
                }
                catch { }
                finally { process.Dispose(); }
            }
            return found;
        }
    }

    internal sealed class Settings
    {
        public string Hand = "both";
        public string Button = "grip";
        public string TriggerMode = "hold";
        public int TimedSeconds = 5;
        public string Appearance = "classic";
        public bool OnlyVrChat = true;
        public bool GrayBackground = false;
        public bool MiniIcon = true;
        public bool ShowDevices = true;
        public bool ShowCpu = true;
        public bool ShowRam = true;
        public bool ShowGpu = true;
        public bool ShowCpuClock = true;
        public bool ShowGpuClock = true;
        public bool ShowTemperatures = true;
        public bool ShowFps = true;
        public bool ShowGame = true;
        public bool ShowPc = true;
        public bool StartMinimized = false;

        public static Settings Read(string path)
        {
            var s = new Settings();
            if (!File.Exists(path)) return s;
            foreach (string raw in File.ReadAllLines(path))
            {
                string[] parts = raw.Split(new[] { '=' }, 2);
                if (parts.Length != 2) continue;
                string key = parts[0].Trim();
                string value = parts[1].Trim();
                bool enabled = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                switch (key)
                {
                    case "Hand": if (new[] { "both", "left", "right" }.Contains(value)) s.Hand = value; break;
                    case "Button": if (new[] { "grip", "menu" }.Contains(value)) s.Button = value; break;
                    case "TriggerMode": if (new[] { "hold", "timed5" }.Contains(value)) s.TriggerMode = value; break;
                    case "TimedSeconds":
                        int seconds;
                        if (int.TryParse(value, out seconds) && seconds >= 1 && seconds <= 300)
                            s.TimedSeconds = seconds;
                        break;
                    case "Appearance": if (new[] { "classic", "vrcx" }.Contains(value)) s.Appearance = value; break;
                    case "OnlyVrChat": s.OnlyVrChat = enabled; break;
                    case "GrayBackground": s.GrayBackground = enabled; break;
                    case "MiniIcon": s.MiniIcon = enabled; break;
                    case "ShowDevices": s.ShowDevices = enabled; break;
                    case "ShowCpu": s.ShowCpu = enabled; break;
                    case "ShowRam": s.ShowRam = enabled; break;
                    case "ShowGpu": s.ShowGpu = enabled; break;
                    case "ShowCpuClock": s.ShowCpuClock = enabled; break;
                    case "ShowGpuClock": s.ShowGpuClock = enabled; break;
                    case "ShowTemperatures": s.ShowTemperatures = enabled; break;
                    case "ShowFps": s.ShowFps = enabled; break;
                    case "ShowGame": s.ShowGame = enabled; break;
                    case "ShowPc": s.ShowPc = enabled; break;
                    case "StartMinimized": s.StartMinimized = enabled; break;
                }
            }
            return s;
        }

        public void Write(string path)
        {
            File.WriteAllLines(path, new[]
            {
                "Hand=" + Hand, "Button=" + Button, "TriggerMode=" + TriggerMode,
                "TimedSeconds=" + TimedSeconds,
                "Appearance=" + Appearance,
                "OnlyVrChat=" + Bool(OnlyVrChat),
                "GrayBackground=" + Bool(GrayBackground), "MiniIcon=" + Bool(MiniIcon),
                "ShowDevices=" + Bool(ShowDevices), "ShowCpu=" + Bool(ShowCpu),
                "ShowRam=" + Bool(ShowRam), "ShowGpu=" + Bool(ShowGpu),
                "ShowCpuClock=" + Bool(ShowCpuClock), "ShowGpuClock=" + Bool(ShowGpuClock),
                "ShowTemperatures=" + Bool(ShowTemperatures), "ShowFps=" + Bool(ShowFps),
                "ShowGame=" + Bool(ShowGame), "ShowPc=" + Bool(ShowPc),
                "StartMinimized=" + Bool(StartMinimized)
            });
        }

        private static string Bool(bool b) { return b ? "1" : "0"; }
    }

    internal sealed class PanelData
    {
        public string Clock = "";
        public string Date = "";
        public double Cpu;
        public double? Ram;
        public double? Gpu;
        public double? CpuClock;
        public double? GpuClock;
        public double? CpuTemperature;
        public double? GpuTemperature;
        public double? Fps;
        public TimeSpan Game;
        public TimeSpan Pc;
        public List<DeviceInfo> Devices = new List<DeviceInfo>();
    }

    internal sealed class DeviceInfo
    {
        public string Kind = "other";
        public string Label = "设备";
        public bool Connected;
        public bool Tracked;
        public int Battery = -1;
        public bool Charging;
    }

    internal sealed class TriggerController
    {
        private bool wasPressed;
        private long visibleUntil;
        private uint timedController = OpenVR.k_unTrackedDeviceIndexInvalid;
        private string previousMode = "hold";

        public uint Evaluate(string mode, uint pressedController, long now, int timedSeconds)
        {
            if (mode != previousMode)
            {
                Reset();
                previousMode = mode;
            }
            bool pressed = pressedController != OpenVR.k_unTrackedDeviceIndexInvalid;
            if (mode != "timed5")
            {
                wasPressed = pressed;
                visibleUntil = 0;
                return pressedController;
            }
            if (pressed && !wasPressed)
            {
                timedController = pressedController;
                visibleUntil = now + (long)timedSeconds * Stopwatch.Frequency;
            }
            wasPressed = pressed;
            return now < visibleUntil ? timedController : OpenVR.k_unTrackedDeviceIndexInvalid;
        }

        public void Reset()
        {
            wasPressed = false;
            visibleUntil = 0;
            timedController = OpenVR.k_unTrackedDeviceIndexInvalid;
        }
    }

    // A raw OpenVR upload completes asynchronously. Only a loaded back buffer may become visible.
    internal sealed class OverlaySwapState
    {
        public int Front { get; private set; }
        public int Pending { get; private set; }
        public bool Visible { get; private set; }

        public OverlaySwapState() { Reset(); }

        public int BeginUpload()
        {
            if (Pending >= 0) return -1;
            Pending = Visible ? 1 - Front : Front;
            return Pending;
        }

        public bool ImageLoaded(int index, bool shouldShow, out int oldFront, out bool hideOld)
        {
            oldFront = Front;
            hideOld = false;
            if (Pending != index) return false;
            Pending = -1;
            hideOld = Visible && oldFront != index;
            Front = index;
            Visible = shouldShow;
            return true;
        }

        public void ImageFailed(int index)
        {
            if (Pending == index) Pending = -1;
        }

        public void Hide() { Visible = false; }

        public void Reset()
        {
            Front = 0;
            Pending = -1;
            Visible = false;
        }
    }

    internal static class Renderer
    {
        // VRCX's wrist surface is 512 by 510 pixels. Colors and spacing below
        // follow src/vr/vr.css; device images are the original VRCX PNG assets.
        private const int Width = 512;
        private const int Height = 510;
        private static readonly Dictionary<string, Bitmap> Icons = new Dictionary<string, Bitmap>();

        public static Bitmap Draw(Settings settings, PanelData data)
        {
            return settings.Appearance == "classic" ? DrawClassic(settings, data) : DrawVrcx(settings, data);
        }

        private static Bitmap DrawVrcx(Settings settings, PanelData data)
        {
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            using (Font title = new Font("Microsoft YaHei UI", 20, FontStyle.Bold))
            using (Font clock = new Font("Microsoft YaHei UI", 23, FontStyle.Bold))
            using (Font label = new Font("Microsoft YaHei UI", 10))
            using (Font value = new Font("Microsoft YaHei UI", 15, FontStyle.Bold))
            using (Font small = new Font("Microsoft YaHei UI", 12))
            using (Font deviceFont = new Font("Microsoft YaHei UI", 12, FontStyle.Bold))
            using (Brush white = new SolidBrush(Color.FromArgb(228, 228, 231)))
            using (Brush muted = new SolidBrush(Color.FromArgb(161, 161, 170)))
            using (Brush warning = new SolidBrush(Color.FromArgb(255, 175, 67)))
            using (Brush surface = new SolidBrush(Color.FromArgb(24, 24, 27)))
            using (Pen border = new Pen(Color.FromArgb(31, 255, 255, 255), 1))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                using (GraphicsPath shape = Rounded(1, 1, Width - 2, Height - 2, 8))
                {
                    if (settings.GrayBackground) g.FillPath(surface, shape);
                    g.DrawPath(border, shape);
                }
                if (settings.MiniIcon)
                {
                    using (Brush icon = new SolidBrush(Color.FromArgb(103, 194, 58)))
                        g.FillEllipse(icon, 12, 23, 13, 13);
                }
                int titleX = settings.MiniIcon ? 31 : 12;
                ShadowText(g, "手腕状态", title, white, titleX, 13);
                using (StringFormat far = new StringFormat { Alignment = StringAlignment.Far })
                {
                    ShadowText(g, data.Date, small, muted, new RectangleF(222, 20, 151, 29), far);
                    ShadowText(g, data.Clock, clock, white, new RectangleF(380, 7, 118, 42), far);
                }
                Divider(g, 62);

                var metrics = Metrics(settings, data);
                for (int i = 0; i < metrics.Count; i++)
                {
                    int x = 13 + (i % 2) * 251;
                    int y = 70 + (i / 2) * 41;
                    ShadowText(g, metrics[i].Key, label, muted, x, y);
                    using (StringFormat far = new StringFormat { Alignment = StringAlignment.Far })
                        ShadowText(g, metrics[i].Value, value, white,
                            new RectangleF(x + 95, y - 3, 139, 29), far);
                }

                int devicesTop = 292;
                if (settings.ShowDevices)
                {
                    Divider(g, devicesTop - 17);
                    ShadowText(g, "VR 设备", small, muted, 12, devicesTop);
                    if (data.Devices.Count == 0)
                        ShadowText(g, "正在连接设备…", small, muted, 12, devicesTop + 42);
                    int count = Math.Min(data.Devices.Count, 12);
                    int columns = count <= 4 ? 4 : 6;
                    int cellWidth = 492 / columns;
                    for (int i = 0; i < count; i++)
                    {
                        int col = i % columns;
                        int row = i / columns;
                        int cellX = 10 + col * cellWidth;
                        int cellY = devicesTop + (count <= columns ? 50 : 25) + row * 72;
                        DeviceInfo device = data.Devices[i];
                        Bitmap icon = GetIcon(IconName(device));
                        if (icon != null) DrawIconFit(g, icon,
                            new Rectangle(cellX + (cellWidth - 50) / 2, cellY, 50, 43));
                        using (StringFormat center = new StringFormat { Alignment = StringAlignment.Center })
                            ShadowText(g, DeviceStatus(device), deviceFont,
                                device.Connected && !device.Tracked ? warning : muted,
                                new RectangleF(cellX, cellY + 45, cellWidth, 23), center);
                    }
                }
            }
            return bitmap;
        }

        private static Bitmap DrawClassic(Settings settings, PanelData data)
        {
            const int classicWidth = 1000;
            const int classicHeight = 540;
            var bitmap = new Bitmap(classicWidth, classicHeight, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            using (Font title = new Font("Microsoft YaHei UI", 32, FontStyle.Bold))
            using (Font clock = new Font("Microsoft YaHei UI", 43, FontStyle.Bold))
            using (Font label = new Font("Microsoft YaHei UI", 17))
            using (Font value = new Font("Microsoft YaHei UI", 24, FontStyle.Bold))
            using (Font small = new Font("Microsoft YaHei UI", 17))
            using (Brush white = new SolidBrush(Color.White))
            using (Brush muted = new SolidBrush(Color.FromArgb(195, 207, 218)))
            using (Brush warning = new SolidBrush(Color.FromArgb(255, 175, 67)))
            using (Brush accent = new SolidBrush(Color.FromArgb(96, 225, 214)))
            using (Brush panel = new SolidBrush(settings.GrayBackground
                ? Color.FromArgb(235, 42, 45, 49)
                : Color.FromArgb(218, 15, 19, 25)))
            using (Pen border = new Pen(Color.FromArgb(125, 105, 121, 135), 2))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                using (GraphicsPath shape = Rounded(6, 6, classicWidth - 12, classicHeight - 12, 34))
                {
                    g.FillPath(panel, shape);
                    g.DrawPath(border, shape);
                }
                if (settings.MiniIcon)
                {
                    using (Brush icon = new SolidBrush(Color.FromArgb(96, 225, 214)))
                        g.FillEllipse(icon, 39, 45, 22, 22);
                }
                g.DrawString("VR 手腕面板", title, white, 75, 27);
                g.DrawString(data.Date, small, muted, 552, 48);
                g.DrawString(data.Clock, clock, accent, 729, 19);
                using (Pen line = new Pen(Color.FromArgb(100, 128, 142, 155), 2))
                    g.DrawLine(line, 37, 105, 963, 105);

                if (settings.ShowDevices)
                {
                    g.DrawString("VR 设备", label, muted, 45, 119);
                    if (data.Devices.Count == 0)
                        g.DrawString("正在连接设备…", small, muted, 45, 170);
                    using (Font deviceFont = new Font("Microsoft YaHei UI", 15, FontStyle.Bold))
                    using (StringFormat center = new StringFormat { Alignment = StringAlignment.Center })
                    {
                        int count = Math.Min(data.Devices.Count, 20);
                        for (int i = 0; i < count; i++)
                        {
                            int col = i % 10;
                            int row = i / 10;
                            int x = 37 + col * 93;
                            int y = 154 + row * 78;
                            DeviceInfo device = data.Devices[i];
                            Bitmap icon = GetIcon(IconName(device));
                            if (icon != null) DrawIconFit(g, icon, new Rectangle(x + 21, y, 50, 46));
                            g.DrawString(DeviceStatus(device), deviceFont,
                                device.Connected && !device.Tracked ? warning : white,
                                new RectangleF(x, y + 46, 92, 26), center);
                        }
                    }
                }
                var metrics = Metrics(settings, data);
                for (int i = 0; i < metrics.Count; i++)
                {
                    int x = 45 + (i % 5) * 185;
                    int y = 315 + (i / 5) * 77;
                    ClassicMetric(g, label, value, muted, white,
                        metrics[i].Key, metrics[i].Value, x, y);
                }
            }
            return bitmap;
        }

        private static string DeviceStatus(DeviceInfo device)
        {
            if (!device.Connected) return "离线";
            if (!device.Tracked) return "丢追";
            return device.Battery < 0 ? "在线" : device.Battery + "%";
        }

        private static void DrawIconFit(Graphics g, Bitmap icon, Rectangle bounds)
        {
            float scale = Math.Min((float)bounds.Width / icon.Width, (float)bounds.Height / icon.Height);
            int width = Math.Max(1, (int)Math.Round(icon.Width * scale));
            int height = Math.Max(1, (int)Math.Round(icon.Height * scale));
            g.DrawImage(icon, bounds.X + (bounds.Width - width) / 2,
                bounds.Y + (bounds.Height - height) / 2, width, height);
        }

        private static List<KeyValuePair<string, string>> Metrics(Settings settings, PanelData data)
        {
            var result = new List<KeyValuePair<string, string>>();
            if (settings.ShowCpu) result.Add(new KeyValuePair<string, string>("CPU 占用", data.Cpu.ToString("0") + "%"));
            if (settings.ShowRam) result.Add(new KeyValuePair<string, string>("内存占用", Value(data.Ram, "%")));
            if (settings.ShowGpu) result.Add(new KeyValuePair<string, string>("GPU 占用", Value(data.Gpu, "%")));
            if (settings.ShowFps) result.Add(new KeyValuePair<string, string>("VR 帧率", Value(data.Fps, " FPS")));
            if (settings.ShowCpuClock) result.Add(new KeyValuePair<string, string>("CPU 频率", Value(data.CpuClock, " MHz")));
            if (settings.ShowGpuClock) result.Add(new KeyValuePair<string, string>("GPU 频率", Value(data.GpuClock, " MHz")));
            if (settings.ShowTemperatures)
            {
                result.Add(new KeyValuePair<string, string>("CPU 温度", Value(data.CpuTemperature, "°C")));
                result.Add(new KeyValuePair<string, string>("GPU 温度", Value(data.GpuTemperature, "°C")));
            }
            if (settings.ShowGame) result.Add(new KeyValuePair<string, string>("游戏运行", Format(data.Game)));
            if (settings.ShowPc) result.Add(new KeyValuePair<string, string>("电脑运行", Format(data.Pc)));
            return result;
        }

        private static string Value(double? value, string suffix)
        {
            return value.HasValue ? value.Value.ToString("0") + suffix : "—";
        }

        private static void ClassicMetric(Graphics g, Font label, Font value,
            Brush muted, Brush white, string name, string number, int x, int y)
        {
            g.DrawString(name, label, muted, x, y);
            g.DrawString(number, value, white, x, y + 29);
        }

        private static void Metric(Graphics g, Font label, Font value, Brush muted, Brush white,
            string name, string number, int y)
        {
            ShadowText(g, name, label, muted, 12, y);
            using (StringFormat far = new StringFormat { Alignment = StringAlignment.Far })
                ShadowText(g, number, value, white, new RectangleF(225, y - 2, 273, 40), far);
            Divider(g, y + 45);
        }

        private static void Divider(Graphics g, int y)
        {
            using (Pen p = new Pen(Color.FromArgb(16, 255, 255, 255), 1))
                g.DrawLine(p, 11, y, Width - 11, y);
        }

        private static void ShadowText(Graphics g, string text, Font font, Brush brush, float x, float y)
        {
            using (Brush shadow = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
            {
                g.DrawString(text, font, shadow, x + 1, y + 1);
                g.DrawString(text, font, brush, x, y);
            }
        }

        private static void ShadowText(Graphics g, string text, Font font, Brush brush,
            RectangleF bounds, StringFormat format)
        {
            using (Brush shadow = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
            {
                RectangleF shifted = bounds;
                shifted.Offset(1, 1);
                g.DrawString(text, font, shadow, shifted, format);
                g.DrawString(text, font, brush, bounds, format);
            }
        }

        private static string IconName(DeviceInfo device)
        {
            string stem = device.Kind == "headset" ? "headset_quest_status"
                : device.Kind == "leftController" ? "left_controller_status"
                : device.Kind == "rightController" ? "right_controller_status"
                : device.Kind == "controller" ? "controller_status"
                : device.Kind == "tracker" ? "tracker_status"
                : device.Kind == "base" ? "base_status" : "other_status";
            if (!device.Connected) return stem + "_off.png";
            if (device.Kind == "base") return stem + "_ready.png";
            if (device.Charging) return stem + "_ready_charging.png";
            if (device.Battery >= 0 && device.Battery < 20) return stem + "_ready_low.png";
            return stem + "_ready.png";
        }

        private static Bitmap GetIcon(string file)
        {
            Bitmap cached;
            if (Icons.TryGetValue(file, out cached)) return cached;
            using (Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("WristLite.assets." + file))
            {
                if (stream == null) return null;
                using (Image image = Image.FromStream(stream))
                    cached = new Bitmap(image);
            }
            Icons[file] = cached;
            return cached;
        }

        private static string Format(TimeSpan span)
        {
            if (span.TotalSeconds < 0) return "—";
            return span.Days > 0
                ? string.Format("{0}天 {1:00}:{2:00}", span.Days, span.Hours, span.Minutes)
                : string.Format("{0:00}:{1:00}:{2:00}", (int)span.TotalHours, span.Minutes, span.Seconds);
        }

        private static GraphicsPath Rounded(int x, int y, int width, int height, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(x, y, d, d, 180, 90);
            p.AddArc(x + width - d, y, d, d, 270, 90);
            p.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            p.AddArc(x, y + height - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    internal static class OverlayPixels
    {
        public static byte[] ToRgba(Bitmap image)
        {
            int rowLength = checked(image.Width * 4);
            byte[] rgba = new byte[checked(rowLength * image.Height)];
            byte[] bgraRow = new byte[rowLength];
            BitmapData data = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), bgraRow, 0, rowLength);
                    int output = y * rowLength;
                    for (int x = 0; x < rowLength; x += 4)
                    {
                        rgba[output + x] = bgraRow[x + 2];
                        rgba[output + x + 1] = bgraRow[x + 1];
                        rgba[output + x + 2] = bgraRow[x];
                        rgba[output + x + 3] = bgraRow[x + 3];
                    }
                }
            }
            finally { image.UnlockBits(data); }
            return rgba;
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly Settings settings;
        private readonly string dataDir;
        private readonly VrWorker worker;
        private readonly Timer timer;
        private readonly NotifyIcon tray;
        private readonly Label status;
        private readonly ComboBox hand;
        private readonly ComboBox button;
        private readonly ComboBox triggerMode;
        private readonly NumericUpDown timedSeconds;
        private readonly ComboBox appearance;
        private readonly Dictionary<string, CheckBox> checks = new Dictionary<string, CheckBox>();
        private bool exiting;

        public MainForm(Settings settings, string dataDir)
        {
            this.settings = settings;
            this.dataDir = dataDir;
            Text = "VRWristLite · 独立 VR 手腕面板";
            Width = 540;
            Height = 743;
            MinimumSize = new Size(540, 743);
            MaximumSize = new Size(540, 743);
            Font = new Font("Microsoft YaHei UI", 10);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            var title = new Label { Text = "独立 VR 手腕面板", Left = 22, Top = 20, Width = 450, Height = 36,
                Font = new Font(Font.FontFamily, 17, FontStyle.Bold) };
            Controls.Add(title);
            status = new Label { Left = 22, Top = 65, Width = 485, Height = 47,
                Text = "正在检查 SteamVR…" };
            Controls.Add(status);
            Controls.Add(new Label { Text = "显示位置", Left = 22, Top = 128, Width = 130 });
            hand = new ComboBox { Left = 165, Top = 124, Width = 235, DropDownStyle = ComboBoxStyle.DropDownList };
            hand.Items.AddRange(new object[] { "双手", "左手", "右手" });
            hand.SelectedIndex = settings.Hand == "left" ? 1 : settings.Hand == "right" ? 2 : 0;
            Controls.Add(hand);
            Controls.Add(new Label { Text = "唤出按键", Left = 22, Top = 170, Width = 130 });
            button = new ComboBox { Left = 165, Top = 166, Width = 235, DropDownStyle = ComboBoxStyle.DropDownList };
            button.Items.AddRange(new object[] { "握持键 (Oculus X/A)", "菜单键 (Oculus Y/B)" });
            button.SelectedIndex = settings.Button == "menu" ? 1 : 0;
            Controls.Add(button);
            Controls.Add(new Label { Text = "触发方式", Left = 22, Top = 212, Width = 130 });
            triggerMode = new ComboBox { Left = 165, Top = 208, Width = 235,
                DropDownStyle = ComboBoxStyle.DropDownList };
            triggerMode.Items.AddRange(new object[] { "按住显示，松开隐藏", "按一次限时显示" });
            triggerMode.SelectedIndex = settings.TriggerMode == "timed5" ? 1 : 0;
            Controls.Add(triggerMode);
            Controls.Add(new Label { Text = "显示时长", Left = 22, Top = 254, Width = 130 });
            timedSeconds = new NumericUpDown { Left = 165, Top = 250, Width = 85,
                Minimum = 1, Maximum = 300, Value = settings.TimedSeconds };
            Controls.Add(timedSeconds);
            Controls.Add(new Label { Text = "秒（按一次显示）", Left = 260, Top = 254,
                Width = 180 });
            timedSeconds.Enabled = triggerMode.SelectedIndex == 1;
            triggerMode.SelectedIndexChanged += (sender, e) =>
                timedSeconds.Enabled = triggerMode.SelectedIndex == 1;
            Controls.Add(new Label { Text = "界面样式", Left = 22, Top = 296, Width = 130 });
            appearance = new ComboBox { Left = 165, Top = 292, Width = 235,
                DropDownStyle = ComboBoxStyle.DropDownList };
            appearance.Items.AddRange(new object[] { "第一版（宽屏）", "VRCX 素材（方形）" });
            appearance.SelectedIndex = settings.Appearance == "vrcx" ? 1 : 0;
            Controls.Add(appearance);
            AddCheck("OnlyVrChat", "仅在 VRChat 运行时显示", settings.OnlyVrChat, 341);
            AddCheck("GrayBackground", "切换背景颜色", settings.GrayBackground, 370);
            AddCheck("MiniIcon", "显示状态指示点", settings.MiniIcon, 399);
            AddCheck("ShowDevices", "VR 设备 / 电量 / 丢追", settings.ShowDevices, 428, 22);
            AddCheck("ShowCpu", "CPU 占用率", settings.ShowCpu, 457, 22);
            AddCheck("ShowRam", "内存占用率", settings.ShowRam, 486, 22);
            AddCheck("ShowGpu", "GPU 占用率", settings.ShowGpu, 515, 22);
            AddCheck("ShowFps", "VR 帧率", settings.ShowFps, 544, 22);
            AddCheck("ShowGame", "游戏运行时长", settings.ShowGame, 573, 22);
            AddCheck("ShowPc", "电脑运行时长", settings.ShowPc, 602, 22);
            AddCheck("ShowCpuClock", "CPU 频率", settings.ShowCpuClock, 428, 270);
            AddCheck("ShowGpuClock", "GPU 频率", settings.ShowGpuClock, 457, 270);
            AddCheck("ShowTemperatures", "CPU / GPU 温度", settings.ShowTemperatures, 486, 270);
            AddCheck("StartMinimized", "启动后收起到通知区域", settings.StartMinimized, 515, 270);
            var save = new Button { Text = "保存设置", Left = 22, Top = 655, Width = 145, Height = 37 };
            save.Click += (sender, e) => SaveSettings(true);
            Controls.Add(save);
            var preview = new Button { Text = "查看静态预览", Left = 182, Top = 655, Width = 145, Height = 37 };
            preview.Click += (sender, e) => Preview();
            Controls.Add(preview);

            tray = new NotifyIcon { Icon = Icon, Text = "VRWristLite", Visible = true };
            var menu = new ContextMenuStrip();
            menu.Items.Add("打开设置", null, (sender, e) => { Show(); WindowState = FormWindowState.Normal; Activate(); });
            menu.Items.Add("退出", null, (sender, e) => { exiting = true; Close(); });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += (sender, e) => { Show(); WindowState = FormWindowState.Normal; Activate(); };

            worker = new VrWorker(settings);
            timer = new Timer { Interval = 50 };
            timer.Tick += (sender, e) =>
            {
                try { status.Text = worker.Tick(); }
                catch (Exception ex) { status.Text = "叠加层错误：" + ex.Message; worker.Reset(); }
            };
            timer.Start();
            Shown += (sender, e) => { if (settings.StartMinimized) Hide(); };
            FormClosing += (sender, e) =>
            {
                if (!exiting)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
            FormClosed += (sender, e) => { timer.Stop(); worker.Dispose(); tray.Visible = false; tray.Dispose(); };
        }

        private void AddCheck(string key, string caption, bool enabled, int top, int left = 22)
        {
            var box = new CheckBox { Text = caption, Checked = enabled, Left = left, Top = top,
                Width = left == 22 ? 245 : 240 };
            checks.Add(key, box);
            Controls.Add(box);
        }

        private void SaveSettings(bool notify)
        {
            settings.Hand = hand.SelectedIndex == 1 ? "left" : hand.SelectedIndex == 2 ? "right" : "both";
            settings.Button = button.SelectedIndex == 1 ? "menu" : "grip";
            settings.TriggerMode = triggerMode.SelectedIndex == 1 ? "timed5" : "hold";
            settings.TimedSeconds = (int)timedSeconds.Value;
            settings.Appearance = appearance.SelectedIndex == 1 ? "vrcx" : "classic";
            settings.OnlyVrChat = checks["OnlyVrChat"].Checked;
            settings.GrayBackground = checks["GrayBackground"].Checked;
            settings.MiniIcon = checks["MiniIcon"].Checked;
            settings.ShowDevices = checks["ShowDevices"].Checked;
            settings.ShowCpu = checks["ShowCpu"].Checked;
            settings.ShowRam = checks["ShowRam"].Checked;
            settings.ShowGpu = checks["ShowGpu"].Checked;
            settings.ShowCpuClock = checks["ShowCpuClock"].Checked;
            settings.ShowGpuClock = checks["ShowGpuClock"].Checked;
            settings.ShowTemperatures = checks["ShowTemperatures"].Checked;
            settings.ShowFps = checks["ShowFps"].Checked;
            settings.ShowGame = checks["ShowGame"].Checked;
            settings.ShowPc = checks["ShowPc"].Checked;
            settings.StartMinimized = checks["StartMinimized"].Checked;
            settings.Write(Path.Combine(dataDir, "settings.ini"));
            worker.OnSettingsChanged();
            if (notify) MessageBox.Show("设置已保存，腕部面板会在下次唤出时使用。", "VRWristLite");
        }

        private void Preview()
        {
            SaveSettings(false);
            string path = Path.Combine(dataDir, "preview.png");
            using (Bitmap b = Renderer.Draw(settings, worker.CurrentPanelData()))
                b.Save(path, ImageFormat.Png);
            Process.Start(path);
        }
    }

    internal sealed class VrWorker : IDisposable
    {
        private readonly Settings settings;
        private readonly TriggerController trigger = new TriggerController();
        private readonly HardwareProbe hardware = new HardwareProbe();
        private readonly Dictionary<uint, DeviceInfo> knownDevices = new Dictionary<uint, DeviceInfo>();
        private CVRSystem system;
        private CVROverlay overlay;
        private readonly ulong[] handles = new ulong[2];
        private readonly OverlaySwapState swap = new OverlaySwapState();
        private DateTime nextConnect = DateTime.MinValue;
        private DateTime nextRender = DateTime.MinValue;
        private DateTime nextGameCheck = DateTime.MinValue;
        private DateTime gameStart = DateTime.MinValue;
        private bool gameRunning;
        private PanelData current = new PanelData();
        private ulong previousIdle, previousKernel, previousUser;
        private double cpu;
        private string error = "";
        private IntPtr pixelBuffer = IntPtr.Zero;
        private int pixelBufferCapacity;

        public VrWorker(Settings settings)
        {
            this.settings = settings;
        }

        public PanelData CurrentPanelData()
        {
            RefreshData();
            return current;
        }

        public void OnSettingsChanged()
        {
            trigger.Reset();
            nextRender = DateTime.MinValue;
        }

        public string Tick()
        {
            if (DateTime.UtcNow >= nextGameCheck)
            {
                nextGameCheck = DateTime.UtcNow.AddSeconds(2);
                UpdateGame();
            }
            if (system == null)
            {
                if (DateTime.UtcNow < nextConnect) return error;
                nextConnect = DateTime.UtcNow.AddSeconds(5);
                try { Connect(); }
                catch (DllNotFoundException) { error = "未找到 openvr_api.dll。请看使用说明。"; return error; }
                catch (BadImageFormatException) { error = "OpenVR DLL 与 64 位程序不匹配。"; return error; }
                if (system == null) return error;
            }
            var vrEvent = new VREvent_t();
            while (system.PollNextEvent(ref vrEvent, (uint)Marshal.SizeOf(vrEvent)))
            {
                if ((EVREventType)vrEvent.eventType == EVREventType.VREvent_Quit)
                {
                    Reset();
                    return "SteamVR 已退出，等待重新连接";
                }
            }
            if (settings.OnlyVrChat && !gameRunning)
            {
                PollOverlayEvents(false);
                HidePanel();
                nextRender = DateTime.MinValue;
                trigger.Reset();
                return "SteamVR 已连接，等待 VRChat 启动";
            }

            uint selected = trigger.Evaluate(settings.TriggerMode, FindHeldController(),
                Stopwatch.GetTimestamp(), settings.TimedSeconds);
            bool shouldShow = selected != OpenVR.k_unTrackedDeviceIndexInvalid;
            PollOverlayEvents(shouldShow);
            if (shouldShow)
            {
                HmdMatrix34_t transform = WristTransform(system.GetControllerRoleForTrackedDeviceIndex(selected));
                foreach (ulong panel in handles)
                    overlay.SetOverlayTransformTrackedDeviceRelative(panel, selected, ref transform);
                if (swap.Pending < 0 && DateTime.UtcNow >= nextRender)
                {
                    nextRender = DateTime.UtcNow.AddSeconds(1);
                    RefreshData();
                    int target = swap.BeginUpload();
                    try
                    {
                        using (Bitmap image = Renderer.Draw(settings, current))
                            UploadImage(handles[target], image);
                    }
                    catch { swap.ImageFailed(target); throw; }
                }
            }
            else
            {
                HidePanel();
                nextRender = DateTime.MinValue;
            }
            return swap.Visible ? "SteamVR 已连接 · 腕部面板显示中"
                : settings.TriggerMode == "timed5"
                    ? "SteamVR 已连接 · 按一次手柄按键显示 " + settings.TimedSeconds + " 秒"
                    : "SteamVR 已连接 · 按住手柄按键唤出面板";
        }

        private void PollOverlayEvents(bool shouldShow)
        {
            for (int i = 0; i < handles.Length; i++)
            {
                var evt = new VREvent_t();
                while (overlay.PollNextOverlayEvent(handles[i], ref evt, (uint)Marshal.SizeOf(evt)))
                {
                    EVREventType type = (EVREventType)evt.eventType;
                    if (type == EVREventType.VREvent_ImageLoaded)
                    {
                        int oldFront;
                        bool hideOld;
                        if (!swap.ImageLoaded(i, shouldShow, out oldFront, out hideOld)) continue;
                        if (!shouldShow)
                        {
                            if (hideOld) overlay.HideOverlay(handles[oldFront]);
                            continue;
                        }
                        EVROverlayError showError = overlay.ShowOverlay(handles[i]);
                        if (showError != EVROverlayError.None)
                            throw new InvalidOperationException("显示面板失败：" + showError);
                        if (hideOld) overlay.HideOverlay(handles[oldFront]);
                    }
                    else if (type == EVREventType.VREvent_ImageFailed)
                    {
                        swap.ImageFailed(i);
                        nextRender = DateTime.UtcNow.AddSeconds(1);
                    }
                }
            }
        }

        private void HidePanel()
        {
            if (!swap.Visible) return;
            overlay.HideOverlay(handles[swap.Front]);
            swap.Hide();
        }

        private void UploadImage(ulong target, Bitmap image)
        {
            byte[] pixels = OverlayPixels.ToRgba(image);
            if (pixels.Length > pixelBufferCapacity)
            {
                IntPtr newBuffer = Marshal.AllocHGlobal(pixels.Length);
                if (pixelBuffer != IntPtr.Zero) Marshal.FreeHGlobal(pixelBuffer);
                pixelBuffer = newBuffer;
                pixelBufferCapacity = pixels.Length;
            }
            Marshal.Copy(pixels, 0, pixelBuffer, pixels.Length);
            EVROverlayError renderError = overlay.SetOverlayRaw(
                target, pixelBuffer, (uint)image.Width, (uint)image.Height, 4);
            if (renderError != EVROverlayError.None)
                throw new InvalidOperationException("更新面板失败：" + renderError);
        }

        private void Connect()
        {
            string dll = FindOpenVrDll();
            if (dll == null)
            {
                error = "未找到 openvr_api.dll。请看使用说明。";
                return;
            }
            SetDllDirectory(Path.GetDirectoryName(dll));
            EVRInitError init = EVRInitError.None;
            system = OpenVR.Init(ref init, EVRApplicationType.VRApplication_Background);
            if (system == null)
            {
                error = "等待 SteamVR：" + init;
                return;
            }
            overlay = OpenVR.Overlay;
            if (overlay == null)
            {
                error = "SteamVR 未提供叠加层接口";
                Reset();
                return;
            }
            for (int i = 0; i < handles.Length; i++)
            {
                EVROverlayError err = overlay.CreateOverlay(
                    "wristlite.standalone.overlay." + i, "WristLite 手腕面板 " + i,
                    ref handles[i]);
                if (err != EVROverlayError.None)
                {
                    error = "创建叠加层失败：" + err;
                    Reset();
                    return;
                }
                overlay.SetOverlayWidthInMeters(handles[i], 1f);
                overlay.SetOverlayAlpha(handles[i], 1f);
                overlay.SetOverlayInputMethod(handles[i], VROverlayInputMethod.None);
            }
            swap.Reset();
            nextRender = DateTime.MinValue;
            error = "";
        }

        private uint FindHeldController()
        {
            ETrackedControllerRole[] roles = settings.Hand == "left"
                ? new[] { ETrackedControllerRole.LeftHand }
                : settings.Hand == "right"
                    ? new[] { ETrackedControllerRole.RightHand }
                    : new[] { ETrackedControllerRole.LeftHand, ETrackedControllerRole.RightHand };
            foreach (ETrackedControllerRole role in roles)
            {
                uint index = system.GetTrackedDeviceIndexForControllerRole(role);
                if (index == OpenVR.k_unTrackedDeviceIndexInvalid) continue;
                var state = new VRControllerState_t();
                if (!system.GetControllerState(index, ref state, (uint)Marshal.SizeOf(state)))
                    continue;
                bool oculus = IsOculus(index);
                ulong mask = settings.Button == "menu" ? 2UL : oculus ? 128UL : 4UL;
                if ((state.ulButtonPressed & mask) != 0)
                    return index;
            }
            return OpenVR.k_unTrackedDeviceIndexInvalid;
        }

        private bool IsOculus(uint index)
        {
            var name = new StringBuilder(128);
            var err = ETrackedPropertyError.TrackedProp_Success;
            system.GetStringTrackedDeviceProperty(index,
                ETrackedDeviceProperty.Prop_TrackingSystemName_String,
                name, (uint)name.Capacity, ref err);
            return name.ToString().IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static HmdMatrix34_t WristTransform(ETrackedControllerRole role)
        {
            bool left = role == ETrackedControllerRole.LeftHand;
            float x = (float)Math.PI / 2f * (left ? 1f : -1f);
            float y = x;
            float z = -(float)Math.PI / 2f;
            Matrix4x4 m = Matrix4x4.CreateScale(0.25f);
            m *= Matrix4x4.CreateRotationX(x);
            m *= Matrix4x4.CreateRotationY(y);
            m *= Matrix4x4.CreateRotationZ(z);
            m *= Matrix4x4.CreateTranslation(left ? -0.07f : 0.07f, -0.05f, 0.06f);
            return new HmdMatrix34_t
            {
                m0 = m.M11, m1 = m.M21, m2 = m.M31, m3 = m.M41,
                m4 = m.M12, m5 = m.M22, m6 = m.M32, m7 = m.M42,
                m8 = m.M13, m9 = m.M23, m10 = m.M33, m11 = m.M43
            };
        }

        private void UpdateGame()
        {
            Process[] found = Process.GetProcessesByName("VRChat");
            gameRunning = found.Length > 0;
            gameStart = DateTime.MinValue;
            if (gameRunning)
            {
                try { gameStart = found[0].StartTime; }
                catch { }
            }
            foreach (Process process in found) process.Dispose();
        }

        private void RefreshData()
        {
            current.Clock = DateTime.Now.ToString("HH:mm");
            current.Date = DateTime.Now.ToString("yyyy-MM-dd");
            current.Pc = TimeSpan.FromMilliseconds(GetTickCount64());
            current.Game = gameStart == DateTime.MinValue ? TimeSpan.Zero : DateTime.Now - gameStart;
            current.Cpu = ReadCpu();
            current.Ram = current.Gpu = current.CpuClock = current.GpuClock = null;
            current.CpuTemperature = current.GpuTemperature = current.Fps = null;
            hardware.Read(current);
            current.Devices.Clear();
            if (system == null) return;
            current.Fps = ReadVrFps();
            var poses = new TrackedDevicePose_t[(int)OpenVR.k_unMaxTrackedDeviceCount];
            bool posesAvailable = true;
            try { system.GetDeviceToAbsoluteTrackingPose(
                ETrackingUniverseOrigin.TrackingUniverseStanding, 0, poses); }
            catch { posesAvailable = false; }
            var seenDevices = new HashSet<uint>();
            int tracker = 0;
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                ETrackedDeviceClass kind = system.GetTrackedDeviceClass(i);
                var device = new DeviceInfo();
                if (kind == ETrackedDeviceClass.HMD) { device.Kind = "headset"; device.Label = "头显"; }
                else if (kind == ETrackedDeviceClass.Controller)
                {
                    ETrackedControllerRole role = system.GetControllerRoleForTrackedDeviceIndex(i);
                    device.Kind = role == ETrackedControllerRole.LeftHand ? "leftController"
                        : role == ETrackedControllerRole.RightHand ? "rightController" : "controller";
                    device.Label = role == ETrackedControllerRole.LeftHand ? "左手"
                        : role == ETrackedControllerRole.RightHand ? "右手" : "手柄";
                }
                else if (kind == ETrackedDeviceClass.GenericTracker)
                {
                    device.Kind = "tracker";
                    device.Label = "追踪器" + (++tracker);
                }
                else if (kind == ETrackedDeviceClass.TrackingReference)
                {
                    device.Kind = "base";
                    device.Label = "基站";
                }
                else continue;
                seenDevices.Add(i);
                device.Connected = system.IsTrackedDeviceConnected(i);
                device.Tracked = device.Connected &&
                    (kind == ETrackedDeviceClass.TrackingReference || !posesAvailable ||
                     (poses[i].bPoseIsValid && poses[i].eTrackingResult == ETrackingResult.Running_OK));
                if (!device.Connected)
                {
                    current.Devices.Add(device);
                    knownDevices[i] = device;
                    continue;
                }
                var err = ETrackedPropertyError.TrackedProp_Success;
                float battery = system.GetFloatTrackedDeviceProperty(i,
                    ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, ref err);
                if (err == ETrackedPropertyError.TrackedProp_Success)
                    device.Battery = Math.Max(0, Math.Min(100, (int)Math.Round(battery * 100)));
                err = ETrackedPropertyError.TrackedProp_Success;
                device.Charging = system.GetBoolTrackedDeviceProperty(i,
                    ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool, ref err);
                current.Devices.Add(device);
                knownDevices[i] = device;
            }
            foreach (var known in knownDevices)
            {
                if (seenDevices.Contains(known.Key)) continue;
                current.Devices.Add(new DeviceInfo {
                    Kind = known.Value.Kind, Label = known.Value.Label,
                    Battery = known.Value.Battery, Connected = false, Tracked = false
                });
            }
        }

        private double? ReadVrFps()
        {
            try
            {
                CVRCompositor compositor = OpenVR.Compositor;
                if (compositor == null) return null;
                uint size = (uint)Marshal.SizeOf(typeof(Compositor_FrameTiming));
                var newest = new Compositor_FrameTiming { m_nSize = size };
                var older = new Compositor_FrameTiming { m_nSize = size };
                if (!compositor.GetFrameTiming(ref newest, 0) ||
                    !compositor.GetFrameTiming(ref older, 9)) return null;
                double elapsed = newest.m_flSystemTimeInSeconds - older.m_flSystemTimeInSeconds;
                uint frames = newest.m_nFrameIndex - older.m_nFrameIndex;
                if (elapsed <= 0 || frames == 0) return null;
                double fps = frames / elapsed;
                return fps > 0 && fps < 300 ? (double?)fps : null;
            }
            catch { return null; }
        }

        private double ReadCpu()
        {
            FILETIME idle, kernel, user;
            if (!GetSystemTimes(out idle, out kernel, out user)) return cpu;
            ulong i = idle.Value, k = kernel.Value, u = user.Value;
            ulong total = (k - previousKernel) + (u - previousUser);
            if (previousKernel != 0 && total > 0)
                cpu = 100.0 * (1.0 - (double)(i - previousIdle) / total);
            previousIdle = i; previousKernel = k; previousUser = u;
            return Math.Max(0, Math.Min(100, cpu));
        }

        private static string FindOpenVrDll()
        {
            string direct = Environment.GetEnvironmentVariable("OPENVR_API_DLL");
            if (!string.IsNullOrEmpty(direct) && File.Exists(direct)) return direct;
            string exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "openvr_api.dll");
            if (File.Exists(exe)) return exe;
            var candidates = new List<string>();
            string[] steamRoots = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
            };
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string install = key.GetValue("SteamPath") as string;
                        if (!string.IsNullOrEmpty(install)) steamRoots = steamRoots.Concat(new[] { install }).ToArray();
                    }
                }
            }
            catch { }
            foreach (string root in steamRoots)
                candidates.Add(Path.Combine(root, "steamapps", "common", "SteamVR", "bin", "win64", "openvr_api.dll"));
            foreach (string candidate in candidates)
                if (File.Exists(candidate)) return candidate;
            return null;
        }

        public void Reset()
        {
            trigger.Reset();
            foreach (ulong panel in handles)
            {
                try { if (panel != 0 && overlay != null) overlay.DestroyOverlay(panel); }
                catch { }
            }
            for (int i = 0; i < handles.Length; i++) handles[i] = 0;
            if (pixelBuffer != IntPtr.Zero) Marshal.FreeHGlobal(pixelBuffer);
            pixelBuffer = IntPtr.Zero;
            pixelBufferCapacity = 0;
            swap.Reset();
            knownDevices.Clear();
            overlay = null;
            if (system != null)
            {
                try { OpenVR.Shutdown(); } catch { }
                system = null;
            }
            nextConnect = DateTime.UtcNow.AddSeconds(5);
        }

        public void Dispose() { Reset(); hardware.Dispose(); }

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint Low, High;
            public ulong Value { get { return ((ulong)High << 32) | Low; } }
        }
        [DllImport("kernel32.dll")] private static extern bool GetSystemTimes(
            out FILETIME idle, out FILETIME kernel, out FILETIME user);
        [DllImport("kernel32.dll")] private static extern ulong GetTickCount64();
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectory(string path);
    }
}
