param([int]$X, [int]$Y)
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class MouseClick {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
  public static void Click(int x, int y) {
    SetCursorPos(x, y);
    mouse_event(0x02, 0, 0, 0, UIntPtr.Zero); // left down
    mouse_event(0x04, 0, 0, 0, UIntPtr.Zero); // left up
  }
}
"@
[MouseClick]::Click($X, $Y)
Write-Output "clicked: $X,$Y"
