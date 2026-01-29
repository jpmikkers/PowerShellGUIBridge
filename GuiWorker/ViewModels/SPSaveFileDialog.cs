using System.Collections.Generic;

namespace GuiWorker.ViewModels;

public record class SPSaveFileDialog
{
    public SPSaveFileDialog()
    {
    }

    public string? Title { get; set; }
    public string? SuggestedStartLocation { get; set; }
    public string? DefaultExtension { get; set; }

    public string? SuggestedFileName
    {
        get; set;
    }

    public string? SuggestedFileType
    {
        get; set;
    }

    public bool? ShowOverwritePrompt { get; set; }

    public List<SPFileFilter>? Filters { get; set; }
}
