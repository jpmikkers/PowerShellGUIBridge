#requires -version 7.5

using module .\GUICommands.psd1


try
{
    $GUIBridge = New-GUIBridge

    $xValues = @(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
    $yValues = @(2, 4, 5, 4, 6, 7, 8, 8, 9, 10)
    
    Invoke-ScatterPlot -bridge $GUIBridge -title "Sample Scatter Plot" -xAxisLabel "Sheep" -yAxisLabel "Wool" -xValues $xValues -yValues $yValues -legendText "WoolProfits" -color "Blue" -markerSize 6.0

    $selectedFolder = Invoke-FolderPickerDialog -bridge $GUIBridge -title "Select a folder" -suggestedStartLocation $HOME

    "selected folder: $selectedFolder"
    
    $imageFileFilters = @( 
        @{ Name = 'Images'; Extensions = @('*.jpg', '*.jpeg', '*.bmp') },
        @{ Name = 'All Files'; Extensions = @('*.*') }
    )

    $imageFiles = Invoke-OpenFileDialog -bridge $GUIBridge -title "Open image file" -suggestedStartLocation $HOME -allowMultiple $true -filters $imageFileFilters

    "selected image files: $imageFiles"

    $textFileFilters = @(
        @{ Name = 'Text Files'; Extensions = @('*.txt', '*.md') },
        @{ Name = 'All Files'; Extensions = @('*.*') }
    )

    $textFile = Invoke-SaveFileDialog -bridge $GUIBridge -title "Save text file" -suggestedStartLocation $HOME -SuggestedFileName "howdiedo.txt" -showOverwritePrompt $true -filters $textFileFilters

    "save text file as: $textFile"
}
catch {
    $errObj = $_
}
finally {
    "Location $(Get-Location) Exception was: $errObj"
    Start-Sleep -seconds 2
    Close-GUIBridge -bridge $GUIBridge
    "closed GUIBridge."
}
