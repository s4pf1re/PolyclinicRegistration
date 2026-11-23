param(
    [int]$ProcessId = 0,
    [string]$WindowTitle = "",
    [Parameter(Mandatory = $true)][string]$ClassPart,
    [string]$ControlText = "",
    [ValidateSet("Click", "SetText")][string]$Action = "Click",
    [string]$Value = ""
)

Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class WindowControls
{
    public delegate bool EnumProc(IntPtr handle, IntPtr value);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")]
    public static extern IntPtr FindWindow(IntPtr className, string title);
    [DllImport("user32.dll")]
    public static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr value);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxLength);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr handle, StringBuilder text, int maxLength);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
    public static extern IntPtr SendText(IntPtr handle, uint message, IntPtr value, string text);
    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr handle, uint message, IntPtr value1, IntPtr value2);

    public static IntPtr FindChild(IntPtr parent, string classPart, string label)
    {
        IntPtr result = IntPtr.Zero;
        EnumChildWindows(parent, (handle, value) =>
        {
            var className = new StringBuilder(256);
            var text = new StringBuilder(256);
            GetClassName(handle, className, 256);
            GetWindowText(handle, text, 256);
            if (className.ToString().Contains(classPart) &&
                (label.Length == 0 || text.ToString() == label))
            {
                result = handle;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return result;
    }
}
'@

if ($WindowTitle.Length -gt 0) {
    $parent = [WindowControls]::FindWindow([IntPtr]::Zero, $WindowTitle)
}
elseif ($ProcessId -gt 0) {
    $parent = (Get-Process -Id $ProcessId -ErrorAction Stop).MainWindowHandle
}
else {
    throw "Укажите ProcessId или WindowTitle."
}

if ($parent -eq [IntPtr]::Zero) { throw "Окно не найдено." }
$child = [WindowControls]::FindChild($parent, $ClassPart, $ControlText)
if ($child -eq [IntPtr]::Zero) { throw "Элемент не найден: $ClassPart $ControlText" }

if ($Action -eq "SetText") {
    [WindowControls]::SendText($child, 0x000C, [IntPtr]::Zero, $Value) | Out-Null
}
else {
    [WindowControls]::PostMessage($child, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
}
Write-Output "$Action $ClassPart $ControlText"
