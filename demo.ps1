#requires -version 7.5

using module .\GUICommands.psd1

try
{
    $GUIBridge = New-GUIBridge

    $imageFileFilters = @( 
        @{ Name = 'Images'; Extensions = @('*.jpg', '*.jpeg', '*.bmp') },
        @{ Name = 'All Files'; Extensions = @('*.*') }
    )

    Invoke-OpenFileDialog -bridge $GUIBridge -title "Open image file" -location "C:\" -allowMultiple $true -filters $imageFileFilters

    $textFileFilters = @(
        @{ Name = 'Text Files'; Extensions = @('*.txt', '*.md') },
        @{ Name = 'All Files'; Extensions = @('*.*') }
    )

    Invoke-SaveFileDialog -bridge $GUIBridge -title "Save text file" -location "C:\" -SuggestedFileName "howdiedo.txt" -showOverwritePrompt $true -filters $textFileFilters
}
finally
{
    Close-GUIBridge -GUIBridge $GUIBridge
    "closed GUIBridge."
}
