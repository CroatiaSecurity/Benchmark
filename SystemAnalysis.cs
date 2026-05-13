using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Management;
using System.Threading;

namespace GorstakBenchmark;

public class SystemAnalysisResult
{
    public string Motherboard { get; set; } = "Unknown";
    public string BiosVersion { get; set; } = "Unknown";
    public string BiosDate { get; set; } = "Unknown";
    public string RamSpeed { get; set; } = "Unknown";
    public string RamModules { get; set; } = "Unknown";
    public string GpuDriver { get; set; } = "Unknown";
    public string GpuDriverDate { get; set; } = "Unknown";

    public int WheaErrorCount { get; set; }
    public int GpuCrashCount { get; set; }
    public int RecentRebootCount { get; set; }
    public int AcpiBiosWarnings { get; set; }
    public int BluetoothErrors { get; set; }

    public List<string> Issues { get; set; } = new();
    public string OverallHealth { get; set; } = "Good";
}

public class SystemAnalyzer
{
    public IProgress<string>? Progress { get; set; }

    private void Report(string msg) => Progress?.Report(msg);

    public SystemAnalysisResult Run(CancellationToken ct = default)
    {
        var result = new SystemAnalysisResult();

        Report("Analyzing motherboard & BIOS...");
        GetMotherboardInfo(result);
        ct.ThrowIfCancellationRequested();

        Report("Analyzing RAM configuration...");
        GetRamInfo(result);
        ct.ThrowIfCancellationRequested();

        Report("Analyzing GPU driver...");
        GetGpuDriverInfo(result);
        ct.ThrowIfCancellationRequested();

        Report("Scanning event logs...");
        ScanEventLogs(result);
        ct.ThrowIfCancellationRequested();

        Report("Evaluating system health...");
        EvaluateHealth(result);

        return result;
    }

    private static void GetMotherboardInfo(SystemAnalysisResult r)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
            using var results = searcher.Get();
            var mb = results.Cast<ManagementObject>().FirstOrDefault();
            if (mb != null)
            {
                string mfr = mb["Manufacturer"]?.ToString() ?? "";
                string prod = mb["Product"]?.ToString() ?? "";
                r.Motherboard = $"{mfr} {prod}".Trim();
            }
        }
        catch { r.Motherboard = "Unknown"; }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");
            using var results = searcher.Get();
            var bios = results.Cast<ManagementObject>().FirstOrDefault();
            if (bios != null)
            {
                r.BiosVersion = bios["SMBIOSBIOSVersion"]?.ToString() ?? "Unknown";
                string dateStr = bios["ReleaseDate"]?.ToString() ?? "";
                if (dateStr.Length >= 8)
                {
                    try
                    {
                        var dt = ManagementDateTimeConverter.ToDateTime(dateStr);
                        r.BiosDate = dt.ToString("yyyy-MM-dd");
                    }
                    catch { r.BiosDate = dateStr[..8]; }
                }
            }
        }
        catch { r.BiosVersion = "Unknown"; r.BiosDate = "Unknown"; }
    }

    private static void GetRamInfo(SystemAnalysisResult r)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT BankLabel, Capacity, ConfiguredClockSpeed, Manufacturer FROM Win32_PhysicalMemory");
            using var results = searcher.Get();
            var modules = results.Cast<ManagementObject>().ToList();
            if (modules.Count > 0)
            {
                var first = modules[0];
                int speed = first["ConfiguredClockSpeed"] != null ? Convert.ToInt32(first["ConfiguredClockSpeed"]) : 0;
                string mfr = first["Manufacturer"]?.ToString()?.Trim() ?? "Unknown";
                long capEach = first["Capacity"] != null ? Convert.ToInt64(first["Capacity"]) : 0;
                int gbEach = (int)(capEach / (1024L * 1024 * 1024));

                r.RamSpeed = speed > 0 ? $"{speed} MHz" : "Unknown";
                r.RamModules = $"{modules.Count}x {gbEach}GB {mfr} @ {r.RamSpeed}";
            }
        }
        catch { r.RamSpeed = "Unknown"; r.RamModules = "Unknown"; }
    }

    private static void GetGpuDriverInfo(SystemAnalysisResult r)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT DriverVersion, DriverDate FROM Win32_VideoController");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                string? ver = mo["DriverVersion"]?.ToString();
                if (!string.IsNullOrEmpty(ver))
                {
                    r.GpuDriver = ver;
                    string dateStr = mo["DriverDate"]?.ToString() ?? "";
                    if (dateStr.Length >= 8)
                    {
                        try
                        {
                            var dt = ManagementDateTimeConverter.ToDateTime(dateStr);
                            r.GpuDriverDate = dt.ToString("yyyy-MM-dd");
                        }
                        catch { r.GpuDriverDate = "Unknown"; }
                    }
                    break;
                }
            }
        }
        catch { r.GpuDriver = "Unknown"; r.GpuDriverDate = "Unknown"; }
    }

    private static void ScanEventLogs(SystemAnalysisResult r)
    {
        try
        {
            var sinceStr = DateTime.Now.AddDays(-7).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z");

            // WHEA hardware errors
            r.WheaErrorCount = CountEvents("System",
                $"*[System[Provider[@Name='WHEA-Logger'] and TimeCreated[@SystemTime>='{sinceStr}']]]");

            // GPU crashes (nvlddmkm TDR, Display 4101/4097)
            r.GpuCrashCount = CountEvents("System",
                $"*[System[(Provider[@Name='nvlddmkm'] or (Provider[@Name='Display'] and (EventID=4101 or EventID=4097))) and TimeCreated[@SystemTime>='{sinceStr}']]]");

            // Reboots in last 24h
            var since24h = DateTime.Now.AddHours(-24).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.000Z");
            r.RecentRebootCount = CountEvents("System",
                $"*[System[Provider[@Name='Microsoft-Windows-Kernel-Power'] and EventID=109 and TimeCreated[@SystemTime>='{since24h}']]]");

            // ACPI BIOS warnings
            r.AcpiBiosWarnings = CountEvents("System",
                $"*[System[Provider[@Name='ACPI'] and (Level=2 or Level=3) and TimeCreated[@SystemTime>='{sinceStr}']]]");

            // Bluetooth errors
            r.BluetoothErrors = CountEvents("System",
                $"*[System[Provider[@Name='BTHUSB'] and (Level=1 or Level=2) and TimeCreated[@SystemTime>='{sinceStr}']]]");
        }
        catch { /* Event log access failed */ }
    }

    private static int CountEvents(string logName, string xpathQuery)
    {
        try
        {
            var logQuery = new EventLogQuery(logName, PathType.LogName, xpathQuery);
            using var reader = new EventLogReader(logQuery);
            int count = 0;
            while (reader.ReadEvent() != null) count++;
            return count;
        }
        catch { return 0; }
    }

    private static void EvaluateHealth(SystemAnalysisResult r)
    {
        if (r.WheaErrorCount > 0)
            r.Issues.Add($"WHEA hardware errors: {r.WheaErrorCount} (memory/PCIe issues)");

        if (r.GpuCrashCount > 0)
            r.Issues.Add($"GPU driver crashes: {r.GpuCrashCount} (TDR/nvlddmkm)");

        if (r.RecentRebootCount > 2)
            r.Issues.Add($"Frequent reboots: {r.RecentRebootCount} in last 24h (updates or instability)");

        if (r.AcpiBiosWarnings > 0)
            r.Issues.Add($"ACPI BIOS warnings: {r.AcpiBiosWarnings} (consider BIOS update)");

        if (r.BluetoothErrors > 0)
            r.Issues.Add($"Bluetooth adapter errors: {r.BluetoothErrors}");

        // Determine overall health
        if (r.WheaErrorCount > 5 || r.GpuCrashCount > 3)
            r.OverallHealth = "Critical";
        else if (r.WheaErrorCount > 0 || r.GpuCrashCount > 0 || r.RecentRebootCount > 3)
            r.OverallHealth = "Warning";
        else if (r.AcpiBiosWarnings > 0 || r.BluetoothErrors > 0 || r.RecentRebootCount > 1)
            r.OverallHealth = "Minor Issues";
        else
            r.OverallHealth = "Good";

        if (r.Issues.Count == 0)
            r.Issues.Add("No issues detected — system looks healthy.");
    }
}
