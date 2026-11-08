Add-Type -TypeDefinition @'
using System.Runtime.InteropServices;
public class GDIC2 {
  [DllImport("user32.dll")] public static extern uint GetGuiResources(System.IntPtr h, uint f);
}
'@
Write-Output "=== TOP GDI/USER HANDLE CONSUMERS ==="
Get-Process | ForEach-Object {
    try {
        $gdi = [GDIC2]::GetGuiResources($_.Handle, 0)
        $usr = [GDIC2]::GetGuiResources($_.Handle, 1)
        if ($gdi -gt 300 -or $usr -gt 300) {
            Write-Output ("{0} (pid {1}): GDI={2} USER={3}" -f $_.ProcessName, $_.Id, $gdi, $usr)
        }
    } catch {}
}
Write-Output "=== POWERSHELL PROCESSES ==="
Get-Process powershell -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Output ("powershell pid " + $_.Id + " started " + $_.StartTime)
}
