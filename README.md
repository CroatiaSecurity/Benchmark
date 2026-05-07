# 📊 Benchmark — Full System Benchmark Suite

> **PowerShell + C# WinForms Benchmark** — Comprehensive system performance testing with CPU, Memory, Disk, GPU, and Network benchmarks. Features a dark-themed GUI with pie charts, bottleneck detection, and HTML/JSON/Screenshot export.

---

## ⚡ Overview

Gorstak Benchmark is a dual-implementation system benchmark suite available as both a PowerShell console script and a C# WinForms desktop application. It tests all major system components, calculates percentage scores against calibrated reference values, detects CPU/GPU bottlenecks, and provides multiple export formats for sharing results.

### ✨ Key Features

- 🧮 **CPU Benchmark** — Prime number calculation (up to 50,000) + 1M math operations (sqrt × π)
- 🧠 **Memory Benchmark** — 10M element array allocation, sequential read, and 100K random access operations
- 💾 **Disk Benchmark** — 100 MB sequential read/write speed test with temp file cleanup
- 🎮 **GPU Benchmark** — 200×200 matrix multiplication + 100K trigonometric compute operations
- 🌐 **Network Benchmark** — Latency test (ping 8.8.8.8, 1.1.1.1, google.com) + download speed test (10 MB file from multiple CDNs)
- 📈 **Percentage Scoring** — Each component scored against calibrated reference values (~100% = high-end system)
- 🔍 **Bottleneck Detection** — Identifies CPU vs GPU imbalance with severity rating and upgrade suggestions
- 🎨 **Dark-Themed GUI** — Blue-on-black WinForms interface with animated spinner and pie chart visualization
- 📋 **Export Options:**
  - Copy to clipboard (shareable one-liner)
  - HTML report with progress bars
  - JSON export with full component data
  - JPG screenshot of the application window

---

## 📁 Files

| File | Description |
|------|-------------|
| `Benchmark.ps1` | PowerShell console benchmark — runs all tests with colored output |
| `BenchmarkEngine.cs` | C# benchmark engine — async test runner with WMI hardware detection |
| `BenchmarkResults.cs` | C# results model — scoring, bottleneck analysis, HTML/JSON/text export |
| `MainForm.cs` | C# WinForms GUI — dark theme, pie chart, spinner, export buttons |
| `Program.cs` | C# entry point |
| `build.bat` | Build script for compiling the C# application |
| `app.manifest` | Application manifest (admin elevation) |
| `Autorun.ico` | Application icon |

---

## 🚀 Usage

### PowerShell Version

```powershell
# Run the console benchmark
powershell -ExecutionPolicy Bypass -File Benchmark.ps1
```

Output includes color-coded results for each component:
- 🟦 Cyan: ≥100% (excellent)
- 🟩 Green: ≥75% (good)
- 🟨 Yellow: ≥50% (average)
- 🟥 Red: <50% (below average)

### C# WinForms Version

```cmd
# Build the application
build.bat

# Run the compiled executable
Benchmark.exe
```

The GUI provides:
1. Click **Run benchmark** to start all tests
2. Watch the animated spinner and live status updates
3. View the **pie chart** showing performance breakdown
4. Check **bottleneck detection** for CPU/GPU balance analysis
5. Use **Share results** to Copy, Export HTML, Export JSON, or take a Screenshot

---

## 📐 Reference Scores

Scores are calibrated so a high-end system scores approximately 100%:

| Component | Reference (C#) | Reference (PS) | Test Method |
|-----------|----------------|-----------------|-------------|
| CPU | 2,500,000 | 75,000 | Primes to 50K + 1M sqrt×π |
| Memory | 4,600 | 4,600 | 10M array ops + 100K random access |
| Disk | 4,600 | 4,600 | 100 MB sequential R/W (MB/s avg) |
| GPU | 500 | 1,000 | 200×200 matrix multiply + 100K trig |
| Network | 25 | 25 | Latency + download speed composite |

### Bottleneck Detection

| Condition | Result |
|-----------|--------|
| CPU% and GPU% within 5% | Balanced (no bottleneck) |
| Both ≥95% | Balanced |
| CPU% < GPU% by >5% | CPU bottleneck |
| GPU% < CPU% by >5% | GPU bottleneck |
| Severity ≥15% | Upgrade suggestion shown |

---

## ⚙️ Requirements

| Version | Requirements |
|---------|-------------|
| PowerShell | Windows 10/11, PowerShell 5.1+, Internet (for network test) |
| C# WinForms | .NET Framework 4.x, Windows 10/11, `csc.exe` or Visual Studio |

---

## 📜 License & Disclaimer

This project is intended for authorized defensive, administrative, research, or educational use only.

- Use only on systems, networks, and environments where you have explicit permission.
- Misuse may violate law, contracts, policy, or acceptable-use terms.
- Running security, hardening, monitoring, or response tooling can impact stability and may disrupt legitimate software.
- Validate all changes in a test environment before production use.
- This project is provided "AS IS", without warranties of any kind, including merchantability, fitness for a particular purpose, and non-infringement.
- Authors and contributors are not liable for direct or indirect damages, data loss, downtime, business interruption, legal exposure, or compliance impact.
- You are solely responsible for lawful operation, configuration choices, and compliance obligations in your jurisdiction.

---

<p align="center">
  <sub>Built with care by <strong>Gorstak</strong></sub>
</p>
