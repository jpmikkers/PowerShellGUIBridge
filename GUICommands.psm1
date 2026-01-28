#requires -version 7.4

using module .\GUIBridge.psm1

<#
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
#>

function Invoke-OpenFileDialog
{
    param (
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [GUIBridge]$bridge,
        [string]$title = "Open File",
        [string]$location = $null,
        [bool]$allowMultiple = $false,
        $filters
    )

    $payload = @{
        Title = $title
        SuggestedStartLocation = $location
        AllowMultiple = $allowMultiple
        Filters = $filters
    }

    $v = $bridge.InvokeCommand("ShowOpenFileDialog",$payload)
}

function Invoke-SaveFileDialog
{
    param (
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [GUIBridge]$bridge,
        [string]$title = "Save File",
        [string]$location = $null,
        [string]$suggestedFileName = $null,
        [bool]$showOverwritePrompt = $false,
        $filters
    )

    $payload = @{
        Title = $title
        SuggestedStartLocation = $location
        SuggestedFileName = $suggestedFileName
        ShowOverwritePrompt = $showOverwritePrompt
        Filters = $filters 
    }

    $v = $bridge.InvokeCommand("ShowSaveFileDialog",$payload)
}

# see https://stephanevg.github.io/powershell/class/module/DATA-How-To-Write-powershell-Modules-with-classes/ why we need these

function New-GUIBridge
{
    return [GUIBridge]::new()
}

function Close-GUIBridge
{
    param (
        [Parameter(Mandatory)]
        [GUIBridge]$bridge
    )

    if($null -ne $bridge)
    {
        $bridge.Dispose()
    }
}

#Export-ModuleMember -Function New-GUIBridge
#Export-ModuleMember -Function Close-GUIBridge
#Export-ModuleMember -Function Invoke-SaveFileDialog
#Export-ModuleMember -Function Invoke-OpenFileDialog
