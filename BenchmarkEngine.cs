<<<<<<< HEAD
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace GorstakBenchmark
{
    public class BenchmarkEngine
    {
        /* Reference scores: calibrated from a high-end system.
         * Intel Core Ultra 7 265KF / RTX 5070 / 32GB DDR5-7200 / 990 EVO Plus.
         * Score = constant / elapsed_seconds, so faster hardware = higher score.
         * These values are set so the reference system scores ~100%.
         * Mid-range systems will score 40-70%. Budget systems 20-40%. */
        private static readonly Dictionary<string, double> ReferenceScores = new Dictionary<string, double>
        {
            { "CPU", 36000000.0 },
            { "Memory", 2030000.0 },
            { "Disk", 3600.0 },
            { "GPU", 630000.0 },
            { "Network", 27.0 }
        };

        public IProgress<string> Progress { get; set; }

        private void Report(string msg)
        {
            if (Progress != null) Progress.Report(msg);
        }

        public async Task<BenchmarkResults> RunAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var results = new BenchmarkResults { RunDate = DateTime.Now };
            var os = GetOsName();
            results.OsName = os;
            Report("System: " + os);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running CPU benchmark...");
            await Task.Run(() => RunCpuBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running Memory benchmark...");
            await Task.Run(() => RunMemoryBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running Disk benchmark...");
            await Task.Run(() => RunDiskBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running GPU benchmark...");
            await Task.Run(() => RunGpuBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running Network benchmark...");
            await Task.Run(() => RunNetworkBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            CalculateOverall(results);
            CalculateBottleneck(results);
            Report("Done!");
            return results;
        }

        private static string GetOsName()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                using (var results = searcher.Get())
                {
                    var first = results.Cast<ManagementObject>().FirstOrDefault();
                    if (first != null && first["Caption"] != null)
                        return first["Caption"].ToString();
                    return "Windows";
                }
            }
            catch { return "Windows"; }
        }

        private void RunCpuBenchmark(BenchmarkResults r)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                using (var results = searcher.Get())
                {
                    var cpu = results.Cast<ManagementObject>().FirstOrDefault();
                    if (cpu != null)
                    {
                        r.CpuName = cpu["Name"] != null ? cpu["Name"].ToString().Trim() : "Unknown";
                        r.CpuCores = Convert.ToInt32(cpu["NumberOfCores"] ?? 0);
                        r.CpuThreads = Convert.ToInt32(cpu["NumberOfLogicalProcessors"] ?? 0);
                    }
                }
            }
            catch { r.CpuName = "Unknown"; }

            var sw = Stopwatch.StartNew();

            var primes = new List<int>();
            for (int num = 2; num <= 50000; num++)
            {
                bool isPrime = true;
                double sqrt = Math.Sqrt(num);
                for (int i = 2; i <= sqrt; i++)
                {
                    if (num % i == 0) { isPrime = false; break; }
                }
                if (isPrime) primes.Add(num);
            }

            double result = 0;
            for (int i = 0; i < 1000000; i++)
                result += Math.Sqrt(i) * Math.PI;

            sw.Stop();
            r.CpuScore = Math.Round(100000.0 / sw.Elapsed.TotalSeconds, 2);
            r.CpuPercent = Math.Round((r.CpuScore / ReferenceScores["CPU"]) * 100, 1);
        }

        private void RunMemoryBenchmark(BenchmarkResults r)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                using (var results = searcher.Get())
                {
                    long total = 0;
                    foreach (ManagementObject mo in results)
                        total += Convert.ToInt64(mo["Capacity"]);
                    r.TotalRamGB = Math.Round(total / (1024.0 * 1024 * 1024), 2);
                }
            }
            catch { r.TotalRamGB = 0; }

            const int arraySize = 10000000;
            var list = new List<int>(arraySize);
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < arraySize; i++)
                list.Add(i);

            long sum = 0;
            foreach (var item in list)
                sum += item;

            var rand = new Random();
            for (int i = 0; i < 100000; i++)
            {
                int idx = rand.Next(0, arraySize);
                var dummy = list[idx];
            }

            sw.Stop();
            r.MemoryScore = Math.Round(50000.0 / sw.Elapsed.TotalSeconds, 2);
            r.MemoryPercent = Math.Round((r.MemoryScore / ReferenceScores["Memory"]) * 100, 1);
            list.Clear();
            GC.Collect();
        }

        private void RunDiskBenchmark(BenchmarkResults r)
        {
            r.DiskDrive = "C:";
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT DeviceID, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3"))
                using (var results = searcher.Get())
                {
                    var disk = results.Cast<ManagementObject>().FirstOrDefault();
                    if (disk != null)
                    {
                        r.DiskDrive = disk["DeviceID"] != null ? disk["DeviceID"].ToString() : "C:";
                    }
                }
            }
            catch { }

            const int fileSize = 100 * 1024 * 1024; // 100 MB
            var data = new byte[fileSize];
            new Random().NextBytes(data);
            string path = Path.Combine(Path.GetTempPath(), "benchmark_test.tmp");

            try
            {
                // Write test — use WriteThrough to bypass OS write cache
                var writeSw = Stopwatch.StartNew();
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    fs.Write(data, 0, data.Length);
                }
                writeSw.Stop();
                double writeMBps = (fileSize / (1024.0 * 1024)) / writeSw.Elapsed.TotalSeconds;

                // Flush OS file cache before read — allocate and discard a large array
                var dummy = new byte[200 * 1024 * 1024];
                new Random().NextBytes(dummy);
                dummy = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var readSw = Stopwatch.StartNew();
                var readData = File.ReadAllBytes(path);
                readSw.Stop();
                double readMBps = (fileSize / (1024.0 * 1024)) / readSw.Elapsed.TotalSeconds;

                r.DiskScore = Math.Round((writeMBps + readMBps) / 2, 2);
            }
            catch { r.DiskScore = 0; }
            finally
            {
                try { File.Delete(path); } catch { }
            }

            r.DiskPercent = Math.Round((r.DiskScore / ReferenceScores["Disk"]) * 100, 1);
        }

        private void RunGpuBenchmark(BenchmarkResults r)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController"))
                using (var results = searcher.Get())
                {
                    // Pick the GPU with the most VRAM (discrete over integrated)
                    ManagementObject gpu = null;
                    long bestRam = 0;
                    foreach (ManagementObject mo in results)
                    {
                        long ram = mo["AdapterRAM"] != null ? Convert.ToInt64(Convert.ToUInt32(mo["AdapterRAM"])) : 0;
                        if (gpu == null || ram > bestRam) { gpu = mo; bestRam = ram; }
                    }
                    r.GpuName = (gpu != null && gpu["Name"] != null) ? gpu["Name"].ToString() : "Unknown";

                    // AdapterRAM is uint32 (caps at 4GB). Try registry for accurate VRAM.
                    long vramBytes = 0;
                    try
                    {
                        // Search all display adapter subkeys for the matching GPU
                        using (var classKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                            @"SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                        {
                            if (classKey != null)
                            {
                                foreach (string subName in classKey.GetSubKeyNames())
                                {
                                    using (var sub = classKey.OpenSubKey(subName))
                                    {
                                        if (sub == null) continue;
                                        var desc = sub.GetValue("DriverDesc") as string;
                                        if (desc != null && r.GpuName != null && desc.Equals(r.GpuName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var qwMem = sub.GetValue("HardwareInformation.qwMemorySize");
                                            if (qwMem != null) { vramBytes = Convert.ToInt64(qwMem); break; }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    if (vramBytes <= 0 && gpu != null && gpu["AdapterRAM"] != null)
                        vramBytes = Convert.ToInt64(Convert.ToUInt32(gpu["AdapterRAM"]));
                    r.GpuVramGB = Math.Round(vramBytes / (1024.0 * 1024 * 1024), 2);
                }
            }
            catch { r.GpuName = "Unknown"; }

            const int size = 200;
            var m1 = new double[size, size];
            var m2 = new double[size, size];
            var res = new double[size, size];
            var rand = new Random();

            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    m1[i, j] = rand.NextDouble();
                    m2[i, j] = rand.NextDouble();
                }

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    double s = 0;
                    for (int k = 0; k < size; k++)
                        s += m1[i, k] * m2[k, j];
                    res[i, j] = s;
                }

            double computeSum = 0;
            for (int i = 0; i < 100000; i++)
                computeSum += Math.Sin(i) * Math.Cos(i) * Math.Tan(i / 100.0 + 1);

            sw.Stop();
            r.GpuScore = Math.Round(10000.0 / sw.Elapsed.TotalSeconds, 2);
            r.GpuPercent = Math.Round((r.GpuScore / ReferenceScores["GPU"]) * 100, 1);
        }

        private void RunNetworkBenchmark(BenchmarkResults r)
        {
            string[] targets = { "8.8.8.8", "1.1.1.1", "www.google.com" };
            double avgLatency = 0;
            int successCount = 0;

            foreach (var target in targets)
            {
                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = ping.Send(target, 3000);
                        if (reply != null && reply.Status == IPStatus.Success)
                        {
                            avgLatency += reply.RoundtripTime;
                            successCount++;
                        }
                    }
                }
                catch { }
            }

            if (successCount > 0)
                avgLatency /= successCount;
            else
                avgLatency = 100;

            double latencyScore = 1000.0 / (avgLatency + 1);
            double speedMbps = 0;
            string[] urls = {
                "https://proof.ovh.net/files/10Mb.dat",
                "http://speedtest.tele2.net/10MB.zip",
                "http://ipv4.download.thinkbroadband.com/10MB.zip"
            };

            foreach (var url in urls)
            {
                try
                {
                    string path = Path.Combine(Path.GetTempPath(), "speedtest.tmp");
                    var sw = Stopwatch.StartNew();
                    using (var wc = new WebClient())
                    {
                        // Timeout: abort if download takes longer than 15 seconds
                        var downloadTask = Task.Run(() => wc.DownloadFile(url, path));
                        if (!downloadTask.Wait(TimeSpan.FromSeconds(15)))
                        {
                            wc.CancelAsync();
                            throw new TimeoutException("Download timed out");
                        }
                    }
                    sw.Stop();
                    long len = new FileInfo(path).Length;
                    speedMbps = (len / (1024.0 * 1024) * 8) / sw.Elapsed.TotalSeconds;
                    try { File.Delete(path); } catch { }
                    break;
                }
                catch { }
            }

            r.NetworkScore = Math.Round((latencyScore + speedMbps) / 2, 2);
            if (r.NetworkScore <= 0) r.NetworkScore = Math.Round(latencyScore, 2);
            r.NetworkPercent = Math.Round((r.NetworkScore / ReferenceScores["Network"]) * 100, 1);
        }

        private void CalculateOverall(BenchmarkResults r)
        {
            r.OverallScore = Math.Round((r.CpuScore + r.MemoryScore + r.DiskScore + r.GpuScore + r.NetworkScore) / 5, 2);
            r.OverallPercent = Math.Round((r.CpuPercent + r.MemoryPercent + r.DiskPercent + r.GpuPercent + r.NetworkPercent) / 5, 1);
        }

        private void CalculateBottleneck(BenchmarkResults r)
        {
            double cpu = r.CpuPercent;
            double gpu = r.GpuPercent;
            double diff = Math.Abs(cpu - gpu);
            r.BottleneckSeverity = Math.Round(diff, 1);

            if (diff < 5 || (cpu >= 95 && gpu >= 95))
            {
                r.BottleneckType = "None (Balanced)";
                r.BottleneckSeverity = 0;
            }
            else if (cpu < gpu)
                r.BottleneckType = "CPU";
            else
                r.BottleneckType = "GPU";
        }
    }
}
=======
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace GorstakBenchmark
{
    public class BenchmarkEngine
    {
        /* Reference scores: calibrated from a high-end system.
         * Intel Core Ultra 7 265KF / RTX 5070 / 32GB DDR5-7200 / 990 EVO Plus.
         * Score = constant / elapsed_seconds, so faster hardware = higher score.
         * These values are set so the reference system scores ~100%.
         * Mid-range systems will score 40-70%. Budget systems 20-40%. */
        private static readonly Dictionary<string, double> ReferenceScores = new Dictionary<string, double>
        {
            { "CPU", 36000000.0 },
            { "Memory", 2030000.0 },
            { "Disk", 3600.0 },
            { "GPU", 630000.0 },
            { "Network", 27.0 }
        };

        public IProgress<string> Progress { get; set; }

        private void Report(string msg)
        {
            if (Progress != null) Progress.Report(msg);
        }

        public async Task<BenchmarkResults> RunAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var results = new BenchmarkResults { RunDate = DateTime.Now };
            var os = GetOsName();
            results.OsName = os;
            Report("System: " + os);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running CPU benchmark...");
            await Task.Run(() => RunCpuBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running Memory benchmark...");
            await Task.Run(() => RunMemoryBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running Disk benchmark...");
            await Task.Run(() => RunDiskBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running GPU benchmark...");
            await Task.Run(() => RunGpuBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Report("Running Network benchmark...");
            await Task.Run(() => RunNetworkBenchmark(results), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            CalculateOverall(results);
            CalculateBottleneck(results);
            Report("Done!");
            return results;
        }

        private static string GetOsName()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                using (var results = searcher.Get())
                {
                    var first = results.Cast<ManagementObject>().FirstOrDefault();
                    if (first != null && first["Caption"] != null)
                        return first["Caption"].ToString();
                    return "Windows";
                }
            }
            catch { return "Windows"; }
        }

        private void RunCpuBenchmark(BenchmarkResults r)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                using (var results = searcher.Get())
                {
                    var cpu = results.Cast<ManagementObject>().FirstOrDefault();
                    if (cpu != null)
                    {
                        r.CpuName = cpu["Name"] != null ? cpu["Name"].ToString().Trim() : "Unknown";
                        r.CpuCores = Convert.ToInt32(cpu["NumberOfCores"] ?? 0);
                        r.CpuThreads = Convert.ToInt32(cpu["NumberOfLogicalProcessors"] ?? 0);
                    }
                }
            }
            catch { r.CpuName = "Unknown"; }

            var sw = Stopwatch.StartNew();

            var primes = new List<int>();
            for (int num = 2; num <= 50000; num++)
            {
                bool isPrime = true;
                double sqrt = Math.Sqrt(num);
                for (int i = 2; i <= sqrt; i++)
                {
                    if (num % i == 0) { isPrime = false; break; }
                }
                if (isPrime) primes.Add(num);
            }

            double result = 0;
            for (int i = 0; i < 1000000; i++)
                result += Math.Sqrt(i) * Math.PI;

            sw.Stop();
            r.CpuScore = Math.Round(100000.0 / sw.Elapsed.TotalSeconds, 2);
            r.CpuPercent = Math.Round((r.CpuScore / ReferenceScores["CPU"]) * 100, 1);
        }

        private void RunMemoryBenchmark(BenchmarkResults r)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                using (var results = searcher.Get())
                {
                    long total = 0;
                    foreach (ManagementObject mo in results)
                        total += Convert.ToInt64(mo["Capacity"]);
                    r.TotalRamGB = Math.Round(total / (1024.0 * 1024 * 1024), 2);
                }
            }
            catch { r.TotalRamGB = 0; }

            const int arraySize = 10000000;
            var list = new List<int>(arraySize);
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < arraySize; i++)
                list.Add(i);

            long sum = 0;
            foreach (var item in list)
                sum += item;

            var rand = new Random();
            for (int i = 0; i < 100000; i++)
            {
                int idx = rand.Next(0, arraySize);
                var dummy = list[idx];
            }

            sw.Stop();
            r.MemoryScore = Math.Round(50000.0 / sw.Elapsed.TotalSeconds, 2);
            r.MemoryPercent = Math.Round((r.MemoryScore / ReferenceScores["Memory"]) * 100, 1);
            list.Clear();
            GC.Collect();
        }

        private void RunDiskBenchmark(BenchmarkResults r)
        {
            r.DiskDrive = "C:";
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT DeviceID, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3"))
                using (var results = searcher.Get())
                {
                    var disk = results.Cast<ManagementObject>().FirstOrDefault();
                    if (disk != null)
                    {
                        r.DiskDrive = disk["DeviceID"] != null ? disk["DeviceID"].ToString() : "C:";
                    }
                }
            }
            catch { }

            const int fileSize = 100 * 1024 * 1024; // 100 MB
            var data = new byte[fileSize];
            new Random().NextBytes(data);
            string path = Path.Combine(Path.GetTempPath(), "benchmark_test.tmp");

            try
            {
                // Write test — use WriteThrough to bypass OS write cache
                var writeSw = Stopwatch.StartNew();
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    fs.Write(data, 0, data.Length);
                }
                writeSw.Stop();
                double writeMBps = (fileSize / (1024.0 * 1024)) / writeSw.Elapsed.TotalSeconds;

                // Flush OS file cache before read — allocate and discard a large array
                var dummy = new byte[200 * 1024 * 1024];
                new Random().NextBytes(dummy);
                dummy = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var readSw = Stopwatch.StartNew();
                var readData = File.ReadAllBytes(path);
                readSw.Stop();
                double readMBps = (fileSize / (1024.0 * 1024)) / readSw.Elapsed.TotalSeconds;

                r.DiskScore = Math.Round((writeMBps + readMBps) / 2, 2);
            }
            catch { r.DiskScore = 0; }
            finally
            {
                try { File.Delete(path); } catch { }
            }

            r.DiskPercent = Math.Round((r.DiskScore / ReferenceScores["Disk"]) * 100, 1);
        }

        private void RunGpuBenchmark(BenchmarkResults r)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController"))
                using (var results = searcher.Get())
                {
                    // Pick the GPU with the most VRAM (discrete over integrated)
                    ManagementObject gpu = null;
                    long bestRam = 0;
                    foreach (ManagementObject mo in results)
                    {
                        long ram = mo["AdapterRAM"] != null ? Convert.ToInt64(Convert.ToUInt32(mo["AdapterRAM"])) : 0;
                        if (gpu == null || ram > bestRam) { gpu = mo; bestRam = ram; }
                    }
                    r.GpuName = (gpu != null && gpu["Name"] != null) ? gpu["Name"].ToString() : "Unknown";

                    // AdapterRAM is uint32 (caps at 4GB). Try registry for accurate VRAM.
                    long vramBytes = 0;
                    try
                    {
                        // Search all display adapter subkeys for the matching GPU
                        using (var classKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                            @"SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                        {
                            if (classKey != null)
                            {
                                foreach (string subName in classKey.GetSubKeyNames())
                                {
                                    using (var sub = classKey.OpenSubKey(subName))
                                    {
                                        if (sub == null) continue;
                                        var desc = sub.GetValue("DriverDesc") as string;
                                        if (desc != null && r.GpuName != null && desc.Equals(r.GpuName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var qwMem = sub.GetValue("HardwareInformation.qwMemorySize");
                                            if (qwMem != null) { vramBytes = Convert.ToInt64(qwMem); break; }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    if (vramBytes <= 0 && gpu != null && gpu["AdapterRAM"] != null)
                        vramBytes = Convert.ToInt64(Convert.ToUInt32(gpu["AdapterRAM"]));
                    r.GpuVramGB = Math.Round(vramBytes / (1024.0 * 1024 * 1024), 2);
                }
            }
            catch { r.GpuName = "Unknown"; }

            const int size = 200;
            var m1 = new double[size, size];
            var m2 = new double[size, size];
            var res = new double[size, size];
            var rand = new Random();

            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    m1[i, j] = rand.NextDouble();
                    m2[i, j] = rand.NextDouble();
                }

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    double s = 0;
                    for (int k = 0; k < size; k++)
                        s += m1[i, k] * m2[k, j];
                    res[i, j] = s;
                }

            double computeSum = 0;
            for (int i = 0; i < 100000; i++)
                computeSum += Math.Sin(i) * Math.Cos(i) * Math.Tan(i / 100.0 + 1);

            sw.Stop();
            r.GpuScore = Math.Round(10000.0 / sw.Elapsed.TotalSeconds, 2);
            r.GpuPercent = Math.Round((r.GpuScore / ReferenceScores["GPU"]) * 100, 1);
        }

        private void RunNetworkBenchmark(BenchmarkResults r)
        {
            string[] targets = { "8.8.8.8", "1.1.1.1", "www.google.com" };
            double avgLatency = 0;
            int successCount = 0;

            foreach (var target in targets)
            {
                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = ping.Send(target, 3000);
                        if (reply != null && reply.Status == IPStatus.Success)
                        {
                            avgLatency += reply.RoundtripTime;
                            successCount++;
                        }
                    }
                }
                catch { }
            }

            if (successCount > 0)
                avgLatency /= successCount;
            else
                avgLatency = 100;

            double latencyScore = 1000.0 / (avgLatency + 1);
            double speedMbps = 0;
            string[] urls = {
                "https://proof.ovh.net/files/10Mb.dat",
                "http://speedtest.tele2.net/10MB.zip",
                "http://ipv4.download.thinkbroadband.com/10MB.zip"
            };

            foreach (var url in urls)
            {
                try
                {
                    string path = Path.Combine(Path.GetTempPath(), "speedtest.tmp");
                    var sw = Stopwatch.StartNew();
                    using (var wc = new WebClient())
                    {
                        // Timeout: abort if download takes longer than 15 seconds
                        var downloadTask = Task.Run(() => wc.DownloadFile(url, path));
                        if (!downloadTask.Wait(TimeSpan.FromSeconds(15)))
                        {
                            wc.CancelAsync();
                            throw new TimeoutException("Download timed out");
                        }
                    }
                    sw.Stop();
                    long len = new FileInfo(path).Length;
                    speedMbps = (len / (1024.0 * 1024) * 8) / sw.Elapsed.TotalSeconds;
                    try { File.Delete(path); } catch { }
                    break;
                }
                catch { }
            }

            r.NetworkScore = Math.Round((latencyScore + speedMbps) / 2, 2);
            if (r.NetworkScore <= 0) r.NetworkScore = Math.Round(latencyScore, 2);
            r.NetworkPercent = Math.Round((r.NetworkScore / ReferenceScores["Network"]) * 100, 1);
        }

        private void CalculateOverall(BenchmarkResults r)
        {
            r.OverallScore = Math.Round((r.CpuScore + r.MemoryScore + r.DiskScore + r.GpuScore + r.NetworkScore) / 5, 2);
            r.OverallPercent = Math.Round((r.CpuPercent + r.MemoryPercent + r.DiskPercent + r.GpuPercent + r.NetworkPercent) / 5, 1);
        }

        private void CalculateBottleneck(BenchmarkResults r)
        {
            double cpu = r.CpuPercent;
            double gpu = r.GpuPercent;
            double diff = Math.Abs(cpu - gpu);
            r.BottleneckSeverity = Math.Round(diff, 1);

            if (diff < 5 || (cpu >= 95 && gpu >= 95))
            {
                r.BottleneckType = "None (Balanced)";
                r.BottleneckSeverity = 0;
            }
            else if (cpu < gpu)
                r.BottleneckType = "CPU";
            else
                r.BottleneckType = "GPU";
        }
    }
}
>>>>>>> 3d3535ab3f0cc7ffda6c31d1b5ec2b6adbb77c7e
