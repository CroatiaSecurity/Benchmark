using System;
using System.Globalization;
using System.Text;

namespace GorstakBenchmark
{
    public class BenchmarkResults
    {
        public string CpuName { get; set; }
        public int CpuCores { get; set; }
        public int CpuThreads { get; set; }
        public double CpuScore { get; set; }
        public double CpuPercent { get; set; }

        public double TotalRamGB { get; set; }
        public double MemoryScore { get; set; }
        public double MemoryPercent { get; set; }

        public string DiskDrive { get; set; }
        public double DiskScore { get; set; }
        public double DiskPercent { get; set; }

        public string GpuName { get; set; }
        public double GpuVramGB { get; set; }
        public double GpuScore { get; set; }
        public double GpuPercent { get; set; }

        public double NetworkScore { get; set; }
        public double NetworkPercent { get; set; }

        public double OverallScore { get; set; }
        public double OverallPercent { get; set; }

        public string BottleneckType { get; set; }
        public double BottleneckSeverity { get; set; }

        public DateTime RunDate { get; set; }
        public string OsName { get; set; }

        public SystemAnalysisResult SystemAnalysis { get; set; }

        private static double Cap(double p) { return Math.Round(p, 1); }

        public string GetShareableText()
        {
            double overall = Cap((Cap(CpuPercent) + Cap(GpuPercent) + Cap(MemoryPercent) + Cap(DiskPercent) + Cap(NetworkPercent)) / 5);
            var sb = new StringBuilder();
            sb.AppendFormat("Gorstak Benchmark v0.4 | Overall: {0:N0} ({1}%) | CPU:{2}% GPU:{3}% RAM:{4}% Disk:{5}% Net:{6}% | Bottleneck: {7} ({8:N1}) | {9:yyyy-MM-dd}",
                OverallScore, overall, Cap(CpuPercent), Cap(GpuPercent), Cap(MemoryPercent), Cap(DiskPercent), Cap(NetworkPercent), BottleneckType, BottleneckSeverity, RunDate);

            if (SystemAnalysis != null)
                sb.AppendFormat(" | Health: {0}", SystemAnalysis.OverallHealth);

            return sb.ToString();
        }

        public string GetJson()
        {
            var sb = new StringBuilder();
            var c = CultureInfo.InvariantCulture;
            sb.Append("{\r\n");
            sb.AppendFormat(c, "  \"cpu\": {{\"score\": {0}, \"percent\": {1}, \"name\": \"{2}\"}},\r\n", CpuScore, CpuPercent, EscapeJson(CpuName ?? ""));
            sb.AppendFormat(c, "  \"memory\": {{\"score\": {0}, \"percent\": {1}, \"ramGB\": {2}}},\r\n", MemoryScore, MemoryPercent, TotalRamGB);
            sb.AppendFormat(c, "  \"disk\": {{\"score\": {0}, \"percent\": {1}, \"drive\": \"{2}\"}},\r\n", DiskScore, DiskPercent, EscapeJson(DiskDrive ?? ""));
            sb.AppendFormat(c, "  \"gpu\": {{\"score\": {0}, \"percent\": {1}, \"name\": \"{2}\", \"vramGB\": {3}}},\r\n", GpuScore, GpuPercent, EscapeJson(GpuName ?? ""), GpuVramGB);
            sb.AppendFormat(c, "  \"network\": {{\"score\": {0}, \"percent\": {1}}},\r\n", NetworkScore, NetworkPercent);
            sb.AppendFormat(c, "  \"overall\": {{\"score\": {0}, \"percent\": {1}}},\r\n", OverallScore, OverallPercent);
            sb.AppendFormat(c, "  \"bottleneck\": {{\"type\": \"{0}\", \"severity\": {1}}},\r\n", BottleneckType, BottleneckSeverity);

            if (SystemAnalysis != null)
            {
                sb.Append("  \"systemAnalysis\": {\r\n");
                sb.AppendFormat(c, "    \"health\": \"{0}\",\r\n", EscapeJson(SystemAnalysis.OverallHealth ?? ""));
                sb.AppendFormat(c, "    \"motherboard\": \"{0}\",\r\n", EscapeJson(SystemAnalysis.Motherboard ?? ""));
                sb.AppendFormat(c, "    \"bios\": \"{0}\",\r\n", EscapeJson(SystemAnalysis.BiosVersion ?? ""));
                sb.AppendFormat(c, "    \"ram\": \"{0}\",\r\n", EscapeJson(SystemAnalysis.RamModules ?? ""));
                sb.AppendFormat(c, "    \"gpuDriver\": \"{0}\",\r\n", EscapeJson(SystemAnalysis.GpuDriver ?? ""));
                sb.AppendFormat(c, "    \"wheaErrors\": {0},\r\n", SystemAnalysis.WheaErrorCount);
                sb.AppendFormat(c, "    \"gpuCrashes\": {0},\r\n", SystemAnalysis.GpuCrashCount);
                sb.AppendFormat(c, "    \"rebootsLast24h\": {0},\r\n", SystemAnalysis.RecentRebootCount);
                sb.Append("    \"issues\": [");
                for (int i = 0; i < SystemAnalysis.Issues.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.AppendFormat("\"{0}\"", EscapeJson(SystemAnalysis.Issues[i]));
                }
                sb.Append("]\r\n  },\r\n");
            }

            sb.AppendFormat(c, "  \"date\": \"{0}\",\r\n", RunDate.ToString("o"));
            sb.AppendFormat(c, "  \"os\": \"{0}\"\r\n", EscapeJson(OsName ?? ""));
            sb.Append("}");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        public string GetHtmlReport()
        {
            var sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><title>Benchmark Results</title>
<style>body{font-family:Segoe UI,sans-serif;max-width:700px;margin:40px auto;padding:20px;background:#0d0e16;color:#e2e4eb;}
h1{color:#6366f1;} h2{color:#a78bfa;margin-top:24px;} .score{font-size:1.2em;font-weight:bold;color:#6366f1;}
.bar{height:20px;background:#1a1c28;border-radius:4px;margin:4px 0;overflow:hidden;}
.bar-fill{height:100%;background:linear-gradient(90deg,#6366f1,#3b82f6);border-radius:4px;}
table{width:100%;border-collapse:collapse;} td{padding:8px;border-bottom:1px solid #2a2d3a;}
.good{color:#22d3ee;} .warning{color:#fbbf24;} .critical{color:#ef4444;} .minor{color:#a78bfa;}
</style></head><body>");
            sb.AppendFormat("<h1>Gorstak Benchmark v0.4</h1>\r\n");
            sb.AppendFormat("<p><strong>Date:</strong> {0:yyyy-MM-dd HH:mm}</p>\r\n", RunDate);
            sb.AppendFormat("<p><strong>Overall Score:</strong> <span class=\"score\">{0:N0}</span> ({1}%)</p>\r\n", OverallScore, OverallPercent);
            sb.AppendFormat("<p><strong>Bottleneck:</strong> {0} (Severity: {1:N1})</p>\r\n", BottleneckType, BottleneckSeverity);
            sb.Append("<table>\r\n");
            AppendHtmlRow(sb, "CPU", CpuScore, CpuPercent);
            AppendHtmlRow(sb, "GPU", GpuScore, GpuPercent);
            AppendHtmlRow(sb, "Memory", MemoryScore, MemoryPercent);
            AppendHtmlRow(sb, "Disk", DiskScore, DiskPercent);
            AppendHtmlRow(sb, "Network", NetworkScore, NetworkPercent);
            sb.Append("</table>\r\n");

            if (SystemAnalysis != null)
            {
                string healthClass = "minor";
                if (SystemAnalysis.OverallHealth == "Good") healthClass = "good";
                else if (SystemAnalysis.OverallHealth == "Warning") healthClass = "warning";
                else if (SystemAnalysis.OverallHealth == "Critical") healthClass = "critical";

                sb.AppendFormat("<h2>System Health: <span class=\"{0}\">{1}</span></h2>\r\n", healthClass, SystemAnalysis.OverallHealth);
                sb.AppendFormat("<p>Motherboard: {0} | BIOS: {1}</p>\r\n", SystemAnalysis.Motherboard, SystemAnalysis.BiosVersion);
                sb.AppendFormat("<p>RAM: {0}</p>\r\n", SystemAnalysis.RamModules);
                sb.AppendFormat("<p>GPU Driver: {0} ({1})</p>\r\n", SystemAnalysis.GpuDriver, SystemAnalysis.GpuDriverDate);
                sb.Append("<ul>\r\n");
                foreach (var issue in SystemAnalysis.Issues)
                    sb.AppendFormat("<li>{0}</li>\r\n", issue);
                sb.Append("</ul>\r\n");
            }

            sb.Append("<p><small>Gorstak Benchmark Suite v0.4.0</small></p>\r\n</body></html>");
            return sb.ToString();
        }

        private static void AppendHtmlRow(StringBuilder sb, string name, double score, double percent)
        {
            sb.AppendFormat("<tr><td>{0}</td><td>{1:N0}</td><td>{2}%</td><td><div class=\"bar\"><div class=\"bar-fill\" style=\"width:{3}%\"></div></div></td></tr>\r\n",
                name, score, percent, Math.Min(percent, 200));
        }
    }
}
