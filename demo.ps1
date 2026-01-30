#requires -version 7.5

using module .\GUICommands.psd1

try
{
    $GUIBridge = New-GUIBridge

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
finally
{
    Close-GUIBridge -bridge $GUIBridge
    "closed GUIBridge."
}
