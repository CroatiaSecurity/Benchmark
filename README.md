# Gorstak Benchmark v2

> .NET 8 WinForms system benchmark with integrated health analysis. Dark-themed GUI with pie charts, bottleneck detection, system event log scanning, and screenshot export.

---

## Overview

Comprehensive system performance testing with CPU, Memory, Disk, GPU, and Network benchmarks plus a system health analysis that scans Windows event logs for hardware errors, GPU crashes, WHEA issues, and reboot patterns.

### Features

- **CPU Benchmark** — Prime calculation (to 50K) + 1M math operations
- **Memory Benchmark** — 10M element array + 100K random access
- **Disk Benchmark** — 100 MB sequential R/W with WriteThrough (bypasses OS cache)
- **GPU Benchmark** — 200×200 matrix multiplication + 100K trig operations
- **Network Benchmark** — Latency (ping) + 10 MB download speed test
- **Bottleneck Detection** — CPU vs GPU imbalance with severity rating
- **System Health Analysis** — Scans event logs for:
  - WHEA hardware errors (memory/PCIe)
  - GPU driver crashes (nvlddmkm/TDR)
  - Reboot frequency (last 24h)
  - ACPI BIOS warnings
  - Bluetooth adapter errors
- **Screenshot Export** — Captures full results + system analysis as JPG/PNG

---

## Build & Run

Requires .NET 8 SDK.

```cmd
dotnet build -c Release
dotnet run -c Release
```

Or publish as a single-file executable:

```cmd
dotnet publish -c Release
```

Output: `bin\Release\net8.0-windows\win-x64\publish\Benchmark.exe`

---

## Reference Scores

Calibrated so a high-end system (Core Ultra 7 265KF / RTX 5070 / 32GB DDR5-7200 / 990 EVO Plus) scores ~100%.

| Component | Reference | Test Method |
|-----------|-----------|-------------|
| CPU | 36,000,000 | Primes to 50K + 1M sqrt×π |
| Memory | 2,030,000 | 10M array ops + 100K random access |
| Disk | 3,600 | 100 MB sequential R/W (MB/s avg) |
| GPU | 630,000 | 200×200 matrix multiply + 100K trig |
| Network | 27 | Latency + download speed composite |

---

## Files

| File | Description |
|------|-------------|
| `Program.cs` | Entry point |
| `MainForm.cs` | WinForms GUI — dark theme, pie chart, system analysis card, screenshot |
| `BenchmarkEngine.cs` | Async benchmark runner with WMI hardware detection |
| `BenchmarkResults.cs` | Results model — scoring, bottleneck, HTML/JSON/text export |
| `SystemAnalysis.cs` | Event log scanner — WHEA, GPU crashes, reboots, ACPI, health rating |
| `Benchmark.csproj` | .NET 8 project file |

---

## License & Disclaimer

This project is intended for authorized defensive, administrative, research, or educational use only.
Provided "AS IS" without warranties of any kind.

---

<p align="center">
  <sub>Built by <strong>Gorstak</strong></sub>
</p>
