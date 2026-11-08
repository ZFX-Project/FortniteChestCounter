Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public class WCheck {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
  [DllImport("user32.dll")] public static extern IntPtr GetWindowLongPtrW(IntPtr h, int idx);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  public static List<string> Found = new List<string>();
  public static uint TargetPid = 0;
  public static bool Cb(IntPtr h, IntPtr l) {
    uint pid; GetWindowThreadProcessId(h, out pid);
    if (pid == TargetPid && IsWindowVisible(h)) {
      RECT r; GetWindowRect(h, out r);
      StringBuilder sb = new StringBuilder(128); GetWindowText(h, sb, 128);
      Found.Add(h + "|" + r.L + "," + r.T + "," + (r.R - r.L) + "x" + (r.B - r.T) + "|title=" + sb.ToString() + "|owner=" + GetWindowLongPtrW(h, -8).ToString());
    }
    return true;
  }
  public static string[] Run(uint pid) { Found.Clear(); TargetPid = pid; EnumWindows(Cb, IntPtr.Zero); return Found.ToArray(); }
}
'@
Add-Type -AssemblyName System.Drawing
$p = Get-Process FortniteChestCounter -ErrorAction SilentlyContinue
if (-not $p) { Write-Output "APP NOT RUNNING"; exit }
Write-Output "=== COUNTER WINDOWS (owner = 0 means no owner) ==="
[WCheck]::Run([uint32]$p.Id) | ForEach-Object { Write-Output $_ }

Write-Output "=== FORTNITE GAME WINDOWS ==="
$fg = Get-Process FortniteClient-Win64-Shipping -ErrorAction SilentlyContinue
if (-not $fg) { Write-Output "GAME NOT RUNNING"; exit }
$fg | ForEach-Object {
  [WCheck]::Pid = [uint32]$_.Id
  # reuse the same enumeration but for the game pid
  [WCheck]::Found.Clear()
  [WCheck]::TargetPid = [uint32]$_.Id
  # call EnumWindows via Run-like path
  [WCheck]::Found.Clear()
  # simple: enumerate manually
}
Write-Output "---"
