using System.Collections.Generic;

namespace GuiWorker.ViewModels;

public record class SPOpenFileDialog
{
    public string? Title { get; set; }
    public string? SuggestedFileName { get; set; }
    public string? SuggestedStartLocation { get; set; }
    public bool? AllowMultiple { get; set; }
    public List<SPFileFilter>? Filters { get; set; }
}
