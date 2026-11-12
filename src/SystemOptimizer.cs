using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MemOptima
{
    public class SystemOptimizer
    {
        [DllImport("psapi.dll", SetLastError = true)]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public class MemoryStatus
        {
            public double TotalGB { get; set; }
            public double UsedGB { get; set; }
            public double FreeGB { get; set; }
            public uint LoadPercentage { get; set; }
        }

        public static MemoryStatus GetMemoryStatus()
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                double total = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                double free = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                return new MemoryStatus
                {
                    TotalGB = Math.Round(total, 2),
                    FreeGB = Math.Round(free, 2),
                    UsedGB = Math.Round(total - free, 2),
                    LoadPercentage = memStatus.dwMemoryLoad
                };
            }
            return new MemoryStatus();
        }

        public static int OptimizeRAM(out double freedMB)
        {
            MemoryStatus before = GetMemoryStatus();
            int successProcesses = 0;
            Process[] processes = Process.GetProcesses();

            foreach (Process proc in processes)
            {
                try
                {
                    if (proc.Id <= 4) continue;
                    if (EmptyWorkingSet(proc.Handle) != 0)
                    {
                        successProcesses++;
                    }
                }
                catch
                {
                    continue;
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);

            MemoryStatus after = GetMemoryStatus();
            freedMB = Math.Max(0, (before.UsedGB - after.UsedGB) * 1024.0);
            return successProcesses;
        }

        public static bool CompressFolder(string folderPath, out string outputLog)
        {
            outputLog = string.Empty;
            if (!Directory.Exists(folderPath))
            {
                outputLog = "Указанная директория не найдена.";
                return false;
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "compact.exe",
                Arguments = $"/C /S:\"{folderPath}\" /EXE:XPRESS8K /I /Q",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (Process p = Process.Start(psi))
            {
                outputLog = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode == 0;
            }
        }

        public static long PurgeApplicationCaches()
        {
            long freedBytes = 0;
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string tempDir = Path.GetTempPath();

            string[] targetFolders = new string[]
            {
                tempDir,
                Path.Combine(localApp, "Discord", "Cache"),
                Path.Combine(localApp, "Telegram Desktop", "tdata", "user_data", "cache"),
                Path.Combine(localApp, "Google", "Chrome", "User Data", "Default", "Cache"),
                Path.Combine(localApp, "Yandex", "YandexBrowser", "User Data", "Default", "Cache")
            };

            foreach (string folder in targetFolders)
            {
                if (!Directory.Exists(folder)) continue;
                DirectoryInfo dir = new DirectoryInfo(folder);
                try
                {
                    foreach (FileInfo file in dir.GetFiles())
                    {
                        try
                        {
                            freedBytes += file.Length;
                            file.Delete();
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return freedBytes / (1024 * 1024);
        }
    }
}
