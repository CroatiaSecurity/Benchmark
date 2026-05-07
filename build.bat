<<<<<<< HEAD
@echo off
setlocal
cd /d "%~dp0"

set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "REF=%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
if not exist "%REF%\mscorlib.dll" set "REF=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319"

echo Building Gorstak Benchmark...
"%CSC%" /target:winexe /out:Benchmark.exe ^
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
) else (
    echo.
    echo Build failed.
    exit /b 1
)
=======
@echo off
setlocal
cd /d "%~dp0"

set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "REF=%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
if not exist "%REF%\mscorlib.dll" set "REF=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319"

echo Building Gorstak Benchmark...
"%CSC%" /target:winexe /out:Benchmark.exe ^
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
) else (
    echo.
    echo Build failed.
    exit /b 1
)
>>>>>>> 3d3535ab3f0cc7ffda6c31d1b5ec2b6adbb77c7e
