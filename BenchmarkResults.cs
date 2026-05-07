using System;
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

        public string BottleneckType { get; set; }  // "None", "CPU", "GPU"
        public double BottleneckSeverity { get; set; }  // 0 = min, higher = worse

        public DateTime RunDate { get; set; }
        public string OsName { get; set; }

        private static double Cap(double p) { return Math.Round(p, 1); }

        public string GetShareableText()
        {
            double overall = Cap((Cap(CpuPercent) + Cap(GpuPercent) + Cap(MemoryPercent) + Cap(DiskPercent) + Cap(NetworkPercent)) / 5);
            return string.Format("Gorstak Benchmark | Overall: {0:N0} ({1}%) | CPU:{2}% GPU:{3}% RAM:{4}% Disk:{5}% Net:{6}% | Bottleneck: {7} ({8:N1}) | {9:yyyy-MM-dd}",
                OverallScore, overall, Cap(CpuPercent), Cap(GpuPercent), Cap(MemoryPercent), Cap(DiskPercent), Cap(NetworkPercent), BottleneckType, BottleneckSeverity, RunDate);
        }

        public string GetJson()
        {
            var sb = new StringBuilder();
            var c = System.Globalization.CultureInfo.InvariantCulture;
            sb.Append("{\r\n");
            sb.AppendFormat(c, "  \"cpu\": {{\"score\": {0}, \"percent\": {1}, \"name\": \"{2}\"}},\r\n", CpuScore, CpuPercent, EscapeJson(CpuName ?? ""));
            sb.AppendFormat(c, "  \"memory\": {{\"score\": {0}, \"percent\": {1}, \"ramGB\": {2}}},\r\n", MemoryScore, MemoryPercent, TotalRamGB);
            sb.AppendFormat(c, "  \"disk\": {{\"score\": {0}, \"percent\": {1}, \"drive\": \"{2}\"}},\r\n", DiskScore, DiskPercent, EscapeJson(DiskDrive ?? ""));
            sb.AppendFormat(c, "  \"gpu\": {{\"score\": {0}, \"percent\": {1}, \"name\": \"{2}\"}},\r\n", GpuScore, GpuPercent, EscapeJson(GpuName ?? ""));
            sb.AppendFormat(c, "  \"network\": {{\"score\": {0}, \"percent\": {1}}},\r\n", NetworkScore, NetworkPercent);
            sb.AppendFormat(c, "  \"overall\": {{\"score\": {0}, \"percent\": {1}}},\r\n", OverallScore, OverallPercent);
            sb.AppendFormat(c, "  \"bottleneck\": {{\"type\": \"{0}\", \"severity\": {1}}},\r\n", BottleneckType, BottleneckSeverity);
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
            return string.Format(@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><title>Benchmark Results</title>
<style>body{{font-family:Segoe UI,sans-serif;max-width:600px;margin:40px auto;padding:20px;}}
h1{{color:#333;}} .score{{font-size:1.2em;font-weight:bold;}} .bar{{height:20px;background:#e0e0e0;border-radius:4px;margin:4px 0;overflow:hidden;}}
.bar-fill{{height:100%;background:linear-gradient(90deg,#4CAF50,#8BC34A);}} table{{width:100%;border-collapse:collapse;}} td{{padding:8px;border-bottom:1px solid #eee;}}</style></head>
<body>
<h1>Gorstak Benchmark Results</h1>
<p><strong>Date:</strong> {0:yyyy-MM-dd HH:mm}</p>
<p><strong>Overall Score:</strong> <span class=""score"">{1:N0}</span> ({2}%)</p>
<p><strong>Bottleneck:</strong> {3} (Severity: {4:N1})</p>
<table>
<tr><td>CPU</td><td>{5:N0}</td><td>{6}%</td><td><div class=""bar""><div class=""bar-fill"" style=""width:{7}%""></div></div></td></tr>
<tr><td>GPU</td><td>{8:N0}</td><td>{9}%</td><td><div class=""bar""><div class=""bar-fill"" style=""width:{10}%""></div></div></td></tr>
<tr><td>Memory</td><td>{11:N0}</td><td>{12}%</td><td><div class=""bar""><div class=""bar-fill"" style=""width:{13}%""></div></div></td></tr>
<tr><td>Disk</td><td>{14:N0}</td><td>{15}%</td><td><div class=""bar""><div class=""bar-fill"" style=""width:{16}%""></div></div></td></tr>
<tr><td>Network</td><td>{17:N0}</td><td>{18}%</td><td><div class=""bar""><div class=""bar-fill"" style=""width:{19}%""></div></div></td></tr>
</table>
<p><small>Gorstak Benchmark Suite</small></p>
</body></html>",
                RunDate, OverallScore, OverallPercent, BottleneckType, BottleneckSeverity,
                CpuScore, CpuPercent, Math.Min(CpuPercent, 200),
                GpuScore, GpuPercent, Math.Min(GpuPercent, 200),
                MemoryScore, MemoryPercent, Math.Min(MemoryPercent, 200),
                DiskScore, DiskPercent, Math.Min(DiskPercent, 200),
                NetworkScore, NetworkPercent, Math.Min(NetworkPercent, 200));
        }
    }
}
