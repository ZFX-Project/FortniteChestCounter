@echo off
rem Build script for Fortnite Chest Counter (no Visual Studio required --
rem uses the csc.exe compiler that ships with Windows / .NET Framework 4.x)
cd /d "%~dp0"

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

if not exist bin mkdir bin

set ARGS=/nologo /target:winexe /codepage:65001 /win32icon:assets\app.ico /out:bin\FortniteChestCounter.exe /res:assets\chest_closed.png,ChestCounter.chest_closed.png /res:assets\chest_open.png,ChestCounter.chest_open.png /res:assets\fa-solid-900.ttf,ChestCounter.fa.ttf /r:System.Windows.Forms.dll /r:System.Drawing.dll src\ChestCounter.cs

"%CSC%" %ARGS%
if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)

rem place a copy next to the script too (skipped if the app is running)
copy /y bin\FortniteChestCounter.exe FortniteChestCounter.exe >nul 2>&1

echo BUILD OK: bin\FortniteChestCounter.exe
