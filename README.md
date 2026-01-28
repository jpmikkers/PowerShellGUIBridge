# PowerShell GUI Bridge

This project demonstrates a _cross platform_ way to do graphical user interface actions from powershell. It achieves this by establishing a bidirectional json encoded channel to a GUI executable (built using [Avalonia](https://avaloniaui.net/)) that can execute the requested GUI actions.

The demo script (demo.ps1) shows how to call open and save file dialogs from powershell. I've tested this on Windows and Linux. (MacOS _should_ also work, but I don't have that hardware)

# Example

```powershell
using module .\GUIBridge.psd1

# ... snip ...

try
{
    $GUIBridge = [GUIBridge]::new()

    $imageFileFilters = @( 
        @{ Name = 'Images'; Extensions = @('*.jpg', '*.jpeg', '*.bmp') },
        @{ Name = 'All Files'; Extensions = @('*.*') }
    )

    Dialog-OpenFile -bridge $GUIBridge -title "Open image file" -location "C:\" -allowMultiple $true -filters $imageFileFilters
}
finally
{
    if($GUIBridge -ne $null)
    {
        $GUIBridge.Dispose()
    }
}
```
