using System.Collections.Generic;

namespace GuiWorker.ViewModels;

public record class SPFileFilter
{
    public string? Name { get; set; }
    public List<string>? Extensions { get; set; }
}
