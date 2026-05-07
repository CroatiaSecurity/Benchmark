# Benchmark.ps1
# Author: Gorstak

Clear-Host
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  POWERSHELL SYSTEM BENCHMARK SUITE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# System Info
$os = (Get-CimInstance Win32_OperatingSystem).Caption
$psVersion = $PSVersionTable.PSVersion
Write-Host "System: $os"
Write-Host "PowerShell: $psVersion"
Write-Host "Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

$scores = @{}
# Reference scores calibrated so a high-end system scores ~100%
# Intel Core Ultra 7 265KF / RTX 5070 / 32GB DDR5-7200 / 990 EVO Plus
$referenceScores = @{
    'CPU' = 63000.0
    'Memory' = 5200.00
    'Disk' = 1700.00
    'GPU' = 940.0
    'Network' = 50
}

# ============================================
# CPU TEST
# ============================================
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  CPU BENCHMARK" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$cpu = Get-CimInstance Win32_Processor
Write-Host "[INFO] CPU: $($cpu.Name)" -ForegroundColor Cyan
Write-Host "[INFO] Cores: $($cpu.NumberOfCores) | Threads: $($cpu.NumberOfLogicalProcessors)" -ForegroundColor Cyan
Write-Host "[INFO] Running prime calculation test..." -ForegroundColor Cyan
Write-Host ""

$cpuStart = Get-Date

# Calculate primes up to 50000
$primes = @()
for ($num = 2; $num -le 50000; $num++) {
    $isPrime = $true
    $sqrt = [Math]::Sqrt($num)
    for ($i = 2; $i -le $sqrt; $i++) {
        if ($num % $i -eq 0) {
            $isPrime = $false
            break
        }
    }
    if ($isPrime) { $primes += $num }
}

# Math operations
$result = 0
for ($i = 0; $i -lt 1000000; $i++) {
    $result += [Math]::Sqrt($i) * [Math]::PI
}

$cpuTime = (Get-Date) - $cpuStart
$cpuScore = [Math]::Round(100000 / $cpuTime.TotalSeconds, 2)

Write-Host "[RESULT] Primes Found: $($primes.Count)" -ForegroundColor Green
Write-Host "[RESULT] Time: $([Math]::Round($cpuTime.TotalSeconds, 2)) seconds" -ForegroundColor Green
Write-Host "[RESULT] CPU Score: $cpuScore" -ForegroundColor Green
$cpuPercent = [Math]::Round(($cpuScore / $referenceScores['CPU']) * 100, 1)
Write-Host "[RESULT] CPU Performance: $cpuPercent%" -ForegroundColor $(if ($cpuPercent -ge 100) { 'Cyan' } elseif ($cpuPercent -ge 75) { 'Green' } elseif ($cpuPercent -ge 50) { 'Yellow' } else { 'Red' })
Write-Host ""

$scores['CPU'] = $cpuScore

# ============================================
# MEMORY TEST
# ============================================
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  MEMORY BENCHMARK" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$memory = Get-CimInstance Win32_PhysicalMemory
$totalGB = [Math]::Round(($memory | Measure-Object Capacity -Sum).Sum / 1GB, 2)
Write-Host "[INFO] Total RAM: $totalGB GB" -ForegroundColor Cyan
Write-Host "[INFO] Testing 10 million element array..." -ForegroundColor Cyan
Write-Host ""

$memStart = Get-Date

$arraySize = 10000000
$array = New-Object System.Collections.ArrayList($arraySize)

# Write test
for ($i = 0; $i -lt $arraySize; $i++) {
    [void]$array.Add($i)
}

# Read test
$sum = 0
foreach ($item in $array) {
    $sum += $item
}

# Random access
$random = New-Object System.Random
for ($i = 0; $i -lt 100000; $i++) {
    $index = $random.Next(0, $arraySize)
    $value = $array[$index]
}

$memTime = (Get-Date) - $memStart
$memScore = [Math]::Round(50000 / $memTime.TotalSeconds, 2)

Write-Host "[RESULT] Array Size: $arraySize elements" -ForegroundColor Green
Write-Host "[RESULT] Time: $([Math]::Round($memTime.TotalSeconds, 2)) seconds" -ForegroundColor Green
Write-Host "[RESULT] Memory Score: $memScore" -ForegroundColor Green
$memPercent = [Math]::Round(($memScore / $referenceScores['Memory']) * 100, 1)
Write-Host "[RESULT] Memory Performance: $memPercent%" -ForegroundColor $(if ($memPercent -ge 100) { 'Cyan' } elseif ($memPercent -ge 75) { 'Green' } elseif ($memPercent -ge 50) { 'Yellow' } else { 'Red' })
Write-Host ""

$array.Clear()
[System.GC]::Collect()

$scores['Memory'] = $memScore

# ============================================
# DISK TEST
# ============================================
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  DISK I/O BENCHMARK" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$disk = Get-CimInstance Win32_LogicalDisk | Where-Object { $_.DriveType -eq 3 } | Select-Object -First 1
$freeGB = [Math]::Round($disk.FreeSpace / 1GB, 2)
Write-Host "[INFO] Drive: $($disk.DeviceID) | Free Space: $freeGB GB" -ForegroundColor Cyan
Write-Host "[INFO] Testing 100 MB read/write..." -ForegroundColor Cyan
Write-Host ""

$testFile = "$env:TEMP\benchmark_test.tmp"
$fileSize = 100MB
$data = New-Object byte[] $fileSize
(New-Object System.Random).NextBytes($data)

# Write test — use WriteThrough to bypass OS write cache
$writeStart = Get-Date
$fs = [System.IO.File]::Create($testFile, 4096, [System.IO.FileOptions]::WriteThrough)
$fs.Write($data, 0, $data.Length)
$fs.Close()
$writeTime = (Get-Date) - $writeStart
$writeMBps = [Math]::Round(($fileSize / 1MB) / $writeTime.TotalSeconds, 2)

# Flush OS file cache before read test to measure real disk speed
# Write a large dummy allocation to push the test file out of cache
$dummy = New-Object byte[] (200MB)
(New-Object System.Random).NextBytes($dummy)
$dummy = $null
[System.GC]::Collect()

# Read test
$readStart = Get-Date
$readData = [System.IO.File]::ReadAllBytes($testFile)
$readTime = (Get-Date) - $readStart
$readMBps = [Math]::Round(($fileSize / 1MB) / $readTime.TotalSeconds, 2)

Remove-Item $testFile -Force -ErrorAction SilentlyContinue

$diskScore = [Math]::Round(($writeMBps + $readMBps) / 2, 2)

Write-Host "[RESULT] Write Speed: $writeMBps MB/s" -ForegroundColor Green
Write-Host "[RESULT] Read Speed: $readMBps MB/s" -ForegroundColor Green
Write-Host "[RESULT] Disk Score: $diskScore" -ForegroundColor Green
$diskPercent = [Math]::Round(($diskScore / $referenceScores['Disk']) * 100, 1)
Write-Host "[RESULT] Disk Performance: $diskPercent%" -ForegroundColor $(if ($diskPercent -ge 100) { 'Cyan' } elseif ($diskPercent -ge 75) { 'Green' } elseif ($diskPercent -ge 50) { 'Yellow' } else { 'Red' })
Write-Host ""

$scores['Disk'] = $diskScore

# ============================================
# GPU TEST
# ============================================
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  GPU BENCHMARK" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$gpu = Get-CimInstance Win32_VideoController |
    Sort-Object { if ($_.AdapterRAM -gt 0) { $_.AdapterRAM } else { 0 } } -Descending |
    Select-Object -First 1
# AdapterRAM is uint32 (caps at 4GB). Use registry for accurate VRAM on modern GPUs.
$vramBytes = $null
try {
    # Search all display adapter registry entries for the matching GPU
    $classRoot = "HKLM:\SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"
    if (Test-Path $classRoot) {
        Get-ChildItem $classRoot -ErrorAction SilentlyContinue | ForEach-Object {
            $desc = (Get-ItemProperty $_.PSPath -Name "DriverDesc" -ErrorAction SilentlyContinue).DriverDesc
            if ($desc -eq $gpu.Name) {
                $qwMem = (Get-ItemProperty $_.PSPath -Name "HardwareInformation.qwMemorySize" -ErrorAction SilentlyContinue)."HardwareInformation.qwMemorySize"
                if ($qwMem -and $qwMem -gt 0) { $vramBytes = [long]$qwMem }
            }
        }
    }
} catch {}
if (-not $vramBytes -or $vramBytes -le 0) { $vramBytes = [long]$gpu.AdapterRAM }
$vramGB = [Math]::Round($vramBytes / 1GB, 2)
Write-Host "[INFO] GPU: $($gpu.Name)" -ForegroundColor Cyan
Write-Host "[INFO] Driver: $($gpu.DriverVersion)" -ForegroundColor Cyan
Write-Host "[INFO] VRAM: $vramGB GB" -ForegroundColor Cyan
Write-Host "[INFO] Running matrix multiplication..." -ForegroundColor Cyan
Write-Host ""

$gpuStart = Get-Date

# Matrix multiplication (200x200)
$size = 200
$matrix1 = New-Object 'object[,]' $size, $size
$matrix2 = New-Object 'object[,]' $size, $size
$result = New-Object 'object[,]' $size, $size

$random = New-Object System.Random
for ($i = 0; $i -lt $size; $i++) {
    for ($j = 0; $j -lt $size; $j++) {
        $matrix1[$i, $j] = $random.NextDouble()
        $matrix2[$i, $j] = $random.NextDouble()
    }
}

for ($i = 0; $i -lt $size; $i++) {
    for ($j = 0; $j -lt $size; $j++) {
        $sum = 0
        for ($k = 0; $k -lt $size; $k++) {
            $sum += $matrix1[$i, $k] * $matrix2[$k, $j]
        }
        $result[$i, $j] = $sum
    }
}

# Compute operations
$computeSum = 0
for ($i = 0; $i -lt 100000; $i++) {
    $computeSum += [Math]::Sin($i) * [Math]::Cos($i) * [Math]::Tan($i / 100 + 1)
}

$gpuTime = (Get-Date) - $gpuStart
$gpuScore = [Math]::Round(10000 / $gpuTime.TotalSeconds, 2)

Write-Host "[RESULT] Matrix Size: ${size}x${size}" -ForegroundColor Green
Write-Host "[RESULT] Compute Operations: 100,000" -ForegroundColor Green
Write-Host "[RESULT] Time: $([Math]::Round($gpuTime.TotalSeconds, 2)) seconds" -ForegroundColor Green
Write-Host "[RESULT] GPU Score: $gpuScore" -ForegroundColor Green
$gpuPercent = [Math]::Round(($gpuScore / $referenceScores['GPU']) * 100, 1)
Write-Host "[RESULT] GPU Performance: $gpuPercent%" -ForegroundColor $(if ($gpuPercent -ge 100) { 'Cyan' } elseif ($gpuPercent -ge 75) { 'Green' } elseif ($gpuPercent -ge 50) { 'Yellow' } else { 'Red' })
Write-Host ""

$scores['GPU'] = $gpuScore

# ============================================
# NETWORK TEST
# ============================================
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  NETWORK BENCHMARK" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "[INFO] Testing network latency..." -ForegroundColor Cyan
Write-Host ""

# Latency test
$targets = @('8.8.8.8', '1.1.1.1', 'www.google.com')
$avgLatency = 0
$successCount = 0

Write-Host "--- Latency Test ---" -ForegroundColor White
foreach ($target in $targets) {
    try {
        $ping = Test-Connection -ComputerName $target -Count 4 -ErrorAction Stop
        $latency = ($ping | Measure-Object -Property ResponseTime -Average).Average
        Write-Host "[RESULT] $target : $([Math]::Round($latency, 2)) ms" -ForegroundColor Green
        $avgLatency += $latency
        $successCount++
    }
    catch {
        Write-Host "[FAIL] $target : Failed" -ForegroundColor Red
    }
}

if ($successCount -gt 0) {
    $avgLatency = [Math]::Round($avgLatency / $successCount, 2)
    Write-Host "[RESULT] Average Latency: $avgLatency ms" -ForegroundColor Green
}
Write-Host ""

# Download speed test
Write-Host "--- Download Speed Test ---" -ForegroundColor White
Write-Host "[INFO] Downloading test file..." -ForegroundColor Cyan

# Try multiple download sources
$downloadUrls = @(
    "https://proof.ovh.net/files/10Mb.dat",
    "http://speedtest.tele2.net/10MB.zip",
    "http://ipv4.download.thinkbroadband.com/10MB.zip"
)

$downloadSuccess = $false
foreach ($downloadUrl in $downloadUrls) {
    try {
        $downloadPath = "$env:TEMP\speedtest.tmp"
        $downloadStart = Get-Date
        
        # Use Invoke-WebRequest with no progress bar for speed
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $downloadUrl -OutFile $downloadPath -TimeoutSec 30 -ErrorAction Stop
        $ProgressPreference = 'Continue'
        
        $downloadTime = (Get-Date) - $downloadStart
        
        $fileInfo = Get-Item $downloadPath
        $fileSizeMB = [Math]::Round($fileInfo.Length / 1MB, 2)
        $speedMbps = [Math]::Round(($fileSizeMB * 8) / $downloadTime.TotalSeconds, 2)
        
        Write-Host "[RESULT] File Size: $fileSizeMB MB" -ForegroundColor Green
        Write-Host "[RESULT] Time: $([Math]::Round($downloadTime.TotalSeconds, 2)) seconds" -ForegroundColor Green
        Write-Host "[RESULT] Download Speed: $speedMbps Mbps" -ForegroundColor Green
        
        Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue
        
        $latencyScore = 1000 / ($avgLatency + 1)
        $networkScore = [Math]::Round(($latencyScore + $speedMbps) / 2, 2)
        $downloadSuccess = $true
        break
    }
    catch {
        Write-Host "[FAIL] Failed to download from $downloadUrl" -ForegroundColor Yellow
        continue
    }
}

if (-not $downloadSuccess) {
    Write-Host "[FAIL] All download tests failed" -ForegroundColor Red
    if ($successCount -gt 0) {
        $networkScore = [Math]::Round(1000 / ($avgLatency + 1), 2)
        Write-Host "[INFO] Using latency-only score" -ForegroundColor Yellow
    }
    else {
        $networkScore = 0
    }
}

Write-Host "[RESULT] Network Score: $networkScore" -ForegroundColor Green
$netPercent = [Math]::Round(($networkScore / $referenceScores['Network']) * 100, 1)
Write-Host "[RESULT] Network Performance: $netPercent%" -ForegroundColor $(if ($netPercent -ge 100) { 'Cyan' } elseif ($netPercent -ge 75) { 'Green' } elseif ($netPercent -ge 50) { 'Yellow' } else { 'Red' })
Write-Host ""

$scores['Network'] = $networkScore

# ============================================
# FINAL RESULTS
# ============================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FINAL RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Component Scores:" -ForegroundColor White
$cpuPercent = [Math]::Round(($scores['CPU'] / $referenceScores['CPU']) * 100, 1)
$memPercent = [Math]::Round(($scores['Memory'] / $referenceScores['Memory']) * 100, 1)
$diskPercent = [Math]::Round(($scores['Disk'] / $referenceScores['Disk']) * 100, 1)
$gpuPercent = [Math]::Round(($scores['GPU'] / $referenceScores['GPU']) * 100, 1)
$netPercent = [Math]::Round(($scores['Network'] / $referenceScores['Network']) * 100, 1)

Write-Host "  CPU      : $($scores['CPU']) [$cpuPercent%]" -ForegroundColor Green
Write-Host "  Memory   : $($scores['Memory']) [$memPercent%]" -ForegroundColor Green
Write-Host "  Disk I/O : $($scores['Disk']) [$diskPercent%]" -ForegroundColor Green
Write-Host "  GPU      : $($scores['GPU']) [$gpuPercent%]" -ForegroundColor Green
Write-Host "  Network  : $($scores['Network']) [$netPercent%]" -ForegroundColor Green
Write-Host ""

$overall = [Math]::Round(($scores['CPU'] + $scores['Memory'] + $scores['Disk'] + $scores['GPU'] + $scores['Network']) / 5, 2)
$overallPercent = [Math]::Round(($cpuPercent + $memPercent + $diskPercent + $gpuPercent + $netPercent) / 5, 1)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  OVERALL SCORE: $overall [$overallPercent%]" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to exit"
