using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace MuelleLauncher
{
    static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        const uint GW_HWNDPREV = 3;

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        static Mutex singleMutex;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            singleMutex = new Mutex(true, "MuelleUnifiedLauncher_SingleInstance", out createdNew);
            if (!createdNew)
            {
                // Another instance is already managing the windows
                return;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string notchpyxDir = Path.Combine(baseDir, "notchpyx");
            string dynamicBarExe = Path.Combine(notchpyxDir, "DynamicBar.exe");
            string muelleExe = Path.Combine(baseDir, "muelle.exe");

            // 1. Launch Muelle Dock if not running
            if (File.Exists(muelleExe) && Process.GetProcessesByName("muelle").Length == 0)
            {
                ProcessStartInfo psiMuelle = new ProcessStartInfo(muelleExe)
                {
                    WorkingDirectory = baseDir,
                    UseShellExecute = true
                };
                try { Process.Start(psiMuelle); } catch { }
                Thread.Sleep(800);
            }

            // 2. Launch Notchpyx DynamicBar if not running
            if (File.Exists(dynamicBarExe) && Process.GetProcessesByName("DynamicBar").Length == 0)
            {
                ProcessStartInfo psiNotch = new ProcessStartInfo(dynamicBarExe)
                {
                    WorkingDirectory = notchpyxDir,
                    UseShellExecute = true
                };
                try { Process.Start(psiNotch); } catch { }
                Thread.Sleep(600);
            }

            // 3. Keep Dynamic Island in front of Muelle's navbar
            while (true)
            {
                Thread.Sleep(150);

                var muelleProcs = Process.GetProcessesByName("muelle");
                var dbProcs = Process.GetProcessesByName("DynamicBar");

                if (muelleProcs.Length == 0 && dbProcs.Length == 0)
                {
                    break;
                }

                IntPtr hDynamicBar = FindWindow("DynamicBarIslandStage", null);
                IntPtr hMuelle = FindWindow("MuelleMenuBar", null);

                if (hDynamicBar != IntPtr.Zero && IsWindow(hDynamicBar))
                {
                    // Check if hDynamicBar is already at the very top of topmost Z-order
                    IntPtr prev = GetWindow(hDynamicBar, GW_HWNDPREV);
                    if (prev != IntPtr.Zero)
                    {
                        // Bring DynamicBar to absolute top of topmost
                        SetWindowPos(hDynamicBar, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

                        // If Muelle navbar exists, place it directly behind DynamicBar
                        if (hMuelle != IntPtr.Zero && IsWindow(hMuelle))
                        {
                            SetWindowPos(hMuelle, hDynamicBar, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                        }
                    }
                }
            }
        }
    }
}
