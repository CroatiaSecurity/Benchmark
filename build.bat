@echo off
setlocal
cd /d "%~dp0"

set "VERSION=1.0.0"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "REF=%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
if not exist "%REF%\mscorlib.dll" set "REF=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319"

echo Building Gorstak Benchmark v%VERSION%...

if not exist "releases\%VERSION%" mkdir "releases\%VERSION%"

"%CSC%" /target:winexe /out:"releases\%VERSION%\Benchmark.exe" ^
  /reference:"%REF%\mscorlib.dll" ^
  /reference:"%REF%\System.dll" ^
  /reference:"%REF%\System.Core.dll" ^
  /reference:"%REF%\System.Drawing.dll" ^
  /reference:"%REF%\System.Windows.Forms.dll" ^
  /reference:"%REF%\System.Management.dll" ^
  /win32manifest:app.manifest ^
  /win32icon:Autorun.ico ^
  Program.cs MainForm.cs BenchmarkResults.cs BenchmarkEngine.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful: Benchmark.exe
    echo Output: releases\%VERSION%\Benchmark.exe
) else (
    echo.
    echo Build failed.
    exit /b 1
)
