@echo off
setlocal
cd /d "%~dp0"
set "ORBIT_CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%ORBIT_CSC%" set "ORBIT_CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%ORBIT_CSC%" (
  echo The .NET Framework compiler was not found.
  exit /b 1
)
"%ORBIT_CSC%" /nologo /target:winexe /optimize+ /win32icon:orbit.ico /out:OrbitLibrary.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll OrbitLibrary.cs Premium.cs ConsoleUI.cs Rendering.cs
exit /b %errorlevel%

