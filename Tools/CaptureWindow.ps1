param(
    [int]$ProcessId = 0,
    [string]$WindowTitle = "",
    [Parameter(Mandatory = $true)][string]$OutputFile
)

Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class WindowCapture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr handle, out Rect rect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr handle);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint threadA, uint threadB, bool attach);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr handle);

    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr handle, bool altTab);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr handle, int command);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr handle, IntPtr hdc, uint flags);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();

    [DllImport("user32.dll")]
    public static extern bool RedrawWindow(IntPtr handle, IntPtr rectangle, IntPtr region, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")]
    public static extern IntPtr FindWindow(IntPtr className, string windowName);
}
'@

[WindowCapture]::SetProcessDPIAware() | Out-Null
if ($WindowTitle.Length -gt 0) {
    $handle = [WindowCapture]::FindWindow([IntPtr]::Zero, $WindowTitle)
}
elseif ($ProcessId -gt 0) {
    $appProcess = Get-Process -Id $ProcessId -ErrorAction Stop
    $handle = $appProcess.MainWindowHandle
}
else {
    throw "Укажите ProcessId или WindowTitle."
}
if ($handle -eq [IntPtr]::Zero) {
    throw "Окно не найдено."
}

$foreground = [WindowCapture]::GetForegroundWindow()
$foregroundProcessId = [uint32]0
$foregroundThread = [WindowCapture]::GetWindowThreadProcessId($foreground, [ref]$foregroundProcessId)
$currentThread = [WindowCapture]::GetCurrentThreadId()
[WindowCapture]::AttachThreadInput($currentThread, $foregroundThread, $true) | Out-Null
try {
    [WindowCapture]::ShowWindow($handle, 9) | Out-Null
    [WindowCapture]::BringWindowToTop($handle) | Out-Null
    [WindowCapture]::SetForegroundWindow($handle) | Out-Null
    [WindowCapture]::SwitchToThisWindow($handle, $true)
    [WindowCapture]::RedrawWindow($handle, [IntPtr]::Zero, [IntPtr]::Zero, 0x181) | Out-Null
}
finally {
    [WindowCapture]::AttachThreadInput($currentThread, $foregroundThread, $false) | Out-Null
}
Start-Sleep -Milliseconds 450
$rect = [WindowCapture+Rect]::new()
if (-not [WindowCapture]::GetWindowRect($handle, [ref]$rect)) {
    throw "Не удалось получить размеры окна."
}

$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
Write-Output "Rect: $($rect.Left),$($rect.Top),$($rect.Right),$($rect.Bottom)"
$isForeground = [WindowCapture]::GetForegroundWindow() -eq $handle
Write-Output "Foreground: $isForeground"
$bitmap = [System.Drawing.Bitmap]::new($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    if ($isForeground) {
        $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    }
    else {
        $deviceContext = $graphics.GetHdc()
        try {
            [WindowCapture]::PrintWindow($handle, $deviceContext, 0) | Out-Null
        }
        finally {
            $graphics.ReleaseHdc($deviceContext)
        }
    }
    $directory = [System.IO.Path]::GetDirectoryName($OutputFile)
    [System.IO.Directory]::CreateDirectory($directory) | Out-Null
    $bitmap.Save($OutputFile, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "$OutputFile ($width x $height)"
}
finally {
    $graphics.Dispose()
    $bitmap.Dispose()
}
