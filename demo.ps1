#requires -version 7.4

using module .\GUIBridge.psd1

function Paint-StrokeThickness
{
    param (
        [GUIBridge]$bridge,
        [double]$thickness
    )
    $bridge.PostCommand("StrokeThickness", $thickness)
}

function Paint-StrokeBrush
{
    param (
        [GUIBridge]$bridge,
        [byte]$a,
        [byte]$r,
        [byte]$g,
        [byte]$b
    )
    $brush = @{
        A = $a
        R = $r
        G = $g
        B = $b
    }
    $bridge.PostCommand("StrokeBrush", $brush)
}

function Paint-FillBrush
{
    param (
        [GUIBridge]$bridge,
        [byte]$a,
        [byte]$r,
        [byte]$g,
        [byte]$b
    )
    $brush = @{
        A = $a
        R = $r
        G = $g
        B = $b
    }
    $bridge.PostCommand("FillBrush", $brush)
}

function Paint-Sync
{
    param (
        [GUIBridge]$bridge
    )
    $null = $bridge.InvokeCommand("Sync", $null)
}

function Paint-Info
{
    param (
        [GUIBridge]$bridge,
        [string]$info
    )
    $bridge.PostCommand("Info", $info)
}

function Paint-Line
{
    param (
        [GUIBridge]$bridge,
        [double]$x1,
        [double]$y1,
        [double]$x2,
        [double]$y2
    )
    $line = @{
        x1 = $x1
        y1 = $y1
        x2 = $x2
        y2 = $y2
    }
    $bridge.PostCommand("Line", $line)
}

function Paint-Rectangle
{
    param (
        [GUIBridge]$bridge,
        [double]$x,
        [double]$y,
        [double]$w,
        [double]$h,
        [double]$radiusX = 0.0,
        [double]$radiusY = 0.0
    )
    $rect = @{
        x = $x
        y = $y
        w = $w
        h = $h
        radiusX = $radiusX
        radiusY = $radiusY
    }
    $bridge.PostCommand("Rectangle", $rect)
}

function Dialog-OpenFile
{
    param (
        [GUIBridge]$bridge,
        [string]$title = "Open File",
        [string]$location = $null,
        [bool]$allowMultiple = $false
    )
    $payload = @{
        Title = $title
        Location = $location
        AllowMultiple = $allowMultiple
        Filters = @( 
            @{ Name = 'Images'; Extensions = @('*.jpg', '*.jpeg', '*.bmp') },
            @{ Name = 'All Files'; Extensions = @('*.*') }
        )
    }

    $v = $bridge.InvokeCommand("OpenFile",$payload)
    $v
}

try
{
    $GUIBridge = [GUIBridge]::new()
    Dialog-OpenFile -bridge $GUIBridge -title "Select a File" -location "C:\" -allowMultiple $true

        "Dialog test complete. Starting drawing..."

    $i = 0

    while($i -lt 10000)
    {
        Paint-StrokeThickness -bridge $GUIBridge -thickness (Get-Random -Minimum 1 -Maximum 20)

        Paint-StrokeBrush -bridge $GUIBridge -a 255 -r (Get-Random -Minimum 0 -Maximum 255) -g (Get-Random -Minimum 0 -Maximum 255) -b (Get-Random -Minimum 0 -Maximum 255)

        Paint-FillBrush -bridge $GUIBridge -a 255 -r (Get-Random -Minimum 0 -Maximum 255) -g (Get-Random -Minimum 0 -Maximum 255) -b (Get-Random -Minimum 0 -Maximum 255)

        Paint-Line -bridge $GUIBridge -x1 (Get-Random -Minimum 0 -Maximum 1000) -y1 (Get-Random -Minimum 0 -Maximum 1000) -x2 (Get-Random -Minimum 0 -Maximum 1000) -y2 (Get-Random -Minimum 0 -Maximum 1000)

        Paint-Rectangle -bridge $GUIBridge -x (Get-Random -Minimum 0 -Maximum 1000) -y (Get-Random -Minimum 0 -Maximum 1000) -w (Get-Random -Minimum 50 -Maximum 500) -h (Get-Random -Minimum 50 -Maximum 500)

        Paint-Sync -bridge $GUIBridge

        $i = $i + 1
        Paint-Info -bridge $GUIBridge -info "Shapes drawn: $i"
        #Start-Sleep -Milliseconds 0
    }
}
finally
{
    if($GUIBridge -ne $null)
    {
        $GUIBridge.Dispose()
        "Avalonia bridge disposed."
    }
}
