using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Valve.VR;

namespace WristLite
{
    internal static class TriggerTests
    {
        private static void Expect(bool condition, string label)
        {
            if (!condition) throw new Exception(label);
        }

        [STAThread]
        public static void Main()
        {
            uint none = OpenVR.k_unTrackedDeviceIndexInvalid;
            uint left = 1;
            long second = Stopwatch.Frequency;
            var trigger = new TriggerController();

            Expect(trigger.Evaluate("hold", none, 0, 5) == none, "hold idle");
            Expect(trigger.Evaluate("hold", left, 0, 5) == left, "hold pressed");
            Expect(trigger.Evaluate("hold", none, 0, 5) == none, "hold release");

            Expect(trigger.Evaluate("timed5", left, 0, 5) == left, "timed first press");
            Expect(trigger.Evaluate("timed5", none, second, 5) == left, "timed survives release");
            Expect(trigger.Evaluate("timed5", none, 5 * second - 1, 5) == left, "timed before deadline");
            Expect(trigger.Evaluate("timed5", none, 5 * second, 5) == none, "timed deadline");
            Expect(trigger.Evaluate("timed5", left, 6 * second, 5) == left, "timed second press");
            Expect(trigger.Evaluate("timed5", left, 11 * second, 5) == none, "held does not extend");
            Expect(trigger.Evaluate("timed5", none, 11 * second, 5) == none, "release after deadline");
            Expect(trigger.Evaluate("timed5", left, 12 * second, 5) == left, "repress rearms");
            trigger.Reset();
            Expect(trigger.Evaluate("timed5", none, 12 * second, 5) == none, "reset clears timer");
            Expect(trigger.Evaluate("hold", left, 13 * second, 5) == left, "switch back to hold");
            trigger.Reset();
            Expect(trigger.Evaluate("timed5", left, 0, 12) == left, "custom duration begins");
            Expect(trigger.Evaluate("timed5", none, 11 * second, 12) == left,
                "custom duration remains visible");
            Expect(trigger.Evaluate("timed5", none, 12 * second, 12) == none,
                "custom duration expires");
            var swap = new OverlaySwapState();
            Expect(swap.BeginUpload() == 0 && !swap.Visible, "initial frame stays hidden while loading");
            int oldFront;
            bool hideOld;
            Expect(!swap.ImageLoaded(1, true, out oldFront, out hideOld) && swap.Pending == 0,
                "unrelated image event cannot reveal unfinished frame");
            Expect(swap.ImageLoaded(0, true, out oldFront, out hideOld) && swap.Visible && !hideOld,
                "first frame appears after image loaded");
            Expect(swap.BeginUpload() == 1 && swap.Front == 0 && swap.Visible,
                "visible front remains while back uploads");
            Expect(swap.ImageLoaded(1, true, out oldFront, out hideOld) &&
                swap.Front == 1 && oldFront == 0 && hideOld,
                "swap only after loaded event");
            Expect(swap.BeginUpload() == 0, "next update uses hidden handle");
            swap.ImageFailed(0);
            Expect(swap.Pending == -1 && swap.Front == 1 && swap.Visible,
                "failed upload retains visible frame");
            swap.Hide();
            Expect(!swap.Visible, "release hides frame");
            Application.EnableVisualStyles();
            using (var form = new MainForm(new Settings(), Path.GetTempPath()))
            {
                form.Show();
                Application.DoEvents();
                var choices = form.Controls.OfType<ComboBox>().ToArray();
                Expect(choices.Length == 4, "previous settings layout uses four native combo boxes");
                var duration = form.Controls.OfType<NumericUpDown>().Single();
                Expect(duration.Value == 5 && !duration.Enabled, "default duration waits for timed mode");
                foreach (ComboBox choice in choices)
                {
                    choice.DroppedDown = true;
                    choice.SelectedIndex = 1;
                    choice.DroppedDown = false;
                }
                Expect(duration.Enabled, "timed mode enables duration input");
                duration.Value = 12;
                form.Hide();
            }
            string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings-test.ini");
            try
            {
                var settings = new Settings { Appearance = "vrcx", TriggerMode = "timed5",
                    TimedSeconds = 12,
                    ShowGpuClock = false, ShowTemperatures = false };
                settings.Write(config);
                Settings restored = Settings.Read(config);
                Expect(restored.Appearance == "vrcx", "appearance persisted");
                Expect(restored.TriggerMode == "timed5", "trigger persisted");
                Expect(restored.TimedSeconds == 12, "custom duration persisted");
                Expect(!restored.ShowGpuClock && !restored.ShowTemperatures,
                    "hardware visibility persisted");
                File.WriteAllText(config, "Button=menu\n");
                Expect(Settings.Read(config).Appearance == "classic", "old settings default to first version");
                Expect(Settings.Read(config).TimedSeconds == 5, "old settings default to five seconds");
                File.WriteAllText(config, "TimedSeconds=999\n");
                Expect(Settings.Read(config).TimedSeconds == 5, "invalid duration keeps default");
            }
            finally { if (File.Exists(config)) File.Delete(config); }
            using (var bitmap = new Bitmap(2, 1, PixelFormat.Format32bppArgb))
            {
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 20, 40, 60));
                bitmap.SetPixel(1, 0, Color.FromArgb(128, 70, 90, 110));
                byte[] rgba = OverlayPixels.ToRgba(bitmap);
                Expect(rgba.Length == 8, "pixel buffer size");
                Expect(rgba[0] == 20 && rgba[1] == 40 && rgba[2] == 60 && rgba[3] == 255,
                    "first RGBA pixel");
                Expect(rgba[4] == 70 && rgba[5] == 90 && rgba[6] == 110 && rgba[7] == 128,
                    "second RGBA pixel and alpha");
            }
            using (var probe = new HardwareProbe())
            {
                var sample = new PanelData();
                probe.Read(sample);
                Expect(sample.Ram.HasValue && sample.Ram.Value >= 0 && sample.Ram.Value <= 100,
                    "physical memory usage available");
                Console.WriteLine("Hardware: RAM " + sample.Ram.Value.ToString("0") +
                    "%, CPU " + (sample.CpuClock.HasValue ? sample.CpuClock.Value.ToString("0") : "—") +
                    " MHz, GPU " + (sample.Gpu.HasValue ? sample.Gpu.Value.ToString("0") : "—") + "%");
            }
            Console.WriteLine("Trigger, settings, pixel, and hardware checks passed.");
        }
    }
}
